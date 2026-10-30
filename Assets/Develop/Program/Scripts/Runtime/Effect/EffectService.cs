using System.Collections.Generic;
using UnityEngine;

/// <summary>シーン共通のプール型エフェクトを再生します。</summary>
[DisallowMultipleComponent]
public sealed class EffectService : MonoBehaviour
{
    [SerializeField, Header("エフェクト定義一覧")]
    private EffectCatalog m_catalog;
    [SerializeField, Header("生成オブジェクトの親（省略時は自身）")]
    private Transform m_poolRoot;
    private readonly Dictionary<EffectId, EffectPool> m_pools =
        new Dictionary<EffectId, EffectPool>();

    /// <summary>指定IDのエフェクトを発生させます。</summary>
    /// <param name="id">エフェクトID。</param>
    /// <param name="request">発生条件。</param>
    /// <returns>有効な再生ハンドル。失敗時は無効です。</returns>
    public EffectHandle Play(EffectId id, EffectSpawnRequest request)
    {
        if (m_catalog == null || !m_catalog.TryGetDefinition(id, out EffectDefinition definition))
        {
            Debug.LogWarning($"[EffectService] IDが登録されていません: {id}", this);
            return default;
        }
        if (!m_pools.TryGetValue(id, out EffectPool pool))
        {
            pool = new EffectPool(definition, m_poolRoot != null ? m_poolRoot : transform);
            m_pools.Add(id, pool);
        }
        EffectInstance instance = pool.Get();
        if (instance == null)
        {
            Debug.LogWarning($"[EffectService] 同時再生上限に達しました: {id}", this);
            return default;
        }
        if (!instance.Play(request, definition.PlaybackMode))
        {
            pool.Release(instance);
            return default;
        }
        return new EffectHandle(instance, instance.Generation);
    }

    /// <summary>シーン終了時にプールを破棄します。</summary>
    private void OnDestroy()
    {
        foreach (EffectPool pool in m_pools.Values)
        {
            pool.Dispose();
        }
        m_pools.Clear();
    }
}
