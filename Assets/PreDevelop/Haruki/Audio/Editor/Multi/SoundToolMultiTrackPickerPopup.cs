#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 2D / 3D の絞り込みモードを表します。
/// </summary>
public enum DimensionFilterMode
{
    FILTER_ALL,
    FILTER_2D_ONLY,
    FILTER_3D_ONLY
}

/// <summary>
/// 同時ミックスへ 2D / 3D サウンドを連続で追加できる検索・カテゴリー付き選択ポップアップです。
/// </summary>
public sealed class SoundToolMultiTrackPickerPopup : PopupWindowContent
{
    // ポップアップの幅
    private const float POPUP_WIDTH = 640.0f;

    // ポップアップの高さ
    private const float POPUP_HEIGHT = 500.0f;

    // 1行の高さ
    private const float ROW_HEIGHT = 28.0f;

    // カテゴリータグの幅
    private const float CATEGORY_TAG_WIDTH = 95.0f;

    // 2D/3D バッジの幅
    private const float BADGE_WIDTH = 34.0f;

    // 追加ボタンの幅
    private const float ADD_BUTTON_WIDTH = 66.0f;

    // 「すべて」タブのインデックス
    private const int CATEGORY_TAB_ALL = 0;

    // 2D バッジの色
    private static readonly Color BADGE_2D_COLOR = new Color(0.25f, 0.55f, 0.80f);

    // 3D バッジの色
    private static readonly Color BADGE_3D_COLOR = new Color(0.85f, 0.50f, 0.20f);

    // 追加済み行の薄い背景色
    private static readonly Color ADDED_ROW_COLOR = new Color(0.25f, 0.70f, 0.40f, 0.14f);

    // 2D/3D フィルタの表示ラベル
    private static readonly string[] DIMENSION_LABELS = { "すべて", "2D のみ", "3D のみ" };

    // カテゴリー値一覧
    private static readonly SoundCategory[] CATEGORY_VALUES = (SoundCategory[])Enum.GetValues(typeof(SoundCategory));

    // 対象の SoundDatabase
    private readonly SoundDatabase m_database;

    // トラック追加時のコールバック
    private readonly Action<AudioID> m_onAddTrack;

    // 絞り込み後の表示リスト
    private readonly List<SoundDatabase.SoundData> m_filteredList = new List<SoundDatabase.SoundData>();

    // カテゴリータブの表示名
    private readonly string[] m_categoryLabels = new string[CATEGORY_VALUES.Length + 1];

    // 検索キーワード
    private string m_searchText = "";

    // カテゴリータブ位置
    private int m_categoryIndex = CATEGORY_TAB_ALL;

    // 2D/3D フィルタ
    private DimensionFilterMode m_dimensionFilter = DimensionFilterMode.FILTER_ALL;

    // I/L/O を1行にまとめるか
    private bool m_groupSequence = true;

    // スクロール位置
    private Vector2 m_scrollPosition = Vector2.zero;

    /// <summary>
    /// トラック選択ポップアップを生成します。
    /// </summary>
    /// <param name="database">対象の SoundDatabase。</param>
    /// <param name="onAddTrack">トラック追加時のコールバック。</param>
    public SoundToolMultiTrackPickerPopup(SoundDatabase database, Action<AudioID> onAddTrack)
    {
        m_database = database;
        m_onAddTrack = onAddTrack;

        m_categoryLabels[CATEGORY_TAB_ALL] = "すべて";
        for (int i = 0; i < CATEGORY_VALUES.Length; i++)
        {
            m_categoryLabels[i + 1] = CATEGORY_VALUES[i].ToString();
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
    /// ポップアップの内容を描画します。
    /// </summary>
    /// <param name="rect">描画領域。</param>
    public override void OnGUI(Rect rect)
    {
        if (m_database == null)
        {
            EditorGUILayout.HelpBox("SoundDatabase が見つかりません。", MessageType.Warning);
            return;
        }

        BuildFilteredList();
        DrawHeader();
        DrawList();
    }

    /// <summary>
    /// 検索・カテゴリー・2D/3D フィルタ欄を描画します。
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

        m_categoryIndex = GUILayout.Toolbar(m_categoryIndex, m_categoryLabels, EditorStyles.miniButton);

        EditorGUILayout.BeginHorizontal();
        m_dimensionFilter = (DimensionFilterMode)GUILayout.Toolbar(
            (int)m_dimensionFilter,
            DIMENSION_LABELS,
            EditorStyles.miniButton,
            GUILayout.Width(210.0f));

        m_groupSequence = GUILayout.Toggle(m_groupSequence, "I/L/O まとめ", EditorStyles.miniButton, GUILayout.Width(96.0f));

        GUILayout.FlexibleSpace();
        GUILayout.Label($"{m_filteredList.Count} 件（クリックで連続追加）", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 候補サウンドの一覧を描画します。
    /// </summary>
    private void DrawList()
    {
        m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition, GUILayout.ExpandHeight(true));

        if (m_filteredList.Count == 0)
        {
            EditorGUILayout.HelpBox("条件に一致するサウンドがありません。", MessageType.Info);
        }

        for (int i = 0; i < m_filteredList.Count; i++)
        {
            DrawRow(m_filteredList[i]);
        }

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 候補1行を描画します。
    /// </summary>
    private void DrawRow(SoundDatabase.SoundData data)
    {
        int addedCount = CountTracksForAudioId(data.m_id);
        Rect rowRect = EditorGUILayout.BeginHorizontal(EditorStyles.helpBox, GUILayout.Height(ROW_HEIGHT));

        if (Event.current.type == EventType.Repaint && addedCount > 0)
        {
            EditorGUI.DrawRect(rowRect, ADDED_ROW_COLOR);
        }

        SoundToolGui.DrawCategoryTag(data.m_category, CATEGORY_TAG_WIDTH);

        // 2D / 3D バッジ
        Rect badgeRect = GUILayoutUtility.GetRect(BADGE_WIDTH, 16.0f, GUILayout.Width(BADGE_WIDTH));
        EditorGUI.DrawRect(badgeRect, data.m_is3D ? BADGE_3D_COLOR : BADGE_2D_COLOR);
        GUI.Label(badgeRect, data.m_is3D ? " 3D" : " 2D", EditorStyles.whiteMiniLabel);

        string idText = data.m_id.ToString();
        SoundToolSequenceHelper.ParseSoundName(idText, out string baseName, out SoundPartType partType);
        string title = (m_groupSequence && partType != SoundPartType.PART_SINGLE)
            ? $"{baseName}  [I/L/O]"
            : idText;

        GUILayout.Label(title, EditorStyles.boldLabel, GUILayout.Width(210.0f));
        GUILayout.Label(data.m_clip != null ? data.m_clip.name : "Clip未設定", EditorStyles.miniLabel, GUILayout.ExpandWidth(true));
        GUILayout.Label(SoundToolGui.GetDecibelText(data.m_volume), EditorStyles.miniLabel, GUILayout.Width(52.0f));

        string buttonText = addedCount > 0 ? $"＋追加({addedCount})" : "＋追加";
        if (GUILayout.Button(buttonText, EditorStyles.miniButton, GUILayout.Width(ADD_BUTTON_WIDTH)))
        {
            m_onAddTrack?.Invoke(data.m_id);
            editorWindow.Repaint();
        }

        EditorGUILayout.EndHorizontal();

        // 行クリックでもそのまま連続追加できる
        Event currentEvent = Event.current;
        if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && rowRect.Contains(currentEvent.mousePosition))
        {
            m_onAddTrack?.Invoke(data.m_id);
            currentEvent.Use();
            editorWindow.Repaint();
        }
    }

    /// <summary>
    /// 指定 AudioID（または同一 I/L/O グループ）が既に何トラック登録されているか数えます。
    /// </summary>
    private int CountTracksForAudioId(AudioID audioId)
    {
        int count = 0;
        SoundToolSequenceHelper.ParseSoundName(audioId.ToString(), out string targetBase, out SoundPartType targetPart);
        IReadOnlyList<SoundToolMultiPreviewPlayer.MixTrack> tracks = SoundToolMultiPreviewPlayer.Tracks;

        for (int i = 0; i < tracks.Count; i++)
        {
            if (tracks[i].AudioId == audioId)
            {
                count++;
                continue;
            }

            if (m_groupSequence && targetPart != SoundPartType.PART_SINGLE)
            {
                SoundToolSequenceHelper.ParseSoundName(tracks[i].AudioId.ToString(), out string trackBase, out SoundPartType trackPart);
                if (trackPart != SoundPartType.PART_SINGLE && string.Equals(targetBase, trackBase, StringComparison.OrdinalIgnoreCase))
                {
                    count++;
                }
            }
        }

        return count;
    }

    /// <summary>
    /// フィルタ条件に合致する候補一覧を作成します。
    /// </summary>
    private void BuildFilteredList()
    {
        m_filteredList.Clear();
        HashSet<string> addedBaseNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string keyword = (m_searchText ?? "").Trim();

        for (int i = 0; i < m_database.m_soundList.Count; i++)
        {
            SoundDatabase.SoundData data = m_database.m_soundList[i];
            if (data == null || data.m_id == AudioID.None)
            {
                continue;
            }

            if (m_categoryIndex != CATEGORY_TAB_ALL && data.m_category != CATEGORY_VALUES[m_categoryIndex - 1])
            {
                continue;
            }

            if (m_dimensionFilter == DimensionFilterMode.FILTER_2D_ONLY && data.m_is3D)
            {
                continue;
            }

            if (m_dimensionFilter == DimensionFilterMode.FILTER_3D_ONLY && !data.m_is3D)
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