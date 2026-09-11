#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

/// <summary>
/// ParticleSystem付きプレハブ専用：EffectIDの追加・削除・データベース登録ツール
/// </summary>
public class EffectIDGeneratorWindow : EditorWindow
{
    private string m_newIDName = "";
    private int m_assignIndex = 0;
    private int m_selectedIndex = 0;
    private GameObject m_assignPrefab;

    [MenuItem("Tools/VFX/EffectID 編集・追加ツール")]
    public static void ShowWindow()
    {
        var window = GetWindow<EffectIDGeneratorWindow>("EffectID 編集");
        window.minSize = new Vector2(350, 380);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);

        //識別子の追加
        EditorGUILayout.LabelField("識別子の追加", EditorStyles.boldLabel);
        m_newIDName = EditorGUILayout.TextField("Effect名:", m_newIDName);
        if (GUILayout.Button("IDを追加する", GUILayout.Height(25)))
        {
            AddID();
        }

        EditorGUILayout.Space(15);

        // プレハブの登録
        string[] currentNames = Enum.GetNames(typeof(EffectID));
        EditorGUILayout.LabelField("プレハブの登録", EditorStyles.boldLabel);
        if (currentNames.Length > 0)
        {
            m_assignIndex = EditorGUILayout.Popup("対象のID:", m_assignIndex, currentNames);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("パーティクルプレハブ:", EditorStyles.boldLabel);

            // 専用選択ウィンドウを開く枠
            EditorGUILayout.BeginHorizontal();

            GUIContent labelContent = new GUIContent(
                m_assignPrefab != null ? m_assignPrefab.name : "None (Particle Prefab)",
                m_assignPrefab != null ? AssetPreview.GetMiniThumbnail(m_assignPrefab) : null
            );

            if (GUILayout.Button(labelContent, EditorStyles.objectField, GUILayout.Height(20)))
            {
                ParticlePickerWindow.ShowWindow((selected) => {
                    m_assignPrefab = selected;
                    Repaint();
                });
            }

            if (GUILayout.Button("", GUI.skin.GetStyle("ObjectFieldButton"), GUILayout.Width(19), GUILayout.Height(18)))
            {
                ParticlePickerWindow.ShowWindow((selected) => {
                    m_assignPrefab = selected;
                    Repaint();
                });
            }
            EditorGUILayout.EndHorizontal();

            // Projectウィンドウからのドラッグ＆ドロップ受け入れ処理
            Rect lastRect = GUILayoutUtility.GetLastRect();
            HandleDragAndDrop(lastRect);

            bool isValidParticle = m_assignPrefab != null && m_assignPrefab.GetComponentInChildren<ParticleSystem>() != null;

            EditorGUILayout.Space(5);

            EditorGUI.BeginDisabledGroup(!isValidParticle);
            if (GUILayout.Button("データベースに登録 / 更新", GUILayout.Height(30)))
            {
                RegisterPrefab(currentNames[m_assignIndex], m_assignPrefab);
            }
            EditorGUI.EndDisabledGroup();
        }
        else
        {
            EditorGUILayout.HelpBox("EffectIDの読み込みに失敗しました。", MessageType.Warning);
        }

        EditorGUILayout.Space(15);

        //識別子の削除
        EditorGUILayout.LabelField("識別子の削除", EditorStyles.boldLabel);
        if (currentNames.Length > 0)
        {
            m_selectedIndex = EditorGUILayout.Popup("対象のEffect:", m_selectedIndex, currentNames);
            if (GUILayout.Button("削除", GUILayout.Height(25)))
            {
                DeleteID(currentNames[m_selectedIndex]);
            }
        }
    }

    private void HandleDragAndDrop(Rect dropArea)
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
                        m_assignPrefab = go;
                        GUI.changed = true;
                        break;
                    }
                }
            }
            evt.Use();
        }
    }

    private void RegisterPrefab(string idName, GameObject prefabObj)
    {
        if (prefabObj == null)
        {
            EditorUtility.DisplayDialog("エラー", "プレハブをセットしてください。", "OK");
            return;
        }

        if (idName == "None")
        {
            EditorUtility.DisplayDialog("エラー", "'None' 以外のIDを選択してください。", "OK");
            return;
        }

        if (!EditorUtility.IsPersistent(prefabObj))
        {
            EditorUtility.DisplayDialog("エラー", "Hierarchyのオブジェクトではなく、Projectウィンドウ内にある「プレハブファイル」をセットしてください。", "OK");
            return;
        }

        EffectNode_Aoki node = prefabObj.GetComponent<EffectNode_Aoki>();
        if (node == null)
        {
            string path = AssetDatabase.GetAssetPath(prefabObj);
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            node = contents.AddComponent<EffectNode_Aoki>();
            PrefabUtility.SaveAsPrefabAsset(contents, path);
            PrefabUtility.UnloadPrefabContents(contents);

            prefabObj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            node = prefabObj.GetComponent<EffectNode_Aoki>();
        }

        EffectDatabase db = GetDatabase();
        if (db == null)
        {
            EditorUtility.DisplayDialog("エラー", "EffectDatabaseが見つかりません。", "OK");
            return;
        }

        EffectID targetID = (EffectID)Enum.Parse(typeof(EffectID), idName);
        node.SetEffectID(targetID);

        bool isUpdated = false;
        for (int i = 0; i < db.m_effectList.Count; i++)
        {
            if (db.m_effectList[i].m_id == targetID)
            {
                var data = db.m_effectList[i];
                data.m_prefab = node;
                db.m_effectList[i] = data;
                isUpdated = true;
                break;
            }
        }

        if (!isUpdated)
        {
            db.m_effectList.Add(new EffectDatabase.EffectData { m_id = targetID, m_prefab = node });
        }

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();

        m_assignPrefab = null;
        EditorUtility.DisplayDialog("完了", $"'{idName}' に '{prefabObj.name}' を登録しました！", "OK");
    }

    private EffectDatabase GetDatabase()
    {
        string[] guids = AssetDatabase.FindAssets("t:EffectDatabase");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<EffectDatabase>(path);
        }
        return null;
    }

    private void AddID()
    {
        if (string.IsNullOrWhiteSpace(m_newIDName)) return;
        string sanitizedID = System.Text.RegularExpressions.Regex.Replace(m_newIDName.Trim(), @"[^\w]", "");
        List<string> existingNames = new List<string>(Enum.GetNames(typeof(EffectID)));
        if (existingNames.Contains(sanitizedID)) return;

        existingNames.Add(sanitizedID);
        GenerateEnumFile(existingNames);
        m_newIDName = "";
        GUI.FocusControl(null);
        AssetDatabase.Refresh();
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
        }
    }

    private void GenerateEnumFile(List<string> idList)
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

/// <summary>
/// ParticleSystem付きプレハブだけをリスト形式で綺麗に表示する専用ウィンドウ
/// </summary>
public class ParticlePickerWindow : EditorWindow
{
    private Action<GameObject> m_onSelect;
    private List<GameObject> m_particlePrefabs = new List<GameObject>();
    private Vector2 m_scrollPos;
    private string m_searchQuery = "";

    public static void ShowWindow(Action<GameObject> onSelect)
    {
        var win = CreateInstance<ParticlePickerWindow>();
        win.titleContent = new GUIContent("パーティクルプレハブを選択");
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

        // 検索ボックス
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("検索:", GUILayout.Width(40));
        m_searchQuery = EditorGUILayout.TextField(m_searchQuery);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);

        // クリアボタン
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
            if (!string.IsNullOrEmpty(m_searchQuery) && !prefab.name.ToLower().Contains(m_searchQuery.ToLower()))
            {
                continue;
            }

            Texture2D icon = AssetPreview.GetMiniThumbnail(prefab);
            string path = AssetDatabase.GetAssetPath(prefab);

            // リスト1行の表示枠
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            // ミニアイコン
            if (icon != null)
            {
                GUILayout.Label(icon, GUILayout.Width(24), GUILayout.Height(24));
            }

            // 名前と保存パスを縦並びで表示
            EditorGUILayout.BeginVertical();
            GUILayout.Label(prefab.name, EditorStyles.boldLabel);
            GUILayout.Label(path, EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            // 選択ボタン
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