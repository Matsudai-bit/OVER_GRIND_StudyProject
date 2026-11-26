#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// ID 管理画面内の操作モードタブを表します。
/// </summary>
public enum IdManageSubTab
{
    TAB_CATEGORY_AND_BULK,
    TAB_ADD_ID,
    TAB_DELETE_ID
}

/// <summary>
/// AudioID のカテゴリー・3D音源・距離減衰の一括／個別変更、新規追加、削除を音源を試聴しながら行える画面です。
/// </summary>
public sealed class SoundToolIdManageView
{
    // ドロップ領域の高さ
    private const float DROP_AREA_HEIGHT = 52.0f;

    // 1行の標準高さ（一括編集・削除）
    private const float COMPACT_ROW_HEIGHT = 26.0f;

    // 行左端のカラー帯の幅
    private const float COLOR_BAR_WIDTH = 4.0f;

    // 行番号欄の幅
    private const float ROW_INDEX_WIDTH = 24.0f;

    // 選択チェックボックスの幅
    private const float SELECT_TOGGLE_WIDTH = 18.0f;

    // 試聴ボタンの幅
    private const float PREVIEW_BUTTON_WIDTH = 26.0f;

    // 2D/3D 切り替えバッジの幅
    private const float DIMENSION_BADGE_WIDTH = 34.0f;

    // 行削除ボタンの幅
    private const float REMOVE_BUTTON_WIDTH = 24.0f;

    // カテゴリー選択ドロップダウンの幅
    private const float CATEGORY_POPUP_WIDTH = 128.0f;

    // 音量ラベルの幅
    private const float VOLUME_LABEL_WIDTH = 32.0f;

    // 詳細ボタンの幅
    private const float DETAIL_BUTTON_WIDTH = 56.0f;

    // カテゴリータグの幅
    private const float CATEGORY_TAG_WIDTH = 96.0f;

    // ID 名表示欄の幅
    private const float ID_NAME_WIDTH = 225.0f;

    // 確認ダイアログに表示する ID 名の最大件数
    private const int DIALOG_PREVIEW_COUNT = 8;

    // 「すべて」カテゴリータブのインデックス
    private const int CATEGORY_TAB_ALL = 0;

    // 2D/3D フィルタ：すべて
    private const int FILTER_DIM_ALL = 0;

    // 2D/3D フィルタ：2D のみ
    private const int FILTER_DIM_2D = 1;

    // 2D/3D フィルタ：3D のみ
    private const int FILTER_DIM_3D = 2;

    // 音量の最小値
    private const float MIN_VOLUME = 0.0f;

    // 音量の最大値
    private const float MAX_VOLUME = 1.0f;

    // ピッチの最小値
    private const float MIN_PITCH = 0.1f;

    // ピッチの最大値
    private const float MAX_PITCH = 3.0f;

    // 距離の最小下限値
    private const float MIN_DISTANCE_LIMIT = 0.01f;

    // 最大距離を最小距離より大きく保つための余白
    private const float DISTANCE_MARGIN = 0.01f;

    // カテゴリー行の淡い背景アルファ値
    private const float ROW_TINT_ALPHA = 0.07f;

    // エラー表示の色
    private static readonly Color ERROR_COLOR = new Color(1.0f, 0.4f, 0.4f);

    // 警告表示の色
    private static readonly Color WARNING_COLOR = new Color(1.0f, 0.68f, 0.25f);

    // 補足表示の色
    private static readonly Color HINT_COLOR = new Color(0.7f, 0.7f, 0.7f);

    // 選択中行の背景ハイライト色
    private static readonly Color SELECTED_ROW_COLOR = new Color(0.28f, 0.58f, 0.96f, 0.20f);

    // 試聴中行の背景ハイライト色
    private static readonly Color PLAYING_ROW_COLOR = new Color(0.28f, 0.78f, 0.40f, 0.22f);

    // 2D バッジの色
    private static readonly Color BADGE_2D_COLOR = new Color(0.25f, 0.55f, 0.80f);

    // 3D バッジの色
    private static readonly Color BADGE_3D_COLOR = new Color(0.88f, 0.50f, 0.18f);

    // 一括適用ボタンの色
    private static readonly Color APPLY_BUTTON_COLOR = new Color(0.45f, 0.85f, 1.0f);

    // 削除ボタンの色
    private static readonly Color DELETE_BUTTON_COLOR = new Color(1.0f, 0.5f, 0.5f);

    // サブタブの表示ラベル
    private static readonly string[] SUB_TAB_LABELS =
    {
        "一括・個別編集（カテゴリー / 3D / 試聴）",
        "ID の新規追加",
        "ID の削除"
    };

    // 2D/3D フィルタの表示ラベル
    private static readonly string[] DIMENSION_FILTER_LABELS = { "すべて", "2D", "3D" };

    // 3D 音源 ON/OFF の選択ラベル
    private static readonly string[] IS_3D_VALUE_LABELS = { "2D 音源 (OFF)", "3D 音源 (ON)" };

    // カテゴリー値一覧
    private static readonly SoundCategory[] CATEGORY_VALUES = (SoundCategory[])Enum.GetValues(typeof(SoundCategory));

    /// <summary>
    /// 追加予定の1行分の入力内容です。
    /// </summary>
    private sealed class AddItem
    {
        /// <summary>
        /// 入力された ID 名を取得または設定します。
        /// </summary>
        public string IdName { get; set; } = "";

        /// <summary>
        /// 登録する AudioClip を取得または設定します。
        /// </summary>
        public AudioClip Clip { get; set; }

        /// <summary>
        /// 詳細設定を開いているかを取得または設定します。
        /// </summary>
        public bool IsDetailOpen { get; set; }

        /// <summary>
        /// 登録する再生設定を取得します。
        /// </summary>
        public SoundDatabase.SoundData Settings { get; } = new SoundDatabase.SoundData();
    }

    // 再描画を要求する処理
    private readonly Action m_requestRepaint;

    // 追加予定の一覧
    private readonly List<AddItem> m_addList = new List<AddItem>();

    // 登録済みの ID（None を除く）
    private readonly List<SoundToolIdRegistry.IdEntry> m_registeredEntries = new List<SoundToolIdRegistry.IdEntry>();

    // 登録済みの ID 名（None を含む・重複確認用）
    private readonly HashSet<string> m_existingNames = new HashSet<string>(StringComparer.Ordinal);

    // 一括編集対象に選択した AudioID
    private readonly HashSet<AudioID> m_bulkSelectedIds = new HashSet<AudioID>();

    // 削除対象に選択した ID 名
    private readonly HashSet<string> m_deleteSelectedNames = new HashSet<string>(StringComparer.Ordinal);

    // 一括適用する詳細設定（カテゴリー・3D・距離減衰・ループ・ピッチ）
    private readonly SoundToolBulkSettings m_bulkSettings = new SoundToolBulkSettings();

    // カテゴリータブの表示名
    private readonly string[] m_categoryTabLabels = new string[CATEGORY_VALUES.Length + 1];

    // カテゴリー別の件数
    private readonly int[] m_categoryCounts = new int[CATEGORY_VALUES.Length + 1];

    // 選択中のサブタブ
    private IdManageSubTab m_subTab = IdManageSubTab.TAB_CATEGORY_AND_BULK;

    // 追加時の初期カテゴリー
    private SoundCategory m_defaultCategory = SoundCategory.BGM;

    // 一括編集一覧のスクロール位置
    private Vector2 m_bulkScrollPosition = Vector2.zero;

    // 追加予定一覧のスクロール位置
    private Vector2 m_addScrollPosition = Vector2.zero;

    // 削除一覧のスクロール位置
    private Vector2 m_deleteScrollPosition = Vector2.zero;

    // 一括編集一覧の検索文字列
    private string m_editSearchText = "";

    // 一括編集一覧のカテゴリーフィルタ位置
    private int m_categoryFilterIndex = CATEGORY_TAB_ALL;

    // 一括編集一覧の 2D/3D フィルタ
    private int m_dimensionFilterIndex = FILTER_DIM_ALL;

    // 下部の一括変更パネルを開いているか
    private bool m_isBulkPanelOpen = true;

    // 最後にチェック操作した行インデックス（Shift 範囲選択用）
    private int m_lastBulkToggledIndex = -1;

    // 削除一覧の検索文字列
    private string m_deleteSearchText = "";

    // 色分け ID 名用のボールドラベルスタイル
    private GUIStyle m_coloredIdStyle;

    // クリップ付きミニラベルスタイル
    private GUIStyle m_clippedMiniLabelStyle;

    /// <summary>
    /// ID 管理画面を生成します。
    /// </summary>
    /// <param name="requestRepaint">再描画を要求する処理。</param>
    public SoundToolIdManageView(Action requestRepaint)
    {
        m_requestRepaint = requestRepaint;
        RefreshCache();
    }

    /// <summary>
    /// 登録済み ID の情報を再取得します。
    /// </summary>
    public void RefreshCache()
    {
        m_registeredEntries.Clear();
        m_existingNames.Clear();

        foreach (SoundToolIdRegistry.IdEntry entry in SoundToolIdRegistry.GetCurrentEntries())
        {
            m_existingNames.Add(entry.Name);

            if (entry.Name != nameof(AudioID.None))
            {
                m_registeredEntries.Add(entry);
            }
        }
    }

    /// <summary>
    /// ID 管理画面を描画します（上部・下部を固定し中央リストのみスクロールさせることで見切れを防ぎます）。
    /// </summary>
    public void Draw()
    {
        SoundDatabase database = SoundToolDatabaseProvider.GetDatabase();

        if (database == null)
        {
            SoundToolGui.DrawMissingDatabase();
            return;
        }

        DrawSubTabBar();
        DrawPreviewStatusBar();

        switch (m_subTab)
        {
            case IdManageSubTab.TAB_ADD_ID:
                DrawAddTab(database);
                break;

            case IdManageSubTab.TAB_DELETE_ID:
                DrawDeleteTab(database);
                break;

            default:
                DrawCategoryAndBulkTab(database);
                break;
        }
    }

    /// <summary>
    /// 画面上部のサブタブを描画します。
    /// </summary>
    private void DrawSubTabBar()
    {
        EditorGUILayout.Space(2.0f);
        m_subTab = (IdManageSubTab)GUILayout.Toolbar((int)m_subTab, SUB_TAB_LABELS, GUILayout.Height(24.0f));
    }

    /// <summary>
    /// 画面上部の試聴停止コントロールバーを描画します。
    /// </summary>
    private static void DrawPreviewStatusBar()
    {
        bool isPlaying = SoundToolPreviewPlayer.HasActivePreview();

        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

        if (isPlaying)
        {
            SoundDatabase.SoundData active = SoundToolPreviewPlayer.CurrentData;
            string nameText = active != null ? active.m_id.ToString() : "追加予定 AudioClip";
            GUILayout.Label($"▶ 試聴中：{nameText}", EditorStyles.miniBoldLabel);
        }
        else
        {
            GUILayout.Label("各行の ▶ で試聴、チェック選択（Shift範囲選択可）でカテゴリー・3D・距離減衰を一括変更できます。", EditorStyles.miniLabel);
        }

        GUILayout.FlexibleSpace();

        SoundToolPreviewPlayer.UseVolume = GUILayout.Toggle(
            SoundToolPreviewPlayer.UseVolume,
            "音量反映",
            EditorStyles.miniButton,
            GUILayout.Width(66.0f));

        using (new EditorGUI.DisabledScope(!isPlaying))
        {
            if (GUILayout.Button("■ 試聴停止", EditorStyles.miniButton, GUILayout.Width(76.0f)))
            {
                SoundToolPreviewPlayer.Stop();
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// カテゴリー・3D・距離減衰の一括および個別編集タブを描画します。
    /// </summary>
    private void DrawCategoryAndBulkTab(SoundDatabase database)
    {
        List<SoundDatabase.SoundData> visibleList = BuildVisibleEditList(database);

        // 上部固定：検索・2D/3Dフィルタ・カテゴリータブ・選択操作
        DrawEditFilterAndSelectionHeader(visibleList);

        // 中央スクロール領域：ID が増えても上下の UI が見切れない
        m_bulkScrollPosition = EditorGUILayout.BeginScrollView(m_bulkScrollPosition, GUILayout.ExpandHeight(true));

        if (visibleList.Count == 0)
        {
            EditorGUILayout.HelpBox("条件に一致する音源がありません。", MessageType.Info);
        }

        for (int i = 0; i < visibleList.Count; i++)
        {
            DrawEditRow(database, visibleList, visibleList[i], i);
        }

        EditorGUILayout.EndScrollView();

        // 下部固定：選択中音源への一括変更パネル（カテゴリー / 3D音源 / 距離減衰 / ループ / ピッチ）
        DrawBulkEditFooterPanel(database);
    }

    /// <summary>
    /// 検索・2D/3D・カテゴリー絞り込み・選択操作のヘッダーを描画します。
    /// </summary>
    private void DrawEditFilterAndSelectionHeader(List<SoundDatabase.SoundData> visibleList)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("検索", GUILayout.Width(32.0f));
        m_editSearchText = EditorGUILayout.TextField(m_editSearchText, EditorStyles.toolbarSearchField);

        if (GUILayout.Button("クリア", EditorStyles.miniButton, GUILayout.Width(48.0f)))
        {
            m_editSearchText = "";
            GUI.FocusControl(null);
        }

        GUILayout.Space(8.0f);
        m_dimensionFilterIndex = GUILayout.Toolbar(m_dimensionFilterIndex, DIMENSION_FILTER_LABELS, EditorStyles.miniButton, GUILayout.Width(138.0f));
        EditorGUILayout.EndHorizontal();

        // カテゴリー絞り込みタブ（横幅に応じて自動折り返し）
        m_categoryFilterIndex = Mathf.Clamp(m_categoryFilterIndex, 0, m_categoryTabLabels.Length - 1);
        m_categoryTabLabels[CATEGORY_TAB_ALL] = $"すべて ({m_categoryCounts[CATEGORY_TAB_ALL]})";
        for (int i = 0; i < CATEGORY_VALUES.Length; i++)
        {
            m_categoryTabLabels[i + 1] = $"{CATEGORY_VALUES[i]} ({m_categoryCounts[i + 1]})";
        }

        int columns = EditorGUIUtility.currentViewWidth < 680.0f ? 3 : m_categoryTabLabels.Length;
        m_categoryFilterIndex = GUILayout.SelectionGrid(m_categoryFilterIndex, m_categoryTabLabels, columns, EditorStyles.miniButton);

        // 選択操作バー
        int visibleSelectedCount = 0;
        for (int i = 0; i < visibleList.Count; i++)
        {
            if (m_bulkSelectedIds.Contains(visibleList[i].m_id))
            {
                visibleSelectedCount++;
            }
        }

        int hiddenSelectedCount = m_bulkSelectedIds.Count - visibleSelectedCount;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"表示 {visibleList.Count} 件 / 選択 {m_bulkSelectedIds.Count} 件", EditorStyles.miniBoldLabel, GUILayout.Width(165.0f));

        if (hiddenSelectedCount > 0)
        {
            SoundToolGui.DrawColoredMiniLabel($"（非表示の選択が {hiddenSelectedCount} 件あります）", WARNING_COLOR);
        }

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("表示中を全選択", EditorStyles.miniButton, GUILayout.Width(96.0f)))
        {
            for (int i = 0; i < visibleList.Count; i++)
            {
                m_bulkSelectedIds.Add(visibleList[i].m_id);
            }
        }

        if (GUILayout.Button("選択解除", EditorStyles.miniButton, GUILayout.Width(64.0f)))
        {
            m_bulkSelectedIds.Clear();
        }

        if (GUILayout.Button("選択反転", EditorStyles.miniButton, GUILayout.Width(64.0f)))
        {
            for (int i = 0; i < visibleList.Count; i++)
            {
                AudioID id = visibleList[i].m_id;
                if (!m_bulkSelectedIds.Remove(id))
                {
                    m_bulkSelectedIds.Add(id);
                }
            }
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 検索・2D/3D・カテゴリー条件に合致する SoundDatabase 内の音源一覧を構築します。
    /// </summary>
    private List<SoundDatabase.SoundData> BuildVisibleEditList(SoundDatabase database)
    {
        List<SoundDatabase.SoundData> result = new List<SoundDatabase.SoundData>();
        Array.Clear(m_categoryCounts, 0, m_categoryCounts.Length);
        string keyword = (m_editSearchText ?? "").Trim();

        for (int i = 0; i < database.m_soundList.Count; i++)
        {
            SoundDatabase.SoundData data = database.m_soundList[i];
            if (data == null || data.m_id == AudioID.None)
            {
                continue;
            }

            m_categoryCounts[CATEGORY_TAB_ALL]++;
            int catIndex = Array.IndexOf(CATEGORY_VALUES, data.m_category);
            if (catIndex >= 0)
            {
                m_categoryCounts[catIndex + 1]++;
            }

            if (m_categoryFilterIndex != CATEGORY_TAB_ALL && data.m_category != CATEGORY_VALUES[m_categoryFilterIndex - 1])
            {
                continue;
            }

            if (m_dimensionFilterIndex == FILTER_DIM_2D && data.m_is3D)
            {
                continue;
            }

            if (m_dimensionFilterIndex == FILTER_DIM_3D && !data.m_is3D)
            {
                continue;
            }

            if (keyword.Length > 0)
            {
                string idName = data.m_id.ToString();
                string clipName = data.m_clip != null ? data.m_clip.name : "";
                bool match = idName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0
                    || clipName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;

                if (!match)
                {
                    continue;
                }
            }

            result.Add(data);
        }

        return result;
    }

    /// <summary>
    /// 一括・個別編集一覧の1行を描画します（チェック選択・試聴・2D/3D切替・色分けID・カテゴリー変更）。
    /// </summary>
    private void DrawEditRow(
        SoundDatabase database,
        List<SoundDatabase.SoundData> visibleList,
        SoundDatabase.SoundData data,
        int rowIndex)
    {
        bool isSelected = m_bulkSelectedIds.Contains(data.m_id);
        bool isPlaying = SoundToolPreviewPlayer.IsPlaying(data.m_id);
        Color categoryColor = SoundToolGui.GetCategoryColor(data.m_category);

        Rect rowRect = EditorGUILayout.BeginHorizontal(EditorStyles.helpBox, GUILayout.Height(COMPACT_ROW_HEIGHT));

        if (Event.current.type == EventType.Repaint)
        {
            Color bgColor;
            if (isPlaying)
            {
                bgColor = PLAYING_ROW_COLOR;
            }
            else if (isSelected)
            {
                bgColor = SELECTED_ROW_COLOR;
            }
            else
            {
                bgColor = new Color(categoryColor.r, categoryColor.g, categoryColor.b, ROW_TINT_ALPHA);
            }

            EditorGUI.DrawRect(rowRect, bgColor);
            EditorGUI.DrawRect(new Rect(rowRect.x, rowRect.y, COLOR_BAR_WIDTH, rowRect.height), categoryColor);
        }

        GUILayout.Space(COLOR_BAR_WIDTH + 2.0f);

        // 一括変更用の選択チェックボックス（Shift 範囲選択対応）
        bool newSelected = EditorGUILayout.Toggle(isSelected, GUILayout.Width(SELECT_TOGGLE_WIDTH));
        if (newSelected != isSelected)
        {
            UpdateBulkSelection(visibleList, rowIndex, data.m_id, newSelected);
        }

        // 試聴ボタン
        using (new EditorGUI.DisabledScope(data.m_clip == null))
        {
            if (GUILayout.Button(isPlaying ? "■" : "▶", EditorStyles.miniButton, GUILayout.Width(PREVIEW_BUTTON_WIDTH), GUILayout.Height(20.0f)))
            {
                if (isPlaying)
                {
                    SoundToolPreviewPlayer.Stop();
                }
                else
                {
                    SoundToolPreviewPlayer.PlaySingle(data);
                }
            }
        }

        // 2D / 3D 状態バッジ（クリックで単体の 3D 設定を即切り替え可能）
        Color prevBg = GUI.backgroundColor;
        GUI.backgroundColor = data.m_is3D ? BADGE_3D_COLOR : BADGE_2D_COLOR;
        if (GUILayout.Button(data.m_is3D ? "3D" : "2D", EditorStyles.miniButton, GUILayout.Width(DIMENSION_BADGE_WIDTH), GUILayout.Height(18.0f)))
        {
            Undo.RecordObject(database, "Toggle Sound 3D");
            data.m_is3D = !data.m_is3D;
            SoundToolDatabaseProvider.MarkDirty(database);
        }
        GUI.backgroundColor = prevBg;

        // カテゴリータグ
        SoundToolGui.DrawCategoryTag(data.m_category, CATEGORY_TAG_WIDTH);

        // カテゴリー別に色分けされた ID 名
        DrawColoredIdLabel(data.m_id.ToString(), categoryColor, ID_NAME_WIDTH);

        // Clip 名（3D時は距離範囲も小さく補足表示）
        string clipBase = data.m_clip != null ? data.m_clip.name : "Clip 未設定";
        string distanceInfo = data.m_is3D ? $"  [{data.m_rolloffMode} {data.m_minDistance:0.#}-{data.m_maxDistance:0.#}m]" : "";
        string infoText = clipBase + distanceInfo;

        Color prevContent = GUI.contentColor;
        if (data.m_clip == null)
        {
            GUI.contentColor = ERROR_COLOR;
        }
        Rect clipRect = GUILayoutUtility.GetRect(GUIContent.none, GetClippedMiniLabelStyle(), GUILayout.ExpandWidth(true), GUILayout.MinWidth(60.0f));
        GUI.Label(clipRect, new GUIContent(infoText, infoText), GetClippedMiniLabelStyle());
        GUI.contentColor = prevContent;

        // 個別カテゴリー変更ドロップダウン
        SoundCategory newCategory = (SoundCategory)EditorGUILayout.EnumPopup(data.m_category, GUILayout.Width(CATEGORY_POPUP_WIDTH));
        if (newCategory != data.m_category)
        {
            Undo.RecordObject(database, "Change Sound Category");
            data.m_category = newCategory;
            SoundToolDatabaseProvider.MarkDirty(database);
            m_requestRepaint?.Invoke();
        }

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 選択チェックの変更と Shift 範囲選択を処理します。
    /// </summary>
    private void UpdateBulkSelection(List<SoundDatabase.SoundData> visibleList, int rowIndex, AudioID id, bool isSelected)
    {
        bool isRange = Event.current.shift && m_lastBulkToggledIndex >= 0 && m_lastBulkToggledIndex < visibleList.Count;

        if (isRange)
        {
            int start = Mathf.Min(m_lastBulkToggledIndex, rowIndex);
            int end = Mathf.Max(m_lastBulkToggledIndex, rowIndex);

            for (int i = start; i <= end; i++)
            {
                if (isSelected)
                {
                    m_bulkSelectedIds.Add(visibleList[i].m_id);
                }
                else
                {
                    m_bulkSelectedIds.Remove(visibleList[i].m_id);
                }
            }
        }
        else
        {
            if (isSelected)
            {
                m_bulkSelectedIds.Add(id);
            }
            else
            {
                m_bulkSelectedIds.Remove(id);
            }
        }

        m_lastBulkToggledIndex = rowIndex;
    }

    /// <summary>
    /// 下部固定の一括変更パネル（カテゴリー・3D音源・距離減衰・ループ・ピッチ）を描画します。
    /// </summary>
    private void DrawBulkEditFooterPanel(SoundDatabase database)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        string headerText = $"一括変更パネル（選択中 {m_bulkSelectedIds.Count} 件：チェックした項目だけを反映）";
        m_isBulkPanelOpen = EditorGUILayout.Foldout(m_isBulkPanelOpen, headerText, true, EditorStyles.foldoutHeader);

        if (!m_isBulkPanelOpen)
        {
            EditorGUILayout.EndVertical();
            return;
        }

        // 1行目：カテゴリー と 3D 音源 ON/OFF
        EditorGUILayout.BeginHorizontal();

        m_bulkSettings.ApplyCategory = EditorGUILayout.ToggleLeft("カテゴリー", m_bulkSettings.ApplyCategory, GUILayout.Width(86.0f));
        using (new EditorGUI.DisabledScope(!m_bulkSettings.ApplyCategory))
        {
            m_bulkSettings.Values.m_category = (SoundCategory)EditorGUILayout.EnumPopup(m_bulkSettings.Values.m_category, GUILayout.Width(138.0f));
        }

        GUILayout.Space(12.0f);

        m_bulkSettings.ApplyIs3D = EditorGUILayout.ToggleLeft("3D 音源", m_bulkSettings.ApplyIs3D, GUILayout.Width(74.0f));
        using (new EditorGUI.DisabledScope(!m_bulkSettings.ApplyIs3D))
        {
            int current3DIndex = m_bulkSettings.Values.m_is3D ? 1 : 0;
            int new3DIndex = GUILayout.Toolbar(current3DIndex, IS_3D_VALUE_LABELS, EditorStyles.miniButton, GUILayout.Width(210.0f));
            bool newIs3D = new3DIndex == 1;

            // 3D 音源を ON に切り替えたタイミングで距離減衰も設定しやすいよう連動チェックする
            if (newIs3D && !m_bulkSettings.Values.m_is3D)
            {
                m_bulkSettings.ApplyRolloff = true;
            }

            m_bulkSettings.Values.m_is3D = newIs3D;
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        // 2行目：距離減衰（モード・最小距離・最大距離・カスタムカーブ）
        m_bulkSettings.ApplyRolloff = EditorGUILayout.ToggleLeft(
            "距離減衰を一括変更（モード / 最小距離 / 最大距離 / カスタムカーブ）",
            m_bulkSettings.ApplyRolloff);

        if (m_bulkSettings.ApplyRolloff)
        {
            EditorGUI.indentLevel++;
            SoundToolGui.DrawRolloffFields(m_bulkSettings.Values);
            EditorGUI.indentLevel--;
        }

        // 3行目：ループ と ピッチ
        EditorGUILayout.BeginHorizontal();

        m_bulkSettings.ApplyLoop = EditorGUILayout.ToggleLeft("ループ", m_bulkSettings.ApplyLoop, GUILayout.Width(70.0f));
        using (new EditorGUI.DisabledScope(!m_bulkSettings.ApplyLoop))
        {
            m_bulkSettings.Values.m_loop = EditorGUILayout.Toggle(m_bulkSettings.Values.m_loop, GUILayout.Width(24.0f));
        }

        GUILayout.Space(12.0f);

        m_bulkSettings.ApplyPitch = EditorGUILayout.ToggleLeft("ピッチ", m_bulkSettings.ApplyPitch, GUILayout.Width(68.0f));
        using (new EditorGUI.DisabledScope(!m_bulkSettings.ApplyPitch))
        {
            m_bulkSettings.Values.m_pitch = EditorGUILayout.Slider(m_bulkSettings.Values.m_pitch, MIN_PITCH, MAX_PITCH);
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(2.0f);

        // 一括適用ボタン
        bool canApply = m_bulkSelectedIds.Count > 0 && m_bulkSettings.HasAnyOptionEnabled();
        using (new EditorGUI.DisabledScope(!canApply))
        {
            Color prevBg = GUI.backgroundColor;
            GUI.backgroundColor = APPLY_BUTTON_COLOR;

            if (GUILayout.Button($"選択中の {m_bulkSelectedIds.Count} 件へ一括適用（Ctrl+Z で元に戻せます）", GUILayout.Height(28.0f)))
            {
                GUI.backgroundColor = prevBg;
                ApplyBulkSettingsToSelected(database);
            }

            GUI.backgroundColor = prevBg;
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 選択中の全音源へ一括設定を適用します。
    /// </summary>
    private void ApplyBulkSettingsToSelected(SoundDatabase database)
    {
        if (database == null || m_bulkSelectedIds.Count == 0 || !m_bulkSettings.HasAnyOptionEnabled())
        {
            return;
        }

        Undo.RecordObject(database, "Bulk Edit Sound Settings");

        int appliedCount = 0;
        for (int i = 0; i < database.m_soundList.Count; i++)
        {
            SoundDatabase.SoundData data = database.m_soundList[i];
            if (data != null && m_bulkSelectedIds.Contains(data.m_id))
            {
                m_bulkSettings.Apply(data);
                appliedCount++;
            }
        }

        SoundToolDatabaseProvider.MarkDirty(database);
        Debug.Log($"[SoundTool] {appliedCount} 件のサウンド設定を一括変更しました。");
        m_requestRepaint?.Invoke();
    }

    /// <summary>
    /// カテゴリー色に合わせて読みやすく調整した文字色で ID 名を描画します。
    /// </summary>
    private void DrawColoredIdLabel(string idText, Color categoryColor, float width)
    {
        Color textColor = EditorGUIUtility.isProSkin
            ? Color.Lerp(categoryColor, Color.white, 0.52f)
            : Color.Lerp(categoryColor, Color.black, 0.35f);

        GUIStyle style = GetColoredIdStyle();
        Color prevColor = style.normal.textColor;
        style.normal.textColor = textColor;

        Rect labelRect = GUILayoutUtility.GetRect(GUIContent.none, style, GUILayout.Width(width), GUILayout.Height(20.0f));
        GUI.Label(labelRect, new GUIContent(idText, idText), style);

        style.normal.textColor = prevColor;
    }

    /// <summary>
    /// ID 新規追加タブを描画します（上部ドロップ欄・下部登録ボタン固定＋中央リストスクロール）。
    /// </summary>
    private void DrawAddTab(SoundDatabase database)
    {
        DrawDropArea();

        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        m_defaultCategory = (SoundCategory)EditorGUILayout.EnumPopup("追加時の初期カテゴリー", m_defaultCategory);

        if (GUILayout.Button("+ 空の行を追加", GUILayout.Width(110.0f)))
        {
            m_addList.Add(CreateAddItem());
        }

        using (new EditorGUI.DisabledScope(m_addList.Count == 0))
        {
            if (GUILayout.Button("全行クリア", GUILayout.Width(80.0f)))
            {
                m_addList.Clear();
            }
        }
        EditorGUILayout.EndHorizontal();

        m_addScrollPosition = EditorGUILayout.BeginScrollView(m_addScrollPosition, GUILayout.ExpandHeight(true));

        if (m_addList.Count == 0)
        {
            EditorGUILayout.HelpBox("上の枠へ AudioClip をドラッグ＆ドロップするか、「+ 空の行を追加」から登録してください。", MessageType.Info);
        }

        int removeIndex = -1;

        for (int i = 0; i < m_addList.Count; i++)
        {
            if (DrawAddRow(m_addList[i], i))
            {
                removeIndex = i;
            }
        }

        if (removeIndex >= 0)
        {
            m_addList.RemoveAt(removeIndex);
        }

        EditorGUILayout.EndScrollView();

        DrawRegisterButton(database);
    }

    /// <summary>
    /// 追加予定の1行を描画します（未登録 AudioClip の試聴ボタン・カテゴリー色帯付き）。
    /// </summary>
    private bool DrawAddRow(AddItem item, int rowIndex)
    {
        bool isRemoveRequested = false;
        bool isPlaying = SoundToolPreviewPlayer.IsPlayingDirectClip(item.Clip);
        Color categoryColor = SoundToolGui.GetCategoryColor(item.Settings.m_category);

        Rect boxRect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        if (Event.current.type == EventType.Repaint)
        {
            if (isPlaying)
            {
                EditorGUI.DrawRect(boxRect, PLAYING_ROW_COLOR);
            }

            EditorGUI.DrawRect(new Rect(boxRect.x, boxRect.y, COLOR_BAR_WIDTH, boxRect.height), categoryColor);
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(COLOR_BAR_WIDTH);
        GUILayout.Label((rowIndex + 1).ToString(), GUILayout.Width(ROW_INDEX_WIDTH));

        using (new EditorGUI.DisabledScope(item.Clip == null))
        {
            if (GUILayout.Button(isPlaying ? "■" : "▶", GUILayout.Width(PREVIEW_BUTTON_WIDTH)))
            {
                if (isPlaying)
                {
                    SoundToolPreviewPlayer.Stop();
                }
                else
                {
                    SoundToolPreviewPlayer.PlayClipDirect(
                        item.Clip,
                        item.Settings.m_volume,
                        item.Settings.m_pitch,
                        item.Settings.m_loop);
                }
            }
        }

        item.IdName = EditorGUILayout.TextField(item.IdName);

        AudioClip newClip = (AudioClip)EditorGUILayout.ObjectField(item.Clip, typeof(AudioClip), false);

        if (newClip != item.Clip)
        {
            SetClipToAddItem(item, newClip);
        }

        if (GUILayout.Button("×", GUILayout.Width(REMOVE_BUTTON_WIDTH)))
        {
            isRemoveRequested = true;
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(COLOR_BAR_WIDTH + ROW_INDEX_WIDTH + PREVIEW_BUTTON_WIDTH + 6.0f);

        item.Settings.m_category = (SoundCategory)EditorGUILayout.EnumPopup(
            item.Settings.m_category,
            GUILayout.Width(CATEGORY_POPUP_WIDTH));

        GUILayout.Label("音量", GUILayout.Width(VOLUME_LABEL_WIDTH));
        item.Settings.m_volume = EditorGUILayout.Slider(item.Settings.m_volume, MIN_VOLUME, MAX_VOLUME);

        item.IsDetailOpen = GUILayout.Toggle(item.IsDetailOpen, "詳細", EditorStyles.miniButton, GUILayout.Width(DETAIL_BUTTON_WIDTH));

        EditorGUILayout.EndHorizontal();

        if (item.IsDetailOpen)
        {
            DrawAddRowDetail(item);
        }

        DrawAddRowMessage(item, rowIndex);

        EditorGUILayout.EndVertical();

        return isRemoveRequested;
    }

    /// <summary>
    /// 追加予定の行の詳細設定を描画します。
    /// </summary>
    private static void DrawAddRowDetail(AddItem item)
    {
        EditorGUI.indentLevel++;

        item.Settings.m_pitch = EditorGUILayout.Slider("ピッチ", item.Settings.m_pitch, MIN_PITCH, MAX_PITCH);
        item.Settings.m_loop = EditorGUILayout.Toggle("ループ", item.Settings.m_loop);
        item.Settings.m_is3D = EditorGUILayout.Toggle("3D 音源", item.Settings.m_is3D);

        if (item.Settings.m_is3D)
        {
            SoundToolGui.DrawRolloffFields(item.Settings);
        }

        EditorGUI.indentLevel--;
    }

    /// <summary>
    /// 追加予定の行の入力チェック結果を描画します。
    /// </summary>
    private void DrawAddRowMessage(AddItem item, int rowIndex)
    {
        string errorMessage = GetAddRowErrorMessage(item, rowIndex);

        if (!string.IsNullOrEmpty(errorMessage))
        {
            SoundToolGui.DrawColoredMiniLabel(errorMessage, ERROR_COLOR);
            return;
        }

        string sanitizedName = SoundToolIdRegistry.SanitizeName(item.IdName);

        if (sanitizedName != item.IdName.Trim())
        {
            SoundToolGui.DrawColoredMiniLabel($"登録名：{sanitizedName}", HINT_COLOR);
        }
    }

    /// <summary>
    /// 追加予定の行の入力エラーを取得します。
    /// </summary>
    private string GetAddRowErrorMessage(AddItem item, int rowIndex)
    {
        if (string.IsNullOrWhiteSpace(item.IdName))
        {
            return "ID 名を入力してください。";
        }

        string sanitizedName = SoundToolIdRegistry.SanitizeName(item.IdName);

        if (!SoundToolIdRegistry.IsValidIdentifier(sanitizedName))
        {
            return "この ID 名は使えません（先頭は英字または _ にしてください）。";
        }

        if (m_existingNames.Contains(sanitizedName))
        {
            return "既に登録されている ID 名です。";
        }

        for (int i = 0; i < rowIndex; i++)
        {
            if (SoundToolIdRegistry.SanitizeName(m_addList[i].IdName) == sanitizedName)
            {
                return "他の行と ID 名が重複しています。";
            }
        }

        if (item.Clip == null)
        {
            return "AudioClip を設定してください。";
        }

        return "";
    }

    /// <summary>
    /// 一括追加ボタンを描画します。
    /// </summary>
    private void DrawRegisterButton(SoundDatabase database)
    {
        bool hasError = false;

        for (int i = 0; i < m_addList.Count; i++)
        {
            if (!string.IsNullOrEmpty(GetAddRowErrorMessage(m_addList[i], i)))
            {
                hasError = true;
                break;
            }
        }

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        using (new EditorGUI.DisabledScope(m_addList.Count == 0 || hasError))
        {
            if (GUILayout.Button($"{m_addList.Count} 件を一括追加", GUILayout.Height(30.0f)))
            {
                RegisterAddItems(database);
            }
        }

        if (hasError)
        {
            SoundToolGui.DrawColoredMiniLabel("エラーのある行を修正すると追加できます。", ERROR_COLOR);
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 追加予定の内容を SoundDatabase と AudioID.cs へ登録します。
    /// </summary>
    private void RegisterAddItems(SoundDatabase database)
    {
        SoundToolPreviewPlayer.Stop();

        List<SoundToolIdRegistry.IdEntry> entries = SoundToolIdRegistry.GetCurrentEntries();
        int nextValue = SoundToolIdRegistry.GetNextValue(entries);
        List<string> addedNames = new List<string>();

        Undo.RecordObject(database, "Add Sound IDs");

        foreach (AddItem item in m_addList)
        {
            string idName = SoundToolIdRegistry.SanitizeName(item.IdName);

            database.m_soundList.Add(CreateSoundData(item, (AudioID)nextValue));
            entries.Add(new SoundToolIdRegistry.IdEntry(idName, nextValue));
            addedNames.Add(idName);

            nextValue++;
        }

        SoundToolDatabaseProvider.MarkDirty(database);
        SoundToolDatabaseProvider.SaveIfNeeded();

        SessionState.SetString(SoundToolIdRegistry.RECENT_ADDED_IDS_KEY, string.Join(",", addedNames));

        SoundToolIdRegistry.GenerateFile(entries, nextValue);

        Debug.Log($"[SoundTool] {addedNames.Count} 件の ID を追加しました。");

        m_addList.Clear();
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 追加予定の行から登録用のサウンド設定を作成します。
    /// </summary>
    private static SoundDatabase.SoundData CreateSoundData(AddItem item, AudioID audioId)
    {
        SoundDatabase.SoundData source = item.Settings;
        AnimationCurve curveCopy = source.m_customRolloffCurve != null
            ? new AnimationCurve(source.m_customRolloffCurve.keys)
            : new AnimationCurve();

        return new SoundDatabase.SoundData
        {
            m_id = audioId,
            m_clip = item.Clip,
            m_category = source.m_category,
            m_volume = source.m_volume,
            m_pitch = source.m_pitch,
            m_loop = source.m_loop,
            m_is3D = source.m_is3D,
            m_minDistance = Mathf.Max(MIN_DISTANCE_LIMIT, source.m_minDistance),
            m_maxDistance = Mathf.Max(source.m_minDistance + DISTANCE_MARGIN, source.m_maxDistance),
            m_rolloffMode = source.m_rolloffMode,
            m_customRolloffCurve = curveCopy
        };
    }

    /// <summary>
    /// AudioClip のドラッグ＆ドロップ領域を描画します。
    /// </summary>
    private void DrawDropArea()
    {
        Rect dropArea = GUILayoutUtility.GetRect(0.0f, DROP_AREA_HEIGHT, GUILayout.ExpandWidth(true));

        GUI.Box(dropArea, GUIContent.none, EditorStyles.helpBox);
        GUI.Label(dropArea, "AudioClip をここにドラッグ＆ドロップ（複数選択に対応）", EditorStyles.centeredGreyMiniLabel);

        HandleDragAndDrop(dropArea);
    }

    /// <summary>
    /// ドラッグ＆ドロップの操作を処理します。
    /// </summary>
    private void HandleDragAndDrop(Rect dropArea)
    {
        Event currentEvent = Event.current;

        if (!dropArea.Contains(currentEvent.mousePosition))
        {
            return;
        }

        if (currentEvent.type != EventType.DragUpdated && currentEvent.type != EventType.DragPerform)
        {
            return;
        }

        bool containsAudioClip = false;

        foreach (UnityEngine.Object draggedObject in DragAndDrop.objectReferences)
        {
            if (draggedObject is AudioClip)
            {
                containsAudioClip = true;
                break;
            }
        }

        if (!containsAudioClip)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
            return;
        }

        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

        if (currentEvent.type == EventType.DragPerform)
        {
            DragAndDrop.AcceptDrag();
            AddDroppedClips(DragAndDrop.objectReferences);
        }

        currentEvent.Use();
    }

    /// <summary>
    /// ドロップされた AudioClip を追加予定へ登録します。空の行を優先して使います。
    /// </summary>
    private void AddDroppedClips(UnityEngine.Object[] draggedObjects)
    {
        foreach (UnityEngine.Object draggedObject in draggedObjects)
        {
            if (!(draggedObject is AudioClip audioClip))
            {
                continue;
            }

            AddItem targetItem = FindEmptyAddItem();

            if (targetItem == null)
            {
                targetItem = CreateAddItem();
                m_addList.Add(targetItem);
            }

            SetClipToAddItem(targetItem, audioClip);
        }

        m_requestRepaint?.Invoke();
    }

    /// <summary>
    /// 初期カテゴリーを適用した追加予定の行を作成します。
    /// </summary>
    private AddItem CreateAddItem()
    {
        AddItem item = new AddItem();
        item.Settings.m_category = m_defaultCategory;
        return item;
    }

    /// <summary>
    /// AudioClip が未設定の追加予定の行を取得します。
    /// </summary>
    private AddItem FindEmptyAddItem()
    {
        foreach (AddItem item in m_addList)
        {
            if (item.Clip == null)
            {
                return item;
            }
        }

        return null;
    }

    /// <summary>
    /// 追加予定の行へ AudioClip を設定します。ID 名が未入力なら Clip 名を大文字化して設定します。
    /// </summary>
    private static void SetClipToAddItem(AddItem item, AudioClip clip)
    {
        item.Clip = clip;

        if (clip == null || !string.IsNullOrWhiteSpace(item.IdName))
        {
            return;
        }

        item.IdName = SoundToolIdRegistry.SanitizeName(clip.name).ToUpperInvariant();
    }

    /// <summary>
    /// ID 削除タブを描画します（上部検索固定・下部削除ボタン固定＋中央リスト全面スクロール）。
    /// </summary>
    private void DrawDeleteTab(SoundDatabase database)
    {
        List<SoundToolIdRegistry.IdEntry> visibleEntries = BuildVisibleDeleteEntries();

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("検索", GUILayout.Width(32.0f));
        m_deleteSearchText = EditorGUILayout.TextField(m_deleteSearchText, EditorStyles.toolbarSearchField);

        if (GUILayout.Button("クリア", EditorStyles.miniButton, GUILayout.Width(48.0f)))
        {
            m_deleteSearchText = "";
            GUI.FocusControl(null);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(
            $"表示 {visibleEntries.Count} 件 / 削除対象 {m_deleteSelectedNames.Count} 件",
            EditorStyles.miniBoldLabel);

        if (GUILayout.Button("表示中を全選択", EditorStyles.miniButton, GUILayout.Width(100.0f)))
        {
            foreach (SoundToolIdRegistry.IdEntry entry in visibleEntries)
            {
                m_deleteSelectedNames.Add(entry.Name);
            }
        }

        if (GUILayout.Button("選択解除", EditorStyles.miniButton, GUILayout.Width(70.0f)))
        {
            m_deleteSelectedNames.Clear();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        DrawDeleteList(database, visibleEntries);
        DrawDeleteButton(database);
    }

    /// <summary>
    /// 検索条件に合う削除候補の一覧を作成します。
    /// </summary>
    private List<SoundToolIdRegistry.IdEntry> BuildVisibleDeleteEntries()
    {
        List<SoundToolIdRegistry.IdEntry> visibleEntries = new List<SoundToolIdRegistry.IdEntry>();
        string keyword = (m_deleteSearchText ?? "").Trim();

        foreach (SoundToolIdRegistry.IdEntry entry in m_registeredEntries)
        {
            if (keyword.Length == 0 || entry.Name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                visibleEntries.Add(entry);
            }
        }

        return visibleEntries;
    }

    /// <summary>
    /// 削除候補の一覧を描画します（カテゴリー色分け＆試聴ボタン付き）。
    /// </summary>
    private void DrawDeleteList(SoundDatabase database, List<SoundToolIdRegistry.IdEntry> visibleEntries)
    {
        m_deleteScrollPosition = EditorGUILayout.BeginScrollView(m_deleteScrollPosition, GUILayout.ExpandHeight(true));

        foreach (SoundToolIdRegistry.IdEntry entry in visibleEntries)
        {
            AudioID audioId = (AudioID)entry.Value;
            SoundDatabase.SoundData data = database.GetSoundData(audioId);
            bool isPlaying = SoundToolPreviewPlayer.IsPlaying(audioId);
            Color categoryColor = data != null ? SoundToolGui.GetCategoryColor(data.m_category) : Color.gray;

            Rect rowRect = EditorGUILayout.BeginHorizontal(EditorStyles.helpBox, GUILayout.Height(COMPACT_ROW_HEIGHT));

            if (Event.current.type == EventType.Repaint)
            {
                Color bgColor = isPlaying
                    ? PLAYING_ROW_COLOR
                    : new Color(categoryColor.r, categoryColor.g, categoryColor.b, ROW_TINT_ALPHA);
                EditorGUI.DrawRect(rowRect, bgColor);
                EditorGUI.DrawRect(new Rect(rowRect.x, rowRect.y, COLOR_BAR_WIDTH, rowRect.height), categoryColor);
            }

            GUILayout.Space(COLOR_BAR_WIDTH + 2.0f);

            using (new EditorGUI.DisabledScope(data == null || data.m_clip == null))
            {
                if (GUILayout.Button(isPlaying ? "■" : "▶", EditorStyles.miniButton, GUILayout.Width(PREVIEW_BUTTON_WIDTH), GUILayout.Height(20.0f)))
                {
                    if (isPlaying)
                    {
                        SoundToolPreviewPlayer.Stop();
                    }
                    else
                    {
                        SoundToolPreviewPlayer.PlaySingle(data);
                    }
                }
            }

            bool isSelected = m_deleteSelectedNames.Contains(entry.Name);
            bool newSelected = EditorGUILayout.Toggle(isSelected, GUILayout.Width(SELECT_TOGGLE_WIDTH));

            if (newSelected != isSelected)
            {
                if (newSelected)
                {
                    m_deleteSelectedNames.Add(entry.Name);
                }
                else
                {
                    m_deleteSelectedNames.Remove(entry.Name);
                }
            }

            if (data != null)
            {
                SoundToolGui.DrawCategoryTag(data.m_category, CATEGORY_TAG_WIDTH);
                DrawColoredIdLabel(entry.Name, categoryColor, ID_NAME_WIDTH);
                string clipText = data.m_clip != null ? data.m_clip.name : "Clip 未設定";
                Rect clipRect = GUILayoutUtility.GetRect(GUIContent.none, GetClippedMiniLabelStyle(), GUILayout.ExpandWidth(true), GUILayout.MinWidth(60.0f));
                GUI.Label(clipRect, new GUIContent(clipText, clipText), GetClippedMiniLabelStyle());
            }
            else
            {
                GUILayout.Label("未登録", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(CATEGORY_TAG_WIDTH));
                DrawColoredIdLabel(entry.Name, ERROR_COLOR, ID_NAME_WIDTH);
                SoundToolGui.DrawColoredMiniLabel("データ未登録の ID", ERROR_COLOR);
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 削除ボタンを描画します。
    /// </summary>
    private void DrawDeleteButton(SoundDatabase database)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        using (new EditorGUI.DisabledScope(m_deleteSelectedNames.Count == 0))
        {
            Color previousColor = GUI.backgroundColor;
            GUI.backgroundColor = DELETE_BUTTON_COLOR;

            if (GUILayout.Button($"選択した {m_deleteSelectedNames.Count} 件の ID を削除", GUILayout.Height(30.0f)))
            {
                GUI.backgroundColor = previousColor;
                ConfirmAndDelete(database);
            }

            GUI.backgroundColor = previousColor;
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 確認ダイアログを表示し、承認されたら選択した ID を削除します。
    /// </summary>
    private void ConfirmAndDelete(SoundDatabase database)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"{m_deleteSelectedNames.Count} 件の ID を削除します。");
        builder.AppendLine();

        int shownCount = 0;

        foreach (string idName in m_deleteSelectedNames)
        {
            if (shownCount >= DIALOG_PREVIEW_COUNT)
            {
                builder.AppendLine($"…ほか {m_deleteSelectedNames.Count - shownCount} 件");
                break;
            }

            builder.AppendLine($"・{idName}");
            shownCount++;
        }

        builder.AppendLine();
        builder.AppendLine("コード内でこの ID を使っている場合、コンパイルエラーになります。");
        builder.AppendLine("この操作は元に戻せません。");

        if (EditorUtility.DisplayDialog("ID の削除", builder.ToString(), "削除する", "キャンセル"))
        {
            ExecuteDelete(database);
        }

        GUIUtility.ExitGUI();
    }

    /// <summary>
    /// 選択した ID を SoundDatabase と AudioID.cs から削除します。
    /// </summary>
    private void ExecuteDelete(SoundDatabase database)
    {
        SoundToolPreviewPlayer.Stop();
        SoundToolMultiPreviewPlayer.StopAll();

        List<SoundToolIdRegistry.IdEntry> entries = SoundToolIdRegistry.GetCurrentEntries();
        int nextValue = SoundToolIdRegistry.GetNextValue(entries);

        HashSet<string> deleteNames = new HashSet<string>(m_deleteSelectedNames, StringComparer.Ordinal);

        Undo.RecordObject(database, "Delete Sound IDs");

        database.m_soundList.RemoveAll(
            soundData => soundData == null || deleteNames.Contains(soundData.m_id.ToString()));

        SoundToolDatabaseProvider.MarkDirty(database);
        SoundToolDatabaseProvider.SaveIfNeeded();

        entries.RemoveAll(entry => deleteNames.Contains(entry.Name));
        SoundToolIdRegistry.GenerateFile(entries, nextValue);

        Debug.Log($"[SoundTool] {deleteNames.Count} 件の ID を削除しました。");

        m_deleteSelectedNames.Clear();
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 色分け ID 表示用のボールドスタイルを取得します。
    /// </summary>
    private GUIStyle GetColoredIdStyle()
    {
        if (m_coloredIdStyle == null)
        {
            m_coloredIdStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip
            };
        }

        return m_coloredIdStyle;
    }

    /// <summary>
    /// はみ出しを防止するミニラベルスタイルを取得します。
    /// </summary>
    private GUIStyle GetClippedMiniLabelStyle()
    {
        if (m_clippedMiniLabelStyle == null)
        {
            m_clippedMiniLabelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip
            };
        }

        return m_clippedMiniLabelStyle;
    }
}

#endif