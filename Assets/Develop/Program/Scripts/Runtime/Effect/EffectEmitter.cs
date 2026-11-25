using UnityEngine;

/// <summary>
/// ゲームオブジェクトからローカル型・共通型エフェクトを再生します。
/// </summary>
[DisallowMultipleComponent]
public sealed class EffectEmitter : MonoBehaviour
{
    [SerializeField, Header("シーン共通のプール型管理者")]
    private EffectService m_effectService;
    [SerializeField, Header("キャラクター専用管理者（任意）")]
    private EffectLocalController m_localController;
    [SerializeField, Header("IDからTransformを引くアンカー表（任意）")]
    private EffectAnchorRegistry m_anchorRegistry;
    [SerializeField, Header("同じIDがローカル・共通の両方に存在する場合")]
    private bool m_playBothWhenRegistered = false;

    /// <summary>
    /// IDに登録されたエフェクトを再生します。通常はローカル優先です。
    /// </summary>
    /// <param name="id">エフェクトID。</param>
    /// <returns>個別または一括停止に利用できるハンドル。</returns>
    public EffectHandle Play(EffectId id)
    {
        bool hasLocal = m_localController != null && m_localController.Contains(id);
        if (!hasLocal)
        {
            return PlayAttached(id);
        }

        if (!m_playBothWhenRegistered || m_effectService == null || !m_effectService.Contains(id))
        {
            return m_localController.Play(id);
        }

        // ローカルと共通に同じIDが登録されていれば両方を再生します。
        EffectHandle localHandle = m_localController.Play(id);
        if (!localHandle.IsValid)
        {
            return default;
        }
        EffectHandle sharedHandle = PlayAttached(id);
        if (!sharedHandle.IsValid)
        {
            localHandle.Stop(EffectStopMode.IMMEDIATE);
            return default;
        }
        return EffectHandle.Combine(new[] { localHandle, sharedHandle });
    }

    /// <summary>
    /// ローカル型のみを再生します。
    /// </summary>
    public EffectHandle PlayLocal(EffectId id)
    {
        if (m_localController == null)
        {
            Debug.LogWarning("[EffectEmitter] LocalControllerがありません。", this);
            return default;
        }
        return m_localController.Play(id);
    }

    /// <summary>
    /// 共通プール型を指定したワールド位置で発生させます。
    /// </summary>
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
        // 爆発など追従しないPrefabにはアンカー表を渡しません。
        return m_effectService.Play(id,
            new EffectSpawnRequest(position, rotation, true));
    }

    /// <summary>
    /// 登録されたアンカーの座標に共通プール型エフェクトを発生させます。
    /// </summary>
    /// <param name="id">エフェクトID。</param>
    /// <param name="anchorId">発生位置のアンカーID。</param>
    /// <returns>再生を操作するハンドル。失敗時は無効です。</returns>
    public EffectHandle PlayAtAnchor(EffectId id, EffectAnchorId anchorId)
    {
        if (m_anchorRegistry == null ||
            !m_anchorRegistry.TryGetTarget(anchorId, out Transform anchor))
        {
            Debug.LogWarning($"[EffectEmitter] アンカーを取得できません: {anchorId}", this);
            return default;
        }

        // 発生時点のワールド座標を使用し、その後は追従させません。
        return PlayAt(id, anchor.position, anchor.rotation);
    }

    /// <summary>
    /// 共通プール型をアンカー表を使って再生します。
    /// </summary>
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

    /// <summary>
    /// モデルを交換したときにアンカーを一括更新します。
    /// </summary>
    /// <param name="newModelRoot">新しいモデルルート。</param>
    public void SetModelRoot(Transform newModelRoot)
    {
        // 旧モデルを参照するローカルエフェクトを安全のため停止します。
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
