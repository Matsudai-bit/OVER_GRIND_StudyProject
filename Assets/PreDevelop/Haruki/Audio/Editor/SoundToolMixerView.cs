#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// ミキサー一覧の並び替え順を表します。
/// </summary>
public enum SoundSortMode
{
    SORT_DEFAULT,
    SORT_NAME_ASC,
    SORT_VOLUME_DESC,
    SORT_VOLUME_ASC,
    SORT_CATEGORY
}

/// <summary>
/// 詳細パネルの表示タブを表します。
/// </summary>
public enum DetailTabMode
{
    DETAIL_GROUP,
    DETAIL_INTRO,
    DETAIL_LOOP,
    DETAIL_OUTRO
}

/// <summary>
/// 検索・試聴・音量調整・リアルタイム解析・I/L/O 一連編集を行うミキサー画面です。
/// </summary>
public sealed class SoundToolMixerView
{
    // 親行・単体行の高さ
    private const float ROW_HEIGHT = 34.0f;

    // グループ展開時の子行の高さ
    private const float CHILD_ROW_HEIGHT = 28.0f;

    // リアルタイムアナライザーパネルの高さ
    private const float ANALYZER_HEIGHT = 96.0f;

    // 選択チェックの幅
    private const float SELECT_TOGGLE_WIDTH = 18.0f;

    // 展開トグルボタンの幅
    private const float FOLD_BUTTON_WIDTH = 20.0f;

    // 子行のインデント幅
    private const float CHILD_INDENT_WIDTH = 22.0f;

    // 試聴ボタンの幅
    private const float PREVIEW_BUTTON_WIDTH = 28.0f;

    // パートバッジの幅
    private const float PART_BADGE_WIDTH = 18.0f;

    // カテゴリータグの幅
    private const float CATEGORY_TAG_WIDTH = 100.0f;

    // 名前欄の幅
    private const float NAME_COLUMN_WIDTH = 230.0f;

    // 音量ゲージの幅
    private const float METER_WIDTH = 36.0f;

    // 音量ゲージの高さ
    private const float METER_HEIGHT = 8.0f;

    // 音量スライダーの最小幅
    private const float SLIDER_MIN_WIDTH = 140.0f;

    // dB 表示欄の幅
    private const float DECIBEL_LABEL_WIDTH = 58.0f;

    // リファレンスボタンの幅
    private const float REF_BUTTON_WIDTH = 32.0f;

    // 相対調整スライダーの範囲（±）
    private const float TRIM_RANGE = 0.5f;

    // 音量ボタンの小さい刻み幅
    private const float VOLUME_STEP_SMALL = 0.05f;

    // 音量ボタンの大きい刻み幅
    private const float VOLUME_STEP_LARGE = 0.10f;

    // キーボード操作の通常刻み幅
    private const float KEY_VOLUME_STEP_NORMAL = 0.02f;

    // キーボード操作の Shift 時刻み幅
    private const float KEY_VOLUME_STEP_SHIFT = 0.10f;

    // dB 増減ボタンの小さい刻み
    private const float DECIBEL_STEP_SMALL = 1.0f;

    // dB 増減ボタンの大きい刻み
    private const float DECIBEL_STEP_LARGE = 3.0f;

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

    // 「すべて」タブのインデックス
    private const int CATEGORY_TAB_ALL = 0;

    // 末尾試聴ジャンプ時の残り秒数
    private const float NEAR_END_SECONDS = 3.0f;

    // 行の背景色（選択中）
    private static readonly Color SELECTED_ROW_COLOR = new Color(0.30f, 0.60f, 1.00f, 0.14f);

    // 行の背景色（詳細編集中・フォーカス中）
    private static readonly Color FOCUSED_ROW_COLOR = new Color(0.25f, 0.50f, 0.95f, 0.30f);

    // 行の背景色（試聴中）
    private static readonly Color PLAYING_ROW_COLOR = new Color(0.30f, 0.80f, 0.40f, 0.20f);

    // 行の背景色（子行のベース背景）
    private static readonly Color CHILD_ROW_BG_COLOR = new Color(0.0f, 0.0f, 0.0f, 0.12f);

    // 警告表示の色
    private static readonly Color WARNING_COLOR = new Color(1.0f, 0.65f, 0.2f);

    // エラー表示の色
    private static readonly Color ERROR_COLOR = new Color(1.0f, 0.4f, 0.4f);

    // 基準（Ref）ボタンのアクティブ色
    private static readonly Color REF_ACTIVE_COLOR = new Color(1.0f, 0.80f, 0.25f);

    // カテゴリー値の一覧
    private static readonly SoundCategory[] CATEGORY_VALUES = (SoundCategory[])Enum.GetValues(typeof(SoundCategory));

    // ソート表示名
    private static readonly string[] SORT_MODE_LABELS =
    {
        "登録順",
        "名前順 (A-Z)",
        "音量 高い順",
        "音量 低い順",
        "カテゴリー順"
    };

    /// <summary>
    /// 画面に表示する1行分の情報です（グループ親行・展開子行・単体行）。
    /// </summary>
    private sealed class VisibleRow
    {
        /// <summary>
        /// 矢印キーやフォーカス管理のための一意キーを取得または設定します。
        /// </summary>
        public string RowKey { get; set; } = "";

        /// <summary>
        /// グループ親行かを取得または設定します。
        /// </summary>
        public bool IsGroupHeader { get; set; }

        /// <summary>
        /// グループ内の展開子行かを取得または設定します。
        /// </summary>
        public bool IsChildRow { get; set; }

        /// <summary>
        /// 所属グループを取得または設定します。
        /// </summary>
        public SoundToolSequenceHelper.SoundGroup Group { get; set; }

        /// <summary>
        /// 対象のサウンドデータを取得または設定します（グループ親行の場合は null）。
        /// </summary>
        public SoundDatabase.SoundData Data { get; set; }

        /// <summary>
        /// パート種別を取得または設定します。
        /// </summary>
        public SoundPartType PartType { get; set; }

        /// <summary>
        /// スクロール追従用の上端 Y 座標を取得または設定します。
        /// </summary>
        public float RowTop { get; set; }

        /// <summary>
        /// スクロール追従用の下端 Y 座標を取得または設定します。
        /// </summary>
        public float RowBottom { get; set; }
    }

    // 再描画を要求する処理
    private readonly Action m_requestRepaint;

    // 選択中のサウンド ID
    private readonly HashSet<AudioID> m_selectedIds = new HashSet<AudioID>();

    // 展開中のグループキー
    private readonly HashSet<string> m_expandedGroupKeys = new HashSet<string>(StringComparer.Ordinal);

    // 全グループキャッシュ
    private readonly List<SoundToolSequenceHelper.SoundGroup> m_allGroups = new List<SoundToolSequenceHelper.SoundGroup>();

    // 絞り込み・並べ替え後のグループ一覧
    private readonly List<SoundToolSequenceHelper.SoundGroup> m_filteredGroups = new List<SoundToolSequenceHelper.SoundGroup>();

    // 実際に描画する行一覧（親・展開子・単体）
    private readonly List<VisibleRow> m_visibleRows = new List<VisibleRow>();

    // グループ内パーツ取得用の一時リスト
    private readonly List<SoundDatabase.SoundData> m_tempPartList = new List<SoundDatabase.SoundData>();

    // 詳細項目の一括設定
    private readonly SoundToolBulkSettings m_bulkSettings = new SoundToolBulkSettings();

    // カテゴリータブの表示文字列
    private readonly string[] m_categoryTabLabels = new string[CATEGORY_VALUES.Length + 1];

    // カテゴリー別の件数（0 は全件）
    private readonly int[] m_categoryCounts = new int[CATEGORY_VALUES.Length + 1];

    // 一覧のスクロール位置
    private Vector2 m_scrollPosition = Vector2.zero;

    // スクロールビューの表示高さ
    private float m_scrollViewHeight = 300.0f;

    // 検索文字列
    private string m_searchText = "";

    // カテゴリータブの選択位置
    private int m_categoryTabIndex = CATEGORY_TAB_ALL;

    // 並べ替えモード
    private SoundSortMode m_sortMode = SoundSortMode.SORT_DEFAULT;

    // I / L / O をまとめて表示するか
    private bool m_groupIloView = true;

    // 行クリック・矢印移動時に即再生するか（デフォルト ON）
    private bool m_autoPlayOnSelect = true;

    // リアルタイム・スペクトラム＆メーターを表示するか（デフォルト ON）
    private bool m_showAnalyzer = true;

    // 一連再生時にループ1周で自動 Outro 移行するか
    private bool m_autoOutroOnSequence = false;

    // 選択中のみ表示するか
    private bool m_showSelectedOnly = false;

    // AudioClip 未設定のみ表示するか
    private bool m_showMissingClipOnly = false;

    // 選択中のフェーダーを連動させるか
    private bool m_isLinkedEdit = true;

    // フォーカス中の行キー
    private string m_focusedRowKey = "";

    // 矢印キー移動後にスクロールを追従させるか
    private bool m_pendingScrollToFocus = false;

    // 詳細編集中の単体または個別サウンド
    private SoundDatabase.SoundData m_focusedData;

    // 詳細編集中のグループ
    private SoundToolSequenceHelper.SoundGroup m_focusedGroup;

    // 詳細パネルのアクティブタブ
    private DetailTabMode m_detailTabMode = DetailTabMode.DETAIL_GROUP;

    // A/B 比較用の基準（リファレンス）音源
    private SoundDatabase.SoundData m_referenceData;

    // 相対調整スライダーの現在値
    private float m_trimValue = 0.0f;

    // 「指定値に揃える」の値
    private float m_unifyVolume = 1.0f;

    // 詳細項目の一括変更パネルを開いているか
    private bool m_isBulkDetailFoldout = false;

    // 最後にチェックを操作した行（Shift 範囲選択用）
    private int m_lastToggledIndex = -1;

    /// <summary>
    /// ミキサー画面を生成します。
    /// </summary>
    /// <param name="requestRepaint">再描画を要求する処理。</param>
    public SoundToolMixerView(Action requestRepaint)
    {
        m_requestRepaint = requestRepaint;
    }

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

        // 表示行リストを構築する
        BuildVisibleRows(database);

        // キーボード操作を処理する
        HandleKeyboardNavigation(database);

        DrawFilterBar();
        DrawSelectionBar();
        DrawVolumeToolBar(database);
        DrawPreviewTransportBar();

        // リアルタイム・スペクトラム（EQ風）＆ステレオ L/R メーター
        if (m_showAnalyzer)
        {
            SoundToolAnalyzerGui.DrawAnalyzerPanel(ANALYZER_HEIGHT);
        }

        DrawChannelList(database);
        DrawDetailPanel(database);
    }

    /// <summary>
    /// 絞り込み・ソート条件に従って表示用の行一覧を構築します。
    /// </summary>
    private void BuildVisibleRows(SoundDatabase database)
    {
        m_filteredGroups.Clear();
        m_visibleRows.Clear();
        Array.Clear(m_categoryCounts, 0, m_categoryCounts.Length);

        string[] keywords = SplitKeywords(m_searchText);

        if (m_groupIloView)
        {
            SoundToolSequenceHelper.BuildGroups(database.m_soundList, m_allGroups);
        }
        else
        {
            BuildFlatGroups(database.m_soundList, m_allGroups);
        }

        for (int i = 0; i < m_allGroups.Count; i++)
        {
            SoundToolSequenceHelper.SoundGroup group = m_allGroups[i];
            group.CollectOrderedParts(m_tempPartList);

            for (int p = 0; p < m_tempPartList.Count; p++)
            {
                SoundDatabase.SoundData part = m_tempPartList[p];
                m_categoryCounts[CATEGORY_TAB_ALL]++;
                int catIndex = Array.IndexOf(CATEGORY_VALUES, part.m_category);

                if (catIndex >= 0)
                {
                    m_categoryCounts[catIndex + 1]++;
                }
            }

            if (IsMatchGroupFilter(group, keywords))
            {
                m_filteredGroups.Add(group);
            }
        }

        SortFilteredGroups();

        for (int i = 0; i < m_filteredGroups.Count; i++)
        {
            SoundToolSequenceHelper.SoundGroup group = m_filteredGroups[i];

            if (!group.IsSequenceGroup)
            {
                SoundDatabase.SoundData single = group.SingleData;

                if (single != null)
                {
                    m_visibleRows.Add(new VisibleRow
                    {
                        RowKey = $"DATA:{(int)single.m_id}",
                        IsGroupHeader = false,
                        IsChildRow = false,
                        Group = group,
                        Data = single,
                        PartType = SoundPartType.PART_SINGLE
                    });
                }

                continue;
            }

            m_visibleRows.Add(new VisibleRow
            {
                RowKey = group.GroupKey,
                IsGroupHeader = true,
                IsChildRow = false,
                Group = group,
                Data = null,
                PartType = SoundPartType.PART_SINGLE
            });

            if (m_expandedGroupKeys.Contains(group.GroupKey))
            {
                AddChildRowIfExists(group, group.IntroData, SoundPartType.PART_INTRO);
                AddChildRowIfExists(group, group.LoopData, SoundPartType.PART_LOOP);
                AddChildRowIfExists(group, group.OutroData, SoundPartType.PART_OUTRO);
            }
        }

        RefreshFocusedReferences();
    }

    /// <summary>
    /// グループ化 OFF 時に全サウンドを単体行として構築します。
    /// </summary>
    private static void BuildFlatGroups(IReadOnlyList<SoundDatabase.SoundData> soundList, List<SoundToolSequenceHelper.SoundGroup> destination)
    {
        destination.Clear();

        if (soundList == null)
        {
            return;
        }

        for (int i = 0; i < soundList.Count; i++)
        {
            SoundDatabase.SoundData data = soundList[i];

            if (data == null || data.m_id == AudioID.None)
            {
                continue;
            }

            string idName = data.m_id.ToString();
            SoundToolSequenceHelper.SoundGroup group = new SoundToolSequenceHelper.SoundGroup($"FLAT:{(int)data.m_id}", idName, false);
            group.SingleData = data;
            destination.Add(group);
        }
    }

    /// <summary>
    /// 展開時の子行を追加します。
    /// </summary>
    private void AddChildRowIfExists(SoundToolSequenceHelper.SoundGroup group, SoundDatabase.SoundData data, SoundPartType partType)
    {
        if (data == null)
        {
            return;
        }

        m_visibleRows.Add(new VisibleRow
        {
            RowKey = $"DATA:{(int)data.m_id}",
            IsGroupHeader = false,
            IsChildRow = true,
            Group = group,
            Data = data,
            PartType = partType
        });
    }

    /// <summary>
    /// フォーカス中の参照を最新リストへ同期します。
    /// </summary>
    private void RefreshFocusedReferences()
    {
        if (string.IsNullOrEmpty(m_focusedRowKey))
        {
            return;
        }

        for (int i = 0; i < m_allGroups.Count; i++)
        {
            SoundToolSequenceHelper.SoundGroup group = m_allGroups[i];

            if (group.GroupKey == m_focusedRowKey)
            {
                m_focusedGroup = group;
                m_focusedData = null;
                return;
            }

            group.CollectOrderedParts(m_tempPartList);

            for (int p = 0; p < m_tempPartList.Count; p++)
            {
                SoundDatabase.SoundData data = m_tempPartList[p];

                if ($"DATA:{(int)data.m_id}" == m_focusedRowKey)
                {
                    m_focusedGroup = group.IsSequenceGroup ? group : null;
                    m_focusedData = data;
                    return;
                }
            }
        }
    }

    /// <summary>
    /// グループを現在のソートモードで並べ替えます。
    /// </summary>
    private void SortFilteredGroups()
    {
        switch (m_sortMode)
        {
            case SoundSortMode.SORT_NAME_ASC:
                m_filteredGroups.Sort((a, b) => string.Compare(a.BaseName, b.BaseName, StringComparison.OrdinalIgnoreCase));
                break;

            case SoundSortMode.SORT_VOLUME_DESC:
                m_filteredGroups.Sort((a, b) => b.GetAverageVolume().CompareTo(a.GetAverageVolume()));
                break;

            case SoundSortMode.SORT_VOLUME_ASC:
                m_filteredGroups.Sort((a, b) => a.GetAverageVolume().CompareTo(b.GetAverageVolume()));
                break;

            case SoundSortMode.SORT_CATEGORY:
                m_filteredGroups.Sort((a, b) =>
                {
                    SoundDatabase.SoundData primaryA = a.GetPrimaryData();
                    SoundDatabase.SoundData primaryB = b.GetPrimaryData();
                    int catA = primaryA != null ? (int)primaryA.m_category : int.MaxValue;
                    int catB = primaryB != null ? (int)primaryB.m_category : int.MaxValue;
                    int cmp = catA.CompareTo(catB);
                    return cmp != 0 ? cmp : string.Compare(a.BaseName, b.BaseName, StringComparison.OrdinalIgnoreCase);
                });
                break;
        }
    }

    /// <summary>
    /// 検索文字列をキーワードに分割します。
    /// </summary>
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
    /// グループ内のいずれかのパートが絞り込み条件に一致するかを確認します。
    /// </summary>
    private bool IsMatchGroupFilter(SoundToolSequenceHelper.SoundGroup group, string[] keywords)
    {
        group.CollectOrderedParts(m_tempPartList);

        for (int i = 0; i < m_tempPartList.Count; i++)
        {
            if (IsMatchPartFilter(m_tempPartList[i], group.BaseName, keywords))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 個別パートが絞り込み条件に一致するかを確認します。
    /// </summary>
    private bool IsMatchPartFilter(SoundDatabase.SoundData data, string baseName, string[] keywords)
    {
        if (data == null)
        {
            return false;
        }

        if (m_categoryTabIndex != CATEGORY_TAB_ALL
            && data.m_category != CATEGORY_VALUES[m_categoryTabIndex - 1])
        {
            return false;
        }

        if (m_showSelectedOnly && !m_selectedIds.Contains(data.m_id))
        {
            return false;
        }

        if (m_showMissingClipOnly && data.m_clip != null)
        {
            return false;
        }

        if (keywords.Length == 0)
        {
            return true;
        }

        string clipName = data.m_clip != null ? data.m_clip.name : "";
        string target = $"{baseName} {data.m_id} {clipName}".ToLowerInvariant();

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
    /// 矢印キーでの上下移動や試聴・音量調整のキーボードショートカットを処理します。
    /// </summary>
    private void HandleKeyboardNavigation(SoundDatabase database)
    {
        Event currentEvent = Event.current;

        if (currentEvent.type != EventType.KeyDown || EditorGUIUtility.editingTextField)
        {
            return;
        }

        if (m_visibleRows.Count == 0)
        {
            return;
        }

        switch (currentEvent.keyCode)
        {
            case KeyCode.UpArrow:
                MoveFocusRow(-1);
                currentEvent.Use();
                break;

            case KeyCode.DownArrow:
                MoveFocusRow(1);
                currentEvent.Use();
                break;

            case KeyCode.Space:
                ToggleFocusedRowPreview();
                currentEvent.Use();
                break;

            case KeyCode.LeftArrow:
                AdjustFocusedVolume(database, -(currentEvent.shift ? KEY_VOLUME_STEP_SHIFT : KEY_VOLUME_STEP_NORMAL));
                currentEvent.Use();
                break;

            case KeyCode.RightArrow:
                AdjustFocusedVolume(database, currentEvent.shift ? KEY_VOLUME_STEP_SHIFT : KEY_VOLUME_STEP_NORMAL);
                currentEvent.Use();
                break;

            case KeyCode.Return:
            case KeyCode.KeypadEnter:
                ToggleFocusedGroupExpand();
                currentEvent.Use();
                break;

            case KeyCode.R:
                ToggleReferenceComparison();
                currentEvent.Use();
                break;
        }
    }

    /// <summary>
    /// フォーカス中の行を上下に移動します。
    /// </summary>
    private void MoveFocusRow(int offset)
    {
        if (m_visibleRows.Count == 0)
        {
            return;
        }

        int currentIndex = FindFocusedRowIndex();
        int nextIndex;

        if (currentIndex < 0)
        {
            nextIndex = offset > 0 ? 0 : m_visibleRows.Count - 1;
        }
        else
        {
            nextIndex = Mathf.Clamp(currentIndex + offset, 0, m_visibleRows.Count - 1);
        }

        VisibleRow nextRow = m_visibleRows[nextIndex];
        ActivateRow(nextRow, m_autoPlayOnSelect);
        m_pendingScrollToFocus = true;
        m_requestRepaint?.Invoke();
    }

    /// <summary>
    /// 現在フォーカス中の行のインデックスを取得します。
    /// </summary>
    private int FindFocusedRowIndex()
    {
        if (string.IsNullOrEmpty(m_focusedRowKey))
        {
            return -1;
        }

        for (int i = 0; i < m_visibleRows.Count; i++)
        {
            if (m_visibleRows[i].RowKey == m_focusedRowKey)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// 指定された行をフォーカス対象にし、必要に応じて即再生します。
    /// </summary>
    private void ActivateRow(VisibleRow row, bool shouldPlay)
    {
        if (row == null)
        {
            return;
        }

        GUI.FocusControl(null);
        m_focusedRowKey = row.RowKey;

        if (row.IsGroupHeader)
        {
            m_focusedGroup = row.Group;
            m_focusedData = null;
            m_detailTabMode = DetailTabMode.DETAIL_GROUP;

            if (shouldPlay)
            {
                PlayGroupSequence(row.Group);
            }
        }
        else
        {
            m_focusedData = row.Data;
            m_focusedGroup = (row.Group != null && row.Group.IsSequenceGroup) ? row.Group : null;
            m_detailTabMode = GetTabModeFromPart(row.PartType);

            if (shouldPlay)
            {
                SoundToolPreviewPlayer.PlaySingle(row.Data, GetReferenceAudioId());
            }
        }

        m_requestRepaint?.Invoke();
    }

    /// <summary>
    /// 現在のリファレンス AudioID を取得します。
    /// </summary>
    private AudioID GetReferenceAudioId()
    {
        return m_referenceData != null ? m_referenceData.m_id : AudioID.None;
    }

    /// <summary>
    /// リファレンス音源を設定または解除し、アナライザー側へ通知します。
    /// </summary>
    private void SetReferenceData(SoundDatabase.SoundData data)
    {
        m_referenceData = data;
        AudioID refId = GetReferenceAudioId();
        SoundToolPreviewPlayer.SetReferenceId(refId);
        SoundToolAudioAnalyzer.ClearReferenceLevel();
    }

    /// <summary>
    /// パート種別から詳細タブモードを取得します。
    /// </summary>
    private static DetailTabMode GetTabModeFromPart(SoundPartType partType)
    {
        switch (partType)
        {
            case SoundPartType.PART_INTRO:
                return DetailTabMode.DETAIL_INTRO;
            case SoundPartType.PART_LOOP:
                return DetailTabMode.DETAIL_LOOP;
            case SoundPartType.PART_OUTRO:
                return DetailTabMode.DETAIL_OUTRO;
            default:
                return DetailTabMode.DETAIL_GROUP;
        }
    }

    /// <summary>
    /// フォーカス中行の再生・停止を切り替えます。
    /// </summary>
    private void ToggleFocusedRowPreview()
    {
        int index = FindFocusedRowIndex();

        if (index < 0)
        {
            return;
        }

        VisibleRow row = m_visibleRows[index];

        if (row.IsGroupHeader)
        {
            ToggleGroupPreview(row.Group);
        }
        else if (row.Data != null)
        {
            ToggleSinglePreview(row.Data);
        }
    }

    /// <summary>
    /// フォーカス中行がグループ親行なら展開・折りたたみを切り替えます。
    /// </summary>
    private void ToggleFocusedGroupExpand()
    {
        int index = FindFocusedRowIndex();

        if (index < 0)
        {
            return;
        }

        VisibleRow row = m_visibleRows[index];

        if (!row.IsGroupHeader || row.Group == null)
        {
            return;
        }

        ToggleGroupExpanded(row.Group.GroupKey);
        m_requestRepaint?.Invoke();
    }

    /// <summary>
    /// フォーカス中の対象音量をキーボード操作で増減します。
    /// </summary>
    private void AdjustFocusedVolume(SoundDatabase database, float delta)
    {
        int index = FindFocusedRowIndex();

        if (index < 0)
        {
            return;
        }

        VisibleRow row = m_visibleRows[index];

        if (row.IsGroupHeader && row.Group != null)
        {
            ApplyVolumeDeltaToGroup(database, row.Group, delta);
        }
        else if (row.Data != null)
        {
            ChangeSingleVolume(database, row.Data, row.Data.m_volume + delta);
        }

        m_requestRepaint?.Invoke();
    }

    /// <summary>
    /// リファレンス音源と現在フォーカス中の音源を交互に聴き比べます。
    /// </summary>
    private void ToggleReferenceComparison()
    {
        if (m_referenceData == null)
        {
            return;
        }

        if (SoundToolPreviewPlayer.IsPlaying(m_referenceData.m_id))
        {
            int index = FindFocusedRowIndex();

            if (index >= 0)
            {
                VisibleRow row = m_visibleRows[index];

                if (row.IsGroupHeader)
                {
                    PlayGroupSequence(row.Group);
                    return;
                }

                if (row.Data != null)
                {
                    SoundToolPreviewPlayer.PlaySingle(row.Data, GetReferenceAudioId());
                    return;
                }
            }

            SoundToolPreviewPlayer.Stop();
            return;
        }

        SoundToolPreviewPlayer.PlaySingle(m_referenceData, GetReferenceAudioId());
    }

    /// <summary>
    /// 検索・カテゴリー・表示条件の操作欄を描画します。
    /// </summary>
    private void DrawFilterBar()
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

        GUILayout.Space(8.0f);
        GUILayout.Label("並び替え", GUILayout.Width(52.0f));
        m_sortMode = (SoundSortMode)EditorGUILayout.Popup((int)m_sortMode, SORT_MODE_LABELS, GUILayout.Width(120.0f));

        EditorGUILayout.EndHorizontal();

        m_categoryTabIndex = Mathf.Clamp(m_categoryTabIndex, 0, m_categoryTabLabels.Length - 1);
        m_categoryTabLabels[CATEGORY_TAB_ALL] = $"すべて ({m_categoryCounts[CATEGORY_TAB_ALL]})";

        for (int i = 0; i < CATEGORY_VALUES.Length; i++)
        {
            m_categoryTabLabels[i + 1] = $"{CATEGORY_VALUES[i]} ({m_categoryCounts[i + 1]})";
        }

        m_categoryTabIndex = GUILayout.Toolbar(m_categoryTabIndex, m_categoryTabLabels, EditorStyles.miniButton);

        EditorGUILayout.BeginHorizontal();

        m_showAnalyzer = GUILayout.Toggle(m_showAnalyzer, "アナライザー(EQ)", EditorStyles.miniButton, GUILayout.Width(104.0f));
        m_groupIloView = GUILayout.Toggle(m_groupIloView, "I/L/O まとめ", EditorStyles.miniButton, GUILayout.Width(86.0f));
        m_autoPlayOnSelect = GUILayout.Toggle(m_autoPlayOnSelect, "クリック即再生", EditorStyles.miniButton, GUILayout.Width(96.0f));
        m_showSelectedOnly = GUILayout.Toggle(m_showSelectedOnly, "選択中のみ", EditorStyles.miniButton, GUILayout.Width(72.0f));
        m_showMissingClipOnly = GUILayout.Toggle(m_showMissingClipOnly, "Clip 未設定", EditorStyles.miniButton, GUILayout.Width(76.0f));

        GUILayout.FlexibleSpace();

        using (new EditorGUI.DisabledScope(m_referenceData == null))
        {
            string refLabel = m_referenceData != null ? $"Ref比較(R): {m_referenceData.m_id}" : "Ref未設定(R)";
            Color prevBg = GUI.backgroundColor;

            if (m_referenceData != null && SoundToolPreviewPlayer.IsPlaying(m_referenceData.m_id))
            {
                GUI.backgroundColor = REF_ACTIVE_COLOR;
            }

            if (GUILayout.Button(refLabel, EditorStyles.miniButton, GUILayout.MaxWidth(185.0f)))
            {
                ToggleReferenceComparison();
            }

            GUI.backgroundColor = prevBg;
        }

        SoundToolPreviewPlayer.UseVolume = GUILayout.Toggle(
            SoundToolPreviewPlayer.UseVolume,
            "音量反映",
            EditorStyles.miniButton,
            GUILayout.Width(64.0f));

        if (GUILayout.Button("■ 停止", EditorStyles.miniButton, GUILayout.Width(54.0f)))
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
        int visibleIdCount = 0;
        int visibleSelectedCount = 0;

        for (int i = 0; i < m_filteredGroups.Count; i++)
        {
            m_filteredGroups[i].CollectOrderedParts(m_tempPartList);

            for (int p = 0; p < m_tempPartList.Count; p++)
            {
                visibleIdCount++;

                if (m_selectedIds.Contains(m_tempPartList[p].m_id))
                {
                    visibleSelectedCount++;
                }
            }
        }

        int hiddenSelectedCount = m_selectedIds.Count - visibleSelectedCount;

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField(
            $"表示 {m_filteredGroups.Count} 項目 ({visibleIdCount} ID) / 選択 {m_selectedIds.Count} 件",
            EditorStyles.miniBoldLabel,
            GUILayout.Width(240.0f));

        if (hiddenSelectedCount > 0)
        {
            SoundToolGui.DrawColoredMiniLabel($"（非表示の選択が {hiddenSelectedCount} 件あります）", WARNING_COLOR);
        }

        GUILayout.FlexibleSpace();

        GUILayout.Label("操作: [↑↓]移動 [Space]再生 [←→]音量 [R]Ref比較", EditorStyles.miniLabel);

        if (GUILayout.Button("表示中を全選択", EditorStyles.miniButton, GUILayout.Width(96.0f)))
        {
            for (int i = 0; i < m_filteredGroups.Count; i++)
            {
                m_filteredGroups[i].CollectOrderedParts(m_tempPartList);

                for (int p = 0; p < m_tempPartList.Count; p++)
                {
                    m_selectedIds.Add(m_tempPartList[p].m_id);
                }
            }
        }

        if (GUILayout.Button("選択解除", EditorStyles.miniButton, GUILayout.Width(64.0f)))
        {
            m_selectedIds.Clear();
        }

        if (GUILayout.Button("選択反転", EditorStyles.miniButton, GUILayout.Width(64.0f)))
        {
            for (int i = 0; i < m_filteredGroups.Count; i++)
            {
                m_filteredGroups[i].CollectOrderedParts(m_tempPartList);

                for (int p = 0; p < m_tempPartList.Count; p++)
                {
                    AudioID id = m_tempPartList[p].m_id;

                    if (!m_selectedIds.Remove(id))
                    {
                        m_selectedIds.Add(id);
                    }
                }
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 選択したサウンドの音量を一括調整する操作欄を描画します。
    /// </summary>
    private void DrawVolumeToolBar(SoundDatabase database)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"音量の一括調整（選択 {m_selectedIds.Count} 件）", EditorStyles.boldLabel, GUILayout.Width(210.0f));
        m_isLinkedEdit = EditorGUILayout.ToggleLeft("選択中フェーダー連動", m_isLinkedEdit);
        EditorGUILayout.EndHorizontal();

        using (new EditorGUI.DisabledScope(m_selectedIds.Count == 0))
        {
            DrawTrimSlider(database);
            DrawVolumeStepButtons(database);
        }

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
    private void DrawTrimSlider(SoundDatabase database)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("相対調整", GUILayout.Width(56.0f));

        float newTrimValue = EditorGUILayout.Slider(m_trimValue, -TRIM_RANGE, TRIM_RANGE);

        if (!Mathf.Approximately(newTrimValue, m_trimValue))
        {
            ApplyVolumeDeltaToSelected(database, newTrimValue - m_trimValue);
            m_trimValue = newTrimValue;
        }

        if (Event.current.rawType == EventType.MouseUp)
        {
            m_trimValue = 0.0f;
        }

        GUILayout.Space(10.0f);
        GUILayout.Label("揃える", GUILayout.Width(42.0f));
        m_unifyVolume = EditorGUILayout.Slider(m_unifyVolume, MIN_VOLUME, MAX_VOLUME, GUILayout.Width(140.0f));

        if (GUILayout.Button("適用", GUILayout.Width(44.0f)))
        {
            SetVolumeToSelected(database, m_unifyVolume);
        }

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 音量の線形増減ボタンと dB 比率維持増減ボタンを描画します。
    /// </summary>
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

        GUILayout.Space(8.0f);
        GUILayout.Label("比率維持(dB):", GUILayout.Width(76.0f));

        if (GUILayout.Button("-3dB"))
        {
            ApplyVolumeMultiplierToSelected(database, SoundToolGui.ConvertDecibelDeltaToMultiplier(-DECIBEL_STEP_LARGE));
        }

        if (GUILayout.Button("-1dB"))
        {
            ApplyVolumeMultiplierToSelected(database, SoundToolGui.ConvertDecibelDeltaToMultiplier(-DECIBEL_STEP_SMALL));
        }

        if (GUILayout.Button("+1dB"))
        {
            ApplyVolumeMultiplierToSelected(database, SoundToolGui.ConvertDecibelDeltaToMultiplier(DECIBEL_STEP_SMALL));
        }

        if (GUILayout.Button("+3dB"))
        {
            ApplyVolumeMultiplierToSelected(database, SoundToolGui.ConvertDecibelDeltaToMultiplier(DECIBEL_STEP_LARGE));
        }

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 詳細項目の一括変更パネルを描画します。
    /// </summary>
    private void DrawBulkDetailPanel(SoundDatabase database)
    {
        EditorGUILayout.HelpBox(
            "チェックを入れた項目だけが、選択中のサウンドへ適用されます。",
            MessageType.None);

        EditorGUILayout.BeginHorizontal();
        m_bulkSettings.ApplyCategory = EditorGUILayout.ToggleLeft("カテゴリー", m_bulkSettings.ApplyCategory, GUILayout.Width(110.0f));

        using (new EditorGUI.DisabledScope(!m_bulkSettings.ApplyCategory))
        {
            m_bulkSettings.Values.m_category = (SoundCategory)EditorGUILayout.EnumPopup(m_bulkSettings.Values.m_category);
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        m_bulkSettings.ApplyPitch = EditorGUILayout.ToggleLeft("ピッチ", m_bulkSettings.ApplyPitch, GUILayout.Width(110.0f));

        using (new EditorGUI.DisabledScope(!m_bulkSettings.ApplyPitch))
        {
            m_bulkSettings.Values.m_pitch = EditorGUILayout.Slider(m_bulkSettings.Values.m_pitch, MIN_PITCH, MAX_PITCH);
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        m_bulkSettings.ApplyLoop = EditorGUILayout.ToggleLeft("ループ", m_bulkSettings.ApplyLoop, GUILayout.Width(110.0f));

        using (new EditorGUI.DisabledScope(!m_bulkSettings.ApplyLoop))
        {
            m_bulkSettings.Values.m_loop = EditorGUILayout.Toggle(m_bulkSettings.Values.m_loop);
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        m_bulkSettings.ApplyIs3D = EditorGUILayout.ToggleLeft("3D 音源", m_bulkSettings.ApplyIs3D, GUILayout.Width(110.0f));

        using (new EditorGUI.DisabledScope(!m_bulkSettings.ApplyIs3D))
        {
            m_bulkSettings.Values.m_is3D = EditorGUILayout.Toggle(m_bulkSettings.Values.m_is3D);
        }

        EditorGUILayout.EndHorizontal();

        m_bulkSettings.ApplyRolloff = EditorGUILayout.ToggleLeft("距離減衰（モード・距離・カーブ）", m_bulkSettings.ApplyRolloff);

        using (new EditorGUI.DisabledScope(!m_bulkSettings.ApplyRolloff))
        {
            SoundToolGui.DrawRolloffFields(m_bulkSettings.Values);
        }

        using (new EditorGUI.DisabledScope(m_selectedIds.Count == 0))
        {
            if (GUILayout.Button($"選択中の {m_selectedIds.Count} 件へ適用", GUILayout.Height(24.0f)))
            {
                ApplyBulkDetailToSelected(database);
            }
        }
    }

    /// <summary>
    /// 試聴中の再生位置シーク・I→L→O 遷移コントロールバーを描画します。
    /// </summary>
    private void DrawPreviewTransportBar()
    {
        bool isPlaying = SoundToolPreviewPlayer.HasActivePreview();
        PreviewSequenceState state = SoundToolPreviewPlayer.CurrentState;
        SoundDatabase.SoundData currentData = SoundToolPreviewPlayer.CurrentData;

        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

        if (!isPlaying || currentData == null)
        {
            GUILayout.Label("試聴待機中：行をクリック（または矢印キー）で即試聴＆アナライズできます", EditorStyles.miniLabel);

            bool newAutoOutro = GUILayout.Toggle(
                m_autoOutroOnSequence,
                "一連再生で L→O 自動移行",
                EditorStyles.miniButton,
                GUILayout.Width(160.0f));

            if (newAutoOutro != m_autoOutroOnSequence)
            {
                m_autoOutroOnSequence = newAutoOutro;
                SoundToolPreviewPlayer.SetAutoOutro(m_autoOutroOnSequence);
            }

            EditorGUILayout.EndHorizontal();
            return;
        }

        string partLabel = GetStateDisplayText(state);
        GUILayout.Label($"▶ {partLabel}: {currentData.m_id}", EditorStyles.miniBoldLabel, GUILayout.Width(240.0f));

        float currentTime = SoundToolPreviewPlayer.GetCurrentTime();
        float totalLength = SoundToolPreviewPlayer.GetCurrentLength();

        GUILayout.Label($"{SoundToolGui.FormatTime(currentTime)} / {SoundToolGui.FormatTime(totalLength)}", EditorStyles.miniLabel, GUILayout.Width(84.0f));

        if (SoundToolPreviewPlayer.UseVolume && totalLength > 0.0f)
        {
            float newTime = GUILayout.HorizontalSlider(currentTime, 0.0f, totalLength, GUILayout.MinWidth(100.0f));

            if (Mathf.Abs(newTime - currentTime) > 0.08f)
            {
                SoundToolPreviewPlayer.Seek(newTime);
            }

            if (GUILayout.Button($"末尾-{NEAR_END_SECONDS:0}秒", EditorStyles.miniButton, GUILayout.Width(64.0f)))
            {
                SoundToolPreviewPlayer.JumpNearEnd(NEAR_END_SECONDS);
            }
        }
        else
        {
            GUILayout.FlexibleSpace();
        }

        if (state == PreviewSequenceState.PREVIEW_INTRO || state == PreviewSequenceState.PREVIEW_LOOP)
        {
            if (GUILayout.Button("→ O (末尾から)", EditorStyles.miniButton, GUILayout.Width(92.0f)))
            {
                SoundToolPreviewPlayer.TriggerOutro(true);
            }

            if (GUILayout.Button("→ O (即切替)", EditorStyles.miniButton, GUILayout.Width(84.0f)))
            {
                SoundToolPreviewPlayer.TriggerOutro(false);
            }
        }

        bool autoOutro = GUILayout.Toggle(m_autoOutroOnSequence, "自動O", EditorStyles.miniButton, GUILayout.Width(52.0f));

        if (autoOutro != m_autoOutroOnSequence)
        {
            m_autoOutroOnSequence = autoOutro;
            SoundToolPreviewPlayer.SetAutoOutro(m_autoOutroOnSequence);
        }

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 試聴状態に応じた短い表示名を取得します。
    /// </summary>
    private static string GetStateDisplayText(PreviewSequenceState state)
    {
        switch (state)
        {
            case PreviewSequenceState.PREVIEW_INTRO:
                return "[INTRO]";
            case PreviewSequenceState.PREVIEW_LOOP:
                return "[LOOP]";
            case PreviewSequenceState.PREVIEW_OUTRO:
                return "[OUTRO]";
            default:
                return "[SINGLE]";
        }
    }

    /// <summary>
    /// サウンド一覧（チャンネルストリップ）を描画します。
    /// </summary>
    private void DrawChannelList(SoundDatabase database)
    {
        m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition, GUILayout.ExpandHeight(true));

        if (m_visibleRows.Count == 0)
        {
            EditorGUILayout.HelpBox("条件に一致するサウンドがありません。", MessageType.Info);
        }

        for (int i = 0; i < m_visibleRows.Count; i++)
        {
            VisibleRow row = m_visibleRows[i];

            if (row.IsGroupHeader)
            {
                DrawGroupHeaderRow(database, row, i);
            }
            else
            {
                DrawSoundDataRow(database, row, i);
            }
        }

        EditorGUILayout.EndScrollView();

        if (Event.current.type == EventType.Repaint)
        {
            Rect scrollRect = GUILayoutUtility.GetLastRect();
            m_scrollViewHeight = scrollRect.height;

            if (m_pendingScrollToFocus)
            {
                m_pendingScrollToFocus = false;
                ScrollToFocusedRow();
            }
        }
    }

    /// <summary>
    /// フォーカス中の行が画面内に収まるようスクロール位置を調整します。
    /// </summary>
    private void ScrollToFocusedRow()
    {
        int index = FindFocusedRowIndex();

        if (index < 0 || m_scrollViewHeight <= 0.0f)
        {
            return;
        }

        VisibleRow row = m_visibleRows[index];
        bool changed = false;

        if (row.RowTop < m_scrollPosition.y)
        {
            m_scrollPosition.y = row.RowTop;
            changed = true;
        }
        else if (row.RowBottom > m_scrollPosition.y + m_scrollViewHeight)
        {
            m_scrollPosition.y = row.RowBottom - m_scrollViewHeight;
            changed = true;
        }

        if (changed)
        {
            m_requestRepaint?.Invoke();
        }
    }

    /// <summary>
    /// シーケンスグループの親行（_I / _L / _O まとめ行）を描画します。
    /// </summary>
    private void DrawGroupHeaderRow(SoundDatabase database, VisibleRow row, int rowIndex)
    {
        SoundToolSequenceHelper.SoundGroup group = row.Group;
        SoundDatabase.SoundData primaryData = group.GetPrimaryData();

        if (primaryData == null)
        {
            return;
        }

        bool isFocused = m_focusedRowKey == row.RowKey;
        bool isPlaying = SoundToolPreviewPlayer.IsPlayingGroup(group.BaseName);
        bool isGroupSelected = IsGroupAllSelected(group);
        bool isExpanded = m_expandedGroupKeys.Contains(group.GroupKey);

        Rect rowRect = EditorGUILayout.BeginHorizontal(EditorStyles.helpBox, GUILayout.Height(ROW_HEIGHT));

        if (Event.current.type == EventType.Repaint)
        {
            row.RowTop = rowRect.y;
            row.RowBottom = rowRect.yMax;
            EditorGUI.DrawRect(rowRect, GetRowBackgroundColor(isGroupSelected, isFocused, isPlaying, false));
        }

        BeginCenteredColumn(SELECT_TOGGLE_WIDTH);
        bool newSelected = EditorGUILayout.Toggle(isGroupSelected, GUILayout.Width(SELECT_TOGGLE_WIDTH));
        EndCenteredColumn();

        if (newSelected != isGroupSelected)
        {
            ChangeGroupSelection(rowIndex, group, newSelected);
        }

        BeginCenteredColumn(FOLD_BUTTON_WIDTH);

        if (GUILayout.Button(isExpanded ? "▾" : "▸", EditorStyles.miniButton, GUILayout.Width(FOLD_BUTTON_WIDTH), GUILayout.Height(20.0f)))
        {
            ToggleGroupExpanded(group.GroupKey);
        }

        EndCenteredColumn();

        BeginCenteredColumn(PREVIEW_BUTTON_WIDTH);

        if (GUILayout.Button(isPlaying ? "■" : "▶", GUILayout.Width(PREVIEW_BUTTON_WIDTH), GUILayout.Height(22.0f)))
        {
            ToggleGroupPreview(group);
        }

        EndCenteredColumn();

        BeginCenteredColumn(CATEGORY_TAG_WIDTH);
        SoundToolGui.DrawCategoryTag(primaryData.m_category, CATEGORY_TAG_WIDTH);
        EndCenteredColumn();

        BeginCenteredColumn(NAME_COLUMN_WIDTH);
        GUILayout.Label(group.BaseName, EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        DrawGroupPartBadges(group, isPlaying);
        EditorGUILayout.EndHorizontal();

        EndCenteredColumn();

        float avgVolume = group.GetAverageVolume();
        BeginCenteredColumn(METER_WIDTH);
        SoundToolGui.DrawVolumeMeter(avgVolume, METER_WIDTH, METER_HEIGHT);
        EndCenteredColumn();

        BeginCenteredColumn();
        float newAvgVolume = EditorGUILayout.Slider(avgVolume, MIN_VOLUME, MAX_VOLUME, GUILayout.MinWidth(SLIDER_MIN_WIDTH));
        EndCenteredColumn();

        if (!Mathf.Approximately(newAvgVolume, avgVolume))
        {
            ApplyVolumeDeltaToGroup(database, group, newAvgVolume - avgVolume);
        }

        BeginCenteredColumn(DECIBEL_LABEL_WIDTH);
        string dbPrefix = group.HasVolumeDifference() ? "~" : "";
        GUILayout.Label(dbPrefix + SoundToolGui.GetDecibelText(avgVolume), EditorStyles.miniLabel, GUILayout.Width(DECIBEL_LABEL_WIDTH));
        EndCenteredColumn();

        GUILayout.Space(REF_BUTTON_WIDTH + 4.0f);

        EditorGUILayout.EndHorizontal();

        HandleRowBodyClick(rowRect, row);
    }

    /// <summary>
    /// グループ行の [I] [L] [O] パート有無＆再生中バッジを描画します。
    /// </summary>
    private static void DrawGroupPartBadges(SoundToolSequenceHelper.SoundGroup group, bool isPlayingGroup)
    {
        PreviewSequenceState state = SoundToolPreviewPlayer.CurrentState;

        SoundToolGui.DrawPartBadge(
            "I",
            SoundPartType.PART_INTRO,
            group.IntroData != null,
            isPlayingGroup && state == PreviewSequenceState.PREVIEW_INTRO,
            PART_BADGE_WIDTH);

        SoundToolGui.DrawPartBadge(
            "L",
            SoundPartType.PART_LOOP,
            group.LoopData != null,
            isPlayingGroup && state == PreviewSequenceState.PREVIEW_LOOP,
            PART_BADGE_WIDTH);

        SoundToolGui.DrawPartBadge(
            "O",
            SoundPartType.PART_OUTRO,
            group.OutroData != null,
            isPlayingGroup && state == PreviewSequenceState.PREVIEW_OUTRO,
            PART_BADGE_WIDTH);

        bool hasMissingClip = (group.IntroData != null && group.IntroData.m_clip == null)
            || (group.LoopData != null && group.LoopData.m_clip == null)
            || (group.OutroData != null && group.OutroData.m_clip == null);

        if (hasMissingClip)
        {
            SoundToolGui.DrawColoredMiniLabel("Clip未設定あり", ERROR_COLOR);
        }
    }

    /// <summary>
    /// 単体行またはグループ展開時の子行を描画します。
    /// </summary>
    private void DrawSoundDataRow(SoundDatabase database, VisibleRow row, int rowIndex)
    {
        SoundDatabase.SoundData data = row.Data;

        if (data == null)
        {
            return;
        }

        bool isSelected = m_selectedIds.Contains(data.m_id);
        bool isFocused = m_focusedRowKey == row.RowKey;
        bool isPlaying = SoundToolPreviewPlayer.IsPlaying(data.m_id);
        float height = row.IsChildRow ? CHILD_ROW_HEIGHT : ROW_HEIGHT;

        Rect rowRect = EditorGUILayout.BeginHorizontal(EditorStyles.helpBox, GUILayout.Height(height));

        if (Event.current.type == EventType.Repaint)
        {
            row.RowTop = rowRect.y;
            row.RowBottom = rowRect.yMax;
            EditorGUI.DrawRect(rowRect, GetRowBackgroundColor(isSelected, isFocused, isPlaying, row.IsChildRow));
        }

        BeginCenteredColumn(SELECT_TOGGLE_WIDTH);
        bool newSelected = EditorGUILayout.Toggle(isSelected, GUILayout.Width(SELECT_TOGGLE_WIDTH));
        EndCenteredColumn();

        if (newSelected != isSelected)
        {
            ChangeSingleSelection(rowIndex, data.m_id, newSelected);
        }

        if (row.IsChildRow)
        {
            GUILayout.Space(CHILD_INDENT_WIDTH);
        }

        BeginCenteredColumn(PREVIEW_BUTTON_WIDTH);

        if (GUILayout.Button(isPlaying ? "■" : "▶", GUILayout.Width(PREVIEW_BUTTON_WIDTH), GUILayout.Height(row.IsChildRow ? 18.0f : 22.0f)))
        {
            ToggleSinglePreview(data);
        }

        EndCenteredColumn();

        if (row.IsChildRow)
        {
            BeginCenteredColumn(CATEGORY_TAG_WIDTH);
            EditorGUILayout.BeginHorizontal();
            SoundToolGui.DrawPartBadge(GetShortPartLabel(row.PartType), row.PartType, true, isPlaying, 28.0f);
            GUILayout.Label(GetPartDescription(row.PartType), EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
            EndCenteredColumn();
        }
        else
        {
            BeginCenteredColumn(CATEGORY_TAG_WIDTH);
            SoundToolGui.DrawCategoryTag(data.m_category, CATEGORY_TAG_WIDTH);
            EndCenteredColumn();
        }

        BeginCenteredColumn(NAME_COLUMN_WIDTH);
        GUILayout.Label(data.m_id.ToString(), row.IsChildRow ? EditorStyles.miniBoldLabel : EditorStyles.boldLabel);

        if (!row.IsChildRow)
        {
            DrawClipSummaryLabel(data);
        }

        EndCenteredColumn();

        BeginCenteredColumn(METER_WIDTH);
        SoundToolGui.DrawVolumeMeter(data.m_volume, METER_WIDTH, METER_HEIGHT);
        EndCenteredColumn();

        BeginCenteredColumn();
        float newVolume = EditorGUILayout.Slider(data.m_volume, MIN_VOLUME, MAX_VOLUME, GUILayout.MinWidth(SLIDER_MIN_WIDTH));
        EndCenteredColumn();

        if (!Mathf.Approximately(newVolume, data.m_volume))
        {
            ChangeSingleVolume(database, data, newVolume);
        }

        BeginCenteredColumn(DECIBEL_LABEL_WIDTH);
        GUILayout.Label(SoundToolGui.GetDecibelText(data.m_volume), EditorStyles.miniLabel, GUILayout.Width(DECIBEL_LABEL_WIDTH));
        EndCenteredColumn();

        BeginCenteredColumn(REF_BUTTON_WIDTH);
        bool isRef = m_referenceData == data;
        Color prevBg = GUI.backgroundColor;

        if (isRef)
        {
            GUI.backgroundColor = REF_ACTIVE_COLOR;
        }

        if (GUILayout.Button("Ref", EditorStyles.miniButton, GUILayout.Width(REF_BUTTON_WIDTH)))
        {
            SetReferenceData(isRef ? null : data);
        }

        GUI.backgroundColor = prevBg;
        EndCenteredColumn();

        EditorGUILayout.EndHorizontal();

        HandleRowBodyClick(rowRect, row);
    }

    /// <summary>
    /// 行内の操作ボタン・スライダー以外の領域をクリックした際に即フォーカス＆即再生を行います。
    /// </summary>
    private void HandleRowBodyClick(Rect rowRect, VisibleRow row)
    {
        Event currentEvent = Event.current;

        if (currentEvent.type == EventType.MouseDown
            && currentEvent.button == 0
            && rowRect.Contains(currentEvent.mousePosition))
        {
            ActivateRow(row, m_autoPlayOnSelect);
            currentEvent.Use();
        }
    }

    /// <summary>
    /// パートの短い表示名を取得します。
    /// </summary>
    private static string GetShortPartLabel(SoundPartType partType)
    {
        switch (partType)
        {
            case SoundPartType.PART_INTRO:
                return "_I";
            case SoundPartType.PART_LOOP:
                return "_L";
            case SoundPartType.PART_OUTRO:
                return "_O";
            default:
                return "-";
        }
    }

    /// <summary>
    /// パートの説明文を取得します。
    /// </summary>
    private static string GetPartDescription(SoundPartType partType)
    {
        switch (partType)
        {
            case SoundPartType.PART_INTRO:
                return "開始";
            case SoundPartType.PART_LOOP:
                return "ループ";
            case SoundPartType.PART_OUTRO:
                return "終了";
            default:
                return "";
        }
    }

    /// <summary>
    /// Clip 名とループ・3D の状態を1行で描画します。
    /// </summary>
    private static void DrawClipSummaryLabel(SoundDatabase.SoundData data)
    {
        string clipText = data.m_clip != null ? data.m_clip.name : "Clip 未設定";
        string flagText = (data.m_loop ? "  [LOOP]" : "") + (data.m_is3D ? "  [3D]" : "");

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
    private static Color GetRowBackgroundColor(bool isSelected, bool isFocused, bool isPlaying, bool isChildRow)
    {
        if (isFocused)
        {
            return FOCUSED_ROW_COLOR;
        }

        if (isPlaying)
        {
            return PLAYING_ROW_COLOR;
        }

        if (isSelected)
        {
            return SELECTED_ROW_COLOR;
        }

        return isChildRow ? CHILD_ROW_BG_COLOR : Color.clear;
    }

    /// <summary>
    /// 縦方向に中央揃えする列の開始位置を描画します（幅指定あり）。
    /// </summary>
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
    /// グループ内の全パートが選択中かを確認します。
    /// </summary>
    private bool IsGroupAllSelected(SoundToolSequenceHelper.SoundGroup group)
    {
        group.CollectOrderedParts(m_tempPartList);

        if (m_tempPartList.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < m_tempPartList.Count; i++)
        {
            if (!m_selectedIds.Contains(m_tempPartList[i].m_id))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// グループの展開状態を切り替えます。
    /// </summary>
    private void ToggleGroupExpanded(string groupKey)
    {
        if (!m_expandedGroupKeys.Remove(groupKey))
        {
            m_expandedGroupKeys.Add(groupKey);
        }
    }

    /// <summary>
    /// グループの選択状態を変更します。
    /// </summary>
    private void ChangeGroupSelection(int rowIndex, SoundToolSequenceHelper.SoundGroup group, bool isSelected)
    {
        bool isRangeSelect = Event.current.shift
            && m_lastToggledIndex >= 0
            && m_lastToggledIndex < m_visibleRows.Count;

        if (isRangeSelect)
        {
            ApplyRangeSelection(m_lastToggledIndex, rowIndex, isSelected);
        }
        else
        {
            group.CollectOrderedParts(m_tempPartList);

            for (int i = 0; i < m_tempPartList.Count; i++)
            {
                if (isSelected)
                {
                    m_selectedIds.Add(m_tempPartList[i].m_id);
                }
                else
                {
                    m_selectedIds.Remove(m_tempPartList[i].m_id);
                }
            }
        }

        m_lastToggledIndex = rowIndex;
    }

    /// <summary>
    /// 単体・子行の選択状態を変更します。Shift 押下時は範囲選択を行います。
    /// </summary>
    private void ChangeSingleSelection(int rowIndex, AudioID id, bool isSelected)
    {
        bool isRangeSelect = Event.current.shift
            && m_lastToggledIndex >= 0
            && m_lastToggledIndex < m_visibleRows.Count;

        if (isRangeSelect)
        {
            ApplyRangeSelection(m_lastToggledIndex, rowIndex, isSelected);
        }
        else
        {
            if (isSelected)
            {
                m_selectedIds.Add(id);
            }
            else
            {
                m_selectedIds.Remove(id);
            }
        }

        m_lastToggledIndex = rowIndex;
    }

    /// <summary>
    /// 指定された行範囲の選択状態を一括で変更します。
    /// </summary>
    private void ApplyRangeSelection(int indexA, int indexB, bool isSelected)
    {
        int startIndex = Mathf.Min(indexA, indexB);
        int endIndex = Mathf.Max(indexA, indexB);

        for (int i = startIndex; i <= endIndex; i++)
        {
            VisibleRow targetRow = m_visibleRows[i];

            if (targetRow.IsGroupHeader && targetRow.Group != null)
            {
                targetRow.Group.CollectOrderedParts(m_tempPartList);

                for (int p = 0; p < m_tempPartList.Count; p++)
                {
                    if (isSelected)
                    {
                        m_selectedIds.Add(m_tempPartList[p].m_id);
                    }
                    else
                    {
                        m_selectedIds.Remove(m_tempPartList[p].m_id);
                    }
                }
            }
            else if (targetRow.Data != null)
            {
                if (isSelected)
                {
                    m_selectedIds.Add(targetRow.Data.m_id);
                }
                else
                {
                    m_selectedIds.Remove(targetRow.Data.m_id);
                }
            }
        }
    }

    /// <summary>
    /// 単体サウンドの試聴を切り替えます。
    /// </summary>
    private void ToggleSinglePreview(SoundDatabase.SoundData data)
    {
        if (data == null)
        {
            return;
        }

        if (SoundToolPreviewPlayer.IsPlaying(data.m_id)
            && string.IsNullOrEmpty(SoundToolPreviewPlayer.PlayingGroupBaseName))
        {
            SoundToolPreviewPlayer.Stop();
            return;
        }

        SoundToolPreviewPlayer.PlaySingle(data, GetReferenceAudioId());
    }

    /// <summary>
    /// グループの一連試聴を切り替えます。
    /// </summary>
    private void ToggleGroupPreview(SoundToolSequenceHelper.SoundGroup group)
    {
        if (group == null)
        {
            return;
        }

        if (SoundToolPreviewPlayer.IsPlayingGroup(group.BaseName))
        {
            SoundToolPreviewPlayer.Stop();
            return;
        }

        PlayGroupSequence(group);
    }

    /// <summary>
    /// グループの I → L → O 一連再生を開始します。
    /// </summary>
    private void PlayGroupSequence(SoundToolSequenceHelper.SoundGroup group)
    {
        if (group == null)
        {
            return;
        }

        SoundToolPreviewPlayer.PlaySequence(
            group.BaseName,
            group.IntroData,
            group.LoopData,
            group.OutroData,
            m_autoOutroOnSequence,
            GetReferenceAudioId());
    }

    /// <summary>
    /// 1行のフェーダー操作を反映します。連動 ON なら選択中すべてへ同じ変化量を加えます。
    /// </summary>
    private void ChangeSingleVolume(SoundDatabase database, SoundDatabase.SoundData data, float newVolume)
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
    /// グループ内の全パートへ同じ変化量を加えます（連動選択中なら選択中すべてへも反映）。
    /// </summary>
    private void ApplyVolumeDeltaToGroup(SoundDatabase database, SoundToolSequenceHelper.SoundGroup group, float delta)
    {
        if (group == null)
        {
            return;
        }

        if (m_isLinkedEdit && IsGroupAllSelected(group) && m_selectedIds.Count > 1)
        {
            ApplyVolumeDeltaToSelected(database, delta);
            return;
        }

        Undo.RecordObject(database, "Adjust Group Volume");
        group.CollectOrderedParts(m_tempPartList);

        for (int i = 0; i < m_tempPartList.Count; i++)
        {
            SoundDatabase.SoundData part = m_tempPartList[i];
            part.m_volume = Mathf.Clamp(part.m_volume + delta, MIN_VOLUME, MAX_VOLUME);
        }

        SoundToolDatabaseProvider.MarkDirty(database);
    }

    /// <summary>
    /// 選択中のサウンドの音量へ同じ変化量を加えます。
    /// </summary>
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
    /// 選択中のサウンドの音量へ同じ倍率を乗算します（比率を維持した dB 調整用）。
    /// </summary>
    private void ApplyVolumeMultiplierToSelected(SoundDatabase database, float multiplier)
    {
        Undo.RecordObject(database, "Scale Sound Volume");

        foreach (SoundDatabase.SoundData data in database.m_soundList)
        {
            if (data != null && m_selectedIds.Contains(data.m_id))
            {
                data.m_volume = Mathf.Clamp(data.m_volume * multiplier, MIN_VOLUME, MAX_VOLUME);
            }
        }

        SoundToolDatabaseProvider.MarkDirty(database);
    }

    /// <summary>
    /// 選択中のサウンドの音量を同じ値に揃えます。
    /// </summary>
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
    /// 詳細編集パネルを描画します（ラグ防止のため SerializedObject を使わず直接編集します）。
    /// </summary>
    private void DrawDetailPanel(SoundDatabase database)
    {
        if (m_focusedGroup == null && m_focusedData == null)
        {
            EditorGUILayout.HelpBox(
                "行をクリック（または ↑↓ キー）すると、即試聴＆詳細編集ができます。",
                MessageType.None);
            return;
        }

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        if (m_focusedGroup != null && m_focusedGroup.IsSequenceGroup)
        {
            DrawGroupDetailPanel(database, m_focusedGroup);
        }
        else if (m_focusedData != null)
        {
            DrawSingleDetailHeader(m_focusedData);
            DrawDirectSoundDataFields(database, m_focusedData);
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// シーケンスグループ用の詳細パネル（一括タブ / 個別パートタブ）を描画します。
    /// </summary>
    private void DrawGroupDetailPanel(SoundDatabase database, SoundToolSequenceHelper.SoundGroup group)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"シーケンス編集：{group.BaseName}", EditorStyles.boldLabel);

        bool isPlayingGroup = SoundToolPreviewPlayer.IsPlayingGroup(group.BaseName);

        if (GUILayout.Button(isPlayingGroup ? "■ 停止" : "▶ I→L→O 一連試聴", GUILayout.Width(130.0f)))
        {
            ToggleGroupPreview(group);
        }

        if (GUILayout.Button("閉じる", GUILayout.Width(54.0f)))
        {
            CloseDetailPanel();
            EditorGUILayout.EndHorizontal();
            return;
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Toggle(m_detailTabMode == DetailTabMode.DETAIL_GROUP, "一括バランス (I/L/O)", EditorStyles.miniButton))
        {
            m_detailTabMode = DetailTabMode.DETAIL_GROUP;
        }

        DrawPartTabButton("_I (開始)", DetailTabMode.DETAIL_INTRO, group.IntroData);
        DrawPartTabButton("_L (ループ)", DetailTabMode.DETAIL_LOOP, group.LoopData);
        DrawPartTabButton("_O (終了)", DetailTabMode.DETAIL_OUTRO, group.OutroData);

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(2.0f);

        switch (m_detailTabMode)
        {
            case DetailTabMode.DETAIL_INTRO:
                if (group.IntroData != null)
                {
                    DrawSingleDetailHeader(group.IntroData);
                    DrawDirectSoundDataFields(database, group.IntroData);
                }
                break;

            case DetailTabMode.DETAIL_LOOP:
                if (group.LoopData != null)
                {
                    DrawSingleDetailHeader(group.LoopData);
                    DrawDirectSoundDataFields(database, group.LoopData);
                }
                break;

            case DetailTabMode.DETAIL_OUTRO:
                if (group.OutroData != null)
                {
                    DrawSingleDetailHeader(group.OutroData);
                    DrawDirectSoundDataFields(database, group.OutroData);
                }
                break;

            default:
                DrawGroupCombinedEditor(database, group);
                break;
        }
    }

    /// <summary>
    /// 個別パート切り替え用のタブボタンを描画します。
    /// </summary>
    private void DrawPartTabButton(string label, DetailTabMode tabMode, SoundDatabase.SoundData partData)
    {
        using (new EditorGUI.DisabledScope(partData == null))
        {
            bool isActive = m_detailTabMode == tabMode && partData != null;

            if (GUILayout.Toggle(isActive, label, EditorStyles.miniButton) && partData != null && m_detailTabMode != tabMode)
            {
                GUI.FocusControl(null);
                m_detailTabMode = tabMode;
                m_focusedData = partData;
            }
        }
    }

    /// <summary>
    /// グループの一括バランス編集画面を描画します（I/L/O 比較スライダー＋一括設定）。
    /// </summary>
    private void DrawGroupCombinedEditor(SoundDatabase database, SoundToolSequenceHelper.SoundGroup group)
    {
        DrawGroupPartBalanceRow(database, "開始 (_I)", SoundPartType.PART_INTRO, group.IntroData);
        DrawGroupPartBalanceRow(database, "ループ (_L)", SoundPartType.PART_LOOP, group.LoopData);
        DrawGroupPartBalanceRow(database, "終了 (_O)", SoundPartType.PART_OUTRO, group.OutroData);

        EditorGUILayout.Space(4.0f);

        SoundDatabase.SoundData primary = group.GetPrimaryData();

        if (primary == null)
        {
            return;
        }

        EditorGUILayout.LabelField("グループ共通変更（変更すると I / L / O すべてに適用）", EditorStyles.miniBoldLabel);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("カテゴリー", GUILayout.Width(76.0f));
        SoundCategory newCategory = (SoundCategory)EditorGUILayout.EnumPopup(primary.m_category, GUILayout.Width(140.0f));

        if (newCategory != primary.m_category)
        {
            ApplyToAllGroupParts(database, group, "Change Group Category", part => part.m_category = newCategory);
        }

        GUILayout.Space(10.0f);
        GUILayout.Label("ピッチ", GUILayout.Width(42.0f));
        float newPitch = EditorGUILayout.Slider(primary.m_pitch, MIN_PITCH, MAX_PITCH);

        if (!Mathf.Approximately(newPitch, primary.m_pitch))
        {
            ApplyToAllGroupParts(database, group, "Change Group Pitch", part => part.m_pitch = newPitch);
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        bool newIs3D = EditorGUILayout.ToggleLeft("3D 音源（全パート）", primary.m_is3D, GUILayout.Width(140.0f));

        if (newIs3D != primary.m_is3D)
        {
            ApplyToAllGroupParts(database, group, "Change Group 3D", part => part.m_is3D = newIs3D);
        }

        if (GUILayout.Button("音量を代表値に揃える", EditorStyles.miniButton, GUILayout.Width(140.0f)))
        {
            float targetVolume = primary.m_volume;
            ApplyToAllGroupParts(database, group, "Unify Group Volume", part => part.m_volume = targetVolume);
        }

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 一括編集内の I/L/O 個別バランス調整1行を描画します。
    /// </summary>
    private void DrawGroupPartBalanceRow(
        SoundDatabase database,
        string partTitle,
        SoundPartType partType,
        SoundDatabase.SoundData data)
    {
        if (data == null)
        {
            return;
        }

        bool isPlaying = SoundToolPreviewPlayer.IsPlaying(data.m_id);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button(isPlaying ? "■" : "▶", EditorStyles.miniButton, GUILayout.Width(26.0f)))
        {
            ToggleSinglePreview(data);
        }

        SoundToolGui.DrawPartBadge(GetShortPartLabel(partType), partType, true, isPlaying, 24.0f);
        GUILayout.Label(partTitle, EditorStyles.miniBoldLabel, GUILayout.Width(72.0f));

        SoundToolGui.DrawVolumeMeter(data.m_volume, 30.0f, 8.0f);

        float newVolume = EditorGUILayout.Slider(data.m_volume, MIN_VOLUME, MAX_VOLUME);

        if (!Mathf.Approximately(newVolume, data.m_volume))
        {
            Undo.RecordObject(database, "Change Part Volume");
            data.m_volume = Mathf.Clamp(newVolume, MIN_VOLUME, MAX_VOLUME);
            SoundToolDatabaseProvider.MarkDirty(database);
        }

        GUILayout.Label(SoundToolGui.GetDecibelText(data.m_volume), EditorStyles.miniLabel, GUILayout.Width(58.0f));

        if (GUILayout.Button("個別", EditorStyles.miniButton, GUILayout.Width(40.0f)))
        {
            GUI.FocusControl(null);
            m_focusedData = data;
            m_detailTabMode = GetTabModeFromPart(partType);
        }

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// グループ内の全パートへ一括で処理を実行します。
    /// </summary>
    private void ApplyToAllGroupParts(
        SoundDatabase database,
        SoundToolSequenceHelper.SoundGroup group,
        string undoLabel,
        Action<SoundDatabase.SoundData> action)
    {
        Undo.RecordObject(database, undoLabel);
        group.CollectOrderedParts(m_tempPartList);

        for (int i = 0; i < m_tempPartList.Count; i++)
        {
            action(m_tempPartList[i]);
        }

        SoundToolDatabaseProvider.MarkDirty(database);
    }

    /// <summary>
    /// 単体詳細編集ヘッダーを描画します。
    /// </summary>
    private void DrawSingleDetailHeader(SoundDatabase.SoundData data)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"詳細設定：{data.m_id}", EditorStyles.boldLabel);

        bool isRef = m_referenceData == data;

        if (GUILayout.Button(isRef ? "Ref解除" : "Refに固定", EditorStyles.miniButton, GUILayout.Width(76.0f)))
        {
            SetReferenceData(isRef ? null : data);
        }

        if (GUILayout.Button(SoundToolPreviewPlayer.IsPlaying(data.m_id) ? "■ 停止" : "▶ 単体試聴", GUILayout.Width(84.0f)))
        {
            ToggleSinglePreview(data);
        }

        if (m_focusedGroup == null)
        {
            if (GUILayout.Button("閉じる", GUILayout.Width(54.0f)))
            {
                CloseDetailPanel();
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// サウンドデータの各項目を高速・直接編集します（SerializedObject による遅延なし）。
    /// </summary>
    private static void DrawDirectSoundDataFields(SoundDatabase database, SoundDatabase.SoundData data)
    {
        if (data == null)
        {
            return;
        }

        EditorGUILayout.BeginHorizontal();
        float newVolume = EditorGUILayout.Slider("音量", data.m_volume, MIN_VOLUME, MAX_VOLUME);
        GUILayout.Label(SoundToolGui.GetDecibelText(newVolume), EditorStyles.miniLabel, GUILayout.Width(60.0f));
        EditorGUILayout.EndHorizontal();

        if (!Mathf.Approximately(newVolume, data.m_volume))
        {
            Undo.RecordObject(database, "Change Sound Volume");
            data.m_volume = Mathf.Clamp(newVolume, MIN_VOLUME, MAX_VOLUME);
            SoundToolDatabaseProvider.MarkDirty(database);
        }

        AudioClip newClip = (AudioClip)EditorGUILayout.ObjectField("AudioClip", data.m_clip, typeof(AudioClip), false);

        if (newClip != data.m_clip)
        {
            Undo.RecordObject(database, "Change Sound Clip");
            data.m_clip = newClip;
            SoundToolDatabaseProvider.MarkDirty(database);
        }

        SoundCategory newCategory = (SoundCategory)EditorGUILayout.EnumPopup("カテゴリー", data.m_category);

        if (newCategory != data.m_category)
        {
            Undo.RecordObject(database, "Change Sound Category");
            data.m_category = newCategory;
            SoundToolDatabaseProvider.MarkDirty(database);
        }

        float newPitch = EditorGUILayout.Slider("ピッチ", data.m_pitch, MIN_PITCH, MAX_PITCH);

        if (!Mathf.Approximately(newPitch, data.m_pitch))
        {
            Undo.RecordObject(database, "Change Sound Pitch");
            data.m_pitch = newPitch;
            SoundToolDatabaseProvider.MarkDirty(database);
        }

        EditorGUILayout.BeginHorizontal();
        bool newLoop = EditorGUILayout.Toggle("ループ", data.m_loop);
        bool newIs3D = EditorGUILayout.Toggle("3D 音源", data.m_is3D);
        EditorGUILayout.EndHorizontal();

        if (newLoop != data.m_loop)
        {
            Undo.RecordObject(database, "Change Sound Loop");
            data.m_loop = newLoop;
            SoundToolDatabaseProvider.MarkDirty(database);
        }

        if (newIs3D != data.m_is3D)
        {
            Undo.RecordObject(database, "Change Sound 3D");
            data.m_is3D = newIs3D;
            SoundToolDatabaseProvider.MarkDirty(database);
        }

        if (data.m_is3D)
        {
            EditorGUI.indentLevel++;

            AudioRolloffMode newRolloff = (AudioRolloffMode)EditorGUILayout.EnumPopup("距離減衰", data.m_rolloffMode);

            if (newRolloff != data.m_rolloffMode)
            {
                Undo.RecordObject(database, "Change Sound Rolloff");
                data.m_rolloffMode = newRolloff;
                SoundToolDatabaseProvider.MarkDirty(database);
            }

            float newMinDistance = Mathf.Max(MIN_DISTANCE_LIMIT, EditorGUILayout.FloatField("最小距離", data.m_minDistance));

            if (!Mathf.Approximately(newMinDistance, data.m_minDistance))
            {
                Undo.RecordObject(database, "Change Sound MinDistance");
                data.m_minDistance = newMinDistance;
                data.m_maxDistance = Mathf.Max(data.m_minDistance + DISTANCE_MARGIN, data.m_maxDistance);
                SoundToolDatabaseProvider.MarkDirty(database);
            }

            if (data.m_rolloffMode != AudioRolloffMode.Logarithmic)
            {
                float newMaxDistance = Mathf.Max(data.m_minDistance + DISTANCE_MARGIN, EditorGUILayout.FloatField("最大距離", data.m_maxDistance));

                if (!Mathf.Approximately(newMaxDistance, data.m_maxDistance))
                {
                    Undo.RecordObject(database, "Change Sound MaxDistance");
                    data.m_maxDistance = newMaxDistance;
                    SoundToolDatabaseProvider.MarkDirty(database);
                }
            }

            if (data.m_rolloffMode == AudioRolloffMode.Custom)
            {
                AnimationCurve sourceCurve = data.m_customRolloffCurve ?? new AnimationCurve();
                AnimationCurve editCurve = new AnimationCurve(sourceCurve.keys);
                EditorGUI.BeginChangeCheck();
                AnimationCurve updatedCurve = EditorGUILayout.CurveField("カスタムカーブ", editCurve);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(database, "Change Sound Rolloff Curve");
                    data.m_customRolloffCurve = updatedCurve;
                    SoundToolDatabaseProvider.MarkDirty(database);
                }
            }

            EditorGUI.indentLevel--;
        }
    }

    /// <summary>
    /// 詳細編集パネルを閉じます。
    /// </summary>
    private void CloseDetailPanel()
    {
        m_focusedRowKey = "";
        m_focusedData = null;
        m_focusedGroup = null;
    }
}

#endif