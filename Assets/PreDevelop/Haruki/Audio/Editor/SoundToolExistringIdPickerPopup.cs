#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 既存 ID 選択ポップアップの使用状態フィルタを表します。
/// </summary>
public enum ExistingIdFilterMode
{
    FILTER_ALL,
    FILTER_UNUSED_ONLY,
    FILTER_USED_ONLY
}

/// <summary>
/// 変更先となる既存の AudioID を、カテゴリー絞り込み・空き/使用中フィルタ・試聴付きで選択するポップアップです。
/// </summary>
public sealed class SoundToolExistingIdPickerPopup : PopupWindowContent
{
    // ポップアップの幅
    private const float POPUP_WIDTH = 620.0f;

    // ポップアップの高さ
    private const float POPUP_HEIGHT = 480.0f;

    // 1行の高さ
    private const float ROW_HEIGHT = 28.0f;

    // 試聴ボタンの幅
    private const float PREVIEW_BUTTON_WIDTH = 26.0f;

    // 状態バッジの幅
    private const float STATUS_BADGE_WIDTH = 48.0f;

    // カテゴリータグの幅
    private const float CATEGORY_TAG_WIDTH = 94.0f;

    // 「すべて」カテゴリータブのインデックス
    private const int CATEGORY_TAB_ALL = 0;

    // 空き ID バッジの色
    private static readonly Color UNUSED_BADGE_COLOR = new Color(0.25f, 0.68f, 0.42f);

    // 使用中 ID バッジの色
    private static readonly Color USED_BADGE_COLOR = new Color(0.85f, 0.55f, 0.20f);

    // 試聴中行の背景色
    private static readonly Color PLAYING_ROW_COLOR = new Color(0.30f, 0.80f, 0.40f, 0.18f);

    // 状態フィルタの表示ラベル
    private static readonly string[] STATUS_FILTER_LABELS = { "すべて", "空き ID のみ", "使用中のみ" };

    // カテゴリー値一覧
    private static readonly SoundCategory[] CATEGORY_VALUES = (SoundCategory[])Enum.GetValues(typeof(SoundCategory));

    // 対象の SoundDatabase
    private readonly SoundDatabase m_database;

    // 変更元の AudioID（一覧から除外する）
    private readonly AudioID m_sourceAudioId;

    // 選択時のコールバック
    private readonly Action<AudioID> m_onSelected;

    // 全既存 ID 一覧（None を除く）
    private readonly List<SoundToolIdRegistry.IdEntry> m_allEntries = new List<SoundToolIdRegistry.IdEntry>();

    // 絞り込み後の表示一覧
    private readonly List<SoundToolIdRegistry.IdEntry> m_filteredEntries = new List<SoundToolIdRegistry.IdEntry>();

    // カテゴリータブの表示名
    private readonly string[] m_categoryLabels = new string[CATEGORY_VALUES.Length + 1];

    // 検索文字列
    private string m_searchText = "";

    // 状態フィルタ
    private ExistingIdFilterMode m_statusFilter = ExistingIdFilterMode.FILTER_ALL;

    // カテゴリーフィルタ
    private int m_categoryIndex = CATEGORY_TAB_ALL;

    // スクロール位置
    private Vector2 m_scrollPosition = Vector2.zero;

    /// <summary>
    /// 既存 ID 選択ポップアップを生成します。
    /// </summary>
    /// <param name="database">対象の SoundDatabase。</param>
    /// <param name="sourceAudioId">変更元の AudioID。</param>
    /// <param name="onSelected">変更先 AudioID 選択時のコールバック。</param>
    public SoundToolExistingIdPickerPopup(
        SoundDatabase database,
        AudioID sourceAudioId,
        Action<AudioID> onSelected)
    {
        m_database = database;
        m_sourceAudioId = sourceAudioId;
        m_onSelected = onSelected;

        m_categoryLabels[CATEGORY_TAB_ALL] = "すべて";
        for (int i = 0; i < CATEGORY_VALUES.Length; i++)
        {
            m_categoryLabels[i + 1] = CATEGORY_VALUES[i].ToString();
        }

        foreach (SoundToolIdRegistry.IdEntry entry in SoundToolIdRegistry.GetCurrentEntries())
        {
            if (entry.Name != nameof(AudioID.None) && (AudioID)entry.Value != m_sourceAudioId)
            {
                m_allEntries.Add(entry);
            }
        }
    }

    /// <summary>
    /// ポップアップのサイズを取得します。
    /// </summary>
    /// <returns>ウィンドウサイズ。</returns>
    public override Vector2 GetWindowSize()
    {
        return new Vector2(POPUP_WIDTH, POPUP_HEIGHT);
    }

    /// <summary>
    /// ポップアップを描画します。
    /// </summary>
    /// <param name="rect">描画領域。</param>
    public override void OnGUI(Rect rect)
    {
        if (m_database == null)
        {
            EditorGUILayout.HelpBox("SoundDatabase が見つかりません。", MessageType.Warning);
            return;
        }

        BuildFilteredEntries();
        DrawHeader();
        DrawList();
    }

    /// <summary>
    /// 検索・状態フィルタ・カテゴリーフィルタ欄を描画します。
    /// </summary>
    private void DrawHeader()
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

        if (GUILayout.Button("閉じる", EditorStyles.miniButton, GUILayout.Width(52.0f)))
        {
            editorWindow.Close();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        m_statusFilter = (ExistingIdFilterMode)GUILayout.Toolbar(
            (int)m_statusFilter,
            STATUS_FILTER_LABELS,
            EditorStyles.miniButton,
            GUILayout.Width(250.0f));

        GUILayout.FlexibleSpace();
        GUILayout.Label($"候補 {m_filteredEntries.Count} 件", EditorStyles.miniBoldLabel);
        EditorGUILayout.EndHorizontal();

        // 使用中 ID をカテゴリーで絞り込むタブ（空き ID のみ表示時は無効）
        using (new EditorGUI.DisabledScope(m_statusFilter == ExistingIdFilterMode.FILTER_UNUSED_ONLY))
        {
            m_categoryIndex = GUILayout.Toolbar(m_categoryIndex, m_categoryLabels, EditorStyles.miniButton);
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 既存 ID 候補の一覧を描画します。
    /// </summary>
    private void DrawList()
    {
        m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition, GUILayout.ExpandHeight(true));

        if (m_filteredEntries.Count == 0)
        {
            EditorGUILayout.HelpBox("条件に一致する既存 ID がありません。", MessageType.Info);
        }

        for (int i = 0; i < m_filteredEntries.Count; i++)
        {
            DrawRow(m_filteredEntries[i]);
        }

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 候補の1行を描画します。
    /// </summary>
    private void DrawRow(SoundToolIdRegistry.IdEntry entry)
    {
        AudioID audioId = (AudioID)entry.Value;
        SoundDatabase.SoundData data = m_database.GetSoundData(audioId);
        bool isUsed = data != null;
        bool isPlaying = SoundToolPreviewPlayer.IsPlaying(audioId);

        Rect rowRect = EditorGUILayout.BeginHorizontal(EditorStyles.helpBox, GUILayout.Height(ROW_HEIGHT));

        if (Event.current.type == EventType.Repaint && isPlaying)
        {
            EditorGUI.DrawRect(rowRect, PLAYING_ROW_COLOR);
        }

        // 使用中 ID なら現在の音を聴いて確認できる
        using (new EditorGUI.DisabledScope(!isUsed || data.m_clip == null))
        {
            if (GUILayout.Button(isPlaying ? "■" : "▶", EditorStyles.miniButton, GUILayout.Width(PREVIEW_BUTTON_WIDTH)))
            {
                if (isPlaying)
                {
                    SoundToolPreviewPlayer.Stop();
                }
                else
                {
                    SoundToolPreviewPlayer.PlaySingle(data);
                }

                editorWindow.Repaint();
            }
        }

        // 空き / 使用中バッジ
        Rect statusRect = GUILayoutUtility.GetRect(STATUS_BADGE_WIDTH, 16.0f, GUILayout.Width(STATUS_BADGE_WIDTH));
        EditorGUI.DrawRect(statusRect, isUsed ? USED_BADGE_COLOR : UNUSED_BADGE_COLOR);
        GUI.Label(statusRect, isUsed ? " 使用中" : "  空き", EditorStyles.whiteMiniLabel);

        // カテゴリータグ
        if (isUsed)
        {
            SoundToolGui.DrawCategoryTag(data.m_category, CATEGORY_TAG_WIDTH);
        }
        else
        {
            GUILayout.Label("（未割当）", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(CATEGORY_TAG_WIDTH));
        }

        // ID 名と Clip 名
        GUILayout.Label(entry.Name, EditorStyles.boldLabel, GUILayout.Width(210.0f));
        string clipText = isUsed ? (data.m_clip != null ? data.m_clip.name : "Clip未設定") : "データ未登録";
        GUILayout.Label(clipText, EditorStyles.miniLabel, GUILayout.ExpandWidth(true));

        if (GUILayout.Button("選択", EditorStyles.miniButton, GUILayout.Width(44.0f)))
        {
            m_onSelected?.Invoke(audioId);
            editorWindow.Close();
        }

        EditorGUILayout.EndHorizontal();

        // 行クリックでも選択確定する
        Event currentEvent = Event.current;
        if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && rowRect.Contains(currentEvent.mousePosition))
        {
            m_onSelected?.Invoke(audioId);
            currentEvent.Use();
            editorWindow.Close();
        }
    }

    /// <summary>
    /// 検索・状態・カテゴリー条件で候補を絞り込みます。
    /// </summary>
    private void BuildFilteredEntries()
    {
        m_filteredEntries.Clear();
        string keyword = (m_searchText ?? "").Trim();

        for (int i = 0; i < m_allEntries.Count; i++)
        {
            SoundToolIdRegistry.IdEntry entry = m_allEntries[i];
            SoundDatabase.SoundData data = m_database.GetSoundData((AudioID)entry.Value);
            bool isUsed = data != null;

            if (m_statusFilter == ExistingIdFilterMode.FILTER_UNUSED_ONLY && isUsed)
            {
                continue;
            }

            if (m_statusFilter == ExistingIdFilterMode.FILTER_USED_ONLY && !isUsed)
            {
                continue;
            }

            if (m_statusFilter != ExistingIdFilterMode.FILTER_UNUSED_ONLY && m_categoryIndex != CATEGORY_TAB_ALL)
            {
                if (!isUsed || data.m_category != CATEGORY_VALUES[m_categoryIndex - 1])
                {
                    continue;
                }
            }

            if (keyword.Length > 0)
            {
                string clipName = (isUsed && data.m_clip != null) ? data.m_clip.name : "";
                bool match = entry.Name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0
                    || clipName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;

                if (!match)
                {
                    continue;
                }
            }

            m_filteredEntries.Add(entry);
        }
    }
}

#endif