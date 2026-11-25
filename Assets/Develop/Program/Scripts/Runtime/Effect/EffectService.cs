using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// シーン共通のエフェクトをID単位で一括再生します。
/// </summary>
[DisallowMultipleComponent]
public sealed class EffectService : MonoBehaviour
{
    [SerializeField, Header("エフェクト定義一覧")]
    private EffectCatalog m_catalog;

    [SerializeField, Header("生成オブジェクトの親（省略時は自身）")]
    private Transform m_poolRoot;

    [SerializeField, Header("ゲームプレイ中にプールを追加生成する")]
    private bool m_allowRuntimeInstantiation = true;

    [SerializeField, Header("プール不足をログに出力する")]
    private bool m_logPoolExhaustion = false;

    // 同じIDの異なる定義にも、それぞれプールを割り当てます。
    private readonly Dictionary<EffectDefinition, EffectPool> m_pools =
        new Dictionary<EffectDefinition, EffectPool>();

    // 追加生成禁止などによって再生できなかった回数
    private int m_poolExhaustionCount;

    /// <summary>
    /// プール不足の発生回数を取得します。
    /// </summary>
    public int PoolExhaustionCount => m_poolExhaustionCount;

    /// <summary>
    /// ランタイムの追加生成許可状態を取得します。
    /// </summary>
    public bool AllowRuntimeInstantiation => m_allowRuntimeInstantiation;

    /// <summary>
    /// ゲームプレイ中の追加生成を許可または禁止します。
    /// </summary>
    public void SetRuntimeInstantiationAllowed(bool isAllowed)
    {
        m_allowRuntimeInstantiation = isAllowed;
    }

    /// <summary>
    /// プール不足カウンターをクリアします。
    /// </summary>
    public void ResetPoolExhaustionCount()
    {
        m_poolExhaustionCount = 0;
    }

    /// <summary>
    /// 指定IDが共通エフェクトに登録されているか取得します。
    /// </summary>
    public bool Contains(EffectId id)
    {
        return m_catalog != null && m_catalog.TryGetDefinitions(id, out _);
    }

    /// <summary>
    /// 指定IDの共通エフェクトを同期的に事前生成します。
    /// </summary>
    /// <param name="id">対象ID。</param>
    /// <returns>
    /// true：必要数を確保しました。
    /// false：登録不備などで確保できませんでした。
    /// </returns>
    public bool Prewarm(EffectId id)
    {
        if (!TryGetDefinitions(id, out IReadOnlyList<EffectDefinition> definitions))
        {
            return false;
        }

        foreach (EffectDefinition definition in definitions)
        {
            EffectPool pool = GetOrCreatePool(definition, false);
            if (pool == null || !pool.Prewarm())
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 指定IDの初期生成を複数フレームへ分散します。
    /// </summary>
    /// <param name="id">対象ID。</param>
    /// <param name="instancesPerFrame">1フレーム当たりの処理数。</param>
    /// <param name="completed">完了通知。trueは成功、falseは失敗。</param>
    public IEnumerator PrewarmIncrementally(
        EffectId id,
        int instancesPerFrame,
        Action<bool> completed = null)
    {
        if (!TryGetDefinitions(id, out IReadOnlyList<EffectDefinition> definitions))
        {
            completed?.Invoke(false);
            yield break;
        }

        int stepCount = 0;
        int limit = Mathf.Max(1, instancesPerFrame);

        foreach (EffectDefinition definition in definitions)
        {
            EffectPool pool = GetOrCreatePool(definition, false);
            if (pool == null)
            {
                completed?.Invoke(false);
                yield break;
            }

            while (pool.TryPrewarmStep())
            {
                stepCount++;
                if (stepCount >= limit)
                {
                    stepCount = 0;
                    yield return null;
                }
            }

            if (!pool.IsPrewarmed)
            {
                completed?.Invoke(false);
                yield break;
            }
        }

        completed?.Invoke(true);
    }

    /// <summary>
    /// 分割事前生成を中断し、一時確保中のインスタンスを返却します。
    /// </summary>
    public void CancelPrewarm()
    {
        foreach (EffectPool pool in m_pools.Values)
        {
            pool.CancelPrewarm();
        }
    }

    /// <summary>
    /// 指定IDに登録されたすべてのエフェクトを発生させます。
    /// </summary>
    /// <param name="id">エフェクトID。</param>
    /// <param name="request">発生条件。</param>
    /// <returns>一括停止できるハンドル。失敗時は無効です。</returns>
    public EffectHandle Play(EffectId id, EffectSpawnRequest request)
    {
        if (!TryGetDefinitions(id, out IReadOnlyList<EffectDefinition> definitions))
        {
            return default;
        }

        // 単一の定義は一時Listを作らずに再生します。
        if (definitions.Count == 1)
        {
            return PlayDefinition(definitions[0], request);
        }

        // 複数定義は既存の一括停止・終了通知仕様を維持します。
        List<EffectHandle> handles = new List<EffectHandle>(definitions.Count);
        foreach (EffectDefinition definition in definitions)
        {
            EffectHandle handle = PlayDefinition(definition, request);
            if (!handle.IsValid)
            {
                foreach (EffectHandle started in handles)
                {
                    started.Stop(EffectStopMode.IMMEDIATE);
                }

                return default;
            }

            handles.Add(handle);
        }

        return EffectHandle.Combine(handles);
    }

    /// <summary>
    /// 1つの定義の再生を実行します。
    /// </summary>
    private EffectHandle PlayDefinition(EffectDefinition definition, EffectSpawnRequest request)
    {
        if (definition == null)
        {
            return default;
        }

        // 追加生成禁止時は、事前生成されていないプールを作成しません。
        if (!m_pools.TryGetValue(definition, out EffectPool pool))
        {
            if (!m_allowRuntimeInstantiation)
            {
                RecordPoolExhaustion(definition);
                return default;
            }

            pool = GetOrCreatePool(definition, true);
        }

        EffectInstance instance = pool?.Get(m_allowRuntimeInstantiation);
        if (instance == null)
        {
            RecordPoolExhaustion(definition);
            return default;
        }

        if (!instance.Play(request, definition.PlaybackMode))
        {
            pool.Release(instance);
            return default;
        }

        return new EffectHandle(instance, instance.Generation);
    }

    /// <summary>
    /// 定義に対応するプールを1つだけ作成します。
    /// </summary>
    private EffectPool GetOrCreatePool(
        EffectDefinition definition,
        bool prewarmImmediately)
    {
        if (definition == null || definition.Prefab == null)
        {
            return null;
        }

        if (m_pools.TryGetValue(definition, out EffectPool pool))
        {
            return pool;
        }

        pool = new EffectPool(
            definition,
            m_poolRoot != null ? m_poolRoot : transform,
            prewarmImmediately);

        m_pools.Add(definition, pool);
        return pool;
    }

    /// <summary>
    /// 登録済み定義を取得します。
    /// </summary>
    private bool TryGetDefinitions(
        EffectId id,
        out IReadOnlyList<EffectDefinition> definitions)
    {
        if (m_catalog != null && m_catalog.TryGetDefinitions(id, out definitions))
        {
            return true;
        }

        definitions = null;
        Debug.LogWarning($"[EffectService] IDが登録されていません: {id}", this);
        return false;
    }

    /// <summary>
    /// プール不足を記録します。
    /// </summary>
    private void RecordPoolExhaustion(EffectDefinition definition)
    {
        m_poolExhaustionCount++;
        if (m_logPoolExhaustion)
        {
            Debug.LogWarning($"[EffectService] 空きがありません: {definition.name}", this);
        }
    }

    /// <summary>
    /// シーン終了時にプールを破棄します。
    /// </summary>
    private void OnDestroy()
    {
        foreach (EffectPool pool in m_pools.Values)
        {
            pool.Dispose();
        }

        m_pools.Clear();
    }
}
