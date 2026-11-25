using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 爆発の攻撃判定のみをプールで再利用します。
/// </summary>
[DisallowMultipleComponent]
public sealed class ExplosionHitboxPool : MonoBehaviour
{
    [SerializeField, Header("爆発Hitbox Prefab")]
    private ExplosionHitboxInstance m_prefab;

    [SerializeField, Header("プール設定"), Min(0)]
    private int m_initialCount = 4;

    [SerializeField, Min(1)]
    private int m_maxRetainedCount = 32;

    [SerializeField, Min(0)]
    private int m_maxActiveCount = 32;

    [SerializeField, Header("攻撃判定の有効時間（秒）"), Min(0.01f)]
    private float m_hitboxDuration = 0.35f;

    private ObjectPool<ExplosionHitboxInstance> m_pool;
    private readonly HashSet<ExplosionHitboxInstance> m_active = new();

    /// <summary>貸出中の爆発判定数を取得します。</summary>
    public int ActiveCount => m_active.Count;

    private void Awake()
    {
        EnsurePool();
    }

    /// <summary>指定位置に一定時間だけ爆発判定を発生させます。</summary>
    /// <returns>true：生成成功。false：設定不足または上限到達。</returns>
    public bool TryActivate(Vector3 position, AttackIdentifier attackIdentifier)
    {
        if (!EnsurePool() || attackIdentifier == null ||
            (m_maxActiveCount > 0 && m_active.Count >= m_maxActiveCount))
        {
            return false;
        }

        ExplosionHitboxInstance instance = m_pool.Get();
        if (instance == null)
        {
            return false;
        }

        m_active.Add(instance);
        if (!instance.Activate(position, attackIdentifier, m_hitboxDuration))
        {
            // 失敗したインスタンスは明示的に停止して返却します。
            m_active.Remove(instance);
            m_pool.Release(instance);
            return false;
        }
        return true;
    }

    private void Release(ExplosionHitboxInstance instance)
    {
        if (instance == null || !m_active.Remove(instance))
        {
            return;
        }
        m_pool?.Release(instance);
    }

    private bool EnsurePool()
    {
        if (m_pool != null)
        {
            return true;
        }
        if (m_prefab == null)
        {
            Debug.LogError("[ExplosionHitboxPool] Prefabがありません。", this);
            return false;
        }

        m_pool = new ObjectPool<ExplosionHitboxInstance>(
            CreateInstance, null, OnReturned, OnDestroyed,
            true, Mathf.Max(1, m_initialCount), Mathf.Max(1, m_maxRetainedCount));
        List<ExplosionHitboxInstance> prewarmed = new();
        for (int index = 0; index < m_initialCount; index++)
        {
            prewarmed.Add(m_pool.Get());
        }
        foreach (ExplosionHitboxInstance instance in prewarmed)
        {
            m_pool.Release(instance);
        }
        return true;
    }

    private ExplosionHitboxInstance CreateInstance()
    {
        ExplosionHitboxInstance instance = Instantiate(m_prefab, transform);
        instance.gameObject.SetActive(false);
        instance.Prepare(Release);
        return instance;
    }

    private void OnReturned(ExplosionHitboxInstance instance)
    {
        if (instance != null)
        {
            instance.gameObject.SetActive(false);
        }
    }

    private void OnDestroyed(ExplosionHitboxInstance instance)
    {
        if (instance != null)
        {
            Destroy(instance.gameObject);
        }
    }

    private void OnDestroy()
    {
        foreach (ExplosionHitboxInstance instance in m_active)
        {
            if (instance != null)
            {
                Destroy(instance.gameObject);
            }
        }
        m_active.Clear();
        m_pool?.Clear();
        m_pool = null;
    }
}
