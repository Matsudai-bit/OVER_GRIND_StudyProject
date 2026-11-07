using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>共通エフェクトをPrefab単位で再利用します。</summary>
public sealed class EffectPool
{
    private readonly EffectDefinition m_definition;
    private readonly Transform m_parent;
    private readonly ObjectPool<EffectInstance> m_pool;
    private readonly HashSet<EffectInstance> m_active = new HashSet<EffectInstance>();
    private bool m_disposed;

    /// <summary>プールを構築し、初期数を確保します。</summary>
    public EffectPool(EffectDefinition definition, Transform parent)
    {
        m_definition = definition;
        m_parent = parent;
        m_pool = new ObjectPool<EffectInstance>(CreateInstance, null, OnRelease,
            DestroyInstance, true, Mathf.Max(1, definition.InitialPoolSize),
            definition.MaxRetainedCount);
        List<EffectInstance> prewarmed = new List<EffectInstance>();
        for (int i = 0; i < definition.InitialPoolSize; i++)
        {
            prewarmed.Add(m_pool.Get());
        }
        foreach (EffectInstance instance in prewarmed)
        {
            m_pool.Release(instance);
        }
    }

    /// <summary>再生可能なインスタンスを取得します。</summary>
    public EffectInstance Get()
    {
        if (m_disposed || (m_definition.MaxActiveCount > 0 &&
            m_active.Count >= m_definition.MaxActiveCount))
        {
            return null;
        }
        EffectInstance instance = m_pool.Get();
        if (instance != null)
        {
            m_active.Add(instance);
        }
        return instance;
    }

    /// <summary>終了したインスタンスをプールへ返却します。</summary>
    public void Release(EffectInstance instance)
    {
        if (m_disposed || instance == null || !m_active.Remove(instance))
        {
            return;
        }
        m_pool.Release(instance);
    }

    /// <summary>プール内のオブジェクトをすべて破棄します。</summary>
    public void Dispose()
    {
        if (m_disposed)
        {
            return;
        }
        m_disposed = true;
        foreach (EffectInstance instance in m_active)
        {
            if (instance != null)
            {
                instance.ResetForReuse();
                Object.Destroy(instance.gameObject);
            }
        }
        m_active.Clear();
        m_pool.Clear();
    }

    /// <summary>Prefabを複製します。</summary>
    private EffectInstance CreateInstance()
    {
        EffectInstance instance = Object.Instantiate(m_definition.Prefab, m_parent);
        instance.Prepare(Release);
        instance.gameObject.SetActive(false);
        return instance;
    }

    /// <summary>返却前の状態を消去します。</summary>
    private void OnRelease(EffectInstance instance)
    {
        if (instance == null)
        {
            return;
        }
        instance.ResetForReuse();
        instance.gameObject.SetActive(false);
    }

    /// <summary>保持上限を超えたインスタンスを破棄します。</summary>
    private void DestroyInstance(EffectInstance instance)
    {
        if (instance != null)
        {
            Object.Destroy(instance.gameObject);
        }
    }
}
