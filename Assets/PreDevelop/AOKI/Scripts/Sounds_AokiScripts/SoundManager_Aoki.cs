/// @ using :: 使用エンジンとコレクション
using UnityEngine;
using System.Collections.Generic;

/// @ className :: サウンドの全体管理およびプールシステム
/// @ name :: Aoki Hayate
/// @ date :: 2026/09/12
public class SoundManager_Aoki : MonoBehaviour
{
    private static SoundManager_Aoki m_instance; // シングルトン用インスタンス

    public static SoundManager_Aoki Instance
    {
        get
        {
            if (m_instance == null)
            {
                m_instance = FindFirstObjectByType<SoundManager_Aoki>();
            }
            return m_instance;
        }
    }

    [SerializeField] private SoundDatabase m_database; // 参照するサウンドデータベース

    private Dictionary<SoundID_Aoki, Queue<SoundNode_Aoki>> m_poolDict = new Dictionary<SoundID_Aoki, Queue<SoundNode_Aoki>>(); // 待機中のプール
    private Dictionary<SoundID_Aoki, List<SoundNode_Aoki>> m_activeDict = new Dictionary<SoundID_Aoki, List<SoundNode_Aoki>>(); // 再生中のリスト
    private int m_handleCounter = 0; // 個別停止用のハンドルIDカウンター

    private void Awake()
    {
        // 重複生成の防止
        if (m_instance != null && m_instance != this)
        {
            Destroy(gameObject);
            return;
        }
        m_instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // 指定したIDのサウンドを再生し、個別のハンドルIDを返す
    public int Play(SoundID_Aoki id, Vector3 position = default, Transform parent = null)
    {
        if (id == SoundID_Aoki.None) return -1;

        SoundDatabase.SoundData data = m_database != null ? m_database.GetSoundData(id) : null;
        if (data == null || data.m_clip == null) return -1;

        SoundNode_Aoki node = GetFromPool(id);
        if (node == null) return -1;

        int handle = ++m_handleCounter;

        node.transform.SetParent(parent);
        node.transform.position = position;
        node.gameObject.SetActive(true);

        if (!m_activeDict.ContainsKey(id)) m_activeDict[id] = new List<SoundNode_Aoki>();
        m_activeDict[id].Add(node);

        node.Init(handle, data, (completedNode) => ReturnToPool(id, completedNode));
        node.Play();

        return handle;
    }

    // 指定IDのサウンドを全て停止する
    public void Stop(SoundID_Aoki id)
    {
        if (m_activeDict.TryGetValue(id, out var list))
        {
            for (int i = list.Count - 1; i >= 0; i--) list[i].Stop();
        }
    }

    // ハンドルIDを指定して個別に停止する
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
                    return;
                }
            }
        }
    }

    public void StopAll()
    {
        foreach (var pair in m_activeDict)
        {
            for (int i = pair.Value.Count - 1; i >= 0; i--) pair.Value[i].Stop();
        }
    }

    public void PauseAll()
    {
        foreach (var pair in m_activeDict)
        {
            foreach (var node in pair.Value) node.Pause();
        }
    }

    public void ResumeAll()
    {
        foreach (var pair in m_activeDict)
        {
            foreach (var node in pair.Value) node.Resume();
        }
    }

    // プールからノードを取得
    private SoundNode_Aoki GetFromPool(SoundID_Aoki id)
    {
        if (!m_poolDict.TryGetValue(id, out var pool))
        {
            pool = new Queue<SoundNode_Aoki>();
            m_poolDict[id] = pool;
        }

        if (pool.Count > 0) return pool.Dequeue();
        else
        {
            GameObject go = new GameObject($"SoundNode_{id}");
            go.transform.SetParent(transform);
            return go.AddComponent<SoundNode_Aoki>();
        }
    }

    // 再生が終了したノードをプールに返却する
    private void ReturnToPool(SoundID_Aoki id, SoundNode_Aoki node)
    {
        node.gameObject.SetActive(false);
        if (m_activeDict.TryGetValue(id, out var list)) list.Remove(node);

        if (!m_poolDict.TryGetValue(id, out var pool))
        {
            pool = new Queue<SoundNode_Aoki>();
            m_poolDict[id] = pool;
        }
        pool.Enqueue(node);
    }
}