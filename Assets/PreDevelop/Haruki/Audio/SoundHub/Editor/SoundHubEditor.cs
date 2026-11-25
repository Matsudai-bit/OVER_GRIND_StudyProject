#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// SoundHub の Inspector です。ミキサー形式で音量バランスを確認・調整できます。
/// </summary>
[CustomEditor(typeof(SoundHub))]
public sealed class SoundHubEditor : Editor
{
    // プロパティ名
    private const string PROP_DATABASE = "m_database";
    private const string PROP_MASTER = "m_masterVolume";
    private const string PROP_MIXES = "m_categoryMixes";
    private const string PROP_PREWARM = "m_prewarmVoiceCount";
    private const string PROP_MAX_VOICES = "m_maxVoicesPerId";
    private const string PROP_PERSISTENT = "m_isPersistent";
    private const string PROP_LOAD_MIX = "m_isLoadSavedMixOnAwake";
    private const string MIX_PROP_VOLUME = "m_volume";
    private const string MIX_PROP_MUTED = "m_isMuted";

    // ミュートボタンの幅
    private const float MUTE_BUTTON_WIDTH = 26.0f;

    // カテゴリー表示の幅
    private const float CATEGORY_TAG_WIDTH = 110.0f;

    // dB 表示の幅
    private const float DECIBEL_LABEL_WIDTH = 62.0f;

    // レベルバーの高さ
    private const float LEVEL_BAR_HEIGHT = 5.0f;

    // 再生中一覧のレベル表示の幅
    private const float VOICE_LEVEL_WIDTH = 90.0f;

    // 再生中一覧の停止ボタンの幅
    private const float STOP_BUTTON_WIDTH = 24.0f;

    // 再生中一覧のカテゴリー表示の幅
    private const float VOICE_TAG_WIDTH = 100.0f;

    // この値以上のレベルは警告色にする
    private const float LEVEL_WARNING_THRESHOLD = 0.9f;

    // レベルバーの背景色
    private static readonly Color LEVEL_TRACK_COLOR = new Color(0.0f, 0.0f, 0.0f, 0.25f);

    // レベルバーの通常色
    private static readonly Color LEVEL_FILL_COLOR = new Color(0.30f, 0.85f, 0.45f);

    // レベルバーの警告色
    private static readonly Color LEVEL_WARNING_COLOR = new Color(1.0f, 0.60f, 0.20f);

    // プロパティのキャッシュ
    private SerializedProperty m_databaseProperty;
    private SerializedProperty m_masterProperty;
    private SerializedProperty m_mixesProperty;
    private SerializedProperty m_prewarmProperty;
    private SerializedProperty m_maxVoicesProperty;
    private SerializedProperty m_persistentProperty;
    private SerializedProperty m_loadMixProperty;

    // 再生中ノードのスナップショット（描画中に一覧が変わって表示が崩れるのを防ぐ）
    private readonly List<SoundVoice> m_voiceSnapshot = new List<SoundVoice>();

    // 再生中一覧を開いているか
    private bool m_isVoiceFoldout = true;

    // 詳細設定を開いているか
    private bool m_isSettingFoldout = false;

    private void OnEnable()
    {
        m_databaseProperty = serializedObject.FindProperty(PROP_DATABASE);
        m_masterProperty = serializedObject.FindProperty(PROP_MASTER);
        m_mixesProperty = serializedObject.FindProperty(PROP_MIXES);
        m_prewarmProperty = serializedObject.FindProperty(PROP_PREWARM);
        m_maxVoicesProperty = serializedObject.FindProperty(PROP_MAX_VOICES);
        m_persistentProperty = serializedObject.FindProperty(PROP_PERSISTENT);
        m_loadMixProperty = serializedObject.FindProperty(PROP_LOAD_MIX);
    }

    /// <summary>
    /// 再生中は常に再描画して、レベル表示を更新します。
    /// </summary>
    /// <returns>
    /// true：常に再描画します。
    /// false：必要なときだけ再描画します。
    /// </returns>
    public override bool RequiresConstantRepaint()
    {
        return Application.isPlaying;
    }

    public override void OnInspectorGUI()
    {
        SoundHub hub = (SoundHub)target;

        UpdateVoiceSnapshot(hub);

        serializedObject.Update();

        DrawDatabaseField();
        EditorGUILayout.Space(6.0f);

        DrawMixer(hub);
        EditorGUILayout.Space(6.0f);

        DrawSettings();

        serializedObject.ApplyModifiedProperties();

        if (Application.isPlaying)
        {
            EditorGUILayout.Space(6.0f);
            DrawRuntimePanel(hub);
        }
    }

    /// <summary>
    /// 再生中ノードのスナップショットを更新します（Layout イベント時のみ）。
    /// </summary>
    /// <param name="hub">対象の SoundHub。</param>
    private void UpdateVoiceSnapshot(SoundHub hub)
    {
        if (!Application.isPlaying)
        {
            m_voiceSnapshot.Clear();
            return;
        }

        if (Event.current.type != EventType.Layout)
        {
            return;
        }

        m_voiceSnapshot.Clear();
        m_voiceSnapshot.AddRange(hub.ActiveVoices);
    }

    /// <summary>
    /// データベースの割り当て欄を描画します。
    /// </summary>
    private void DrawDatabaseField()
    {
        EditorGUILayout.PropertyField(m_databaseProperty, new GUIContent("SoundDatabase"));

        if (m_databaseProperty.objectReferenceValue == null)
        {
            EditorGUILayout.HelpBox(
                "未設定の場合は Resources/SoundDatabase を探します。Inspector での割り当てを推奨します。",
                MessageType.Info);
        }
    }

    /// <summary>
    /// ミキサー（マスターとカテゴリー別の音量）を描画します。
    /// </summary>
    /// <param name="hub">対象の SoundHub。</param>
    private void DrawMixer(SoundHub hub)
    {
        EditorGUILayout.LabelField("ミキサー（カテゴリー別の音量バランス）", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        DrawMasterRow();

        for (int i = 0; i < m_mixesProperty.arraySize; i++)
        {
            DrawCategoryRow(hub, i);
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// マスター音量の行を描画します。
    /// </summary>
    private void DrawMasterRow()
    {
        EditorGUILayout.BeginHorizontal();

        GUILayout.Space(MUTE_BUTTON_WIDTH + 4.0f);
        EditorGUILayout.LabelField("MASTER", EditorStyles.boldLabel, GUILayout.Width(CATEGORY_TAG_WIDTH));

        m_masterProperty.floatValue = EditorGUILayout.Slider(m_masterProperty.floatValue, 0.0f, 1.0f);

        GUILayout.Label(
            SoundToolGui.GetDecibelText(m_masterProperty.floatValue),
            EditorStyles.miniLabel,
            GUILayout.Width(DECIBEL_LABEL_WIDTH));

        EditorGUILayout.EndHorizontal();

        DrawLevelBar(GetPeakLevel(false, SoundCategory.BGM));
    }

    /// <summary>
    /// カテゴリー1つ分の行を描画します。
    /// </summary>
    /// <param name="hub">対象の SoundHub。</param>
    /// <param name="index">ミキサー設定の番号。</param>
    private void DrawCategoryRow(SoundHub hub, int index)
    {
        // 表示用のカテゴリーはオブジェクト側から取得する
        if (index >= hub.CategoryMixes.Count)
        {
            return;
        }

        SoundCategory category = hub.CategoryMixes[index].Category;

        SerializedProperty element = m_mixesProperty.GetArrayElementAtIndex(index);
        SerializedProperty volumeProperty = element.FindPropertyRelative(MIX_PROP_VOLUME);
        SerializedProperty mutedProperty = element.FindPropertyRelative(MIX_PROP_MUTED);

        EditorGUILayout.BeginHorizontal();

        // ミュート
        mutedProperty.boolValue = GUILayout.Toggle(
            mutedProperty.boolValue,
            "M",
            EditorStyles.miniButton,
            GUILayout.Width(MUTE_BUTTON_WIDTH));

        // カテゴリー名
        SoundToolGui.DrawCategoryTag(category, CATEGORY_TAG_WIDTH);

        // 音量フェーダー（ミュート中は操作不可）
        using (new EditorGUI.DisabledScope(mutedProperty.boolValue))
        {
            volumeProperty.floatValue = EditorGUILayout.Slider(volumeProperty.floatValue, 0.0f, 1.0f);
        }

        // dB 表示
        string decibelText = mutedProperty.boolValue ? "MUTE" : SoundToolGui.GetDecibelText(volumeProperty.floatValue);
        GUILayout.Label(decibelText, EditorStyles.miniLabel, GUILayout.Width(DECIBEL_LABEL_WIDTH));

        EditorGUILayout.EndHorizontal();

        DrawLevelBar(GetPeakLevel(true, category));
    }

    /// <summary>
    /// 再生中の最終音量の最大値を取得します。
    /// </summary>
    /// <param name="isFilterByCategory">カテゴリーで絞り込むか。</param>
    /// <param name="category">絞り込むカテゴリー。</param>
    /// <returns>最終音量の最大値（0～1）。</returns>
    private float GetPeakLevel(bool isFilterByCategory, SoundCategory category)
    {
        float peak = 0.0f;

        foreach (SoundVoice voice in m_voiceSnapshot)
        {
            if (voice == null || (isFilterByCategory && voice.Category != category))
            {
                continue;
            }

            peak = Mathf.Max(peak, voice.FinalVolume);
        }

        return peak;
    }

    /// <summary>
    /// 細いレベルバーを描画します。
    /// </summary>
    /// <param name="level">表示するレベル（0～1）。</param>
    private static void DrawLevelBar(float level)
    {
        Rect barRect = GUILayoutUtility.GetRect(0.0f, LEVEL_BAR_HEIGHT, GUILayout.ExpandWidth(true));

        EditorGUI.DrawRect(barRect, LEVEL_TRACK_COLOR);

        if (level <= 0.0f)
        {
            return;
        }

        Rect fillRect = new Rect(barRect.x, barRect.y, barRect.width * Mathf.Clamp01(level), barRect.height);
        Color fillColor = level >= LEVEL_WARNING_THRESHOLD ? LEVEL_WARNING_COLOR : LEVEL_FILL_COLOR;

        EditorGUI.DrawRect(fillRect, fillColor);
    }

    /// <summary>
    /// 詳細設定を描画します。
    /// </summary>
    private void DrawSettings()
    {
        m_isSettingFoldout = EditorGUILayout.Foldout(m_isSettingFoldout, "詳細設定", true);

        if (!m_isSettingFoldout)
        {
            return;
        }

        EditorGUILayout.PropertyField(m_prewarmProperty, new GUIContent("先に作る再生ノード数"));
        EditorGUILayout.PropertyField(m_maxVoicesProperty, new GUIContent("同じ ID の最大同時発音数", "0 で無制限"));
        EditorGUILayout.PropertyField(m_persistentProperty, new GUIContent("シーンをまたいで残す"));
        EditorGUILayout.PropertyField(m_loadMixProperty, new GUIContent("起動時に保存済みミックスを読み込む"));
    }

    /// <summary>
    /// ゲーム実行中の操作パネルと再生中一覧を描画します。
    /// </summary>
    /// <param name="hub">対象の SoundHub。</param>
    private void DrawRuntimePanel(SoundHub hub)
    {
        EditorGUILayout.LabelField("実行中の操作", EditorStyles.boldLabel);

        // 全体操作
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("全停止"))
        {
            hub.StopAll();
        }

        if (GUILayout.Button("全一時停止"))
        {
            hub.PauseAll();
        }

        if (GUILayout.Button("全再開"))
        {
            hub.ResumeAll();
        }

        EditorGUILayout.EndHorizontal();

        // ミックスの保存・読み込み
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("ミックスを保存"))
        {
            hub.SaveMix();
        }

        if (GUILayout.Button("ミックスを読み込み"))
        {
            hub.LoadMix();
        }

        EditorGUILayout.EndHorizontal();

        DrawVoiceList(hub);
    }

    /// <summary>
    /// 再生中のサウンド一覧を描画します。
    /// </summary>
    /// <param name="hub">対象の SoundHub。</param>
    private void DrawVoiceList(SoundHub hub)
    {
        m_isVoiceFoldout = EditorGUILayout.Foldout(m_isVoiceFoldout, $"再生中のサウンド（{m_voiceSnapshot.Count}）", true);

        if (!m_isVoiceFoldout)
        {
            return;
        }

        int stopHandle = SoundHub.INVALID_HANDLE;

        foreach (SoundVoice voice in m_voiceSnapshot)
        {
            if (voice == null)
            {
                continue;
            }

            if (DrawVoiceRow(voice))
            {
                stopHandle = voice.HandleId;
            }
        }

        // 描画中に一覧が変わらないよう、停止は描画後に行う
        if (stopHandle != SoundHub.INVALID_HANDLE)
        {
            hub.Stop(stopHandle);
        }
    }

    /// <summary>
    /// 再生中のサウンド1件分の行を描画します。
    /// </summary>
    /// <param name="voice">表示するノード。</param>
    /// <returns>
    /// true：停止ボタンが押されました。
    /// false：押されていません。
    /// </returns>
    private bool DrawVoiceRow(SoundVoice voice)
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

        SoundToolGui.DrawCategoryTag(voice.Category, VOICE_TAG_WIDTH);

        // ID 名と状態
        string stateText = $"#{voice.HandleId} {(voice.Is3D ? "3D" : "2D")}"
            + (voice.IsPaused ? " 一時停止" : "")
            + (voice.IsStopping ? " フェードアウト" : "");

        EditorGUILayout.LabelField(voice.SoundId.ToString(), EditorStyles.miniBoldLabel);
        EditorGUILayout.LabelField(stateText, EditorStyles.miniLabel, GUILayout.Width(120.0f));

        // 最終音量
        Rect levelRect = GUILayoutUtility.GetRect(VOICE_LEVEL_WIDTH, EditorGUIUtility.singleLineHeight, GUILayout.Width(VOICE_LEVEL_WIDTH));
        EditorGUI.ProgressBar(levelRect, Mathf.Clamp01(voice.FinalVolume), voice.FinalVolume.ToString("F2"));

        bool isStopPressed = GUILayout.Button("■", GUILayout.Width(STOP_BUTTON_WIDTH));

        EditorGUILayout.EndHorizontal();

        return isStopPressed;
    }
}

#endif