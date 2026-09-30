using System.Collections.Generic;

/// <summary>
/// プレイヤーの攻撃に関するパラメータを保持します。
/// </summary>
public readonly struct PlayerAttackParameter
{
    /// <summary>
    /// 接地時パラメータを生成します。
    /// </summary>
    /// <param name="animationEventReceiver"> アニメーションイベントの受信元    </param>
    /// <param name="attackHitboxes">         攻撃時に有効化するヒットボックス  </param>
    /// <param name="baseHitsPerSecond">      多段ヒット攻撃の基本ヒットレート  </param>
    public PlayerAttackParameter(
        PlayerAnimationEventReceiver animationEventReceiver,
        List<AttackHitbox> attackHitboxes,
        float baseHitsPerSecond)
    {
        m_animationEventReceiver = animationEventReceiver;
        m_attackHitboxes = attackHitboxes;
        m_baseHitsPerSecond = baseHitsPerSecond;
    }

    /// <summary>
    /// アニメーションイベントの受信元を取得します。
    /// </summary>
    private PlayerAnimationEventReceiver m_animationEventReceiver { get; }

    /// <summary>
    /// 攻撃時に有効化するヒットボックスを取得します。
    /// </summary>
    private List<AttackHitbox> m_attackHitboxes { get; }

    /// <summary>
    /// 多段ヒット攻撃の基本ヒットレートを取得します。
    /// </summary>
    private float m_baseHitsPerSecond { get; }
}
