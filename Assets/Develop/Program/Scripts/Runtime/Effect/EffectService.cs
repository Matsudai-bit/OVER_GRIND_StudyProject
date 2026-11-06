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

    // 同じIDの異なる定義にも、それぞれプールを割り当てます。
    private readonly Dictionary<EffectDefinition, EffectPool> m_pools =
        new Dictionary<EffectDefinition, EffectPool>();

    /// <summary>
    /// 指定IDが共通エフェクトに登録されているか取得します。
    /// </summary>
    public bool Contains(EffectId id)
    {
        return m_catalog != null && m_catalog.TryGetDefinitions(id, out _);
    }

    /// <summary>
    /// 指定IDに登録されたすべてのエフェクトを発生させます。
    /// </summary>
    /// <param name="id">エフェクトID。</param>
    /// <param name="request">発生条件。</param>
    /// <returns>一括停止できるハンドル。失敗時は無効です。</returns>
    public EffectHandle Play(EffectId id, EffectSpawnRequest request)
    {
        if (m_catalog == null || !m_catalog.TryGetDefinitions(id, out IReadOnlyList<EffectDefinition> definitions))
        {
            Debug.LogWarning($"[EffectService] IDが登録されていません: {id}", this);
            return default;
        }

        List<EffectHandle> handles = new List<EffectHandle>(definitions.Count);
        foreach (EffectDefinition definition in definitions)
        {
            if (!m_pools.TryGetValue(definition, out EffectPool pool))
            {
                pool = new EffectPool(definition, m_poolRoot != null ? m_poolRoot : transform);
                m_pools.Add(definition, pool);
            }
            EffectInstance instance = pool.Get();
            if (instance == null || !instance.Play(request, definition.PlaybackMode))
            {
                // 使用できなかったインスタンスを返却してからグループ全体を中止します。
                if (instance != null)
                {
                    pool.Release(instance);
                }
                foreach (EffectHandle started in handles)
                {
                    started.Stop(EffectStopMode.IMMEDIATE);
                }
                Debug.LogWarning($"[EffectService] グループ再生に失敗しました: {id} / {definition.name}", this);
                return default;
            }
            handles.Add(new EffectHandle(instance, instance.Generation));
        }
        return EffectHandle.Combine(handles);
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
