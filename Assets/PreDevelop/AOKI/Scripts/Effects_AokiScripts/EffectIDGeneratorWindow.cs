#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class EffectIDGeneratorWindow : EditorWindow
{
    private string m_newIDName = "";
    private int m_assignIndex = 0;
    private GameObject m_assignPrefab;
    private string m_lastCheckedID = "";
    private int m_selectedIndex = 0;

    [MenuItem("Tools/VFX/EffectID 編集・追加ツール")]
    public static void ShowWindow()
    {
        var window = GetWindow<EffectIDGeneratorWindow>("EffectID 編集");
        window.minSize = new Vector2(380, 400);
    }

    private void OnEnable()
    {
        m_lastCheckedID = ""; // ウィンドウを開くたびにリセットして初回ロードを強制
    }

    private void OnGUI()
    {
        EffectDatabase db = GetDatabase();

        EditorGUILayout.Space(5);

        // 識別子の追加
        EditorGUILayout.LabelField("識別子（ID）の追加", EditorStyles.boldLabel);
        m_newIDName = EditorGUILayout.TextField("Effect名:", m_newIDName);
        if (GUILayout.Button("IDを追加する", GUILayout.Height(25))) AddID();

        EditorGUILayout.Space(15);

        // プレハブの登録
        EditorGUILayout.LabelField("プレハブの登録", EditorStyles.boldLabel);
        string[] currentNames = Enum.GetNames(typeof(EffectID));

        if (currentNames.Length > 0)
        {
            EditorGUI.BeginChangeCheck();
            m_assignIndex = EditorGUILayout.Popup("対象のID:", m_assignIndex, currentNames);
            string currentID = currentNames[m_assignIndex];

            // IDが切り替わった瞬間、またはツールを開いた直後に自動ロード
            if (EditorGUI.EndChangeCheck() || m_lastCheckedID != currentID)
            {
                m_lastCheckedID = currentID;
                LoadAssignedPrefab(currentID);
                GUI.FocusControl(null);
            }

            EditorGUILayout.Space(5);

            // 安定動作のため、Unity標準のObjectFieldに変更
            EditorGUILayout.BeginHorizontal();
            m_assignPrefab = (GameObject)EditorGUILayout.ObjectField("パーティクルプレハブ:", m_assignPrefab, typeof(GameObject), false);

            if (GUILayout.Button("一覧から選択", GUILayout.Width(80)))
            {
                OpenParticlePicker();
            }
            EditorGUILayout.EndHorizontal();

            bool isValidParticle = m_assignPrefab != null && m_assignPrefab.GetComponentInChildren<ParticleSystem>() != null;

            EditorGUILayout.Space(5);
            EditorGUI.BeginDisabledGroup(!isValidParticle);
            if (GUILayout.Button("データベースに登録 / 更新", GUILayout.Height(30)))
            {
                RegisterPrefab(currentID, m_assignPrefab);
            }
            EditorGUI.EndDisabledGroup();
        }

        EditorGUILayout.Space(15);

        // 識別子の削除
        EditorGUILayout.LabelField("識別子の削除", EditorStyles.boldLabel);
        if (currentNames.Length > 0)
        {
            m_selectedIndex = EditorGUILayout.Popup("対象のEffect:", m_selectedIndex, currentNames);
            if (GUILayout.Button("削除", GUILayout.Height(25))) DeleteID(currentNames[m_selectedIndex]);
        }
    }

    private void OpenParticlePicker()
    {
        ParticlePickerWindow.ShowWindow((selected) => {
            m_assignPrefab = selected;
            Repaint();
        });
    }

    private void LoadAssignedPrefab(string idName)
    {
        m_assignPrefab = null;
        if (idName == "None") return;

        EffectDatabase db = GetDatabase();
        if (db == null) return;

        try
        {
            EffectID targetID = (EffectID)Enum.Parse(typeof(EffectID), idName);
            foreach (var data in db.m_effectList)
            {
                if (data.m_id == targetID && data.m_prefab != null)
                {
                    // データベースから見つかったらセット
                    m_assignPrefab = data.m_prefab.gameObject;
                    break;
                }
            }
        }
        catch { }

        Repaint(); // 画面を更新して即座に反映
    }

    private void RegisterPrefab(string idName, GameObject prefabObj)
    {
        if (prefabObj == null || idName == "None") return;

        if (!EditorUtility.IsPersistent(prefabObj))
        {
            EditorUtility.DisplayDialog("エラー", "Projectウィンドウにあるプレハブを登録してください。", "OK");
            return;
        }

        string path = AssetDatabase.GetAssetPath(prefabObj);

        // プレハブを安全に編集
        GameObject contents = PrefabUtility.LoadPrefabContents(path);
        EffectNode_Aoki node = contents.GetComponent<EffectNode_Aoki>();
        if (node == null)
        {
            node = contents.AddComponent<EffectNode_Aoki>();
        }

        EffectID targetID = (EffectID)Enum.Parse(typeof(EffectID), idName);
        node.SetEffectID(targetID);

        PrefabUtility.SaveAsPrefabAsset(contents, path);
        PrefabUtility.UnloadPrefabContents(contents);

        // 最新のプレハブを再取得
        GameObject updatedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        EffectNode_Aoki finalNode = updatedPrefab.GetComponent<EffectNode_Aoki>();

        EffectDatabase db = GetDatabase();
        if (db == null) return;

        //「セーブ対象」として強制認識させる処理
        Undo.RecordObject(db, "Update EffectDatabase");

        bool isUpdated = false;
        for (int i = 0; i < db.m_effectList.Count; i++)
        {
            if (db.m_effectList[i].m_id == targetID)
            {
                var data = db.m_effectList[i];
                data.m_prefab = finalNode;
                db.m_effectList[i] = data; // リストの中身を上書き
                isUpdated = true;
                break;
            }
        }

        if (!isUpdated)
        {
            db.m_effectList.Add(new EffectDatabase.EffectData { m_id = targetID, m_prefab = finalNode });
        }

        // 変更を確定してセーブ
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();

        LoadAssignedPrefab(idName);
        EditorUtility.DisplayDialog("完了", $"'{idName}' に '{updatedPrefab.name}' を確実に登録しました！", "OK");
    }

    private EffectDatabase GetDatabase()
    {
        string[] guids = AssetDatabase.FindAssets("t:EffectDatabase");
        if (guids.Length > 0)
        {
            return AssetDatabase.LoadAssetAtPath<EffectDatabase>(AssetDatabase.GUIDToAssetPath(guids[0]));
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
        m_lastCheckedID = "";
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