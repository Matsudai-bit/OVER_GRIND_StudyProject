using UnityEngine;

/// <summary>
/// ステージ1フェーズ1のアニメーション攻撃を実行します。
/// </summary>
public sealed class S1P1BossAttackState : StateBase<BossController>
{
    // Animator Trigger名
    private readonly string m_animationTriggerName;

    // 攻撃ID
    private readonly AttackIdentifier m_attackIdentifier;

    // Animator Trigger ID
    private int m_animationTriggerID;

    // 使用中のAnimationEventReceiver
    private AnimationEventReceiver m_animationEventReceiver;

    /// <summary>
    /// 攻撃ステートを生成します。
    /// </summary>
    /// <param name="animationTriggerName">Animator Trigger名。</param>
    /// <param name="attackIdentifier">攻撃ID。</param>
    public S1P1BossAttackState(
        string animationTriggerName,
        AttackIdentifier attackIdentifier)
    {
        m_animationTriggerName = animationTriggerName;
        m_attackIdentifier = attackIdentifier;
    }

    /// <summary>
    /// 攻撃を開始します。
    /// </summary>
    protected override void OnStartState()
    {
        if (string.IsNullOrEmpty(m_animationTriggerName) ||
            m_attackIdentifier == null ||
            Owner.AnimationController == null)
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);
            return;
        }

        m_animationEventReceiver =
            Owner.AnimationController.CurrentAnimationEventReceiver;

        if (m_animationEventReceiver == null)
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);
            return;
        }

        m_animationTriggerID =
            Animator.StringToHash(m_animationTriggerName);

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.RUNNING);

        m_animationEventReceiver.AttackEventReceived +=
            HandleAttackEvent;

        Owner.AnimationController.SetTrigger(
            m_animationTriggerID);
    }

    /// <summary>
    /// 攻撃AnimationEventを処理します。
    /// </summary>
    /// <param name="attackEventData">攻撃イベント情報。</param>
    private void HandleAttackEvent(AttackEventData attackEventData)
    {
        if (attackEventData == null ||
            attackEventData.AttackIdentifier != m_attackIdentifier)
        {
            return;
        }

        switch (attackEventData.AttackEventType)
        {
            case AttackEventType.HITBOX_ENABLE:
                Owner.AttackHitboxRegistry?.EnableHitbox(
                    m_attackIdentifier, Owner.AttackDamageController);
                break;

            case AttackEventType.HITBOX_DISABLE:
                Owner.AttackHitboxRegistry?.DisableHitbox(
                    m_attackIdentifier);
                break;

            case AttackEventType.ANIMATION_END:
                Owner.SetStateExecutionStatus(
                    StateExecutionStatus.SUCCEEDED);
                break;
        }
    }

    /// <summary>
    /// 攻撃を終了します。
    /// </summary>
    protected override void OnExitState()
    {
        if (m_animationEventReceiver != null)
        {
            m_animationEventReceiver.AttackEventReceived -=
                HandleAttackEvent;
        }

        Owner.AttackHitboxRegistry?.DisableHitbox(
            m_attackIdentifier);

        if (Owner.GetStateExecutionStatus() ==
            StateExecutionStatus.RUNNING)
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);
        }
    }
}
