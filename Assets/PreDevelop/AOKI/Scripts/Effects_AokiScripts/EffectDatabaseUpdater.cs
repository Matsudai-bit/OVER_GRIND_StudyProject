#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

public class EffectDatabaseUpdater
{
    // エフェクトのプレハブを入れておくフォルダのパス
    private const string TARGET_FOLDER = "Assets/Prefabs/Effects";
    // データベース（ScriptableObject）の保存先
    private const string DATABASE_PATH = "Assets/Resources/EffectDatabase.asset";

    [MenuItem("Tools/VFX/エフェクトを自動登録する (Enum連携)")]
    public static void UpdateDatabase()
    {
        // 1. データベースファイルを読み込む（無ければ作る）
        EffectDatabase db = AssetDatabase.LoadAssetAtPath<EffectDatabase>(DATABASE_PATH);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<EffectDatabase>();
            // フォルダが無ければエラーになるので、必要に応じて手動でResourcesフォルダを作ってください
            AssetDatabase.CreateAsset(db, DATABASE_PATH);
        }

        db.m_effectList = new List<EffectDatabase.EffectData>();

        // 2. EffectID(Enum) に登録されているすべての名前を取得
        Array enumValues = Enum.GetValues(typeof(EffectID));

        foreach (EffectID id in enumValues)
        {
            string enumName = id.ToString(); 

            // Enum名と同じ名前のプレハブを検索
            string[] guids = AssetDatabase.FindAssets($"{enumName} t:GameObject", new[] { TARGET_FOLDER });

            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                EffectNode_Aoki node = obj.GetComponent<EffectNode_Aoki>();
                if (node != null)
                {
                    // 見つかったらリストに追加
                    db.m_effectList.Add(new EffectDatabase.EffectData
                    {
                        m_id = id,
                        m_prefab = node
                    });
                }
                else
                {
                    Debug.LogWarning($"プレハブ '{enumName}' に EffectNode_Aoki がアタッチされていません。");
                }
            }
            else
            {
                Debug.LogWarning($"Enum '{enumName}' に対応するプレハブが見つかりません。名前が一致しているか確認してください。");
            }
        }

        // 3. 変更を保存して完了
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();

        Debug.Log($"【VFX】データベース更新完了！ 登録数: {db.m_effectList.Count} / Enum総数: {enumValues.Length}");
    }
}
#endif