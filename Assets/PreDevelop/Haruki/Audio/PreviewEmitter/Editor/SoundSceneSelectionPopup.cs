#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// シーン試聴エミッター用に、カテゴリー・3Dフィルタ・検索・即試聴を備えたサウンド選択ポップアップです。
/// </summary>
public sealed class SoundSceneSelectionPopup : PopupWindowContent
{
    // ポップアップの幅
    private const float POPUP_WIDTH = 620.0f;

    // ポップアップの高さ
    private const float POPUP_HEIGHT = 480.0f;

    // 1行の高さ
    private const float ROW_HEIGHT = 28.0f;

    // カテゴリータグの幅
    private const float CATEGORY_TAG_WIDTH = 95.0f;

    // バッジの幅
    private const float BADGE_WIDTH = 34.0f;

    // 音量ゲージの幅
    private const float METER_WIDTH = 40.0f;

    // 音量ゲージの高さ
    private const float METER_HEIGHT = 8.0f;

    // dB 表示の幅
    private const float DB_WIDTH = 56.0f;

    // 「すべて」タブのインデックス
    private const int CATEGORY_TAB_ALL = 0;

    // 選択中行の背景色
    private static readonly Color SELECTED_ROW_COLOR = new Color(0.25f, 0.55f, 0.95f, 0.35f);

    // 3Dバッジの色
    private static readonly Color BADGE_3D_COLOR = new Color(0.15f, 0.65f, 0.75f);

    // カテゴリー値の一覧
    private static readonly SoundCategory[] CATEGORY_VALUES = (SoundCategory[])Enum.GetValues(typeof(SoundCategory));

    // 対象のエミッター
    private readonly SoundScenePreviewEmitter m_emitter;

    // 選択変更時のコールバック
    private readonly Action<AudioID> m_onSelected;

    // 絞り込み後のサウンドリスト
    private readonly List<SoundDatabase.SoundData> m_filteredList = new List<SoundDatabase.SoundData>();

    // カテゴリータブの表示名
    private readonly string[] m_categoryTabLabels = new string[CATEGORY_VALUES.Length + 1];

    // 検索文字列
    private string m_searchText = "";

    // 選択中のカテゴリータブ
    private int m_categoryTabIndex = CATEGORY_TAB_ALL;

    // 3D音源のみ表示するか
    private bool m_only3D = false;

    // I/L/O の場合は代表（_I または _L）のみにまとめるか
    private bool m_groupSequence = true;

    // クリック選択時に即エミッターから再生するか
    private bool m_playOnSelect = true;

    // スクロール位置
    private Vector2 m_scrollPosition = Vector2.zero;

    /// <summary>
    /// サウンド選択ポップアップを生成します。
    /// </summary>
    /// <param name="emitter">対象のエミッター。</param>
    /// <param name="onSelected">AudioID 選択時のコールバック。</param>
    public SoundSceneSelectionPopup(SoundScenePreviewEmitter emitter, Action<AudioID> onSelected)
    {
        m_emitter = emitter;
        m_onSelected = onSelected;

        m_categoryTabLabels[CATEGORY_TAB_ALL] = "すべて";
        for (int i = 0; i < CATEGORY_VALUES.Length; i++)
        {
            m_categoryTabLabels[i + 1] = CATEGORY_VALUES[i].ToString();
        }
    }

    /// <summary>
    /// ポップアップウィンドウのサイズを取得します。
    /// </summary>
    /// <returns>ウィンドウサイズ。</returns>
    public override Vector2 GetWindowSize()
    {
        return new Vector2(POPUP_WIDTH, POPUP_HEIGHT);
    }

    /// <summary>
    /// ポップアップの中身を描画します。
    /// </summary>
    /// <param name="rect">描画領域。</param>
    public override void OnGUI(Rect rect)
    {
        SoundDatabase database = m_emitter != null ? m_emitter.Database : SoundToolDatabaseProvider.GetDatabase();
        if (database == null)
        {
            EditorGUILayout.HelpBox("SoundDatabase が見つかりません。", MessageType.Warning);
            return;
        }

        BuildFilteredList(database);
        HandleKeyboardInput();

        DrawHeaderControls();
        DrawSoundList();
    }

    /// <summary>
    /// 検索・カテゴリー・フィルタ条件のヘッダーを描画します。
    /// </summary>
    private void DrawHeaderControls()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // 検索バー
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("検索", GUILayout.Width(32.0f));
        m_searchText = EditorGUILayout.TextField(m_searchText, EditorStyles.toolbarSearchField);

        if (GUILayout.Button("クリア", EditorStyles.miniButton, GUILayout.Width(48.0f)))
        {
            m_searchText = "";
            GUI.FocusControl(null);
        }

        EditorGUILayout.EndHorizontal();

        // カテゴリータブ
        m_categoryTabIndex = GUILayout.Toolbar(m_categoryTabIndex, m_categoryTabLabels, EditorStyles.miniButton);

        // フィルタトグル
        EditorGUILayout.BeginHorizontal();
        m_only3D = GUILayout.Toggle(m_only3D, "3D音源のみ表示", EditorStyles.miniButton, GUILayout.Width(105.0f));
        m_groupSequence = GUILayout.Toggle(m_groupSequence, "I/L/O を1行にまとめる", EditorStyles.miniButton, GUILayout.Width(135.0f));
        m_playOnSelect = GUILayout.Toggle(m_playOnSelect, "選択時にシーンで即再生", EditorStyles.miniButton, GUILayout.Width(145.0f));

        GUILayout.FlexibleSpace();
        GUILayout.Label($"{m_filteredList.Count} 件", EditorStyles.miniBoldLabel);

        if (GUILayout.Button("閉じる", EditorStyles.miniButton, GUILayout.Width(52.0f)))
        {
            editorWindow.Close();
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// サウンド一覧を描画します。
    /// </summary>
    private void DrawSoundList()
    {
        m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition, GUILayout.ExpandHeight(true));

        if (m_filteredList.Count == 0)
        {
            EditorGUILayout.HelpBox("条件に一致するサウンドがありません。", MessageType.Info);
        }

        AudioID currentId = m_emitter != null ? m_emitter.SelectedAudioId : AudioID.None;

        for (int i = 0; i < m_filteredList.Count; i++)
        {
            SoundDatabase.SoundData data = m_filteredList[i];
            DrawRow(data, data.m_id == currentId);
        }

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// サウンド1行分を描画します。
    /// </summary>
    private void DrawRow(SoundDatabase.SoundData data, bool isCurrent)
    {
        Rect rowRect = EditorGUILayout.BeginHorizontal(EditorStyles.helpBox, GUILayout.Height(ROW_HEIGHT));

        if (Event.current.type == EventType.Repaint && isCurrent)
        {
            EditorGUI.DrawRect(rowRect, SELECTED_ROW_COLOR);
        }

        // カテゴリータグ
        SoundToolGui.DrawCategoryTag(data.m_category, CATEGORY_TAG_WIDTH);

        // 3D / 2D バッジ
        Rect badgeRect = GUILayoutUtility.GetRect(BADGE_WIDTH, 16.0f, GUILayout.Width(BADGE_WIDTH));
        EditorGUI.DrawRect(badgeRect, data.m_is3D ? BADGE_3D_COLOR : new Color(0.3f, 0.3f, 0.3f, 0.5f));
        GUI.Label(badgeRect, data.m_is3D ? " 3D" : " 2D", EditorStyles.whiteMiniLabel);

        // 表示名（I/L/O まとめ時はベース名＋サフィックス案内）
        string idText = data.m_id.ToString();
        SoundToolSequenceHelper.ParseSoundName(idText, out string baseName, out SoundPartType partType);
        string displayTitle = (m_groupSequence && partType != SoundPartType.PART_SINGLE)
            ? $"{baseName}  [I/L/O]"
            : idText;

        GUILayout.Label(displayTitle, EditorStyles.boldLabel, GUILayout.Width(210.0f));

        // Clip 名
        string clipName = data.m_clip != null ? data.m_clip.name : "Clip未設定";
        GUILayout.Label(clipName, EditorStyles.miniLabel, GUILayout.ExpandWidth(true));

        // 音量ゲージと dB
        SoundToolGui.DrawVolumeMeter(data.m_volume, METER_WIDTH, METER_HEIGHT);
        GUILayout.Label(SoundToolGui.GetDecibelText(data.m_volume), EditorStyles.miniLabel, GUILayout.Width(DB_WIDTH));

        EditorGUILayout.EndHorizontal();

        // クリックで選択＆即試聴（ダブルクリックで確定して閉じる）
        Event currentEvent = Event.current;
        if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && rowRect.Contains(currentEvent.mousePosition))
        {
            SelectSound(data.m_id);
            currentEvent.Use();

            if (currentEvent.clickCount >= 2)
            {
                editorWindow.Close();
            }
        }
    }

    /// <summary>
    /// 矢印キーによる上下選択と Enter キー確定を処理します。
    /// </summary>
    private void HandleKeyboardInput()
    {
        Event currentEvent = Event.current;
        if (currentEvent.type != EventType.KeyDown || m_filteredList.Count == 0)
        {
            return;
        }

        if (currentEvent.keyCode == KeyCode.DownArrow || currentEvent.keyCode == KeyCode.UpArrow)
        {
            int offset = currentEvent.keyCode == KeyCode.DownArrow ? 1 : -1;
            AudioID currentId = m_emitter != null ? m_emitter.SelectedAudioId : AudioID.None;
            int currentIndex = m_filteredList.FindIndex(item => item.m_id == currentId);
            int nextIndex = currentIndex < 0 ? 0 : Mathf.Clamp(currentIndex + offset, 0, m_filteredList.Count - 1);

            SelectSound(m_filteredList[nextIndex].m_id);
            currentEvent.Use();
        }
        else if (currentEvent.keyCode == KeyCode.Return || currentEvent.keyCode == KeyCode.KeypadEnter)
        {
            editorWindow.Close();
            currentEvent.Use();
        }
    }

    /// <summary>
    /// 指定した AudioID をエミッターに反映し、必要に応じて即再生します。
    /// </summary>
    private void SelectSound(AudioID audioId)
    {
        m_onSelected?.Invoke(audioId);

        if (m_playOnSelect && m_emitter != null)
        {
            m_emitter.PlayPreview();
        }

        if (editorWindow != null)
        {
            editorWindow.Repaint();
        }
    }

    /// <summary>
    /// フィルタ条件に合うサウンド一覧を構築します。
    /// </summary>
    private void BuildFilteredList(SoundDatabase database)
    {
        m_filteredList.Clear();
        HashSet<string> addedBaseNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string keyword = (m_searchText ?? "").Trim();

        for (int i = 0; i < database.m_soundList.Count; i++)
        {
            SoundDatabase.SoundData data = database.m_soundList[i];
            if (data == null || data.m_id == AudioID.None)
            {
                continue;
            }

            if (m_categoryTabIndex != CATEGORY_TAB_ALL && data.m_category != CATEGORY_VALUES[m_categoryTabIndex - 1])
            {
                continue;
            }

            if (m_only3D && !data.m_is3D)
            {
                continue;
            }

            string idName = data.m_id.ToString();
            SoundToolSequenceHelper.ParseSoundName(idName, out string baseName, out SoundPartType partType);

            if (!string.IsNullOrEmpty(keyword))
            {
                string clipName = data.m_clip != null ? data.m_clip.name : "";
                bool match = idName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0
                    || baseName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0
                    || clipName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;

                if (!match)
                {
                    continue;
                }
            }

            if (m_groupSequence && partType != SoundPartType.PART_SINGLE)
            {
                if (!addedBaseNames.Add(baseName))
                {
                    continue;
                }
            }

            m_filteredList.Add(data);
        }
    }
}

#endif