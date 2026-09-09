using UnityEngine;
using System.Collections.Generic;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    [SerializeField] private EffectDatabase m_database;

    // プール管理用辞書
    private Dictionary<EffectID, Queue<EffectNode_Aoki>> m_poolDict = new Dictionary<EffectID, Queue<EffectNode_Aoki>>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (m_database != null)
            {
                m_database.Initialize();
            }
            else
            {
                Debug.LogError("VFXManagerにEffectDatabaseがセットされていません！");
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 単発再生
    /// 終わったら自動で消えるエフェクト用。呼び出し元は戻り値を気にしなくてOKです。
    /// </summary>
    public void PlayOneShot(EffectID id, Vector3 position)
    {
        EffectNode_Aoki node = GetFromPool(id);
        if (node == null) return;

        node.transform.position = position;
        node.gameObject.SetActive(true);

        // ループをfalseにして再生し、終了時にReturnToPoolを呼ぶようにActionを渡す
        node.EffectsPlayer(false, (n) => ReturnToPool(id, n));
    }

    /// <summary>
    /// ループ再生
    /// 手動で止めるまで消えないエフェクト用。呼び出し元が後で停止できるように参照を返します。
    /// </summary>
    public EffectNode_Aoki PlayLoop(EffectID id, Vector3 position)
    {
        EffectNode_Aoki node = GetFromPool(id);
        if (node == null) return null;

        node.transform.position = position;
        node.gameObject.SetActive(true);

        // ループをtrueにして再生
        node.EffectsPlayer(true, (n) => ReturnToPool(id, n));

        return node;
    }

    private EffectNode_Aoki GetFromPool(EffectID id)
    {
        if (!m_poolDict.ContainsKey(id))
        {
            m_poolDict[id] = new Queue<EffectNode_Aoki>();
        }

        // プールに待機中のものがあれば取り出す
        if (m_poolDict[id].Count > 0)
        {
            return m_poolDict[id].Dequeue();
        }

        // なければデータベースからプレハブを取得して新規生成
        EffectNode_Aoki prefab = m_database.GetPrefab(id);
        if (prefab != null)
        {
            // マネージャー自身の子オブジェクトとして生成し、ヒエラルキーを綺麗に保つ
            return Instantiate(prefab, transform);
        }

        return null;
    }

    private void ReturnToPool(EffectID id, EffectNode_Aoki node)
    {
        // オブジェクトを非表示にしてプール（Queue）に戻す
        node.gameObject.SetActive(false);
        m_poolDict[id].Enqueue(node);
    }
}