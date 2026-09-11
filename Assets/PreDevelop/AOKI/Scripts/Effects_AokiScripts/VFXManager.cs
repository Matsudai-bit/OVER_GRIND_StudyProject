using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class VFXManager : MonoBehaviour
{
    private static VFXManager m_instance;

    // [System.Obsolete] <- 削除
    public static VFXManager Instance
    {
        get
        {
            if (m_instance == null)
            {
                m_instance = FindFirstObjectByType<VFXManager>();
            }
            return m_instance;
        }
    }
    [SerializeField]
    private EffectDatabase m_database;

    // プール管理（非アクティブなオブジェクト）
    private Dictionary<EffectID, Queue<EffectNode_Aoki>> m_poolDict = new Dictionary<EffectID, Queue<EffectNode_Aoki>>();

    // 再生中エフェクトの追跡管理（アクティブなオブジェクト）
    private Dictionary<EffectID, List<EffectNode_Aoki>> m_activeDict = new Dictionary<EffectID, List<EffectNode_Aoki>>();

    private int m_handleCounter = 0;

    private void Awake()
    {
        if (m_instance != null && m_instance != this)
        {
            Destroy(gameObject);
            return;
        }
        m_instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // --- 【再生】 ---
    /// <summary>
    /// エフェクト再生（ID指定）
    /// </summary>
    /// <returns>個別に識別したい場合に使用するHandleID（不要なら無視してOK）</returns>
    public int Play(EffectID id, Vector3 position, Quaternion rotation = default, Transform parent = null)
    {
        EffectNode_Aoki node = GetFromPool(id);
        if (node == null) return -1;

        int handle = ++m_handleCounter;

        node.transform.SetParent(parent);
        node.transform.SetPositionAndRotation(position, rotation == default ? Quaternion.identity : rotation);
        node.gameObject.SetActive(true);

        // アクティブリストに登録
        if (!m_activeDict.ContainsKey(id))
        {
            m_activeDict[id] = new List<EffectNode_Aoki>();
        }
        m_activeDict[id].Add(node);

        node.Init(handle, (completedNode) => ReturnToPool(id, completedNode));
        node.Play();

        return handle;
    }

 
    /// <summary>
    /// 指定したEffectIDの再生中エフェクトをすべて停止
    /// </summary>
    public void Stop(EffectID id)
    {
        if (m_activeDict.TryGetValue(id, out var list))
        {
            // Stop呼び出し中にリストが変更されるのを防ぐため逆順処理
            for (int i = list.Count - 1; i >= 0; i--)
            {
                list[i].Stop();
            }
        }
    }

    /// <summary>
    /// 画面上の全エフェクトを停止
    /// </summary>
    public void StopAll()
    {
        foreach (var pair in m_activeDict)
        {
            for (int i = pair.Value.Count - 1; i >= 0; i--)
            {
                pair.Value[i].Stop();
            }
        }
    }

    /// <summary>
    /// 指定したEffectIDのエフェクトを一時停止
    /// </summary>
    public void Pause(EffectID id)
    {
        if (m_activeDict.TryGetValue(id, out var list))
        {
            foreach (var node in list) node.Pause();
        }
    }

    /// <summary>
    /// 指定したEffectIDのエフェクトを再開
    /// </summary>
    public void Resume(EffectID id)
    {
        if (m_activeDict.TryGetValue(id, out var list))
        {
            foreach (var node in list) node.Resume();
        }
    }

    /// <summary>
    /// 全エフェクトを一括一時停止（ポーズ画面用）
    /// </summary>
    public void PauseAll()
    {
        foreach (var pair in m_activeDict)
        {
            foreach (var node in pair.Value) node.Pause();
        }
    }

    /// <summary>
    /// 全エフェクトを一括再開（ポーズ解除用）
    /// </summary>
    public void ResumeAll()
    {
        foreach (var pair in m_activeDict)
        {
            foreach (var node in pair.Value) node.Resume();
        }
    }

    // --- プール内部処理 ---
    private EffectNode_Aoki GetFromPool(EffectID id)
    {
        if (m_database == null)
        {
            Debug.LogError("[VFXManager] EffectDatabaseがアタッチされていません。");
            return null;
        }

        if (!m_poolDict.TryGetValue(id, out var pool))
        {
            pool = new Queue<EffectNode_Aoki>();
            m_poolDict[id] = pool;
        }

        if (pool.Count > 0)
        {
            return pool.Dequeue();
        }
        else
        {
            EffectNode_Aoki prefab = m_database.GetPrefab(id);
            if (prefab == null) return null;

            return Instantiate(prefab, transform);
        }
    }

    private void ReturnToPool(EffectID id, EffectNode_Aoki node)
    {
        node.gameObject.SetActive(false);

        // アクティブリストから削除
        if (m_activeDict.TryGetValue(id, out var list))
        {
            list.Remove(node);
        }

        if (!m_poolDict.TryGetValue(id, out var pool))
        {
            pool = new Queue<EffectNode_Aoki>();
            m_poolDict[id] = pool;
        }
        pool.Enqueue(node);
    }

    /// <summary>
    /// 指定した個別のエフェクトだけを停止する（Play時に受け取ったハンドルIDを使用）
    /// </summary>
    public void Stop(int handle)
    {
        if (handle <= 0) return;

        foreach (var pair in m_activeDict)
        {
            foreach (var node in pair.Value)
            {
                if (node.HandleID == handle)
                {
                    node.Stop();
                    return; // 見つかったら終了
                }
            }
        }
    }
}