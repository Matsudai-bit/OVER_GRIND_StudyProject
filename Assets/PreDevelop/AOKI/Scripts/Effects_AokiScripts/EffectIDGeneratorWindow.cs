#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

/// <summary>
/// Inspector/Unity上から EffectID を追加・削除・データベース登録する専用ウィンドウ
/// </summary>
public class EffectIDGeneratorWindow : EditorWindow
{
    private string m_newIDName = "";
    private int m_selectedIndex = 0;

    // 紐付け用（普通のGameObjectで受け取れるように変更）
    private int m_assignIndex = 0;
    private GameObject m_assignPrefabObj;

    [MenuItem("Tools/VFX/EffectID 編集・追加ツール")]
    public static void ShowWindow()
    {
        var window = GetWindow<EffectIDGeneratorWindow>("EffectID 編集");
        window.minSize = new Vector2(350, 420);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);

        // --- 追加機能エリア ---
        EditorGUILayout.LabelField("【 識別子の追加 】", EditorStyles.boldLabel);
        m_newIDName = EditorGUILayout.TextField("Effect名:", m_newIDName);

        EditorGUILayout.Space(5);
        if (GUILayout.Button("追加", GUILayout.Height(30)))
        {
            AddID();
        }

        EditorGUILayout.Space(20);

        // 既存のEnumをプルダウンで取得
        string[] currentNames = Enum.GetNames(typeof(EffectID));

        // --- プレハブ登録エリア ---
        EditorGUILayout.LabelField("【 プレハブの紐付け・登録 】", EditorStyles.boldLabel);
        if (currentNames.Length > 0)
        {
            m_assignIndex = EditorGUILayout.Popup("対象のID:", m_assignIndex, currentNames);

            // 普通のGameObjectプレハブを何でも選べるように設定
            m_assignPrefabObj = (GameObject)EditorGUILayout.ObjectField("プレハブ:", m_assignPrefabObj, typeof(GameObject), false);

            EditorGUILayout.Space(5);
            if (GUILayout.Button("データベースに登録 / 更新", GUILayout.Height(30)))
            {
                AssignPrefabToDatabase(currentNames[m_assignIndex], m_assignPrefabObj);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("EffectIDの読み込みに失敗しました。", MessageType.Warning);
        }

        EditorGUILayout.Space(20);

        // --- 削除機能エリア ---
        EditorGUILayout.LabelField("【 識別子の削除 】", EditorStyles.boldLabel);

        if (currentNames.Length > 0)
        {
            m_selectedIndex = EditorGUILayout.Popup("対象のEffect:", m_selectedIndex, currentNames);

            EditorGUILayout.Space(5);
            if (GUILayout.Button("削除", GUILayout.Height(30)))
            {
                DeleteID(currentNames[m_selectedIndex]);
            }
        }
    }

    private void AssignPrefabToDatabase(string idName, GameObject prefabObj)
    {
        if (prefabObj == null)
        {
            EditorUtility.DisplayDialog("エラー", "プレハブ（GameObject）が指定されていません。", "OK");
            return;
        }

        if (idName == "None")
        {
            EditorUtility.DisplayDialog("エラー", "'None' にはプレハブを登録できません。", "OK");
            return;
        }

        // Project内のプレハブアセットであるか確認
        if (!EditorUtility.IsPersistent(prefabObj))
        {
            EditorUtility.DisplayDialog("エラー", "ヒエラルキーのオブジェクトではなく、Projectウィンドウ内にある「プレハブファイル」をセットしてください。", "OK");
            return;
        }

        // EffectNode_Aoki がアタッチされていなければ自動で追加
        EffectNode_Aoki node = prefabObj.GetComponent<EffectNode_Aoki>();
        if (node == null)
        {
            node = prefabObj.AddComponent<EffectNode_Aoki>();
            EditorUtility.SetDirty(prefabObj);
        }

        // EffectDatabase を検索して取得
        EffectDatabase db = GetDatabase();
        if (db == null)
        {
            EditorUtility.DisplayDialog("エラー", "EffectDatabaseが見つかりません。プロジェクト内に作成してください。", "OK");
            return;
        }

        EffectID targetID = (EffectID)Enum.Parse(typeof(EffectID), idName);

        // プレハブ側に EffectID をセットして保存
        node.SetEffectID(targetID);
        EditorUtility.SetDirty(node.gameObject);

        // データベースのリストを更新 (CS1612エラー回避の構造)
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
            db.m_effectList.Add(new EffectDatabase.EffectData
            {
                m_id = targetID,
                m_prefab = node
            });
        }

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("完了", $"'{idName}' をデータベースに登録・更新しました！", "OK");
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
        if (EditorUtility.DisplayDialog("削除の確認", $"本当に '{targetID}' を削除しますか？", "削除する", "キャンセル"))
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
#endif