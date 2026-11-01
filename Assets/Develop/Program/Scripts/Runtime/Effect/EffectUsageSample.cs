using UnityEngine;

/// <summary>エフェクトシステムの呼び出し例です。</summary>
[DisallowMultipleComponent]
public sealed class EffectUsageSample : MonoBehaviour
{
    [SerializeField, Header("発生元")]
    private EffectEmitter m_emitter;
    [SerializeField, Header("爆発の発生地点")]
    private Transform m_explosionPoint;
    private EffectHandle m_loopHandle;

    /// <summary>指定地点で爆発させます。</summary>
    [ContextMenu("Effects/爆発を発生")]
    public void PlayExplosion()
    {
        if (m_emitter == null || m_explosionPoint == null)
        {
            return;
        }
        m_emitter.PlayAt(EffectId.COMMON_EXPLOSION,
            m_explosionPoint.position, m_explosionPoint.rotation);
    }

    /// <summary>右腕・左腕等の複数アンカーに追従するボス専用エフェクトを再生します。</summary>
    [ContextMenu("Effects/ボス攻撃を発生")]
    public void PlayLocalAttack()
    {
        m_emitter?.Play(EffectId.S1P1_BOSS_ATTACK);
    }

    /// <summary>繰り返しエフェクトを開始します。</summary>
    [ContextMenu("Effects/ループ開始")]
    public void StartLoop()
    {
        if (m_emitter != null && !m_loopHandle.IsValid)
        {
            m_loopHandle = m_emitter.Play(EffectId.S1P1_BOSS_CHARGE);
        }
    }

    /// <summary>放出を止めて自然消滅を待ちます。</summary>
    [ContextMenu("Effects/ループ自然停止")]
    public void StopLoop()
    {
        m_loopHandle.Stop(EffectStopMode.GRACEFUL);
    }
}
