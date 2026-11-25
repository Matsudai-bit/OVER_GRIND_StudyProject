#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 縦型フェーダーとリアルタイムレベルメーターを横に並べたミキサー卓（コンソール）画面です。
/// 全体の音量バランスを俯瞰しながら視覚的に調整できます。
/// </summary>
public sealed class SoundToolConsoleView
{
    // 1チャンネルストリップの幅
    private const float STRIP_WIDTH = 92.0f;

    // 縦フェーダーとメーターの高さ
    private const float FADER_HEIGHT = 210.0f;

    // アナライザーパネルの高さ
    private const float ANALYZER_HEIGHT = 112.0f;

    // 音量の最小値
    private const float MIN_VOLUME = 0.0f;

    // 音量の最大値
    private const float MAX_VOLUME = 1.0f;

    // 「すべて」タブのインデックス
    private const int CATEGORY_TAB_ALL = 0;

    // 再生中ストリップの強調背景色
    private static readonly Color PLAYING_STRIP_COLOR = new Color(0.25f, 0.72f, 0.40f, 0.16f);

    // 基準（Ref）ボタンのアクティブ色
    private static readonly Color REF_ACTIVE_COLOR = new Color(1.0f, 0.80f, 0.25f);

    // カテゴリー値一覧
    private static readonly SoundCategory[] CATEGORY_VALUES = (SoundCategory[])Enum.GetValues(typeof(SoundCategory));

    // 全グループキャッシュ
    private readonly List<SoundToolSequenceHelper.SoundGroup> m_allGroups = new List<SoundToolSequenceHelper.SoundGroup>();

    // 絞り込み後のグループ一覧
    private readonly List<SoundToolSequenceHelper.SoundGroup> m_filteredGroups = new List<SoundToolSequenceHelper.SoundGroup>();

    // 一時パーツリスト
    private readonly List<SoundDatabase.SoundData> m_tempParts = new List<SoundDatabase.SoundData>();

    // カテゴリータブ表示名
    private readonly string[] m_categoryTabLabels = new string[CATEGORY_VALUES.Length + 1];

    // カテゴリー別件数
    private readonly int[] m_categoryCounts = new int[CATEGORY_VALUES.Length + 1];

    // 横スクロール位置
    private Vector2 m_scrollPosition = Vector2.zero;

    // 検索文字列
    private string m_searchText = "";

    // 選択中のカテゴリータブ
    private int m_categoryTabIndex = CATEGORY_TAB_ALL;

    // I/L/O まとめ表示するか
    private bool m_groupIloView = true;

    // 自動 Outro 移行するか
    private bool m_autoOutro = false;

    // リファレンス比較用の AudioID
    private AudioID m_referenceId = AudioID.None;

    // チャンネル名表示用の折り返しスタイル
    private GUIStyle m_stripNameStyle;

    /// <summary>
    /// 現在のリファレンス比較対象 AudioID を取得します。
    /// </summary>
    public AudioID ReferenceId => m_referenceId;

    /// <summary>
    /// コンソール画面を描画します。
    /// </summary>
    public void Draw()
    {
        SoundDatabase database = SoundToolDatabaseProvider.GetDatabase();
        if (database == null)
        {
            SoundToolGui.DrawMissingDatabase();
            return;
        }

        BuildFilteredGroups(database);

        DrawTopControls(database);

        // 上部に大型リアルタイム・スペクトラム＆ステレオメーターを描画する
        SoundToolAnalyzerGui.DrawAnalyzerPanel(ANALYZER_HEIGHT);

        EditorGUILayout.Space(4.0f);
        DrawConsoleStrips(database);
    }

    /// <summary>
    /// 検索・カテゴリー・試聴制御バーを描画します。
    /// </summary>
    private void DrawTopControls(SoundDatabase database)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("検索", GUILayout.Width(32.0f));
        m_searchText = EditorGUILayout.TextField(m_searchText, EditorStyles.toolbarSearchField);

        if (GUILayout.Button("クリア", EditorStyles.miniButton, GUILayout.Width(48.0f)))
        {
            m_searchText = "";
            GUI.FocusControl(null);
        }

        m_groupIloView = GUILayout.Toggle(m_groupIloView, "I/L/O まとめ", EditorStyles.miniButton, GUILayout.Width(92.0f));
        m_autoOutro = GUILayout.Toggle(m_autoOutro, "自動O", EditorStyles.miniButton, GUILayout.Width(52.0f));

        // Ref 比較再生ボタン
        using (new EditorGUI.DisabledScope(m_referenceId == AudioID.None))
        {
            string refText = m_referenceId != AudioID.None ? $"Ref再生: {m_referenceId}" : "Ref未設定";
            if (GUILayout.Button(refText, EditorStyles.miniButton, GUILayout.MaxWidth(180.0f)))
            {
                SoundDatabase.SoundData refData = database.GetSoundData(m_referenceId);
                if (refData != null)
                {
                    SoundToolPreviewPlayer.PlaySingle(refData, m_referenceId);
                }
            }
        }

        if (GUILayout.Button("■ 停止", EditorStyles.miniButton, GUILayout.Width(58.0f)))
        {
            SoundToolPreviewPlayer.Stop();
        }

        EditorGUILayout.EndHorizontal();

        // カテゴリータブ
        m_categoryTabIndex = Mathf.Clamp(m_categoryTabIndex, 0, m_categoryTabLabels.Length - 1);
        m_categoryTabLabels[CATEGORY_TAB_ALL] = $"すべて ({m_categoryCounts[CATEGORY_TAB_ALL]})";
        for (int i = 0; i < CATEGORY_VALUES.Length; i++)
        {
            m_categoryTabLabels[i + 1] = $"{CATEGORY_VALUES[i]} ({m_categoryCounts[i + 1]})";
        }

        m_categoryTabIndex = GUILayout.Toolbar(m_categoryTabIndex, m_categoryTabLabels, EditorStyles.miniButton);

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 横並びの縦型チャンネルストリップ一覧を描画します。
    /// </summary>
    private void DrawConsoleStrips(SoundDatabase database)
    {
        m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition, GUILayout.ExpandHeight(true));

        if (m_filteredGroups.Count == 0)
        {
            EditorGUILayout.HelpBox("条件に一致するサウンドがありません。", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }

        EditorGUILayout.BeginHorizontal();

        for (int i = 0; i < m_filteredGroups.Count; i++)
        {
            DrawSingleChannelStrip(database, m_filteredGroups[i]);
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 1本分の縦型チャンネルストリップを描画します。
    /// </summary>
    private void DrawSingleChannelStrip(SoundDatabase database, SoundToolSequenceHelper.SoundGroup group)
    {
        SoundDatabase.SoundData primary = group.GetPrimaryData();
        if (primary == null)
        {
            return;
        }

        bool isPlaying = group.IsSequenceGroup
            ? SoundToolPreviewPlayer.IsPlayingGroup(group.BaseName)
            : SoundToolPreviewPlayer.IsPlaying(primary.m_id);

        Rect stripRect = EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(STRIP_WIDTH));

        if (Event.current.type == EventType.Repaint && isPlaying)
        {
            EditorGUI.DrawRect(stripRect, PLAYING_STRIP_COLOR);
        }

        // カテゴリーカラー帯
        SoundToolGui.DrawCategoryTag(primary.m_category, STRIP_WIDTH - 10.0f);

        // 試聴ボタン
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(isPlaying ? "■ 停止" : "▶ 試聴", EditorStyles.miniButton))
        {
            if (isPlaying)
            {
                SoundToolPreviewPlayer.Stop();
            }
            else if (group.IsSequenceGroup)
            {
                SoundToolPreviewPlayer.PlaySequence(
                    group.BaseName,
                    group.IntroData,
                    group.LoopData,
                    group.OutroData,
                    m_autoOutro,
                    m_referenceId);
            }
            else
            {
                SoundToolPreviewPlayer.PlaySingle(primary, m_referenceId);
            }
        }
        EditorGUILayout.EndHorizontal();

        // 縦型フェーダー ＋ リアルタイム縦型レベルメーター
        float currentVol = group.GetAverageVolume();
        EditorGUILayout.BeginHorizontal(GUILayout.Height(FADER_HEIGHT));
        GUILayout.FlexibleSpace();

        // 縦フェーダー（上が 1.0、下が 0.0）
        float newVol = GUILayout.VerticalSlider(currentVol, MAX_VOLUME, MIN_VOLUME, GUILayout.Height(FADER_HEIGHT), GUILayout.Width(24.0f));
        if (!Mathf.Approximately(newVol, currentVol))
        {
            ApplyGroupVolumeDelta(database, group, newVol - currentVol);
        }

        GUILayout.Space(6.0f);

        // 縦レベルメーター（試聴中はリアルタイム解析値、待機中は設定音量の目安を表示）
        Rect meterRect = GUILayoutUtility.GetRect(16.0f, FADER_HEIGHT, GUILayout.Width(16.0f), GUILayout.Height(FADER_HEIGHT));
        float meterLevel = isPlaying
            ? Mathf.Max(SoundToolAudioAnalyzer.LeftPeakNormalized, SoundToolAudioAnalyzer.RightPeakNormalized)
            : SoundToolAudioAnalyzer.DecibelToNormalized(SoundToolAudioAnalyzer.LinearToDecibel(currentVol * 0.5f));
        float meterPeak = isPlaying
            ? Mathf.Max(SoundToolAudioAnalyzer.LeftPeakHold, SoundToolAudioAnalyzer.RightPeakHold)
            : 0.0f;

        SoundToolAnalyzerGui.DrawVerticalChannelMeter(meterRect, meterLevel, meterPeak);

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        // 数値＆dB 表示
        GUILayout.Label($"{currentVol:F2}", EditorStyles.centeredGreyMiniLabel);
        GUILayout.Label(SoundToolGui.GetDecibelText(currentVol), EditorStyles.centeredGreyMiniLabel);

        // チャンネル名（折り返し表示）
        string title = group.IsSequenceGroup ? $"{group.BaseName}\n[I/L/O]" : primary.m_id.ToString();
        GUILayout.Label(new GUIContent(title, title), GetStripNameStyle(), GUILayout.Height(36.0f));

        // Ref ピンボタン
        bool isRef = m_referenceId == primary.m_id;
        Color prevBg = GUI.backgroundColor;
        if (isRef)
        {
            GUI.backgroundColor = REF_ACTIVE_COLOR;
        }

        if (GUILayout.Button(isRef ? "Ref ★" : "Ref 設定", EditorStyles.miniButton))
        {
            m_referenceId = isRef ? AudioID.None : primary.m_id;
            SoundToolAudioAnalyzer.ClearReferenceLevel();
        }

        GUI.backgroundColor = prevBg;
        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// グループ内の全パートへ音量変化量を適用します。
    /// </summary>
    private void ApplyGroupVolumeDelta(SoundDatabase database, SoundToolSequenceHelper.SoundGroup group, float delta)
    {
        Undo.RecordObject(database, "Console Adjust Volume");
        group.CollectOrderedParts(m_tempParts);

        for (int i = 0; i < m_tempParts.Count; i++)
        {
            m_tempParts[i].m_volume = Mathf.Clamp(m_tempParts[i].m_volume + delta, MIN_VOLUME, MAX_VOLUME);
        }

        SoundToolDatabaseProvider.MarkDirty(database);
    }

    /// <summary>
    /// 絞り込み条件に一致するグループ一覧を構築します。
    /// </summary>
    private void BuildFilteredGroups(SoundDatabase database)
    {
        m_filteredGroups.Clear();
        Array.Clear(m_categoryCounts, 0, m_categoryCounts.Length);

        if (m_groupIloView)
        {
            SoundToolSequenceHelper.BuildGroups(database.m_soundList, m_allGroups);
        }
        else
        {
            m_allGroups.Clear();
            for (int i = 0; i < database.m_soundList.Count; i++)
            {
                SoundDatabase.SoundData data = database.m_soundList[i];
                if (data == null || data.m_id == AudioID.None)
                {
                    continue;
                }

                SoundToolSequenceHelper.SoundGroup flat = new SoundToolSequenceHelper.SoundGroup($"F:{(int)data.m_id}", data.m_id.ToString(), false);
                flat.SingleData = data;
                m_allGroups.Add(flat);
            }
        }

        string keyword = (m_searchText ?? "").Trim();

        for (int i = 0; i < m_allGroups.Count; i++)
        {
            SoundToolSequenceHelper.SoundGroup group = m_allGroups[i];
            SoundDatabase.SoundData primary = group.GetPrimaryData();
            if (primary == null)
            {
                continue;
            }

            m_categoryCounts[CATEGORY_TAB_ALL]++;
            int catIndex = Array.IndexOf(CATEGORY_VALUES, primary.m_category);
            if (catIndex >= 0)
            {
                m_categoryCounts[catIndex + 1]++;
            }

            if (m_categoryTabIndex != CATEGORY_TAB_ALL && primary.m_category != CATEGORY_VALUES[m_categoryTabIndex - 1])
            {
                continue;
            }

            if (!string.IsNullOrEmpty(keyword))
            {
                string clipName = primary.m_clip != null ? primary.m_clip.name : "";
                bool match = group.BaseName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0
                    || primary.m_id.ToString().IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0
                    || clipName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;

                if (!match)
                {
                    continue;
                }
            }

            m_filteredGroups.Add(group);
        }
    }

    /// <summary>
    /// チャンネル名用の折り返しラベルスタイルを取得します。
    /// </summary>
    private GUIStyle GetStripNameStyle()
    {
        if (m_stripNameStyle == null)
        {
            m_stripNameStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.UpperCenter,
                wordWrap = true,
                clipping = TextClipping.Clip,
                fontSize = 10
            };
        }

        return m_stripNameStyle;
    }
}

#endif