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
/// AudioIDの追加・削除とSoundDatabaseの編集を行うEditorWindowです。
/// </summary>
/// @ name :: Aoki Hayate
/// @ date :: 2026/09/13
public sealed class SoundIDGeneratorWindow : EditorWindow
{
    private const string PENDING_AUDIO_PACKAGE_KEY =
        "PendingAudio_BatchPackage";

    private const string NEXT_AUDIO_ID_PREFIX =
        "// Next AudioID Value: ";

    private const string DEFAULT_AUDIO_ID_PATH =
        "Assets/AudioID.cs";

    private const float WINDOW_MIN_WIDTH = 450.0f;

    private const float WINDOW_MIN_HEIGHT = 750.0f;

    /// <summary>
    /// 新規追加するサウンド情報です。
    /// </summary>
    [Serializable]
    private sealed class AddItem
    {
        public string idName = "";
        public AudioClip clip = null;
        public bool is3D = false;
        public bool loop = false;
    }

    /// <summary>
    /// スクリプトリロード後にSoundDatabaseへ登録する情報です。
    /// </summary>
    [Serializable]
    private sealed class PendingEntry
    {
        public string idName;
        public string clipPath;
        public bool is3D;
        public bool loop;
    }

    /// <summary>
    /// スクリプトリロード後に処理するサウンド情報一覧です。
    /// </summary>
    [Serializable]
    private sealed class PendingPackage
    {
        public List<PendingEntry> entries =
            new List<PendingEntry>();
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
        public AudioIDEntry(
            string name,
            int value)
        {
            Name = name;
            Value = value;
        }
    }

    // 新規追加するサウンド一覧
    private readonly List<AddItem> m_addList =
        new List<AddItem>();

    // 削除対象の選択インデックス
    private int m_deleteSelectedIndex = 0;

    // 試聴対象の選択インデックス
    private int m_previewSelectedIndex = 0;

    // 更新対象の選択インデックス
    private int m_updateSelectedIndex = 0;

    // 更新対象のAudioClip
    private AudioClip m_updateClip = null;

    // 更新対象が3D音源か
    private bool m_updateIs3D = false;

    // 更新対象がループ再生か
    private bool m_updateLoop = false;

    // 最後に選択した更新対象ID
    private string m_lastUpdateSelectedID = "";

    /// <summary>
    /// AudioID編集ウィンドウを開きます。
    /// </summary>
    [MenuItem("Tools/Audio/AudioID 編集・追加ツール")]
    public static void ShowWindow()
    {
        SoundIDGeneratorWindow window =
            GetWindow<SoundIDGeneratorWindow>(
                "AudioID 編集");

        window.minSize =
            new Vector2(
                WINDOW_MIN_WIDTH,
                WINDOW_MIN_HEIGHT);
    }

    private void OnEnable()
    {
        if (m_addList.Count == 0)
        {
            m_addList.Add(
                new AddItem());
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

        DrawSeparator();

        string[] currentNames =
            Enum.GetNames(typeof(AudioID));

        DrawPreviewSection(currentNames);

        DrawSeparator();

        DrawUpdateSection(currentNames);

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

        GUILayout.FlexibleSpace();

        if (GUILayout.Button(
                "+ 行を追加",
                GUILayout.Width(100.0f),
                GUILayout.Height(24.0f)))
        {
            m_addList.Add(
                new AddItem());
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5.0f);

        int removeIndex = -1;

        for (int i = 0;
             i < m_addList.Count;
             i++)
        {
            AddItem item =
                m_addList[i];

            EditorGUILayout.BeginHorizontal(
                EditorStyles.helpBox);

            EditorGUILayout.BeginVertical(
                GUILayout.Width(120.0f));

            EditorGUILayout.LabelField(
                "識別子 (ID):",
                EditorStyles.miniLabel);

            item.idName =
                EditorGUILayout.TextField(
                    item.idName);

            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();

            EditorGUILayout.LabelField(
                "AudioClip:",
                EditorStyles.miniLabel);

            item.clip =
                (AudioClip)EditorGUILayout.ObjectField(
                    item.clip,
                    typeof(AudioClip),
                    false);

            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(
                GUILayout.Width(90.0f));

            item.is3D =
                EditorGUILayout.ToggleLeft(
                    "3D音源",
                    item.is3D);

            item.loop =
                EditorGUILayout.ToggleLeft(
                    "ループ",
                    item.loop);

            EditorGUILayout.EndVertical();

            if (m_addList.Count > 1)
            {
                EditorGUILayout.BeginVertical(
                    GUILayout.Width(25.0f));

                EditorGUILayout.Space(10.0f);

                if (GUILayout.Button(
                        "×",
                        GUILayout.Width(22.0f),
                        GUILayout.Height(20.0f)))
                {
                    removeIndex = i;
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndHorizontal();
        }

        if (removeIndex >= 0)
        {
            m_addList.RemoveAt(
                removeIndex);
        }

        EditorGUILayout.Space(8.0f);

        if (GUILayout.Button(
                "一括追加",
                GUILayout.Height(35.0f)))
        {
            BatchAddProcess();
        }
    }

    /// <summary>
    /// サウンド試聴UIを描画します。
    /// </summary>
    private void DrawPreviewSection(
        string[] currentNames)
    {
        EditorGUILayout.LabelField(
            "サウンド試聴",
            EditorStyles.boldLabel);

        if (currentNames.Length == 0)
        {
            EditorGUILayout.HelpBox(
                "登録されているサウンドIDがありません。",
                MessageType.Info);

            return;
        }

        m_previewSelectedIndex =
            Mathf.Clamp(
                m_previewSelectedIndex,
                0,
                currentNames.Length - 1);

        EditorGUILayout.BeginHorizontal(
            EditorStyles.helpBox);

        int newPreviewIndex =
            EditorGUILayout.Popup(
                "試聴するID:",
                m_previewSelectedIndex,
                currentNames);

        if (newPreviewIndex !=
            m_previewSelectedIndex)
        {
            m_previewSelectedIndex =
                newPreviewIndex;

            PlaySelectedSound(
                currentNames[
                    m_previewSelectedIndex]);
        }

        if (GUILayout.Button(
                "再生",
                GUILayout.Width(60.0f),
                GUILayout.Height(20.0f)))
        {
            PlaySelectedSound(
                currentNames[
                    m_previewSelectedIndex]);
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
    /// 登録済みサウンド更新UIを描画します。
    /// </summary>
    private void DrawUpdateSection(
        string[] currentNames)
    {
        EditorGUILayout.LabelField(
            "登録済みデータの確認・更新",
            EditorStyles.boldLabel);

        if (currentNames.Length == 0)
        {
            return;
        }

        m_updateSelectedIndex =
            Mathf.Clamp(
                m_updateSelectedIndex,
                0,
                currentNames.Length - 1);

        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox);

        int newUpdateIndex =
            EditorGUILayout.Popup(
                "対象のID:",
                m_updateSelectedIndex,
                currentNames);

        string selectedID =
            currentNames[newUpdateIndex];

        if (newUpdateIndex !=
                m_updateSelectedIndex ||
            selectedID !=
                m_lastUpdateSelectedID)
        {
            m_updateSelectedIndex =
                newUpdateIndex;

            m_lastUpdateSelectedID =
                selectedID;

            LoadSoundDataForUpdate(
                m_lastUpdateSelectedID);
        }

        m_updateClip =
            (AudioClip)EditorGUILayout.ObjectField(
                "AudioClip:",
                m_updateClip,
                typeof(AudioClip),
                false);

        m_updateIs3D =
            EditorGUILayout.Toggle(
                "3D音源:",
                m_updateIs3D);

        m_updateLoop =
            EditorGUILayout.Toggle(
                "ループ:",
                m_updateLoop);

        EditorGUILayout.Space(5.0f);

        if (GUILayout.Button(
                "選択中のサウンドを更新",
                GUILayout.Height(25.0f)))
        {
            UpdateExistingSound(
                currentNames[
                    m_updateSelectedIndex]);
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// AudioID削除UIを描画します。
    /// </summary>
    private void DrawDeleteSection(
        string[] currentNames)
    {
        EditorGUILayout.LabelField(
            "識別子の削除",
            EditorStyles.boldLabel);

        if (currentNames.Length == 0)
        {
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

        if (GUILayout.Button(
                "削除",
                GUILayout.Height(25.0f)))
        {
            DeleteID(
                currentNames[
                    m_deleteSelectedIndex]);
        }
    }

    /// <summary>
    /// UI区切り線を描画します。
    /// </summary>
    private void DrawSeparator()
    {
        EditorGUILayout.Space(15.0f);

        Rect lineRect =
            EditorGUILayout.GetControlRect(
                false,
                1.0f);

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
    /// 指定したAudioIDのサウンドを試聴します。
    /// </summary>
    private void PlaySelectedSound(
        string idName)
    {
        StopAllPreviewClips();

        if (idName ==
            nameof(AudioID.None))
        {
            return;
        }

        SoundDatabase database =
            GetDatabaseStatic();

        if (database == null)
        {
            Debug.LogWarning(
                "[SoundIDGeneratorWindow] " +
                "SoundDatabaseが見つかりません。");

            return;
        }

        if (!Enum.TryParse(
                idName,
                out AudioID targetID))
        {
            return;
        }

        SoundDatabase.SoundData data =
            database.GetSoundData(
                targetID);

        if (data == null ||
            data.m_clip == null)
        {
            Debug.LogWarning(
                $"[SoundIDGeneratorWindow] " +
                $"{idName} に対応するAudioClipが設定されていません。");

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
        List<AudioIDEntry> idEntries =
            GetCurrentAudioIDEntries();

        HashSet<string> existingNames =
            new HashSet<string>(
                StringComparer.Ordinal);

        foreach (AudioIDEntry entry
                 in idEntries)
        {
            existingNames.Add(
                entry.Name);
        }

        int nextAudioIDValue =
            GetNextAudioIDValue(
                idEntries);

        PendingPackage package =
            new PendingPackage();

        for (int i = 0;
             i < m_addList.Count;
             i++)
        {
            AddItem item =
                m_addList[i];

            if (string.IsNullOrWhiteSpace(
                    item.idName))
            {
                Debug.LogWarning(
                    "[SoundIDGeneratorWindow] " +
                    "AudioID名を入力してください。");

                return;
            }

            string sanitized =
                Regex.Replace(
                    item.idName.Trim(),
                    @"[^\w]",
                    "");

            if (!IsValidIdentifier(
                    sanitized))
            {
                Debug.LogWarning(
                    "[SoundIDGeneratorWindow] " +
                    $"「{item.idName}」は有効なAudioID名ではありません。");

                return;
            }

            if (existingNames.Contains(
                    sanitized))
            {
                Debug.LogWarning(
                    "[SoundIDGeneratorWindow] " +
                    $"AudioID「{sanitized}」は既に存在します。");

                return;
            }

            if (item.clip == null)
            {
                Debug.LogWarning(
                    "[SoundIDGeneratorWindow] " +
                    $"AudioID「{sanitized}」にAudioClipが設定されていません。");

                return;
            }

            existingNames.Add(
                sanitized);

            idEntries.Add(
                new AudioIDEntry(
                    sanitized,
                    nextAudioIDValue));

            nextAudioIDValue++;

            package.entries.Add(
                new PendingEntry
                {
                    idName = sanitized,
                    clipPath =
                        AssetDatabase.GetAssetPath(
                            item.clip),
                    is3D = item.is3D,
                    loop = item.loop
                });
        }

        SessionState.SetString(
            PENDING_AUDIO_PACKAGE_KEY,
            JsonUtility.ToJson(
                package));

        GenerateEnumFile(
            idEntries,
            nextAudioIDValue);

        m_addList.Clear();

        m_addList.Add(
            new AddItem());

        AssetDatabase.Refresh();
    }

    /// <summary>
    /// スクリプトリロード後にSoundDatabaseへ
    /// 追加予定のサウンドを反映します。
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

        SessionState.EraseString(
            PENDING_AUDIO_PACKAGE_KEY);

        PendingPackage package =
            JsonUtility.FromJson<PendingPackage>(
                json);

        if (package == null)
        {
            return;
        }

        SoundDatabase database =
            GetDatabaseStatic();

        if (database == null)
        {
            Debug.LogWarning(
                "[SoundIDGeneratorWindow] " +
                "SoundDatabaseが見つかりません。");

            return;
        }

        Undo.RecordObject(
            database,
            "Batch Add Sounds");

        foreach (PendingEntry entry
                 in package.entries)
        {
            AudioClip clip =
                AssetDatabase.LoadAssetAtPath<AudioClip>(
                    entry.clipPath);

            if (!Enum.TryParse(
                    entry.idName,
                    out AudioID targetID))
            {
                Debug.LogWarning(
                    "[SoundIDGeneratorWindow] " +
                    $"AudioID「{entry.idName}」を取得できませんでした。");

                continue;
            }

            bool exists = false;

            for (int i = 0;
                 i < database.m_soundList.Count;
                 i++)
            {
                if (database.m_soundList[i].m_id !=
                    targetID)
                {
                    continue;
                }

                database.m_soundList[i].m_clip =
                    clip;

                database.m_soundList[i].m_is3D =
                    entry.is3D;

                database.m_soundList[i].m_loop =
                    entry.loop;

                exists = true;

                break;
            }

            if (exists)
            {
                continue;
            }

            database.m_soundList.Add(
                new SoundDatabase.SoundData
                {
                    m_id = targetID,
                    m_clip = clip,
                    m_is3D = entry.is3D,
                    m_loop = entry.loop,
                    m_volume = 1.0f,
                    m_pitch = 1.0f
                });
        }

        EditorUtility.SetDirty(
            database);

        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// SoundDatabaseを取得します。
    /// </summary>
    private static SoundDatabase GetDatabaseStatic()
    {
        string[] guids =
            AssetDatabase.FindAssets(
                "t:SoundDatabase");

        if (guids.Length == 0)
        {
            return null;
        }

        string path =
            AssetDatabase.GUIDToAssetPath(
                guids[0]);

        return AssetDatabase.LoadAssetAtPath<SoundDatabase>(
            path);
    }

    /// <summary>
    /// 指定したAudioIDを削除します。
    /// </summary>
    private void DeleteID(
        string targetID)
    {
        if (targetID ==
            nameof(AudioID.None))
        {
            Debug.LogWarning(
                "[SoundIDGeneratorWindow] " +
                "Noneは削除できません。");

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
                    soundData.m_id.ToString() ==
                    targetID);

            EditorUtility.SetDirty(
                database);

            AssetDatabase.SaveAssets();
        }

        List<AudioIDEntry> idEntries =
            GetCurrentAudioIDEntries();

        // 削除対象を含んだ状態で次の番号を取得することで、
        // 削除された番号が再利用されることを防ぎます。
        int nextAudioIDValue =
            GetNextAudioIDValue(
                idEntries);

        idEntries.RemoveAll(
            entry =>
                entry.Name == targetID);

        GenerateEnumFile(
            idEntries,
            nextAudioIDValue);

        AssetDatabase.Refresh();
    }

    /// <summary>
    /// AudioIDのenumファイルを生成します。
    /// </summary>
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

        builder.AppendLine(
            "public enum AudioID");

        builder.AppendLine(
            "{");

        builder.AppendLine(
            "    None = 0,");

        foreach (AudioIDEntry entry
                 in idEntries)
        {
            if (entry.Name ==
                    nameof(AudioID.None) ||
                string.IsNullOrWhiteSpace(
                    entry.Name))
            {
                continue;
            }

            builder.AppendLine(
                $"    {entry.Name} = {entry.Value},");
        }

        builder.AppendLine(
            "}");

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
    private static List<AudioIDEntry>
        GetCurrentAudioIDEntries()
    {
        List<AudioIDEntry> entries =
            new List<AudioIDEntry>();

        string[] names =
            Enum.GetNames(
                typeof(AudioID));

        foreach (string name
                 in names)
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
    private static int GetNextAudioIDValue(
        IReadOnlyList<AudioIDEntry> entries)
    {
        int maxValue = 0;

        foreach (AudioIDEntry entry
                 in entries)
        {
            if (entry.Value >
                maxValue)
            {
                maxValue =
                    entry.Value;
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

        foreach (string line
                 in lines)
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
    private static string GetAudioIDFilePath()
    {
        string[] guids =
            AssetDatabase.FindAssets(
                "AudioID t:MonoScript");

        foreach (string guid
                 in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid);

            string fileName =
                Path.GetFileNameWithoutExtension(
                    path);

            if (fileName ==
                nameof(AudioID))
            {
                return path;
            }
        }

        return DEFAULT_AUDIO_ID_PATH;
    }

    /// <summary>
    /// 文字列がC#の識別子として使用可能か確認します。
    /// </summary>
    private static bool IsValidIdentifier(
        string identifier)
    {
        if (string.IsNullOrWhiteSpace(
                identifier))
        {
            return false;
        }

        if (!(char.IsLetter(
                  identifier[0]) ||
              identifier[0] == '_'))
        {
            return false;
        }

        for (int i = 1;
             i < identifier.Length;
             i++)
        {
            char character =
                identifier[i];

            if (char.IsLetterOrDigit(
                    character) ||
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

        method?.Invoke(
            null,
            null);
    }

    /// <summary>
    /// 更新対象のサウンド情報をSoundDatabaseから読み込みます。
    /// </summary>
    private void LoadSoundDataForUpdate(
        string idName)
    {
        if (idName ==
            nameof(AudioID.None))
        {
            m_updateClip = null;
            m_updateIs3D = false;
            m_updateLoop = false;

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
            database.m_soundList.Find(
                soundData =>
                    soundData.m_id ==
                    targetID);

        if (data == null)
        {
            m_updateClip = null;
            m_updateIs3D = false;
            m_updateLoop = false;

            return;
        }

        m_updateClip =
            data.m_clip;

        m_updateIs3D =
            data.m_is3D;

        m_updateLoop =
            data.m_loop;
    }

    /// <summary>
    /// 選択中のサウンド情報を更新します。
    /// </summary>
    private void UpdateExistingSound(
        string idName)
    {
        if (idName ==
            nameof(AudioID.None))
        {
            return;
        }

        SoundDatabase database =
            GetDatabaseStatic();

        if (database == null)
        {
            Debug.LogWarning(
                "[SoundIDGeneratorWindow] " +
                "SoundDatabaseが見つかりません。");

            return;
        }

        if (!Enum.TryParse(
                idName,
                out AudioID targetID))
        {
            return;
        }

        SoundDatabase.SoundData data =
            database.m_soundList.Find(
                soundData =>
                    soundData.m_id ==
                    targetID);

        if (data == null)
        {
            Debug.LogWarning(
                "[SoundIDGeneratorWindow] " +
                $"AudioID「{idName}」に対応するデータが見つかりません。");

            return;
        }

        Undo.RecordObject(
            database,
            "Update Sound Data");

        data.m_clip =
            m_updateClip;

        data.m_is3D =
            m_updateIs3D;

        data.m_loop =
            m_updateLoop;

        EditorUtility.SetDirty(
            database);

        AssetDatabase.SaveAssets();

        Debug.Log(
            "[SoundIDGeneratorWindow] " +
            $"{idName} のサウンド情報を更新しました。");
    }
}

#endif