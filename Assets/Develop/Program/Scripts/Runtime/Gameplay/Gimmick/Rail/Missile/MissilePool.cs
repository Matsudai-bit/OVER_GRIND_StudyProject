using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// ミサイル本体をPrefab単位で再利用します。ボス固有の挙動には依存しません。
/// </summary>
[DisallowMultipleComponent]
public sealed class MissilePool : MonoBehaviour
{
    [SerializeField, Header("ミサイルPrefab")]
    private PooledMissile m_prefab;

    [SerializeField, Header("インスタンスの親（未設定なら自身）")]
    private Transform m_poolRoot;

    [SerializeField, Header("共有エフェクト発生元")]
    private EffectEmitter m_sceneEffectEmitter;

    [SerializeField, Header("爆発攻撃判定プール")]
    private ExplosionHitboxPool m_explosionHitboxPool;

    [SerializeField, Header("プール設定"), Min(0)]
    private int m_initialCount = 4;

    [SerializeField, Min(1)]
    private int m_maxRetainedCount = 32;

    [SerializeField, Min(0)]
    private int m_maxActiveCount = 32;

    // プール本体と貸出中の一覧
    private ObjectPool<PooledMissile> m_pool;
    private readonly HashSet<PooledMissile> m_active = new();

    /// <summary>発射・爆発エフェクト用の共有Emitterを取得します。</summary>
    public EffectEmitter SceneEffectEmitter => m_sceneEffectEmitter;

    /// <summary>爆発Hitboxプールを取得します。</summary>
    public ExplosionHitboxPool ExplosionHitboxPool => m_explosionHitboxPool;

    /// <summary>貸出中のミサイル数を取得します。</summary>
    public int ActiveCount => m_active.Count;

    private void Awake()
    {
        EnsurePool();
    }

    /// <summary>指定位置へミサイルを貸し出します。</summary>
    /// <returns>true：貸出成功。false：設定不足または上限到達。</returns>
    public bool TrySpawn(Vector3 position, Quaternion rotation, out PooledMissile missile)
    {
        missile = null;
        if (!EnsurePool() || (m_maxActiveCount > 0 && m_active.Count >= m_maxActiveCount))
        {
            return false;
        }

        PooledMissile instance = m_pool.Get();
        if (instance == null)
        {
            return false;
        }

        m_active.Add(instance);
        if (!instance.Spawn(position, rotation))
        {
            Release(instance);
            return false;
        }

        missile = instance;
        return true;
    }

    /// <summary>ミサイルを返却します。外部からの失敗時返却にも使用できます。</summary>
    public void Release(PooledMissile missile)
    {
        if (missile == null || !m_active.Contains(missile))
        {
            return;
        }

        if (missile.IsSpawned)
        {
            missile.Release();
            return;
        }

        m_active.Remove(missile);
        if (m_pool != null)
        {
            m_pool.Release(missile);
        }
    }

    /// <summary>プールを初期化します。</summary>
    /// <returns>true：使用可能。false：Prefab不足。</returns>
    private bool EnsurePool()
    {
        if (m_pool != null)
        {
            return true;
        }
        if (m_prefab == null)
        {
            Debug.LogError("[MissilePool] ミサイルPrefabがありません。", this);
            return false;
        }

        m_pool = new ObjectPool<PooledMissile>(
            CreateInstance, null, OnReturned, OnDestroyed,
            true, Mathf.Max(1, m_initialCount), Mathf.Max(1, m_maxRetainedCount));

        // 初期確保は非アクティブのまま実施します。
        List<PooledMissile> prewarmed = new();
        for (int index = 0; index < m_initialCount; index++)
        {
            prewarmed.Add(m_pool.Get());
        }
        foreach (PooledMissile instance in prewarmed)
        {
            m_pool.Release(instance);
        }
        return true;
    }

    /// <summary>ミサイルPrefabから新しいインスタンスを作成します。</summary>
    private PooledMissile CreateInstance()
    {
        PooledMissile instance = Instantiate(
            m_prefab, m_poolRoot != null ? m_poolRoot : transform);
        instance.gameObject.SetActive(false);
        instance.Prepare(Release);
        return instance;
    }

    private void OnReturned(PooledMissile instance)
    {
        if (instance != null)
        {
            instance.gameObject.SetActive(false);
        }
    }

    private void OnDestroyed(PooledMissile instance)
    {
        if (instance != null)
        {
            Destroy(instance.gameObject);
        }
    }

    private void OnDestroy()
    {
        foreach (PooledMissile instance in m_active)
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
