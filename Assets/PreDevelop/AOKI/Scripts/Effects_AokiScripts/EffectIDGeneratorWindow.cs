#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

/// <summary>
/// ParticleSystem付きプレハブ専用：EffectIDの一括追加・個別に編集・データベース登録ツール
/// </summary>
public class EffectIDGeneratorWindow : EditorWindow
{
    // 一括追加用のデータ構造
    [Serializable]
    private class AddItem
    {
        public string idName = "";
        public GameObject prefab = null;
    }

    [Serializable]
    private class PendingEntry
    {
        public string idName;
        public string prefabPath;
    }

    [Serializable]
    private class PendingPackage
    {
        public List<PendingEntry> entries = new List<PendingEntry>();
    }

    private List<AddItem> m_addList = new List<AddItem>();
    private Vector2 m_scrollPos;

    // 個別プレハブ確認・編集用
    private int m_assignIndex = 0;
    private GameObject m_assignPrefab;
    private string m_lastCheckedID = "";
    private int m_selectedIndex = 0;

    [MenuItem("Tools/VFX/EffectID 編集・追加ツール")]
    public static void ShowWindow()
    {
        var window = GetWindow<EffectIDGeneratorWindow>("EffectID 編集");
        window.minSize = new Vector2(400, 500);
    }

    private void OnEnable()
    {
        // 初期状態として1行用意
        if (m_addList.Count == 0)
        {
            m_addList.Add(new AddItem());
        }
        m_lastCheckedID = "";
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);

        //識別子 ＆ プレハブ の一括追加      
        EditorGUILayout.LabelField("識別子 ＆ プレハブ の一括追加", EditorStyles.boldLabel);

        // ＋ 行を追加ボタン
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("＋ 行を追加", GUILayout.Width(100), GUILayout.Height(24)))
        {
            m_addList.Add(new AddItem());
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // 動的な追加リストを表示
        int removeIndex = -1;
        for (int i = 0; i < m_addList.Count; i++)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            //識別子名 入力
            EditorGUILayout.BeginVertical(GUILayout.Width(130));
            EditorGUILayout.LabelField("識別子:", EditorStyles.miniLabel);
            m_addList[i].idName = EditorGUILayout.TextField(m_addList[i].idName);
            EditorGUILayout.EndVertical();

            //プレハブ 選択
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("プレハブ:", EditorStyles.miniLabel);

            EditorGUILayout.BeginHorizontal();
            GUIContent labelContent = new GUIContent(
                m_addList[i].prefab != null ? m_addList[i].prefab.name : "None (Prefab)",
                m_addList[i].prefab != null ? AssetPreview.GetMiniThumbnail(m_addList[i].prefab) : null
            );

            int captureIndex = i;
            if (GUILayout.Button(labelContent, EditorStyles.objectField, GUILayout.Height(20)))
            {
                ParticlePickerWindow.ShowWindow((selected) => {
                    m_addList[captureIndex].prefab = selected;
                    Repaint();
                });
            }
            if (GUILayout.Button("", GUI.skin.GetStyle("ObjectFieldButton"), GUILayout.Width(19), GUILayout.Height(18)))
            {
                ParticlePickerWindow.ShowWindow((selected) => {
                    m_addList[captureIndex].prefab = selected;
                    Repaint();
                });
            }
            EditorGUILayout.EndHorizontal();

            // ドラッグ＆ドロップ受け入れ
            HandleDragAndDrop(GUILayoutUtility.GetLastRect(), ref m_addList[i].prefab);

            EditorGUILayout.EndVertical();

            // 削除 (－) ボタン
            if (m_addList.Count > 1)
            {
                EditorGUILayout.BeginVertical(GUILayout.Width(25));
                EditorGUILayout.Space(14);
                if (GUILayout.Button("X", GUILayout.Width(22), GUILayout.Height(20)))
                {
                    removeIndex = i;
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndHorizontal();
        }

        if (removeIndex != -1)
        {
            m_addList.RemoveAt(removeIndex);
        }

        EditorGUILayout.Space(8);

        // 一括追加するボタン
        if (GUILayout.Button("一 括 追 加 す る", GUILayout.Height(35)))
        {
            BatchAddProcess();
        }

        EditorGUILayout.Space(15);
        Rect lineRect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(lineRect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
        EditorGUILayout.Space(15);

      
        // 登録済みデータの確認・個別更新
        EditorGUILayout.LabelField("登録済みデータの確認・更新", EditorStyles.boldLabel);
        string[] currentNames = Enum.GetNames(typeof(EffectID));

        if (currentNames.Length > 0)
        {
            EditorGUI.BeginChangeCheck();
            m_assignIndex = EditorGUILayout.Popup("対象のID:", m_assignIndex, currentNames);
            if (m_assignIndex >= currentNames.Length) m_assignIndex = 0;
            string currentID = currentNames[m_assignIndex];

            if (EditorGUI.EndChangeCheck() || m_lastCheckedID != currentID)
            {
                m_lastCheckedID = currentID;
                LoadAssignedPrefab(currentID);
                GUI.FocusControl(null);
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            GUIContent singleLabel = new GUIContent(
                m_assignPrefab != null ? m_assignPrefab.name : "None (Particle Prefab)",
                m_assignPrefab != null ? AssetPreview.GetMiniThumbnail(m_assignPrefab) : null
            );

            if (GUILayout.Button(singleLabel, EditorStyles.objectField, GUILayout.Height(20)))
            {
                ParticlePickerWindow.ShowWindow((selected) => { m_assignPrefab = selected; Repaint(); });
            }
            if (GUILayout.Button("", GUI.skin.GetStyle("ObjectFieldButton"), GUILayout.Width(19), GUILayout.Height(18)))
            {
                ParticlePickerWindow.ShowWindow((selected) => { m_assignPrefab = selected; Repaint(); });
            }
            EditorGUILayout.EndHorizontal();
            HandleDragAndDrop(GUILayoutUtility.GetLastRect(), ref m_assignPrefab);

            bool isValidSingle = m_assignPrefab != null && m_assignPrefab.GetComponentInChildren<ParticleSystem>() != null;

            EditorGUILayout.Space(5);
            EditorGUI.BeginDisabledGroup(!isValidSingle);
            if (GUILayout.Button("選択中のプレハブを更新", GUILayout.Height(25)))
            {
                RegisterSinglePrefab(currentID, m_assignPrefab);
            }
            EditorGUI.EndDisabledGroup();
        }

        EditorGUILayout.Space(15);

        //識別子の削除
        EditorGUILayout.LabelField("識別子の削除", EditorStyles.boldLabel);
        if (currentNames.Length > 0)
        {
            m_selectedIndex = EditorGUILayout.Popup("削除するEffect:", m_selectedIndex, currentNames);
            if (m_selectedIndex >= currentNames.Length) m_selectedIndex = 0;

            if (GUILayout.Button("削除", GUILayout.Height(25)))
            {
                DeleteID(currentNames[m_selectedIndex]);
            }
        }
    }

    private void HandleDragAndDrop(Rect dropArea, ref GameObject targetObject)
    {
        Event evt = Event.current;
        if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
        {
            if (!dropArea.Contains(evt.mousePosition)) return;

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                foreach (UnityEngine.Object draggedObject in DragAndDrop.objectReferences)
                {
                    GameObject go = draggedObject as GameObject;
                    if (go != null && go.GetComponentInChildren<ParticleSystem>() != null)
                    {
                        targetObject = go;
                        GUI.changed = true;
                        break;
                    }
                }
            }
            evt.Use();
        }
    }

    private void BatchAddProcess()
    {
        List<string> existingNames = new List<string>(Enum.GetNames(typeof(EffectID)));
        PendingPackage package = new PendingPackage();

        List<string> newIDsToGenerate = new List<string>(existingNames);

        for (int i = 0; i < m_addList.Count; i++)
        {
            var item = m_addList[i];
            if (string.IsNullOrWhiteSpace(item.idName))
            {
                EditorUtility.DisplayDialog("エラー", $"{i + 1} 行目の識別子名が空欄です。", "OK");
                return;
            }

            string sanitizedID = System.Text.RegularExpressions.Regex.Replace(item.idName.Trim(), @"[^\w]", "");

            if (newIDsToGenerate.Contains(sanitizedID))
            {
                EditorUtility.DisplayDialog("エラー", $"'{sanitizedID}' は既に存在するか、重複しています。", "OK");
                return;
            }

            if (item.prefab == null || item.prefab.GetComponentInChildren<ParticleSystem>() == null)
            {
                EditorUtility.DisplayDialog("エラー", $"'{sanitizedID}' に有効なパーティクルプレハブがセットされていません。", "OK");
                return;
            }

            if (!EditorUtility.IsPersistent(item.prefab))
            {
                EditorUtility.DisplayDialog("エラー", $"'{sanitizedID}' のプレハブはProjectウィンドウ内のものを指定してください。", "OK");
                return;
            }

            newIDsToGenerate.Add(sanitizedID);
            package.entries.Add(new PendingEntry
            {
                idName = sanitizedID,
                prefabPath = AssetDatabase.GetAssetPath(item.prefab)
            });
        }

        if (package.entries.Count == 0) return;

        // JSON化してSessionStateに保存（コンパイル後へ引き継ぎ）
        string json = JsonUtility.ToJson(package);
        SessionState.SetString("PendingVFX_BatchPackage", json);

        // Enumの生成とコンパイル開始
        GenerateEnumFile(newIDsToGenerate);

        m_addList.Clear();
        m_addList.Add(new AddItem());
        GUI.FocusControl(null);

        AssetDatabase.Refresh();
    }

    [UnityEditor.Callbacks.DidReloadScripts]
    private static void OnScriptsReloaded()
    {
        string json = SessionState.GetString("PendingVFX_BatchPackage", "");
        if (string.IsNullOrEmpty(json)) return;

        SessionState.EraseString("PendingVFX_BatchPackage");

        PendingPackage package = JsonUtility.FromJson<PendingPackage>(json);
        if (package == null || package.entries.Count == 0) return;

        EffectDatabase db = GetDatabaseStatic();
        if (db == null) return;

        Undo.RecordObject(db, "Batch Add Effects");

        foreach (var entry in package.entries)
        {
            GameObject prefabObj = AssetDatabase.LoadAssetAtPath<GameObject>(entry.prefabPath);
            if (prefabObj == null) continue;

            // プレハブへのEffectNode付与・ID設定
            GameObject contents = PrefabUtility.LoadPrefabContents(entry.prefabPath);
            EffectNode_Aoki node = contents.GetComponent<EffectNode_Aoki>();
            if (node == null) node = contents.AddComponent<EffectNode_Aoki>();

            EffectID targetID = (EffectID)Enum.Parse(typeof(EffectID), entry.idName);
            node.SetEffectID(targetID);

            PrefabUtility.SaveAsPrefabAsset(contents, entry.prefabPath);
            PrefabUtility.UnloadPrefabContents(contents);

            // データベースに書き込み
            GameObject updatedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(entry.prefabPath);
            EffectNode_Aoki finalNode = updatedPrefab.GetComponent<EffectNode_Aoki>();

            bool isUpdated = false;
            for (int i = 0; i < db.m_effectList.Count; i++)
            {
                if (db.m_effectList[i].m_id == targetID)
                {
                    var data = db.m_effectList[i];
                    data.m_prefab = finalNode;
                    db.m_effectList[i] = data;
                    isUpdated = true;
                    break;
                }
            }

            if (!isUpdated)
            {
                db.m_effectList.Add(new EffectDatabase.EffectData { m_id = targetID, m_prefab = finalNode });
            }
        }

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();

        Debug.Log($"[VFXManager] {package.entries.Count} 件の識別子とプレハブを一括登録完了しました！");
    }

    private void LoadAssignedPrefab(string idName)
    {
        m_assignPrefab = null;
        if (idName == "None") return;

        EffectDatabase db = GetDatabaseStatic();
        if (db == null) return;

        try
        {
            EffectID targetID = (EffectID)Enum.Parse(typeof(EffectID), idName);
            foreach (var data in db.m_effectList)
            {
                if (data.m_id == targetID && data.m_prefab != null)
                {
                    m_assignPrefab = data.m_prefab.gameObject;
                    break;
                }
            }
        }
        catch { }
        Repaint();
    }

    private void RegisterSinglePrefab(string idName, GameObject prefabObj)
    {
        if (prefabObj == null || idName == "None") return;
        string path = AssetDatabase.GetAssetPath(prefabObj);

        GameObject contents = PrefabUtility.LoadPrefabContents(path);
        EffectNode_Aoki node = contents.GetComponent<EffectNode_Aoki>();
        if (node == null) node = contents.AddComponent<EffectNode_Aoki>();

        EffectID targetID = (EffectID)Enum.Parse(typeof(EffectID), idName);
        node.SetEffectID(targetID);

        PrefabUtility.SaveAsPrefabAsset(contents, path);
        PrefabUtility.UnloadPrefabContents(contents);

        GameObject updatedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        EffectNode_Aoki finalNode = updatedPrefab.GetComponent<EffectNode_Aoki>();

        EffectDatabase db = GetDatabaseStatic();
        if (db == null) return;

        Undo.RecordObject(db, "Update Single EffectDatabase");

        bool isUpdated = false;
        for (int i = 0; i < db.m_effectList.Count; i++)
        {
            if (db.m_effectList[i].m_id == targetID)
            {
                var data = db.m_effectList[i];
                data.m_prefab = finalNode;
                db.m_effectList[i] = data;
                isUpdated = true;
                break;
            }
        }

        if (!isUpdated)
        {
            db.m_effectList.Add(new EffectDatabase.EffectData { m_id = targetID, m_prefab = finalNode });
        }

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();

        LoadAssignedPrefab(idName);
        EditorUtility.DisplayDialog("完了", $"'{idName}' を更新しました！", "OK");
    }

    private static EffectDatabase GetDatabaseStatic()
    {
        string[] guids = AssetDatabase.FindAssets("t:EffectDatabase");
        if (guids.Length > 0)
        {
            return AssetDatabase.LoadAssetAtPath<EffectDatabase>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
        return null;
    }

    private void DeleteID(string targetID)
    {
        if (targetID == "None") return;
        if (EditorUtility.DisplayDialog("確認", $"本当に '{targetID}' を削除しますか？", "削除", "キャンセル"))
        {
            List<string> existingNames = new List<string>(Enum.GetNames(typeof(EffectID)));
            existingNames.Remove(targetID);
            GenerateEnumFile(existingNames);
            m_selectedIndex = 0;
            GUI.FocusControl(null);
            AssetDatabase.Refresh();
            m_lastCheckedID = "";
        }
    }

    private static void GenerateEnumFile(List<string> idList)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("// 自動生成されたファイルです。手動で直接編集しないでください。");
        sb.AppendLine("public enum EffectID");
        sb.AppendLine("{");
        if (!idList.Contains("None")) sb.AppendLine("    None = 0,");
        foreach (var id in idList)
        {
            if (id == "None") continue;
            sb.AppendLine($"    {id},");
        }
        sb.AppendLine("}");

        string[] guids = AssetDatabase.FindAssets("EffectID t:MonoScript");
        string path = guids.Length > 0 ? AssetDatabase.GUIDToAssetPath(guids[0]) : "Assets/PreDevelop/AOKI/Scripts/Effects_AokiScripts/EffectID.cs";
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }
}

public class ParticlePickerWindow : EditorWindow
{
    private Action<GameObject> m_onSelect;
    private List<GameObject> m_particlePrefabs = new List<GameObject>();
    private Vector2 m_scrollPos;
    private string m_searchQuery = "";

    public static void ShowWindow(Action<GameObject> onSelect)
    {
        var win = CreateInstance<ParticlePickerWindow>();
        win.titleContent = new GUIContent("プレハブを選択");
        win.minSize = new Vector2(380, 420);
        win.m_onSelect = onSelect;
        win.LoadParticlePrefabs();
        win.ShowAuxWindow();
    }

    private void LoadParticlePrefabs()
    {
        m_particlePrefabs.Clear();
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null && prefab.GetComponentInChildren<ParticleSystem>() != null)
            {
                m_particlePrefabs.Add(prefab);
            }
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("検索:", GUILayout.Width(40));
        m_searchQuery = EditorGUILayout.TextField(m_searchQuery);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(8);

        if (GUILayout.Button("None (選択解除)", EditorStyles.miniButton, GUILayout.Height(22)))
        {
            m_onSelect?.Invoke(null);
            Close();
        }

        EditorGUILayout.Space(5);
        m_scrollPos = EditorGUILayout.BeginScrollView(m_scrollPos);

        foreach (var prefab in m_particlePrefabs)
        {
            if (prefab == null) continue;
            if (!string.IsNullOrEmpty(m_searchQuery) && !prefab.name.ToLower().Contains(m_searchQuery.ToLower())) continue;

            Texture2D icon = AssetPreview.GetMiniThumbnail(prefab);
            string path = AssetDatabase.GetAssetPath(prefab);

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            if (icon != null) GUILayout.Label(icon, GUILayout.Width(24), GUILayout.Height(24));

            EditorGUILayout.BeginVertical();
            GUILayout.Label(prefab.name, EditorStyles.boldLabel);
            GUILayout.Label(path, EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("選択", GUILayout.Width(55), GUILayout.Height(26)))
            {
                m_onSelect?.Invoke(prefab);
                Close();
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }
}
#endif