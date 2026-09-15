#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

/// @ className :: SoundIDGeneratorWindow
/// @ name :: Aoki Hayate
/// @ date :: 2026/09/13
public class SoundIDGeneratorWindow : EditorWindow
{
    [Serializable]
    private class AddItem
    {
        public string idName = "";
        public AudioClip clip = null;
        public bool is3D = false;
        public bool loop = false;
    }

    [Serializable]
    private class PendingEntry
    {
        public string idName;
        public string clipPath;
        public bool is3D;
        public bool loop;
    }

    [Serializable]
    private class PendingPackage
    {
        public List<PendingEntry> entries = new List<PendingEntry>();
    }

    private List<AddItem> m_addList = new List<AddItem>();
    private int m_deleteSelectedIndex = 0;
    private int m_previewSelectedIndex = 0;

    // 更新機能用の変数
    private int m_updateSelectedIndex = 0;
    private AudioClip m_updateClip = null;
    private bool m_updateIs3D = false;
    private bool m_updateLoop = false;
    private string m_lastUpdateSelectedID = "";

    [MenuItem("Tools/Audio/SoundID_Aoki 編集・追加ツール")]
    public static void ShowWindow()
    {
        var window = GetWindow<SoundIDGeneratorWindow>("SoundID_Aoki 編集");
        window.minSize = new Vector2(450, 750); // ウィンドウが少し大きくなるよう調整
    }

    private void OnEnable()
    {
        if (m_addList.Count == 0) m_addList.Add(new AddItem());
    }

    private void OnDisable()
    {
        StopAllPreviewClips();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("識別子 ＆ AudioClip の一括追加", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("＋ 行を追加", GUILayout.Width(100), GUILayout.Height(24)))
        {
            m_addList.Add(new AddItem());
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        int removeIndex = -1;
        for (int i = 0; i < m_addList.Count; i++)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            EditorGUILayout.BeginVertical(GUILayout.Width(120));
            EditorGUILayout.LabelField("識別子 (ID):", EditorStyles.miniLabel);
            m_addList[i].idName = EditorGUILayout.TextField(m_addList[i].idName);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("AudioClip:", EditorStyles.miniLabel);
            m_addList[i].clip = (AudioClip)EditorGUILayout.ObjectField(m_addList[i].clip, typeof(AudioClip), false);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUILayout.Width(90));
            m_addList[i].is3D = EditorGUILayout.ToggleLeft("3D音響", m_addList[i].is3D);
            m_addList[i].loop = EditorGUILayout.ToggleLeft("ループ", m_addList[i].loop);
            EditorGUILayout.EndVertical();

            if (m_addList.Count > 1)
            {
                EditorGUILayout.BeginVertical(GUILayout.Width(25));
                EditorGUILayout.Space(10);
                if (GUILayout.Button("×", GUILayout.Width(22), GUILayout.Height(20)))
                {
                    removeIndex = i;
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndHorizontal();
        }

        if (removeIndex != -1) m_addList.RemoveAt(removeIndex);

        EditorGUILayout.Space(8);
        if (GUILayout.Button("一 括 追 加 す る", GUILayout.Height(35)))
        {
            BatchAddProcess();
        }

        DrawSeparator();


        // サウンド試聴
        EditorGUILayout.LabelField("サウンド試聴", EditorStyles.boldLabel);
        string[] currentNames = Enum.GetNames(typeof(SoundID_Aoki));

        if (currentNames.Length > 0)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            int newPreviewIndex = EditorGUILayout.Popup("試聴するID:", m_previewSelectedIndex, currentNames);
            if (newPreviewIndex != m_previewSelectedIndex)
            {
                m_previewSelectedIndex = newPreviewIndex;
                PlaySelectedSound(currentNames[m_previewSelectedIndex]);
            }

            if (GUILayout.Button("再生", GUILayout.Width(60), GUILayout.Height(20)))
            {
                PlaySelectedSound(currentNames[m_previewSelectedIndex]);
            }

            if (GUILayout.Button("停止", GUILayout.Width(60), GUILayout.Height(20)))
            {
                StopAllPreviewClips();
            }

            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.HelpBox("登録されているサウンドIDがありません。", MessageType.Info);
        }

        DrawSeparator();


        // 登録済みデータの確認・更新
        EditorGUILayout.LabelField("登録済みデータの確認・更新", EditorStyles.boldLabel);
        if (currentNames.Length > 0)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            int newUpdateIndex = EditorGUILayout.Popup("対象のID:", m_updateSelectedIndex, currentNames);

            // 選択が切り替わった時にデータベースから現在の設定を読み込む
            if (newUpdateIndex != m_updateSelectedIndex || (newUpdateIndex < currentNames.Length && currentNames[newUpdateIndex] != m_lastUpdateSelectedID))
            {
                m_updateSelectedIndex = newUpdateIndex;
                m_lastUpdateSelectedID = currentNames[m_updateSelectedIndex];
                LoadSoundDataForUpdate(m_lastUpdateSelectedID);
            }

            m_updateClip = (AudioClip)EditorGUILayout.ObjectField("AudioClip:", m_updateClip, typeof(AudioClip), false);
            m_updateIs3D = EditorGUILayout.Toggle("3D音響:", m_updateIs3D);
            m_updateLoop = EditorGUILayout.Toggle("ループ:", m_updateLoop);

            EditorGUILayout.Space(5);
            if (GUILayout.Button("選択中のサウンドを更新", GUILayout.Height(25)))
            {
                UpdateExistingSound(currentNames[m_updateSelectedIndex]);
            }

            EditorGUILayout.EndVertical();
        }

        DrawSeparator();


        // 識別子の削除
        EditorGUILayout.LabelField("識別子の削除", EditorStyles.boldLabel);
        if (currentNames.Length > 0)
        {
            m_deleteSelectedIndex = EditorGUILayout.Popup("削除するID:", m_deleteSelectedIndex, currentNames);
            if (m_deleteSelectedIndex >= currentNames.Length) m_deleteSelectedIndex = 0;

            if (GUILayout.Button("削除", GUILayout.Height(25)))
            {
                DeleteID(currentNames[m_deleteSelectedIndex]);
            }
        }
    }

    private void DrawSeparator()
    {
        EditorGUILayout.Space(15);
        Rect lineRect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(lineRect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
        EditorGUILayout.Space(15);
    }

    private void PlaySelectedSound(string idName)
    {
        StopAllPreviewClips();
        if (idName == "None") return;

        SoundDatabase db = GetDatabaseStatic();
        if (db == null)
        {
            Debug.LogWarning("[SoundIDGeneratorWindow] SoundDatabase が見つかりません。");
            return;
        }

        if (Enum.TryParse<SoundID_Aoki>(idName, out var targetID))
        {
            var data = db.GetSoundData(targetID);
            if (data != null && data.m_clip != null)
            {
                PlayPreviewClip(data.m_clip, 0, data.m_loop);
            }
            else
            {
                Debug.LogWarning($"[SoundIDGeneratorWindow] {idName} に対応する AudioClip が設定されていません。");
            }
        }
    }

    private void BatchAddProcess()
    {
        List<string> existingNames = new List<string>(Enum.GetNames(typeof(SoundID_Aoki)));
        PendingPackage package = new PendingPackage();
        List<string> newIDs = new List<string>(existingNames);

        for (int i = 0; i < m_addList.Count; i++)
        {
            var item = m_addList[i];
            if (string.IsNullOrWhiteSpace(item.idName)) return;

            string sanitized = System.Text.RegularExpressions.Regex.Replace(item.idName.Trim(), @"[^\w]", "");
            if (newIDs.Contains(sanitized)) return;
            if (item.clip == null) return;

            newIDs.Add(sanitized);
            package.entries.Add(new PendingEntry
            {
                idName = sanitized,
                clipPath = AssetDatabase.GetAssetPath(item.clip),
                is3D = item.is3D,
                loop = item.loop
            });
        }

        SessionState.SetString("PendingAudio_BatchPackage", JsonUtility.ToJson(package));
        GenerateEnumFile(newIDs);
        m_addList.Clear();
        m_addList.Add(new AddItem());
        AssetDatabase.Refresh();
    }

    [UnityEditor.Callbacks.DidReloadScripts]
    private static void OnScriptsReloaded()
    {
        string json = SessionState.GetString("PendingAudio_BatchPackage", "");
        if (string.IsNullOrEmpty(json)) return;

        SessionState.EraseString("PendingAudio_BatchPackage");
        PendingPackage package = JsonUtility.FromJson<PendingPackage>(json);
        if (package == null) return;

        SoundDatabase db = GetDatabaseStatic();
        if (db == null) return;

        Undo.RecordObject(db, "Batch Add Sounds");

        foreach (var entry in package.entries)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(entry.clipPath);
            SoundID_Aoki targetID = (SoundID_Aoki)Enum.Parse(typeof(SoundID_Aoki), entry.idName);

            bool exists = false;
            for (int i = 0; i < db.m_soundList.Count; i++)
            {
                if (db.m_soundList[i].m_id == targetID)
                {
                    db.m_soundList[i].m_clip = clip;
                    db.m_soundList[i].m_is3D = entry.is3D;
                    db.m_soundList[i].m_loop = entry.loop;
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                db.m_soundList.Add(new SoundDatabase.SoundData
                {
                    m_id = targetID,
                    m_clip = clip,
                    m_is3D = entry.is3D,
                    m_loop = entry.loop,
                    m_volume = 1.0f,
                    m_pitch = 1.0f
                });
            }
        }

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
    }

    private static SoundDatabase GetDatabaseStatic()
    {
        string[] guids = AssetDatabase.FindAssets("t:SoundDatabase");
        return guids.Length > 0 ? AssetDatabase.LoadAssetAtPath<SoundDatabase>(AssetDatabase.GUIDToAssetPath(guids[0])) : null;
    }

    private void DeleteID(string targetID)
    {
        if (targetID == "None") return;

        SoundDatabase db = GetDatabaseStatic();
        if (db != null)
        {
            Undo.RecordObject(db, "Delete Sound ID");
            db.m_soundList.RemoveAll(x => x.m_id.ToString() == targetID);
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
        }

        List<string> existingNames = new List<string>(Enum.GetNames(typeof(SoundID_Aoki)));
        existingNames.Remove(targetID);
        GenerateEnumFile(existingNames);
        AssetDatabase.Refresh();
    }

    private static void GenerateEnumFile(List<string> idList)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("// 自動生成用ファイルです。直接編集しないでください。");
        sb.AppendLine("public enum SoundID_Aoki");
        sb.AppendLine("{");

        sb.AppendLine("    None = 0,");

        foreach (var id in idList)
        {
            if (id == "None" || string.IsNullOrWhiteSpace(id)) continue;
            sb.AppendLine($"    {id},");
        }
        sb.AppendLine("}");

        string[] guids = AssetDatabase.FindAssets("SoundID_Aoki t:MonoScript");
        string path = guids.Length > 0 ? AssetDatabase.GUIDToAssetPath(guids[0]) : "Assets/SoundID_Aoki.cs";
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    private static void PlayPreviewClip(AudioClip clip, int startSample = 0, bool loop = false)
    {
        Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
        Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
        MethodInfo method = audioUtilClass.GetMethod(
            "PlayPreviewClip",
            BindingFlags.Static | BindingFlags.Public,
            null,
            new Type[] { typeof(AudioClip), typeof(int), typeof(bool) },
            null
        );
        method?.Invoke(null, new object[] { clip, startSample, loop });
    }

    private static void StopAllPreviewClips()
    {
        Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
        Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
        MethodInfo method = audioUtilClass.GetMethod(
            "StopAllPreviewClips",
            BindingFlags.Static | BindingFlags.Public
        );
        method?.Invoke(null, null);
    }

    // 更新用のデータを読み込む処理
    private void LoadSoundDataForUpdate(string idName)
    {
        if (idName == "None")
        {
            m_updateClip = null; m_updateIs3D = false; m_updateLoop = false;
            return;
        }

        SoundDatabase db = GetDatabaseStatic();
        if (db == null) return;

        if (Enum.TryParse<SoundID_Aoki>(idName, out var targetID))
        {
            var data = db.m_soundList.Find(x => x.m_id == targetID);
            if (data != null)
            {
                m_updateClip = data.m_clip;
                m_updateIs3D = data.m_is3D;
                m_updateLoop = data.m_loop;
            }
        }
    }

    // 更新ボタンを押したときの処理
    private void UpdateExistingSound(string idName)
    {
        if (idName == "None") return;

        SoundDatabase db = GetDatabaseStatic();
        if (db == null) return;

        Undo.RecordObject(db, "Update Sound Data");

        if (Enum.TryParse<SoundID_Aoki>(idName, out var targetID))
        {
            var data = db.m_soundList.Find(x => x.m_id == targetID);
            if (data != null)
            {
                data.m_clip = m_updateClip;
                data.m_is3D = m_updateIs3D;
                data.m_loop = m_updateLoop;

                EditorUtility.SetDirty(db);
                AssetDatabase.SaveAssets();
                Debug.Log($"[SoundIDGeneratorWindow] {idName} のサウンド設定を更新しました。");
            }
        }
    }
}
#endif