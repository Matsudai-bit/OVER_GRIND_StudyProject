#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// AudioID の追加と削除を行う画面です。追加と削除を同じ画面に並べています。
/// </summary>
public sealed class SoundToolIdManageView
{
    // ドロップ領域の高さ
    private const float DROP_AREA_HEIGHT = 56.0f;

    // 行番号欄の幅
    private const float ROW_INDEX_WIDTH = 24.0f;

    // 行削除ボタンの幅
    private const float REMOVE_BUTTON_WIDTH = 24.0f;

    // カテゴリー選択欄の幅
    private const float CATEGORY_POPUP_WIDTH = 130.0f;

    // 音量ラベルの幅
    private const float VOLUME_LABEL_WIDTH = 32.0f;

    // 詳細ボタンの幅
    private const float DETAIL_BUTTON_WIDTH = 60.0f;

    // 削除一覧の最大高さ
    private const float DELETE_LIST_MAX_HEIGHT = 260.0f;

    // 削除一覧の ID 名欄の幅
    private const float DELETE_NAME_WIDTH = 330.0f;

    // カテゴリータグの幅
    private const float CATEGORY_TAG_WIDTH = 100.0f;

    // 削除確認ダイアログに表示する ID 名の最大件数
    private const int DELETE_DIALOG_PREVIEW_COUNT = 8;

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

    // エラー表示の色
    private static readonly Color ERROR_COLOR = new Color(1.0f, 0.4f, 0.4f);

    // 補足表示の色
    private static readonly Color HINT_COLOR = new Color(0.7f, 0.7f, 0.7f);

    // 削除ボタンの色
    private static readonly Color DELETE_BUTTON_COLOR = new Color(1.0f, 0.5f, 0.5f);

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

    // 削除対象に選択した ID 名
    private readonly HashSet<string> m_deleteSelectedNames = new HashSet<string>(StringComparer.Ordinal);

    // 追加時の初期カテゴリー
    private SoundCategory m_defaultCategory = SoundCategory.BGM;

    // 画面全体のスクロール位置
    private Vector2 m_scrollPosition = Vector2.zero;

    // 削除一覧のスクロール位置
    private Vector2 m_deleteScrollPosition = Vector2.zero;

    // 追加欄を開いているか
    private bool m_isAddFoldout = true;

    // 削除欄を開いているか
    private bool m_isDeleteFoldout = true;

    // 削除一覧の検索文字列
    private string m_deleteSearchText = "";

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
    /// ID 管理画面を描画します。
    /// </summary>
    public void Draw()
    {
        SoundDatabase database = SoundToolDatabaseProvider.GetDatabase();

        if (database == null)
        {
            SoundToolGui.DrawMissingDatabase();
            return;
        }

        m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);

        DrawAddSection(database);
        SoundToolGui.DrawSeparator();
        DrawDeleteSection(database);

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// ID 追加欄を描画します。
    /// </summary>
    private void DrawAddSection(SoundDatabase database)
    {
        m_isAddFoldout = EditorGUILayout.Foldout(
            m_isAddFoldout,
            "ID の追加（AudioClip をドロップして一括登録）",
            true,
            EditorStyles.foldoutHeader);

        if (!m_isAddFoldout)
        {
            return;
        }

        DrawDropArea();

        EditorGUILayout.BeginHorizontal();

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

        DrawRegisterButton(database);
    }

    /// <summary>
    /// 追加予定の1行を描画します。
    /// </summary>
    private bool DrawAddRow(AddItem item, int rowIndex)
    {
        bool isRemoveRequested = false;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label((rowIndex + 1).ToString(), GUILayout.Width(ROW_INDEX_WIDTH));

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
        GUILayout.Space(ROW_INDEX_WIDTH);

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
        string errorMessage = GetRowErrorMessage(item, rowIndex);

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
    private string GetRowErrorMessage(AddItem item, int rowIndex)
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
        EditorGUILayout.Space(4.0f);

        bool hasError = false;

        for (int i = 0; i < m_addList.Count; i++)
        {
            if (!string.IsNullOrEmpty(GetRowErrorMessage(m_addList[i], i)))
            {
                hasError = true;
                break;
            }
        }

        using (new EditorGUI.DisabledScope(m_addList.Count == 0 || hasError))
        {
            if (GUILayout.Button($"{m_addList.Count} 件を一括追加", GUILayout.Height(32.0f)))
            {
                RegisterAddItems(database);
            }
        }

        if (hasError)
        {
            SoundToolGui.DrawColoredMiniLabel("エラーのある行を修正すると追加できます。", ERROR_COLOR);
        }
    }

    /// <summary>
    /// 追加予定の内容を SoundDatabase と AudioID.cs へ登録します。
    /// </summary>
    private void RegisterAddItems(SoundDatabase database)
    {
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
        GUI.Label(dropArea, "AudioClip をここにドラッグ＆ドロップ\n（複数選択に対応）", EditorStyles.centeredGreyMiniLabel);

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
    /// ID 削除欄を描画します。
    /// </summary>
    private void DrawDeleteSection(SoundDatabase database)
    {
        m_isDeleteFoldout = EditorGUILayout.Foldout(m_isDeleteFoldout, "ID の削除", true, EditorStyles.foldoutHeader);

        if (!m_isDeleteFoldout)
        {
            return;
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("検索", GUILayout.Width(32.0f));
        m_deleteSearchText = EditorGUILayout.TextField(m_deleteSearchText, EditorStyles.toolbarSearchField);
        EditorGUILayout.EndHorizontal();

        List<SoundToolIdRegistry.IdEntry> visibleEntries = BuildVisibleDeleteEntries();

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
    /// 削除候補の一覧を描画します。
    /// </summary>
    private void DrawDeleteList(SoundDatabase database, List<SoundToolIdRegistry.IdEntry> visibleEntries)
    {
        m_deleteScrollPosition = EditorGUILayout.BeginScrollView(
            m_deleteScrollPosition,
            EditorStyles.helpBox,
            GUILayout.MaxHeight(DELETE_LIST_MAX_HEIGHT));

        foreach (SoundToolIdRegistry.IdEntry entry in visibleEntries)
        {
            EditorGUILayout.BeginHorizontal();

            bool isSelected = m_deleteSelectedNames.Contains(entry.Name);
            bool newSelected = EditorGUILayout.ToggleLeft(entry.Name, isSelected, GUILayout.Width(DELETE_NAME_WIDTH));

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

            SoundDatabase.SoundData data = database.GetSoundData((AudioID)entry.Value);

            if (data != null)
            {
                SoundToolGui.DrawCategoryTag(data.m_category, CATEGORY_TAG_WIDTH);
                EditorGUILayout.LabelField(data.m_clip != null ? data.m_clip.name : "Clip 未設定", EditorStyles.miniLabel);
            }
            else
            {
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
            if (shownCount >= DELETE_DIALOG_PREVIEW_COUNT)
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
}

#endif