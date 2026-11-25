using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 共通エフェクトをPrefab単位で再利用します。
/// </summary>
public sealed class EffectPool
{
    // プール設定
    private readonly EffectDefinition m_definition;
    private readonly Transform m_parent;
    private readonly ObjectPool<EffectInstance> m_pool;

    // 貸出中のインスタンス
    private readonly HashSet<EffectInstance> m_active = new HashSet<EffectInstance>();

    // 分割事前生成中に一時確保するインスタンス
    private readonly List<EffectInstance> m_prewarmHeld = new List<EffectInstance>();

    private bool m_isPrewarming;
    private bool m_hasPrewarmFailed;
    private bool m_disposed;

    /// <summary>
    /// プールを作成します。従来の呼び出しでは初期生成まで実行します。
    /// </summary>
    /// <param name="definition">エフェクト定義。</param>
    /// <param name="parent">生成先。</param>
    /// <param name="prewarmImmediately">構築時に初期数を確保するか。</param>
    public EffectPool(EffectDefinition definition, Transform parent, bool prewarmImmediately = true)
    {
        m_definition = definition;
        m_parent = parent;

        m_pool = new ObjectPool<EffectInstance>(
            CreateInstance,
            null,
            OnRelease,
            DestroyInstance,
            true,
            Mathf.Max(1, GetPrewarmTargetCount()),
            definition.MaxRetainedCount);

        if (prewarmImmediately)
        {
            Prewarm();
        }
    }

    /// <summary>
    /// 事前生成が完了しているか取得します。
    /// </summary>
    public bool IsPrewarmed =>
        !m_disposed && !m_isPrewarming && !m_hasPrewarmFailed &&
        m_pool.CountInactive >= GetPrewarmTargetCount();

    /// <summary>
    /// 事前生成に失敗したか取得します。
    /// </summary>
    public bool HasPrewarmFailed => m_hasPrewarmFailed;

    /// <summary>
    /// 使用中のエフェクト数を取得します。
    /// </summary>
    public int ActiveCount => m_active.Count;

    /// <summary>
    /// 未使用のエフェクト数を取得します。
    /// </summary>
    public int InactiveCount => m_pool.CountInactive;

    /// <summary>
    /// 初期生成数まで同期的に確保します。
    /// </summary>
    /// <returns>
    /// true：事前生成できました。
    /// false：生成できませんでした。
    /// </returns>
    public bool Prewarm()
    {
        while (TryPrewarmStep())
        {
        }

        return IsPrewarmed;
    }

    /// <summary>
    /// 事前生成を1ステップ進めます。分割ロードで使用します。
    /// </summary>
    /// <returns>
    /// true：1ステップ処理しました。
    /// false：完了済み、または失敗しました。
    /// </returns>
    public bool TryPrewarmStep()
    {
        if (m_disposed || m_hasPrewarmFailed)
        {
            return false;
        }

        int targetCount = GetPrewarmTargetCount();
        if (!m_isPrewarming)
        {
            if (m_pool.CountInactive >= targetCount)
            {
                return false;
            }

            // 途中再生中の事前生成は、空きの確保数を保証できないため拒否します。
            if (m_active.Count > 0)
            {
                Debug.LogWarning("[EffectPool] 再生中に事前生成はできません。");
                m_hasPrewarmFailed = true;
                return false;
            }

            m_isPrewarming = true;
        }

        // Getしたオブジェクトを保持することで、必要数だけ新規生成します。
        // 毎回Get直後にReleaseすると、同じ1個が使い回されてしまいます。
        EffectInstance instance = m_pool.Get();
        if (instance == null)
        {
            m_hasPrewarmFailed = true;
            CancelPrewarm();
            return false;
        }

        m_prewarmHeld.Add(instance);

        if (m_prewarmHeld.Count >= targetCount)
        {
            ReleasePrewarmHeld();
        }

        return true;
    }

    /// <summary>
    /// 分割事前生成を取り消し、途中確保分を戻します。
    /// </summary>
    public void CancelPrewarm()
    {
        if (!m_disposed)
        {
            ReleasePrewarmHeld();
        }
    }

    /// <summary>
    /// 空きインスタンスを取得します。
    /// </summary>
    /// <param name="allowRuntimeInstantiation">プール不足時の追加生成を許可するか。</param>
    /// <returns>使用可能なインスタンス。取得できなければnull。</returns>
    public EffectInstance Get(bool allowRuntimeInstantiation = true)
    {
        if (m_disposed || m_isPrewarming ||
            (m_definition.MaxActiveCount > 0 &&
             m_active.Count >= m_definition.MaxActiveCount))
        {
            return null;
        }

        // ObjectPool.Get()は空きがないとInstantiateするため、先に判定します。
        if (!allowRuntimeInstantiation && m_pool.CountInactive == 0)
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

    /// <summary>
    /// 終了したインスタンスをプールへ返却します。
    /// </summary>
    /// <param name="instance">返却対象。</param>
    public void Release(EffectInstance instance)
    {
        if (m_disposed || instance == null || !m_active.Remove(instance))
        {
            return;
        }

        m_pool.Release(instance);
    }

    /// <summary>
    /// プールを破棄します。
    /// </summary>
    public void Dispose()
    {
        if (m_disposed)
        {
            return;
        }

        ReleasePrewarmHeld();
        m_disposed = true;

        foreach (EffectInstance instance in m_active)
        {
            if (instance == null)
            {
                continue;
            }

            instance.ResetForReuse();
            Object.Destroy(instance.gameObject);
        }

        m_active.Clear();
        m_pool.Clear();
    }

    /// <summary>
    /// 初期生成目標を保持上限・同時再生上限内に制限します。
    /// </summary>
    private int GetPrewarmTargetCount()
    {
        int target = Mathf.Min(m_definition.InitialPoolSize, m_definition.MaxRetainedCount);
        if (m_definition.MaxActiveCount > 0)
        {
            target = Mathf.Min(target, m_definition.MaxActiveCount);
        }

        return Mathf.Max(0, target);
    }

    /// <summary>
    /// 事前生成中に借りたインスタンスをまとめて返却します。
    /// </summary>
    private void ReleasePrewarmHeld()
    {
        foreach (EffectInstance instance in m_prewarmHeld)
        {
            if (instance != null)
            {
                m_pool.Release(instance);
            }
        }

        m_prewarmHeld.Clear();
        m_isPrewarming = false;
    }

    /// <summary>
    /// 新しいインスタンスを生成・初期化します。
    /// </summary>
    private EffectInstance CreateInstance()
    {
        if (m_definition == null || m_definition.Prefab == null)
        {
            return null;
        }

        EffectInstance instance = Object.Instantiate(m_definition.Prefab, m_parent);
        if (!instance.Prepare(Release))
        {
            Object.Destroy(instance.gameObject);
            return null;
        }

        instance.gameObject.SetActive(false);
        return instance;
    }

    /// <summary>
    /// 再利用待ちに戻す際の初期化を実行します。
    /// </summary>
    private void OnRelease(EffectInstance instance)
    {
        if (instance == null)
        {
            return;
        }

        instance.ResetForReuse();
        instance.gameObject.SetActive(false);
    }

    /// <summary>
    /// 保持上限を超えたインスタンスを破棄します。
    /// </summary>
    private void DestroyInstance(EffectInstance instance)
    {
        if (instance != null)
        {
            Object.Destroy(instance.gameObject);
        }
    }
}
