#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 検索・試聴・音量調整を行うミキサー画面です。
/// </summary>
public sealed class SoundToolMixerView
{
    // 1行の高さ
    private const float ROW_HEIGHT = 34.0f;

    // 選択チェックの幅
    private const float SELECT_TOGGLE_WIDTH = 18.0f;

    // 試聴ボタンの幅
    private const float PREVIEW_BUTTON_WIDTH = 28.0f;

    // カテゴリータグの幅
    private const float CATEGORY_TAG_WIDTH = 100.0f;

    // 名前欄の幅
    private const float NAME_COLUMN_WIDTH = 240.0f;

    // 音量スライダーの最小幅
    private const float SLIDER_MIN_WIDTH = 160.0f;

    // dB 表示欄の幅
    private const float DECIBEL_LABEL_WIDTH = 62.0f;

    // 相対調整スライダーの範囲（±）
    private const float TRIM_RANGE = 0.5f;

    // 音量ボタンの小さい刻み幅
    private const float VOLUME_STEP_SMALL = 0.05f;

    // 音量ボタンの大きい刻み幅
    private const float VOLUME_STEP_LARGE = 0.10f;

    // 音量の最小値
    private const float MIN_VOLUME = 0.0f;

    // 音量の最大値
    private const float MAX_VOLUME = 1.0f;

    // ピッチの最小値
    private const float MIN_PITCH = 0.1f;

    // ピッチの最大値
    private const float MAX_PITCH = 3.0f;

    // 「すべて」タブのインデックス
    private const int CATEGORY_TAB_ALL = 0;

    // SoundDatabase の音声リストのプロパティ名
    private const string SOUND_LIST_PROPERTY_NAME = "m_soundList";

    // 行の背景色（選択中）
    private static readonly Color SELECTED_ROW_COLOR = new Color(0.30f, 0.60f, 1.00f, 0.15f);

    // 行の背景色（詳細編集中）
    private static readonly Color FOCUSED_ROW_COLOR = new Color(0.25f, 0.50f, 0.95f, 0.30f);

    // 行の背景色（試聴中）
    private static readonly Color PLAYING_ROW_COLOR = new Color(0.30f, 0.80f, 0.40f, 0.20f);

    // 警告表示の色
    private static readonly Color WARNING_COLOR = new Color(1.0f, 0.65f, 0.2f);

    // エラー表示の色
    private static readonly Color ERROR_COLOR = new Color(1.0f, 0.4f, 0.4f);

    // カテゴリー値の一覧
    private static readonly SoundCategory[] CATEGORY_VALUES = (SoundCategory[])Enum.GetValues(typeof(SoundCategory));

    // 選択中のサウンド ID
    private readonly HashSet<AudioID> m_selectedIds = new HashSet<AudioID>();

    // 絞り込み後に表示するサウンド
    private readonly List<SoundDatabase.SoundData> m_filteredList = new List<SoundDatabase.SoundData>();

    // 詳細項目の一括設定
    private readonly SoundToolBulkSettings m_bulkSettings = new SoundToolBulkSettings();

    // カテゴリータブの表示文字列
    private readonly string[] m_categoryTabLabels = new string[CATEGORY_VALUES.Length + 1];

    // カテゴリー別の件数（0 は全件）
    private readonly int[] m_categoryCounts = new int[CATEGORY_VALUES.Length + 1];

    // 一覧のスクロール位置
    private Vector2 m_scrollPosition = Vector2.zero;

    // 検索文字列
    private string m_searchText = "";

    // カテゴリータブの選択位置
    private int m_categoryTabIndex = CATEGORY_TAB_ALL;

    // 選択中のみ表示するか
    private bool m_showSelectedOnly = false;

    // AudioClip 未設定のみ表示するか
    private bool m_showMissingClipOnly = false;

    // 選択中のフェーダーを連動させるか
    private bool m_isLinkedEdit = true;

    // 詳細編集の対象 ID
    private AudioID m_focusedId = AudioID.None;

    // 相対調整スライダーの現在値
    private float m_trimValue = 0.0f;

    // 「指定値に揃える」の値
    private float m_unifyVolume = 1.0f;

    // 詳細項目の一括変更パネルを開いているか
    private bool m_isBulkDetailFoldout = false;

    // 最後にチェックを操作した行（Shift 範囲選択用）
    private int m_lastToggledIndex = -1;

    // 詳細パネル用の SerializedObject
    private SerializedObject m_serializedDatabase;

    /// <summary>
    /// ID 追加直後の ID を選択状態にします。
    /// </summary>
    /// <returns>
    /// true：選択する ID がありました。
    /// false：選択する ID はありません。
    /// </returns>
    public bool SelectRecentAddedIds()
    {
        string idText = SessionState.GetString(SoundToolIdRegistry.RECENT_ADDED_IDS_KEY, "");

        if (string.IsNullOrEmpty(idText))
        {
            return false;
        }

        SessionState.EraseString(SoundToolIdRegistry.RECENT_ADDED_IDS_KEY);

        // 追加された ID だけを選択し、それだけを表示する
        m_selectedIds.Clear();

        foreach (string idName in idText.Split(','))
        {
            if (Enum.TryParse(idName, out AudioID audioId))
            {
                m_selectedIds.Add(audioId);
            }
        }

        m_showSelectedOnly = m_selectedIds.Count > 0;

        return m_selectedIds.Count > 0;
    }

    /// <summary>
    /// ミキサー画面を描画します。
    /// </summary>
    public void Draw()
    {
        SoundDatabase database = SoundToolDatabaseProvider.GetDatabase();

        if (database == null)
        {
            SoundToolGui.DrawMissingDatabase();
            return;
        }

        // 表示対象を作成してから、上から順に描画する
        BuildFilteredList(database);

        DrawFilterBar();
        DrawSelectionBar();
        DrawVolumeToolBar(database);
        DrawChannelList(database);
        DrawDetailPanel(database);

        // 試聴中の音量を最新の設定に追従させる
        SyncPreviewVolume(database);
    }

    /// <summary>
    /// 絞り込み条件に合うサウンドの一覧を作成します。
    /// </summary>
    /// <param name="database">対象の SoundDatabase。</param>
    private void BuildFilteredList(SoundDatabase database)
    {
        m_filteredList.Clear();
        Array.Clear(m_categoryCounts, 0, m_categoryCounts.Length);

        string[] keywords = SplitKeywords(m_searchText);

        foreach (SoundDatabase.SoundData data in database.m_soundList)
        {
            if (data == null || data.m_id == AudioID.None)
            {
                continue;
            }

            // カテゴリータブの件数を数える（絞り込み前）
            m_categoryCounts[CATEGORY_TAB_ALL]++;
            m_categoryCounts[Array.IndexOf(CATEGORY_VALUES, data.m_category) + 1]++;

            if (IsMatchFilter(data, keywords))
            {
                m_filteredList.Add(data);
            }
        }
    }

    /// <summary>
    /// 検索文字列をキーワードに分割します。
    /// </summary>
    /// <param name="searchText">検索文字列。</param>
    /// <returns>小文字化したキーワード配列。</returns>
    private static string[] SplitKeywords(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return Array.Empty<string>();
        }

        return searchText.ToLowerInvariant().Split(
            new[] { ' ', '　' },
            StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// サウンドが絞り込み条件に合うかを確認します。
    /// </summary>
    /// <param name="data">確認するサウンド。</param>
    /// <param name="keywords">検索キーワード。</param>
    /// <returns>
    /// true：表示対象です。
    /// false：表示対象外です。
    /// </returns>
    private bool IsMatchFilter(SoundDatabase.SoundData data, string[] keywords)
    {
        // カテゴリー
        if (m_categoryTabIndex != CATEGORY_TAB_ALL
            && data.m_category != CATEGORY_VALUES[m_categoryTabIndex - 1])
        {
            return false;
        }

        // 選択中のみ
        if (m_showSelectedOnly && !m_selectedIds.Contains(data.m_id))
        {
            return false;
        }

        // AudioClip 未設定のみ
        if (m_showMissingClipOnly && data.m_clip != null)
        {
            return false;
        }

        if (keywords.Length == 0)
        {
            return true;
        }

        // すべてのキーワードが ID 名または Clip 名に含まれること（AND 検索）
        string clipName = data.m_clip != null ? data.m_clip.name : "";
        string target = (data.m_id.ToString() + " " + clipName).ToLowerInvariant();

        foreach (string keyword in keywords)
        {
            if (!target.Contains(keyword))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 検索・カテゴリー・表示条件の操作欄を描画します。
    /// </summary>
    private void DrawFilterBar()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // 検索欄
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("検索", GUILayout.Width(32.0f));
        m_searchText = EditorGUILayout.TextField(m_searchText, EditorStyles.toolbarSearchField);

        if (GUILayout.Button("クリア", EditorStyles.miniButton, GUILayout.Width(50.0f)))
        {
            m_searchText = "";
            GUI.FocusControl(null);
        }

        EditorGUILayout.EndHorizontal();

        // カテゴリータブ（件数付き）
        m_categoryTabIndex = Mathf.Clamp(m_categoryTabIndex, 0, m_categoryTabLabels.Length - 1);
        m_categoryTabLabels[CATEGORY_TAB_ALL] = $"すべて ({m_categoryCounts[CATEGORY_TAB_ALL]})";

        for (int i = 0; i < CATEGORY_VALUES.Length; i++)
        {
            m_categoryTabLabels[i + 1] = $"{CATEGORY_VALUES[i]} ({m_categoryCounts[i + 1]})";
        }

        m_categoryTabIndex = GUILayout.Toolbar(m_categoryTabIndex, m_categoryTabLabels, EditorStyles.miniButton);

        // 表示条件と試聴設定
        EditorGUILayout.BeginHorizontal();

        m_showSelectedOnly = GUILayout.Toggle(m_showSelectedOnly, "選択中のみ", EditorStyles.miniButton, GUILayout.Width(80.0f));
        m_showMissingClipOnly = GUILayout.Toggle(m_showMissingClipOnly, "Clip 未設定のみ", EditorStyles.miniButton, GUILayout.Width(110.0f));

        GUILayout.FlexibleSpace();

        SoundToolPreviewPlayer.UseVolume = GUILayout.Toggle(
            SoundToolPreviewPlayer.UseVolume,
            "試聴に音量を反映",
            EditorStyles.miniButton,
            GUILayout.Width(120.0f));

        if (GUILayout.Button("■ 試聴停止", EditorStyles.miniButton, GUILayout.Width(80.0f)))
        {
            SoundToolPreviewPlayer.Stop();
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 選択状況と選択操作のボタンを描画します。
    /// </summary>
    private void DrawSelectionBar()
    {
        // 絞り込みで隠れている選択件数を数える
        int visibleSelectedCount = 0;

        foreach (SoundDatabase.SoundData data in m_filteredList)
        {
            if (m_selectedIds.Contains(data.m_id))
            {
                visibleSelectedCount++;
            }
        }

        int hiddenSelectedCount = m_selectedIds.Count - visibleSelectedCount;

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField(
            $"表示 {m_filteredList.Count} 件 / 選択 {m_selectedIds.Count} 件",
            EditorStyles.miniBoldLabel,
            GUILayout.Width(170.0f));

        // 隠れた選択も一括調整の対象になるため警告する
        if (hiddenSelectedCount > 0)
        {
            SoundToolGui.DrawColoredMiniLabel($"（非表示の選択が {hiddenSelectedCount} 件あります）", WARNING_COLOR);
        }

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("表示中を全選択", EditorStyles.miniButton, GUILayout.Width(100.0f)))
        {
            foreach (SoundDatabase.SoundData data in m_filteredList)
            {
                m_selectedIds.Add(data.m_id);
            }
        }

        if (GUILayout.Button("選択解除", EditorStyles.miniButton, GUILayout.Width(70.0f)))
        {
            m_selectedIds.Clear();
        }

        if (GUILayout.Button("表示中を反転", EditorStyles.miniButton, GUILayout.Width(90.0f)))
        {
            foreach (SoundDatabase.SoundData data in m_filteredList)
            {
                if (!m_selectedIds.Remove(data.m_id))
                {
                    m_selectedIds.Add(data.m_id);
                }
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 選択したサウンドの音量を一括調整する操作欄を描画します。
    /// </summary>
    /// <param name="database">対象の SoundDatabase。</param>
    private void DrawVolumeToolBar(SoundDatabase database)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField($"音量の一括調整（選択 {m_selectedIds.Count} 件）", EditorStyles.boldLabel);

        m_isLinkedEdit = EditorGUILayout.ToggleLeft(
            "連動：選択中の行のフェーダーを1本動かすと、選択中すべてが同じ量だけ動きます",
            m_isLinkedEdit);

        using (new EditorGUI.DisabledScope(m_selectedIds.Count == 0))
        {
            DrawTrimSlider(database);
            DrawVolumeStepButtons(database);
        }

        // 詳細項目の一括変更
        m_isBulkDetailFoldout = EditorGUILayout.Foldout(
            m_isBulkDetailFoldout,
            "詳細項目の一括変更（カテゴリー / ピッチ / ループ / 3D）",
            true);

        if (m_isBulkDetailFoldout)
        {
            DrawBulkDetailPanel(database);
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 選択中の音量をまとめて上下させる相対調整スライダーを描画します。
    /// </summary>
    /// <param name="database">対象の SoundDatabase。</param>
    private void DrawTrimSlider(SoundDatabase database)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("相対調整", GUILayout.Width(60.0f));

        float newTrimValue = EditorGUILayout.Slider(m_trimValue, -TRIM_RANGE, TRIM_RANGE);

        // 動かした分だけ選択中の音量へ加算する
        if (!Mathf.Approximately(newTrimValue, m_trimValue))
        {
            ApplyVolumeDeltaToSelected(database, newTrimValue - m_trimValue);
            m_trimValue = newTrimValue;
        }

        // マウスを離したらスライダーを中央へ戻す
        if (Event.current.rawType == EventType.MouseUp)
        {
            m_trimValue = 0.0f;
        }

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 音量の増減ボタンと「指定値に揃える」操作を描画します。
    /// </summary>
    /// <param name="database">対象の SoundDatabase。</param>
    private void DrawVolumeStepButtons(SoundDatabase database)
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("-10%"))
        {
            ApplyVolumeDeltaToSelected(database, -VOLUME_STEP_LARGE);
        }

        if (GUILayout.Button("-5%"))
        {
            ApplyVolumeDeltaToSelected(database, -VOLUME_STEP_SMALL);
        }

        if (GUILayout.Button("+5%"))
        {
            ApplyVolumeDeltaToSelected(database, VOLUME_STEP_SMALL);
        }

        if (GUILayout.Button("+10%"))
        {
            ApplyVolumeDeltaToSelected(database, VOLUME_STEP_LARGE);
        }

        GUILayout.Space(12.0f);

        GUILayout.Label("指定値に揃える", GUILayout.Width(90.0f));
        m_unifyVolume = EditorGUILayout.Slider(m_unifyVolume, MIN_VOLUME, MAX_VOLUME);

        if (GUILayout.Button("揃える", GUILayout.Width(60.0f)))
        {
            SetVolumeToSelected(database, m_unifyVolume);
        }

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 詳細項目の一括変更パネルを描画します。
    /// </summary>
    /// <param name="database">対象の SoundDatabase。</param>
    private void DrawBulkDetailPanel(SoundDatabase database)
    {
        EditorGUILayout.HelpBox(
            "チェックを入れた項目だけが、選択中のサウンドへ適用されます。",
            MessageType.None);

        // カテゴリー
        EditorGUILayout.BeginHorizontal();
        m_bulkSettings.ApplyCategory = EditorGUILayout.ToggleLeft("カテゴリー", m_bulkSettings.ApplyCategory, GUILayout.Width(110.0f));

        using (new EditorGUI.DisabledScope(!m_bulkSettings.ApplyCategory))
        {
            m_bulkSettings.Values.m_category = (SoundCategory)EditorGUILayout.EnumPopup(m_bulkSettings.Values.m_category);
        }

        EditorGUILayout.EndHorizontal();

        // ピッチ
        EditorGUILayout.BeginHorizontal();
        m_bulkSettings.ApplyPitch = EditorGUILayout.ToggleLeft("ピッチ", m_bulkSettings.ApplyPitch, GUILayout.Width(110.0f));

        using (new EditorGUI.DisabledScope(!m_bulkSettings.ApplyPitch))
        {
            m_bulkSettings.Values.m_pitch = EditorGUILayout.Slider(m_bulkSettings.Values.m_pitch, MIN_PITCH, MAX_PITCH);
        }

        EditorGUILayout.EndHorizontal();

        // ループ
        EditorGUILayout.BeginHorizontal();
        m_bulkSettings.ApplyLoop = EditorGUILayout.ToggleLeft("ループ", m_bulkSettings.ApplyLoop, GUILayout.Width(110.0f));

        using (new EditorGUI.DisabledScope(!m_bulkSettings.ApplyLoop))
        {
            m_bulkSettings.Values.m_loop = EditorGUILayout.Toggle(m_bulkSettings.Values.m_loop);
        }

        EditorGUILayout.EndHorizontal();

        // 3D 音源
        EditorGUILayout.BeginHorizontal();
        m_bulkSettings.ApplyIs3D = EditorGUILayout.ToggleLeft("3D 音源", m_bulkSettings.ApplyIs3D, GUILayout.Width(110.0f));

        using (new EditorGUI.DisabledScope(!m_bulkSettings.ApplyIs3D))
        {
            m_bulkSettings.Values.m_is3D = EditorGUILayout.Toggle(m_bulkSettings.Values.m_is3D);
        }

        EditorGUILayout.EndHorizontal();

        // 距離減衰
        m_bulkSettings.ApplyRolloff = EditorGUILayout.ToggleLeft("距離減衰（モード・距離・カーブ）", m_bulkSettings.ApplyRolloff);

        using (new EditorGUI.DisabledScope(!m_bulkSettings.ApplyRolloff))
        {
            SoundToolGui.DrawRolloffFields(m_bulkSettings.Values);
        }

        // 適用ボタン
        using (new EditorGUI.DisabledScope(m_selectedIds.Count == 0))
        {
            if (GUILayout.Button($"選択中の {m_selectedIds.Count} 件へ適用", GUILayout.Height(26.0f)))
            {
                ApplyBulkDetailToSelected(database);
            }
        }
    }

    /// <summary>
    /// サウンド一覧（チャンネルストリップ）を描画します。
    /// </summary>
    /// <param name="database">対象の SoundDatabase。</param>
    private void DrawChannelList(SoundDatabase database)
    {
        m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition, GUILayout.ExpandHeight(true));

        if (m_filteredList.Count == 0)
        {
            EditorGUILayout.HelpBox("条件に一致するサウンドがありません。", MessageType.Info);
        }

        for (int i = 0; i < m_filteredList.Count; i++)
        {
            DrawChannelRow(database, m_filteredList[i], i);
        }

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 1つのサウンドの行を描画します。
    /// </summary>
    /// <param name="database">対象の SoundDatabase。</param>
    /// <param name="data">描画するサウンド。</param>
    /// <param name="rowIndex">一覧内の行番号。</param>
    private void DrawChannelRow(SoundDatabase database, SoundDatabase.SoundData data, int rowIndex)
    {
        bool isSelected = m_selectedIds.Contains(data.m_id);
        bool isFocused = data.m_id == m_focusedId;
        bool isPlaying = SoundToolPreviewPlayer.IsPlaying(data.m_id);

        // 行の背景（選択・詳細編集・試聴中で色分け）
        Rect rowRect = EditorGUILayout.BeginHorizontal(EditorStyles.helpBox, GUILayout.Height(ROW_HEIGHT));

        if (Event.current.type == EventType.Repaint)
        {
            EditorGUI.DrawRect(rowRect, GetRowBackgroundColor(isSelected, isFocused, isPlaying));
        }

        // 選択チェック
        BeginCenteredColumn(SELECT_TOGGLE_WIDTH);
        bool newSelected = EditorGUILayout.Toggle(isSelected, GUILayout.Width(SELECT_TOGGLE_WIDTH));
        EndCenteredColumn();

        if (newSelected != isSelected)
        {
            ChangeSelection(rowIndex, data.m_id, newSelected);
        }

        // 試聴ボタン
        BeginCenteredColumn(PREVIEW_BUTTON_WIDTH);

        if (GUILayout.Button(isPlaying ? "■" : "▶", GUILayout.Width(PREVIEW_BUTTON_WIDTH), GUILayout.Height(22.0f)))
        {
            TogglePreview(data);
        }

        EndCenteredColumn();

        // カテゴリータグ
        BeginCenteredColumn(CATEGORY_TAG_WIDTH);
        SoundToolGui.DrawCategoryTag(data.m_category, CATEGORY_TAG_WIDTH);
        EndCenteredColumn();

        // ID 名（クリックで詳細編集）と Clip 名
        BeginCenteredColumn(NAME_COLUMN_WIDTH);

        if (GUILayout.Button(data.m_id.ToString(), EditorStyles.miniBoldLabel))
        {
            m_focusedId = data.m_id;
        }

        DrawClipSummaryLabel(data);
        EndCenteredColumn();

        // 音量フェーダー
        BeginCenteredColumn();
        float newVolume = EditorGUILayout.Slider(data.m_volume, MIN_VOLUME, MAX_VOLUME, GUILayout.MinWidth(SLIDER_MIN_WIDTH));
        EndCenteredColumn();

        if (!Mathf.Approximately(newVolume, data.m_volume))
        {
            ChangeVolume(database, data, newVolume);
        }

        // dB 表示
        BeginCenteredColumn(DECIBEL_LABEL_WIDTH);
        GUILayout.Label(SoundToolGui.GetDecibelText(data.m_volume), EditorStyles.miniLabel, GUILayout.Width(DECIBEL_LABEL_WIDTH));
        EndCenteredColumn();

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// Clip 名とループ・3D の状態を1行で描画します。
    /// </summary>
    /// <param name="data">描画するサウンド。</param>
    private void DrawClipSummaryLabel(SoundDatabase.SoundData data)
    {
        string clipText = data.m_clip != null ? data.m_clip.name : "Clip 未設定";
        string flagText = (data.m_loop ? "  [LOOP]" : "") + (data.m_is3D ? "  [3D]" : "");

        // Clip 未設定は赤字で目立たせる
        Color previousColor = GUI.contentColor;

        if (data.m_clip == null)
        {
            GUI.contentColor = ERROR_COLOR;
        }

        GUILayout.Label(clipText + flagText, EditorStyles.miniLabel);

        GUI.contentColor = previousColor;
    }

    /// <summary>
    /// 行の背景色を取得します。
    /// </summary>
    /// <param name="isSelected">選択中か。</param>
    /// <param name="isFocused">詳細編集中か。</param>
    /// <param name="isPlaying">試聴中か。</param>
    /// <returns>背景色。</returns>
    private static Color GetRowBackgroundColor(bool isSelected, bool isFocused, bool isPlaying)
    {
        if (isFocused)
        {
            return FOCUSED_ROW_COLOR;
        }

        if (isPlaying)
        {
            return PLAYING_ROW_COLOR;
        }

        return isSelected ? SELECTED_ROW_COLOR : Color.clear;
    }

    /// <summary>
    /// 縦方向に中央揃えする列の開始位置を描画します（幅指定あり）。
    /// </summary>
    /// <param name="width">列の幅。</param>
    private static void BeginCenteredColumn(float width)
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(width));
        GUILayout.FlexibleSpace();
    }

    /// <summary>
    /// 縦方向に中央揃えする列の開始位置を描画します（幅は可変）。
    /// </summary>
    private static void BeginCenteredColumn()
    {
        EditorGUILayout.BeginVertical();
        GUILayout.FlexibleSpace();
    }

    /// <summary>
    /// 縦方向に中央揃えする列の終了位置を描画します。
    /// </summary>
    private static void EndCenteredColumn()
    {
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 選択状態を変更します。Shift を押しながらの操作では範囲選択します。
    /// </summary>
    /// <param name="rowIndex">操作した行番号。</param>
    /// <param name="id">操作したサウンド ID。</param>
    /// <param name="isSelected">変更後の選択状態。</param>
    private void ChangeSelection(int rowIndex, AudioID id, bool isSelected)
    {
        bool isRangeSelect = Event.current.shift
            && m_lastToggledIndex >= 0
            && m_lastToggledIndex < m_filteredList.Count;

        // 範囲選択か単独選択かで対象行を決める
        int startIndex = isRangeSelect ? Mathf.Min(m_lastToggledIndex, rowIndex) : rowIndex;
        int endIndex = isRangeSelect ? Mathf.Max(m_lastToggledIndex, rowIndex) : rowIndex;

        for (int i = startIndex; i <= endIndex; i++)
        {
            AudioID targetId = m_filteredList[i].m_id;

            if (isSelected)
            {
                m_selectedIds.Add(targetId);
            }
            else
            {
                m_selectedIds.Remove(targetId);
            }
        }

        m_lastToggledIndex = rowIndex;
    }

    /// <summary>
    /// 試聴の再生・停止を切り替えます。
    /// </summary>
    /// <param name="data">試聴するサウンド。</param>
    private void TogglePreview(SoundDatabase.SoundData data)
    {
        if (SoundToolPreviewPlayer.IsPlaying(data.m_id))
        {
            SoundToolPreviewPlayer.Stop();
            return;
        }

        SoundToolPreviewPlayer.Play(data.m_id, data.m_clip, data.m_volume, data.m_pitch, data.m_loop);
    }

    /// <summary>
    /// 1行のフェーダー操作を反映します。連動ONなら選択中すべてへ同じ変化量を加えます。
    /// </summary>
    /// <param name="database">対象の SoundDatabase。</param>
    /// <param name="data">操作したサウンド。</param>
    /// <param name="newVolume">変更後の音量。</param>
    private void ChangeVolume(SoundDatabase database, SoundDatabase.SoundData data, float newVolume)
    {
        bool isLinked = m_isLinkedEdit && m_selectedIds.Contains(data.m_id) && m_selectedIds.Count > 1;

        if (isLinked)
        {
            ApplyVolumeDeltaToSelected(database, newVolume - data.m_volume);
            return;
        }

        Undo.RecordObject(database, "Change Sound Volume");
        data.m_volume = Mathf.Clamp(newVolume, MIN_VOLUME, MAX_VOLUME);
        SoundToolDatabaseProvider.MarkDirty(database);
    }

    /// <summary>
    /// 選択中のサウンドの音量へ同じ変化量を加えます。
    /// </summary>
    /// <param name="database">対象の SoundDatabase。</param>
    /// <param name="delta">加える変化量。</param>
    private void ApplyVolumeDeltaToSelected(SoundDatabase database, float delta)
    {
        Undo.RecordObject(database, "Adjust Sound Volume");

        foreach (SoundDatabase.SoundData data in database.m_soundList)
        {
            if (data != null && m_selectedIds.Contains(data.m_id))
            {
                data.m_volume = Mathf.Clamp(data.m_volume + delta, MIN_VOLUME, MAX_VOLUME);
            }
        }

        SoundToolDatabaseProvider.MarkDirty(database);
    }

    /// <summary>
    /// 選択中のサウンドの音量を同じ値に揃えます。
    /// </summary>
    /// <param name="database">対象の SoundDatabase。</param>
    /// <param name="volume">揃える音量。</param>
    private void SetVolumeToSelected(SoundDatabase database, float volume)
    {
        Undo.RecordObject(database, "Unify Sound Volume");

        foreach (SoundDatabase.SoundData data in database.m_soundList)
        {
            if (data != null && m_selectedIds.Contains(data.m_id))
            {
                data.m_volume = Mathf.Clamp(volume, MIN_VOLUME, MAX_VOLUME);
            }
        }

        SoundToolDatabaseProvider.MarkDirty(database);
    }

    /// <summary>
    /// 詳細項目の一括設定を選択中のサウンドへ適用します。
    /// </summary>
    /// <param name="database">対象の SoundDatabase。</param>
    private void ApplyBulkDetailToSelected(SoundDatabase database)
    {
        Undo.RecordObject(database, "Apply Bulk Sound Settings");

        foreach (SoundDatabase.SoundData data in database.m_soundList)
        {
            if (data != null && m_selectedIds.Contains(data.m_id))
            {
                m_bulkSettings.Apply(data);
            }
        }

        SoundToolDatabaseProvider.MarkDirty(database);
    }

    /// <summary>
    /// 試聴中のサウンドの音量を最新の設定に合わせます。
    /// </summary>
    /// <param name="database">対象の SoundDatabase。</param>
    private void SyncPreviewVolume(SoundDatabase database)
    {
        AudioID playingId = SoundToolPreviewPlayer.PlayingId;

        if (playingId == AudioID.None)
        {
            return;
        }

        SoundDatabase.SoundData data = database.GetSoundData(playingId);

        if (data != null)
        {
            SoundToolPreviewPlayer.SetVolume(data.m_volume);
        }
    }

    /// <summary>
    /// 詳細編集パネルを描画します。
    /// </summary>
    /// <param name="database">対象の SoundDatabase。</param>
    private void DrawDetailPanel(SoundDatabase database)
    {
        int dataIndex = FindDataIndex(database, m_focusedId);

        // 対象が無い場合は操作の案内を表示する
        if (dataIndex < 0)
        {
            EditorGUILayout.HelpBox(
                "一覧の ID 名をクリックすると、ここで Clip・カテゴリー・ピッチ・ループ・3D を詳細編集できます。",
                MessageType.None);
            return;
        }

        // SerializedObject を最新の状態にする
        if (m_serializedDatabase == null || m_serializedDatabase.targetObject != database)
        {
            m_serializedDatabase = new SerializedObject(database);
        }

        m_serializedDatabase.Update();

        SerializedProperty listProperty = m_serializedDatabase.FindProperty(SOUND_LIST_PROPERTY_NAME);

        if (listProperty == null || dataIndex >= listProperty.arraySize)
        {
            return;
        }

        SerializedProperty element = listProperty.GetArrayElementAtIndex(dataIndex);
        SoundDatabase.SoundData data = database.m_soundList[dataIndex];

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // ヘッダー（試聴・閉じる）
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"詳細設定：{data.m_id}", EditorStyles.boldLabel);

        if (GUILayout.Button(SoundToolPreviewPlayer.IsPlaying(data.m_id) ? "■ 停止" : "▶ 試聴", GUILayout.Width(70.0f)))
        {
            TogglePreview(data);
        }

        if (GUILayout.Button("閉じる", GUILayout.Width(60.0f)))
        {
            m_focusedId = AudioID.None;
        }

        EditorGUILayout.EndHorizontal();

        // 各項目
        DrawPropertyField(element, "m_volume", "音量");
        DrawPropertyField(element, "m_clip", "AudioClip");
        DrawPropertyField(element, "m_category", "カテゴリー");
        DrawPropertyField(element, "m_pitch", "ピッチ");
        DrawPropertyField(element, "m_loop", "ループ");
        DrawPropertyField(element, "m_is3D", "3D 音源");

        // 3D の場合のみ距離減衰を表示する
        SerializedProperty is3DProperty = element.FindPropertyRelative("m_is3D");

        if (is3DProperty != null && is3DProperty.boolValue)
        {
            DrawPropertyField(element, "m_rolloffMode", "距離減衰");
            DrawPropertyField(element, "m_minDistance", "最小距離");
            DrawPropertyField(element, "m_maxDistance", "最大距離");

            SerializedProperty rolloffProperty = element.FindPropertyRelative("m_rolloffMode");

            if (rolloffProperty != null && (AudioRolloffMode)rolloffProperty.intValue == AudioRolloffMode.Custom)
            {
                DrawPropertyField(element, "m_customRolloffCurve", "カスタムカーブ");
            }
        }

        EditorGUILayout.EndVertical();

        // 変更があれば未保存として記録する（Undo は自動で記録される）
        if (m_serializedDatabase.ApplyModifiedProperties())
        {
            SoundToolDatabaseProvider.MarkDirty(database);
        }
    }

    /// <summary>
    /// 要素内のプロパティを日本語ラベルで描画します。
    /// </summary>
    /// <param name="element">サウンド 1 件分のプロパティ。</param>
    /// <param name="propertyName">プロパティ名。</param>
    /// <param name="label">表示ラベル。</param>
    private static void DrawPropertyField(SerializedProperty element, string propertyName, string label)
    {
        SerializedProperty property = element.FindPropertyRelative(propertyName);

        if (property != null)
        {
            EditorGUILayout.PropertyField(property, new GUIContent(label), true);
        }
    }

    /// <summary>
    /// 指定 ID のサウンドが音声リスト内の何番目かを検索します。
    /// </summary>
    /// <param name="database">対象の SoundDatabase。</param>
    /// <param name="id">検索するサウンド ID。</param>
    /// <returns>リスト内の位置。見つからない場合は -1。</returns>
    private static int FindDataIndex(SoundDatabase database, AudioID id)
    {
        if (id == AudioID.None)
        {
            return -1;
        }

        for (int i = 0; i < database.m_soundList.Count; i++)
        {
            SoundDatabase.SoundData data = database.m_soundList[i];

            if (data != null && data.m_id == id)
            {
                return i;
            }
        }

        return -1;
    }
}

#endif