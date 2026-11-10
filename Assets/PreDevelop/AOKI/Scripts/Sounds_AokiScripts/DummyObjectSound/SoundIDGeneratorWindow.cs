#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// AudioIDの追加・削除とSoundDatabaseの編集を行います。
/// </summary>
public sealed class SoundIDGeneratorWindow : EditorWindow
{
    private const string PENDING_AUDIO_PACKAGE_KEY = "PendingAudio_BatchPackage";
    private const string NEXT_AUDIO_ID_PREFIX = "// Next AudioID Value: ";
    private const string DEFAULT_AUDIO_ID_PATH = "Assets/AudioID.cs";
    private const float WINDOW_MIN_WIDTH = 520.0f;
    private const float WINDOW_MIN_HEIGHT = 780.0f;

    /// <summary>
    /// 新規追加するサウンド情報です。
    /// </summary>
    [Serializable]
    private sealed class AddItem
    {
        public string idName = "";
        public AudioClip clip = null;
        public SoundCategory category = SoundCategory.BGM;
        public bool is3D = false;
        public bool loop = false;
    }

    /// <summary>
    /// スクリプトリロード後に登録するサウンド情報です。
    /// </summary>
    [Serializable]
    private sealed class PendingEntry
    {
        public string idName;
        public string clipPath;
        public SoundCategory category;
        public bool is3D;
        public bool loop;
    }

    /// <summary>
    /// スクリプトリロード後に処理するサウンド一覧です。
    /// </summary>
    [Serializable]
    private sealed class PendingPackage
    {
        public List<PendingEntry> entries = new List<PendingEntry>();
    }

    /// <summary>
    /// AudioIDの名前と固定整数値を保持します。
    /// </summary>
    private sealed class AudioIDEntry
    {
        /// <summary>
        /// AudioID名を取得します。
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// AudioIDの固定整数値を取得します。
        /// </summary>
        public int Value { get; }

        /// <summary>
        /// AudioID情報を生成します。
        /// </summary>
        /// <param name="name">AudioID名。</param>
        /// <param name="value">固定整数値。</param>
        public AudioIDEntry(string name, int value)
        {
            Name = name;
            Value = value;
        }
    }

    // 新規追加するサウンド一覧
    private readonly List<AddItem> m_addList = new List<AddItem>();

    // カテゴリフィルタの選択インデックス。0はすべて
    private int m_updateCategoryFilterIndex = 0;

    // 削除対象の選択インデックス
    private int m_deleteSelectedIndex = 0;

    // 試聴対象の選択インデックス
    private int m_previewSelectedIndex = 0;

    // 更新対象の選択インデックス
    private int m_updateSelectedIndex = 0;

    // 更新対象のAudioClip
    private AudioClip m_updateClip = null;

    // 更新対象のカテゴリ
    private SoundCategory m_updateCategory = SoundCategory.BGM;

    // 更新対象が3D音源か
    private bool m_updateIs3D = false;

    // 更新対象がループ再生か
    private bool m_updateLoop = false;

    // 最後に選択した更新対象ID
    private string m_lastUpdateSelectedID = "";

    // カテゴリー検索結果一覧のスクロール位置
    private Vector2 m_updateSearchScrollPosition = Vector2.zero;

    /// <summary>
    /// AudioID編集ウィンドウを開きます。
    /// </summary>
    [MenuItem("Tools/Audio/AudioID 編集・追加ツール")]
    public static void ShowWindow()
    {
        SoundIDGeneratorWindow window =
            GetWindow<SoundIDGeneratorWindow>("AudioID 編集");

        window.minSize =
            new Vector2(
                WINDOW_MIN_WIDTH,
                WINDOW_MIN_HEIGHT);
    }

    private void OnEnable()
    {
        if (m_addList.Count == 0)
        {
            m_addList.Add(new AddItem());
        }
    }

    private void OnDisable()
    {
        StopAllPreviewClips();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10.0f);

        DrawAddSection();

        string[] currentNames =
            Enum.GetNames(typeof(AudioID));

        DrawSeparator();

        DrawPreviewSection(currentNames);

        DrawSeparator();

        DrawUpdateSection();

        DrawSeparator();

        DrawDeleteSection(currentNames);
    }

    /// <summary>
    /// サウンド追加UIを描画します。
    /// </summary>
    private void DrawAddSection()
    {
        EditorGUILayout.LabelField(
            "識別子 + AudioClip の一括追加",
            EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button(
                "+ 行を追加",
                GUILayout.Width(100.0f),
                GUILayout.Height(24.0f)))
        {
            m_addList.Add(new AddItem());
        }

        using (new EditorGUI.DisabledScope(m_addList.Count == 0))
        {
            if (GUILayout.Button(
                    "最後の行を削除",
                    GUILayout.Width(110.0f),
                    GUILayout.Height(24.0f)))
            {
                m_addList.RemoveAt(m_addList.Count - 1);
            }

            if (GUILayout.Button(
                    "全行削除",
                    GUILayout.Width(90.0f),
                    GUILayout.Height(24.0f)))
            {
                m_addList.Clear();
            }
        }

        GUILayout.FlexibleSpace();

        EditorGUILayout.LabelField(
            $"追加要素数: {m_addList.Count}",
            GUILayout.Width(100.0f));

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5.0f);

        if (m_addList.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "追加する要素がありません。［+ 行を追加］から追加してください。",
                MessageType.Info);
        }

        int removeIndex = -1;

        for (int i = 0; i < m_addList.Count; i++)
        {
            AddItem item = m_addList[i];

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical(GUILayout.Width(130.0f));
            EditorGUILayout.LabelField("識別子 (ID):", EditorStyles.miniLabel);
            item.idName = EditorGUILayout.TextField(item.idName);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("AudioClip:", EditorStyles.miniLabel);
            item.clip =
                (AudioClip)EditorGUILayout.ObjectField(
                    item.clip,
                    typeof(AudioClip),
                    false);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUILayout.Width(120.0f));
            EditorGUILayout.LabelField("カテゴリー:", EditorStyles.miniLabel);
            item.category =
                (SoundCategory)EditorGUILayout.EnumPopup(item.category);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUILayout.Width(85.0f));
            item.is3D = EditorGUILayout.ToggleLeft("3D音源", item.is3D);
            item.loop = EditorGUILayout.ToggleLeft("ループ", item.loop);
            EditorGUILayout.EndVertical();

            if (GUILayout.Button(
                    "×",
                    GUILayout.Width(24.0f),
                    GUILayout.Height(20.0f)))
            {
                removeIndex = i;
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        if (removeIndex >= 0)
        {
            m_addList.RemoveAt(removeIndex);
        }

        EditorGUILayout.Space(8.0f);

        using (new EditorGUI.DisabledScope(m_addList.Count == 0))
        {
            if (GUILayout.Button(
                    "一括追加",
                    GUILayout.Height(35.0f)))
            {
                BatchAddProcess();
            }
        }
    }

    /// <summary>
    /// サウンド試聴UIを描画します。
    /// </summary>
    /// <param name="currentNames">表示対象のAudioID一覧。</param>
    private void DrawPreviewSection(string[] currentNames)
    {
        EditorGUILayout.LabelField(
            "サウンド試聴",
            EditorStyles.boldLabel);

        if (currentNames.Length == 0)
        {
            DrawNoFilteredSoundMessage();
            return;
        }

        m_previewSelectedIndex =
            Mathf.Clamp(
                m_previewSelectedIndex,
                0,
                currentNames.Length - 1);

        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

        int newPreviewIndex =
            EditorGUILayout.Popup(
                "試聴するID:",
                m_previewSelectedIndex,
                currentNames);

        if (newPreviewIndex != m_previewSelectedIndex)
        {
            m_previewSelectedIndex = newPreviewIndex;
            PlaySelectedSound(currentNames[m_previewSelectedIndex]);
        }

        if (GUILayout.Button(
                "再生",
                GUILayout.Width(60.0f),
                GUILayout.Height(20.0f)))
        {
            PlaySelectedSound(currentNames[m_previewSelectedIndex]);
        }

        if (GUILayout.Button(
                "停止",
                GUILayout.Width(60.0f),
                GUILayout.Height(20.0f)))
        {
            StopAllPreviewClips();
        }

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 登録済みサウンドの検索・確認・更新UIを描画します。
    /// </summary>
    private void DrawUpdateSection()
    {
        EditorGUILayout.LabelField(
            "登録済みデータの確認・更新",
            EditorStyles.boldLabel);

        DrawUpdateCategoryFilter();

        string[] currentNames =
            GetFilteredAudioIDNamesForUpdate();

        if (currentNames.Length == 0)
        {
            DrawNoFilteredSoundMessage();
            return;
        }

        DrawUpdateSearchResultList(currentNames);

        EditorGUILayout.Space(8.0f);

        m_updateSelectedIndex =
            Mathf.Clamp(
                m_updateSelectedIndex,
                0,
                currentNames.Length - 1);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        int newUpdateIndex =
            EditorGUILayout.Popup(
                "対象のID:",
                m_updateSelectedIndex,
                currentNames);

        string selectedID =
            currentNames[newUpdateIndex];

        if (newUpdateIndex != m_updateSelectedIndex ||
            selectedID != m_lastUpdateSelectedID)
        {
            m_updateSelectedIndex = newUpdateIndex;
            m_lastUpdateSelectedID = selectedID;
            LoadSoundDataForUpdate(m_lastUpdateSelectedID);
        }

        m_updateClip =
            (AudioClip)EditorGUILayout.ObjectField(
                "AudioClip:",
                m_updateClip,
                typeof(AudioClip),
                false);

        m_updateCategory =
            (SoundCategory)EditorGUILayout.EnumPopup(
                "カテゴリー:",
                m_updateCategory);

        m_updateIs3D =
            EditorGUILayout.Toggle(
                "3D音源:",
                m_updateIs3D);

        m_updateLoop =
            EditorGUILayout.Toggle(
                "ループ:",
                m_updateLoop);

        EditorGUILayout.Space(5.0f);

        using (new EditorGUI.DisabledScope(
                   selectedID == nameof(AudioID.None)))
        {
            if (GUILayout.Button(
                    "選択中のサウンドを更新",
                    GUILayout.Height(25.0f)))
            {
                UpdateExistingSound(selectedID);
            }
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// カテゴリー検索結果を一覧表示します。
    /// </summary>
    /// <param name="currentNames">検索結果のAudioID一覧。</param>
    private void DrawUpdateSearchResultList(string[] currentNames)
    {
        SoundDatabase database =
            GetDatabaseStatic();

        if (database == null)
        {
            return;
        }

        EditorGUILayout.LabelField(
            $"検索結果: {currentNames.Length}件",
            EditorStyles.boldLabel);

        m_updateSearchScrollPosition =
            EditorGUILayout.BeginScrollView(
                m_updateSearchScrollPosition,
                GUILayout.MinHeight(100.0f),
                GUILayout.MaxHeight(220.0f));

        foreach (string idName in currentNames)
        {
            if (!Enum.TryParse(
                    idName,
                    out AudioID audioID))
            {
                continue;
            }

            SoundDatabase.SoundData soundData =
                database.GetSoundData(audioID);

            if (soundData == null)
            {
                continue;
            }

            EditorGUILayout.BeginHorizontal(
                EditorStyles.helpBox);

            EditorGUILayout.LabelField(
                idName,
                GUILayout.Width(170.0f));

            string clipName =
                soundData.m_clip != null
                    ? soundData.m_clip.name
                    : "未設定";

            EditorGUILayout.LabelField(
                clipName,
                GUILayout.MinWidth(150.0f));

            EditorGUILayout.LabelField(
                soundData.m_category.ToString(),
                GUILayout.Width(100.0f));

            if (GUILayout.Button(
                    "選択",
                    GUILayout.Width(55.0f)))
            {
                SelectUpdateSound(
                    idName,
                    currentNames);
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 検索結果一覧から更新対象のサウンドを選択します。
    /// </summary>
    /// <param name="idName">選択するAudioID名。</param>
    /// <param name="currentNames">現在の検索結果一覧。</param>
    private void SelectUpdateSound(
        string idName,
        string[] currentNames)
    {
        int selectedIndex =
            Array.IndexOf(
                currentNames,
                idName);

        if (selectedIndex < 0)
        {
            return;
        }

        m_updateSelectedIndex =
            selectedIndex;

        m_lastUpdateSelectedID =
            idName;

        LoadSoundDataForUpdate(
            idName);

        GUI.FocusControl(null);
        Repaint();
    }

    /// <summary>
    /// AudioID削除UIを描画します。
    /// </summary>
    /// <param name="currentNames">表示対象のAudioID一覧。</param>
    private void DrawDeleteSection(string[] currentNames)
    {
        EditorGUILayout.LabelField(
            "識別子の削除",
            EditorStyles.boldLabel);

        if (currentNames.Length == 0)
        {
            DrawNoFilteredSoundMessage();
            return;
        }

        m_deleteSelectedIndex =
            Mathf.Clamp(
                m_deleteSelectedIndex,
                0,
                currentNames.Length - 1);

        m_deleteSelectedIndex =
            EditorGUILayout.Popup(
                "削除するID:",
                m_deleteSelectedIndex,
                currentNames);

        string selectedID =
            currentNames[m_deleteSelectedIndex];

        using (new EditorGUI.DisabledScope(
                   selectedID == nameof(AudioID.None)))
        {
            if (GUILayout.Button(
                    "削除",
                    GUILayout.Height(25.0f)))
            {
                DeleteID(selectedID);
            }
        }
    }

    /// <summary>
    /// 検索対象が存在しない場合のメッセージを描画します。
    /// </summary>
    private void DrawNoFilteredSoundMessage()
    {
        EditorGUILayout.HelpBox(
            "選択中のカテゴリーに登録されているサウンドがありません。",
            MessageType.Info);
    }

    /// <summary>
    /// UI区切り線を描画します。
    /// </summary>
    private void DrawSeparator()
    {
        EditorGUILayout.Space(15.0f);

        Rect lineRect =
            EditorGUILayout.GetControlRect(false, 1.0f);

        EditorGUI.DrawRect(
            lineRect,
            new Color(
                0.5f,
                0.5f,
                0.5f,
                0.5f));

        EditorGUILayout.Space(15.0f);
    }

    /// <summary>
    /// 登録済みデータ確認用のカテゴリー検索UIを描画します。
    /// </summary>
    private void DrawUpdateCategoryFilter()
    {
        string[] filterNames =
            GetCategoryFilterNames();

        int newFilterIndex =
            EditorGUILayout.Popup(
                "カテゴリー検索:",
                m_updateCategoryFilterIndex,
                filterNames);

        if (newFilterIndex == m_updateCategoryFilterIndex)
        {
            return;
        }

        m_updateCategoryFilterIndex = newFilterIndex;

        // 絞り込み対象が変わるため、選択中データを初期化する。
        m_updateSelectedIndex = 0;
        m_lastUpdateSelectedID = "";
        m_updateClip = null;
        m_updateCategory = SoundCategory.BGM;
        m_updateIs3D = false;
        m_updateLoop = false;
    }

    /// <summary>
    /// カテゴリフィルタ用の表示名を取得します。
    /// </summary>
    /// <returns>カテゴリフィルタ名一覧。</returns>
    private static string[] GetCategoryFilterNames()
    {
        string[] categoryNames =
            Enum.GetNames(typeof(SoundCategory));

        string[] filterNames =
            new string[categoryNames.Length + 1];

        filterNames[0] = "すべて";

        for (int i = 0; i < categoryNames.Length; i++)
        {
            filterNames[i + 1] = categoryNames[i];
        }

        return filterNames;
    }

    /// <summary>
    /// 確認・更新画面で選択しているカテゴリフィルタを取得します。
    /// </summary>
    /// <returns>カテゴリー。すべての場合はnull。</returns>
    private SoundCategory? GetSelectedUpdateCategoryFilter()
    {
        if (m_updateCategoryFilterIndex == 0)
        {
            return null;
        }

        SoundCategory[] categories =
            (SoundCategory[])Enum.GetValues(
                typeof(SoundCategory));

        int categoryIndex =
            m_updateCategoryFilterIndex - 1;

        if (categoryIndex < 0 ||
            categoryIndex >= categories.Length)
        {
            return null;
        }

        return categories[categoryIndex];
    }

    /// <summary>
    /// 確認・更新画面で選択中カテゴリーに該当するAudioID名を取得します。
    /// </summary>
    /// <returns>検索対象のAudioID一覧。</returns>
    private string[] GetFilteredAudioIDNamesForUpdate()
    {
        SoundDatabase database =
            GetDatabaseStatic();

        if (database == null)
        {
            return Array.Empty<string>();
        }

        SoundCategory? categoryFilter =
            GetSelectedUpdateCategoryFilter();

        string[] allNames =
            Enum.GetNames(typeof(AudioID));

        List<string> filteredNames =
            new List<string>();

        foreach (string idName in allNames)
        {
            if (idName == nameof(AudioID.None))
            {
                continue;
            }

            if (!Enum.TryParse(
                    idName,
                    out AudioID audioID))
            {
                continue;
            }

            SoundDatabase.SoundData soundData =
                database.GetSoundData(audioID);

            // SoundDatabaseに登録されていないIDは確認対象に含めない。
            if (soundData == null)
            {
                continue;
            }

            if (categoryFilter.HasValue &&
                soundData.m_category != categoryFilter.Value)
            {
                continue;
            }

            filteredNames.Add(idName);
        }

        return filteredNames.ToArray();
    }

    /// <summary>
    /// 指定したAudioIDのサウンドを試聴します。
    /// </summary>
    /// <param name="idName">AudioID名。</param>
    private void PlaySelectedSound(string idName)
    {
        StopAllPreviewClips();

        if (idName == nameof(AudioID.None))
        {
            return;
        }

        SoundDatabase database =
            GetDatabaseStatic();

        if (database == null)
        {
            Debug.LogWarning(
                "[SoundIDGeneratorWindow] SoundDatabaseが見つかりません。");
            return;
        }

        if (!Enum.TryParse(
                idName,
                out AudioID targetID))
        {
            return;
        }

        SoundDatabase.SoundData data =
            database.GetSoundData(targetID);

        if (data == null ||
            data.m_clip == null)
        {
            Debug.LogWarning(
                $"[SoundIDGeneratorWindow] {idName} に対応するAudioClipが設定されていません。");
            return;
        }

        PlayPreviewClip(
            data.m_clip,
            0,
            data.m_loop);
    }

    /// <summary>
    /// 入力されたサウンドを一括追加します。
    /// </summary>
    private void BatchAddProcess()
    {
        if (m_addList.Count == 0)
        {
            Debug.LogWarning(
                "[SoundIDGeneratorWindow] 追加する要素がありません。");
            return;
        }

        List<AudioIDEntry> idEntries =
            GetCurrentAudioIDEntries();

        HashSet<string> existingNames =
            new HashSet<string>(StringComparer.Ordinal);

        foreach (AudioIDEntry entry in idEntries)
        {
            existingNames.Add(entry.Name);
        }

        int nextAudioIDValue =
            GetNextAudioIDValue(idEntries);

        PendingPackage package =
            new PendingPackage();

        for (int i = 0; i < m_addList.Count; i++)
        {
            AddItem item =
                m_addList[i];

            if (string.IsNullOrWhiteSpace(item.idName))
            {
                Debug.LogWarning(
                    $"[SoundIDGeneratorWindow] {i + 1}行目のAudioID名を入力してください。");
                return;
            }

            string sanitized =
                Regex.Replace(
                    item.idName.Trim(),
                    @"[^\w]",
                    "");

            if (!IsValidIdentifier(sanitized))
            {
                Debug.LogWarning(
                    $"[SoundIDGeneratorWindow] 「{item.idName}」は有効なAudioID名ではありません。");
                return;
            }

            if (existingNames.Contains(sanitized))
            {
                Debug.LogWarning(
                    $"[SoundIDGeneratorWindow] AudioID「{sanitized}」は既に存在します。");
                return;
            }

            if (item.clip == null)
            {
                Debug.LogWarning(
                    $"[SoundIDGeneratorWindow] AudioID「{sanitized}」にAudioClipが設定されていません。");
                return;
            }

            existingNames.Add(sanitized);

            idEntries.Add(
                new AudioIDEntry(
                    sanitized,
                    nextAudioIDValue));

            nextAudioIDValue++;

            package.entries.Add(
                new PendingEntry
                {
                    idName = sanitized,
                    clipPath = AssetDatabase.GetAssetPath(item.clip),
                    category = item.category,
                    is3D = item.is3D,
                    loop = item.loop
                });
        }

        SessionState.SetString(
            PENDING_AUDIO_PACKAGE_KEY,
            JsonUtility.ToJson(package));

        GenerateEnumFile(
            idEntries,
            nextAudioIDValue);

        // 追加完了後は入力要素をすべて削除する
        m_addList.Clear();

        AssetDatabase.Refresh();
    }

    /// <summary>
    /// スクリプトリロード後にSoundDatabaseへ追加予定のサウンドを反映します。
    /// </summary>
    [UnityEditor.Callbacks.DidReloadScripts]
    private static void OnScriptsReloaded()
    {
        string json =
            SessionState.GetString(
                PENDING_AUDIO_PACKAGE_KEY,
                "");

        if (string.IsNullOrEmpty(json))
        {
            return;
        }

        SessionState.EraseString(PENDING_AUDIO_PACKAGE_KEY);

        PendingPackage package =
            JsonUtility.FromJson<PendingPackage>(json);

        if (package == null)
        {
            return;
        }

        SoundDatabase database =
            GetDatabaseStatic();

        if (database == null)
        {
            Debug.LogWarning(
                "[SoundIDGeneratorWindow] SoundDatabaseが見つかりません。");
            return;
        }

        Undo.RecordObject(
            database,
            "Batch Add Sounds");

        foreach (PendingEntry entry in package.entries)
        {
            AudioClip clip =
                AssetDatabase.LoadAssetAtPath<AudioClip>(
                    entry.clipPath);

            if (!Enum.TryParse(
                    entry.idName,
                    out AudioID targetID))
            {
                Debug.LogWarning(
                    $"[SoundIDGeneratorWindow] AudioID「{entry.idName}」を取得できませんでした。");
                continue;
            }

            SoundDatabase.SoundData data =
                database.GetSoundData(targetID);

            if (data == null)
            {
                data =
                    new SoundDatabase.SoundData
                    {
                        m_id = targetID,
                        m_volume = 1.0f,
                        m_pitch = 1.0f
                    };

                database.m_soundList.Add(data);
            }

            data.m_clip = clip;
            data.m_category = entry.category;
            data.m_is3D = entry.is3D;
            data.m_loop = entry.loop;
        }

        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// SoundDatabaseを取得します。
    /// </summary>
    /// <returns>SoundDatabase。見つからない場合はnull。</returns>
    private static SoundDatabase GetDatabaseStatic()
    {
        string[] guids =
            AssetDatabase.FindAssets("t:SoundDatabase");

        if (guids.Length == 0)
        {
            return null;
        }

        string path =
            AssetDatabase.GUIDToAssetPath(guids[0]);

        return AssetDatabase.LoadAssetAtPath<SoundDatabase>(path);
    }

    /// <summary>
    /// 指定したAudioIDを削除します。
    /// </summary>
    /// <param name="targetID">削除対象ID。</param>
    private void DeleteID(string targetID)
    {
        if (targetID == nameof(AudioID.None))
        {
            Debug.LogWarning(
                "[SoundIDGeneratorWindow] Noneは削除できません。");
            return;
        }

        SoundDatabase database =
            GetDatabaseStatic();

        if (database != null)
        {
            Undo.RecordObject(
                database,
                "Delete Sound ID");

            database.m_soundList.RemoveAll(
                soundData =>
                    soundData.m_id.ToString() == targetID);

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
        }

        List<AudioIDEntry> idEntries =
            GetCurrentAudioIDEntries();

        // 削除した番号を新規IDに再利用しない
        int nextAudioIDValue =
            GetNextAudioIDValue(idEntries);

        idEntries.RemoveAll(
            entry => entry.Name == targetID);

        GenerateEnumFile(
            idEntries,
            nextAudioIDValue);

        m_previewSelectedIndex = 0;
        m_updateSelectedIndex = 0;
        m_deleteSelectedIndex = 0;
        m_lastUpdateSelectedID = "";

        AssetDatabase.Refresh();
    }

    /// <summary>
    /// AudioIDのenumファイルを生成します。
    /// </summary>
    /// <param name="idEntries">AudioID一覧。</param>
    /// <param name="nextAudioIDValue">次に使用する固定値。</param>
    private static void GenerateEnumFile(
        IReadOnlyList<AudioIDEntry> idEntries,
        int nextAudioIDValue)
    {
        StringBuilder builder =
            new StringBuilder();

        builder.AppendLine(
            "// 自動生成用ファイルです。直接編集しないでください。");

        builder.AppendLine(
            $"{NEXT_AUDIO_ID_PREFIX}{nextAudioIDValue}");

        builder.AppendLine();
        builder.AppendLine("public enum AudioID");
        builder.AppendLine("{");
        builder.AppendLine("    None = 0,");

        foreach (AudioIDEntry entry in idEntries)
        {
            if (entry.Name == nameof(AudioID.None) ||
                string.IsNullOrWhiteSpace(entry.Name))
            {
                continue;
            }

            builder.AppendLine(
                $"    {entry.Name} = {entry.Value},");
        }

        builder.AppendLine("}");

        string path =
            GetAudioIDFilePath();

        File.WriteAllText(
            path,
            builder.ToString(),
            new UTF8Encoding(true));
    }

    /// <summary>
    /// 現在定義されているAudioIDと固定値を取得します。
    /// </summary>
    /// <returns>AudioID一覧。</returns>
    private static List<AudioIDEntry> GetCurrentAudioIDEntries()
    {
        List<AudioIDEntry> entries =
            new List<AudioIDEntry>();

        string[] names =
            Enum.GetNames(typeof(AudioID));

        foreach (string name in names)
        {
            if (!Enum.TryParse(
                    name,
                    out AudioID audioID))
            {
                continue;
            }

            entries.Add(
                new AudioIDEntry(
                    name,
                    (int)audioID));
        }

        return entries;
    }

    /// <summary>
    /// 次に使用するAudioIDの整数値を取得します。
    /// </summary>
    /// <param name="entries">現在のAudioID一覧。</param>
    /// <returns>次に使用する整数値。</returns>
    private static int GetNextAudioIDValue(
        IReadOnlyList<AudioIDEntry> entries)
    {
        int maxValue = 0;

        foreach (AudioIDEntry entry in entries)
        {
            if (entry.Value > maxValue)
            {
                maxValue = entry.Value;
            }
        }

        int nextValue =
            maxValue + 1;

        string path =
            GetAudioIDFilePath();

        if (!File.Exists(path))
        {
            return nextValue;
        }

        string[] lines =
            File.ReadAllLines(path);

        foreach (string line in lines)
        {
            if (!line.StartsWith(
                    NEXT_AUDIO_ID_PREFIX,
                    StringComparison.Ordinal))
            {
                continue;
            }

            string valueText =
                line.Substring(
                    NEXT_AUDIO_ID_PREFIX.Length);

            if (!int.TryParse(
                    valueText,
                    out int storedNextValue))
            {
                continue;
            }

            return Mathf.Max(
                nextValue,
                storedNextValue);
        }

        return nextValue;
    }

    /// <summary>
    /// AudioID.csのAssetパスを取得します。
    /// </summary>
    /// <returns>AudioID.csのパス。</returns>
    private static string GetAudioIDFilePath()
    {
        string[] guids =
            AssetDatabase.FindAssets("AudioID t:MonoScript");

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guid);

            string fileName =
                Path.GetFileNameWithoutExtension(path);

            if (fileName == nameof(AudioID))
            {
                return path;
            }
        }

        return DEFAULT_AUDIO_ID_PATH;
    }

    /// <summary>
    /// 文字列がC#の識別子として使用可能か確認します。
    /// </summary>
    /// <param name="identifier">確認対象文字列。</param>
    /// <returns>
    /// true：使用可能です。
    /// false：使用できません。
    /// </returns>
    private static bool IsValidIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return false;
        }

        if (!(char.IsLetter(identifier[0]) ||
              identifier[0] == '_'))
        {
            return false;
        }

        for (int i = 1; i < identifier.Length; i++)
        {
            char character =
                identifier[i];

            if (char.IsLetterOrDigit(character) ||
                character == '_')
            {
                continue;
            }

            return false;
        }

        return true;
    }

    /// <summary>
    /// Editor上でAudioClipを再生します。
    /// </summary>
    /// <param name="clip">再生するAudioClip。</param>
    /// <param name="startSample">開始サンプル。</param>
    /// <param name="loop">ループ再生するか。</param>
    private static void PlayPreviewClip(
        AudioClip clip,
        int startSample = 0,
        bool loop = false)
    {
        Assembly unityEditorAssembly =
            typeof(AudioImporter).Assembly;

        Type audioUtilClass =
            unityEditorAssembly.GetType(
                "UnityEditor.AudioUtil");

        if (audioUtilClass == null)
        {
            return;
        }

        MethodInfo method =
            audioUtilClass.GetMethod(
                "PlayPreviewClip",
                BindingFlags.Static |
                BindingFlags.Public,
                null,
                new[]
                {
                    typeof(AudioClip),
                    typeof(int),
                    typeof(bool)
                },
                null);

        method?.Invoke(
            null,
            new object[]
            {
                clip,
                startSample,
                loop
            });
    }

    /// <summary>
    /// Editor上で再生中のAudioClipを停止します。
    /// </summary>
    private static void StopAllPreviewClips()
    {
        Assembly unityEditorAssembly =
            typeof(AudioImporter).Assembly;

        Type audioUtilClass =
            unityEditorAssembly.GetType(
                "UnityEditor.AudioUtil");

        if (audioUtilClass == null)
        {
            return;
        }

        MethodInfo method =
            audioUtilClass.GetMethod(
                "StopAllPreviewClips",
                BindingFlags.Static |
                BindingFlags.Public);

        method?.Invoke(null, null);
    }

    /// <summary>
    /// 更新対象のサウンド情報をSoundDatabaseから読み込みます。
    /// </summary>
    /// <param name="idName">AudioID名。</param>
    private void LoadSoundDataForUpdate(string idName)
    {
        ResetUpdateValues();

        if (idName == nameof(AudioID.None))
        {
            return;
        }

        SoundDatabase database =
            GetDatabaseStatic();

        if (database == null)
        {
            return;
        }

        if (!Enum.TryParse(
                idName,
                out AudioID targetID))
        {
            return;
        }

        SoundDatabase.SoundData data =
            database.GetSoundData(targetID);

        if (data == null)
        {
            return;
        }

        m_updateClip = data.m_clip;
        m_updateCategory = data.m_category;
        m_updateIs3D = data.m_is3D;
        m_updateLoop = data.m_loop;
    }

    /// <summary>
    /// 更新欄の値を初期化します。
    /// </summary>
    private void ResetUpdateValues()
    {
        m_updateClip = null;
        m_updateCategory = SoundCategory.BGM;
        m_updateIs3D = false;
        m_updateLoop = false;
    }

    /// <summary>
    /// 選択中のサウンド情報を更新します。
    /// </summary>
    /// <param name="idName">AudioID名。</param>
    private void UpdateExistingSound(string idName)
    {
        if (idName == nameof(AudioID.None))
        {
            return;
        }

        SoundDatabase database =
            GetDatabaseStatic();

        if (database == null)
        {
            Debug.LogWarning(
                "[SoundIDGeneratorWindow] SoundDatabaseが見つかりません。");
            return;
        }

        if (!Enum.TryParse(
                idName,
                out AudioID targetID))
        {
            return;
        }

        SoundDatabase.SoundData data =
            database.GetSoundData(targetID);

        if (data == null)
        {
            Debug.LogWarning(
                $"[SoundIDGeneratorWindow] AudioID「{idName}」に対応するデータが見つかりません。");
            return;
        }

        Undo.RecordObject(
            database,
            "Update Sound Data");

        data.m_clip = m_updateClip;
        data.m_category = m_updateCategory;
        data.m_is3D = m_updateIs3D;
        data.m_loop = m_updateLoop;

        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();

        Debug.Log(
            $"[SoundIDGeneratorWindow] {idName} のサウンド情報を更新しました。");
    }
}

#endif
