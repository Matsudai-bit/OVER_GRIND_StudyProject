#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

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
    private int m_selectedIndex = 0;

    [MenuItem("Tools/Audio/SoundID_Aoki 編集・追加ツール")]
    public static void ShowWindow()
    {
        var window = GetWindow<SoundIDGeneratorWindow>("SoundID_Aoki 編集");
        window.minSize = new Vector2(450, 500);
    }

    private void OnEnable()
    {
        if (m_addList.Count == 0) m_addList.Add(new AddItem());
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
                if (GUILayout.Button("X", GUILayout.Width(22), GUILayout.Height(20)))
                {
                    removeIndex = i;
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndHorizontal();
        }

        if (removeIndex != -1) m_addList.RemoveAt(removeIndex);

        EditorGUILayout.Space(8);
        if (GUILayout.Button("一括追加", GUILayout.Height(35)))
        {
            BatchAddProcess();
        }

        EditorGUILayout.Space(15);
        Rect lineRect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(lineRect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
        EditorGUILayout.Space(15);

        EditorGUILayout.LabelField("識別子の削除", EditorStyles.boldLabel);
        string[] currentNames = Enum.GetNames(typeof(SoundID_Aoki));
        if (currentNames.Length > 0)
        {
            m_selectedIndex = EditorGUILayout.Popup("削除するID:", m_selectedIndex, currentNames);
            if (m_selectedIndex >= currentNames.Length) m_selectedIndex = 0;

            if (GUILayout.Button("削除", GUILayout.Height(25)))
            {
                DeleteID(currentNames[m_selectedIndex]);
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
        List<string> existingNames = new List<string>(Enum.GetNames(typeof(SoundID_Aoki)));
        existingNames.Remove(targetID);
        GenerateEnumFile(existingNames);
        AssetDatabase.Refresh();
    }

    private static void GenerateEnumFile(List<string> idList)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("// 自動生成されるファイルです。絶対触るな!!!!");
        sb.AppendLine("public enum SoundID_Aoki");
        sb.AppendLine("{");
        if (!idList.Contains("None")) sb.AppendLine("    None = 0,");
        foreach (var id in idList)
        {
            if (id == "None") continue;
            sb.AppendLine($"    {id},");
        }
        sb.AppendLine("}");

        string[] guids = AssetDatabase.FindAssets("SoundID_Aoki t:MonoScript");
        string path = guids.Length > 0 ? AssetDatabase.GUIDToAssetPath(guids[0]) : "Assets/SoundID_Aoki.cs";
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }
}
#endif