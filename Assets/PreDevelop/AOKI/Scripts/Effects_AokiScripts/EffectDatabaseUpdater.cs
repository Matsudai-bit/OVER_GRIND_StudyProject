#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

public class EffectDatabaseUpdater
{
    // データベース（ScriptableObject）の保存先だけを残します
    private const string DATABASE_PATH = "Assets/PreDevelop/AOKI/EffectDatabase.asset";

    [MenuItem("Tools/VFX/エフェクトを自動登録する (Enum連携)")]
    public static void UpdateDatabase()
    {
        //  データベースファイルを読み込む（無ければ作る）
        EffectDatabase db = AssetDatabase.LoadAssetAtPath<EffectDatabase>(DATABASE_PATH);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<EffectDatabase>();
            AssetDatabase.CreateAsset(db, DATABASE_PATH);
        }

        db.m_effectList = new List<EffectDatabase.EffectData>();

        // 2. EffectID(Enum) に登録されているすべての名前を取得
        Array enumValues = Enum.GetValues(typeof(EffectID));

        foreach (EffectID id in enumValues)
        {
            string enumName = id.ToString();

            // フォルダ指定を削除し、プロジェクト全体から検索するように変更
            string[] guids = AssetDatabase.FindAssets($"{enumName} t:GameObject");

            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                EffectNode_Aoki node = obj.GetComponent<EffectNode_Aoki>();
                if (node != null)
                {
                    // プレハブ側に EffectID を自動セットして保存
                    node.SetEffectID(id);
                    EditorUtility.SetDirty(node.gameObject);

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