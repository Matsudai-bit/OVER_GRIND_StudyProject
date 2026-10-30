using UnityEngine;

/// <summary>ゲームオブジェクトからローカル型・共通型のエフェクトを呼び出します。</summary>
[DisallowMultipleComponent]
public sealed class EffectEmitter : MonoBehaviour
{
    [SerializeField, Header("シーン共通のプール型管理者")]
    private EffectService m_effectService;
    [SerializeField, Header("キャラクター専用管理者（任意）")]
    private EffectLocalController m_localController;
    [SerializeField, Header("IDからTransformを引くアンカー表（任意）")]
    private EffectAnchorRegistry m_anchorRegistry;

    /// <summary>ローカル優先でIDから再生します。</summary>
    /// <param name="id">エフェクトID。</param>
    /// <returns>再生ハンドル。</returns>
    public EffectHandle Play(EffectId id)
    {
        if (m_localController != null && m_localController.Contains(id))
        {
            return m_localController.Play(id);
        }
        return PlayAttached(id);
    }

    /// <summary>ローカル配置のエフェクトを再生します。</summary>
    public EffectHandle PlayLocal(EffectId id)
    {
        if (m_localController == null)
        {
            Debug.LogWarning("[EffectEmitter] LocalControllerがありません。", this);
            return default;
        }
        return m_localController.Play(id);
    }

    /// <summary>共通プール型を指定ワールド位置で発生させます。</summary>
    /// <param name="id">エフェクトID。</param>
    /// <param name="position">発生位置。</param>
    /// <param name="rotation">発生回転。</param>
    public EffectHandle PlayAt(EffectId id, Vector3 position, Quaternion rotation)
    {
        if (m_effectService == null)
        {
            Debug.LogWarning("[EffectEmitter] EffectServiceがありません。", this);
            return default;
        }
        // 爆発など追従しないPrefabはAnchorRegistryを指定しません。
        return m_effectService.Play(id,
            new EffectSpawnRequest(position, rotation, true));
    }

    /// <summary>共通プール型をアンカー表を使って再生します。</summary>
    public EffectHandle PlayAttached(EffectId id)
    {
        if (m_effectService == null)
        {
            Debug.LogWarning("[EffectEmitter] EffectServiceがありません。", this);
            return default;
        }
        return m_effectService.Play(id,
            new EffectSpawnRequest(transform.position, transform.rotation, true, m_anchorRegistry));
    }

    /// <summary>モデルを交換したときに全アンカーを一括更新します。</summary>
    /// <param name="newModelRoot">新しいモデルルート。</param>
    public void SetModelRoot(Transform newModelRoot)
    {
        // 旧モデルを参照する再生中のローカルエフェクトを終了させます。
        if (m_localController != null)
        {
            m_localController.StopAll();
        }
        if (m_anchorRegistry != null)
        {
            m_anchorRegistry.SetModelRoot(newModelRoot);
        }
    }
}
