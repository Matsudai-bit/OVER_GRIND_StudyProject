using UnityEngine;

/// <summary>
/// プレイヤーの歩行状態を管理します。
/// </summary>
public sealed class PlayerAttackingState
    : StateBase<PlayerStateMachineComponent>
{
    int m_currentComboStage = 0; // 攻撃の段数
    bool m_isNextAttackRequested;


    protected override void OnStartState()
    {
        Owner.AttackController.ClearAllEventHistory();
        m_isNextAttackRequested = false;
        m_currentComboStage = 1;
        Owner.AnimationPresenter.PlayAttackAnimation();
        Debug.Log("アタックアニメーションの有効化");

       
    }

    protected override void OnUpdate(float deltaTime)
    {
 
        if (Owner.InputReader.ConsumeAttackInput())
        {
            m_isNextAttackRequested = true;
        }

        if (Owner.AttackController.HasReceivedEvent(m_currentComboStage, PlayerAttackController.AttackAnimationEventType.ENABLE_HITBOX))
        {
            Owner.AttackController.EnableAttackHitboxes();
            Owner.AttackController.ClearEventHistory(m_currentComboStage, PlayerAttackController.AttackAnimationEventType.ENABLE_HITBOX) ;
        }

        if (Owner.AttackController.HasReceivedEvent(m_currentComboStage, PlayerAttackController.AttackAnimationEventType.FINISH_ANIMATION))
        {
            Debug.Log("アニメーション終了");
       
            if (m_isNextAttackRequested && Owner.AttackController.HasReceivedEvent(m_currentComboStage + 1, PlayerAttackController.AttackAnimationEventType.START_ANIMATION))
            {
                if (Owner.AttackController.HasReceivedEvent(m_currentComboStage, PlayerAttackController.AttackAnimationEventType.DISABLE_HITBOX))
                {
                    Owner.AttackController.DisableAttackHitboxes();
                }
                m_currentComboStage++;
                m_isNextAttackRequested = false;
            }
            else if (!m_isNextAttackRequested || m_currentComboStage >= 4)
            {
                Machine.ChangeState<PlayerIdlingState>();
            }

            if (Owner.Monitor.IsGrounded && Owner.InputReader.HasJumpInput)
            {
                Machine.ChangeState<PlayerJumpingState>();
                return;
            }
        }

    }

    protected override void OnExitState()
    {
        Owner.AnimationPresenter.StopAttackAnimation();
        Owner.AttackController.DisableAttackHitboxes();

    }
}