using UnityEngine;

/// <summary>
/// プレイヤーの歩行状態を管理します。
/// </summary>
public sealed class PlayerAttackingState
    : StateBase<PlayerStateMachineComponent>
{
    int m_currentComboStage = 0; // 攻撃の段数
    bool m_isNextAttackRequested;

    bool m_isFirstFrame;

    protected override void OnStartState()
    {
        Owner.AttackController.ClearAllEventHistory();
        m_isNextAttackRequested = false;
        m_currentComboStage = 1;
        Owner.AnimationPresenter.PlayAttackAnimation();
        Debug.Log("アタックアニメーションの有効化");

        m_isFirstFrame = true;

       
    }

    protected override void OnUpdate(float deltaTime)
    {
        if (m_isFirstFrame) 
        { 
            m_isFirstFrame = false;
            return;
        }

        if (Owner.InputReader.ConsumeAttackInput())
        {
            m_isNextAttackRequested = true;
        }

        if (Owner.AttackController.HasReceivedEvent(m_currentComboStage, PlayerAttackController.AttackAnimationEventType.FINISH_ANIMATION))
        {
            Debug.Log("アニメーション終了");
       
            if (m_isNextAttackRequested && Owner.AttackController.HasReceivedEvent(m_currentComboStage + 1, PlayerAttackController.AttackAnimationEventType.START_ANIMATION))
            {
                m_currentComboStage++;
                m_isNextAttackRequested = false;
            }
            else if (!m_isNextAttackRequested || m_currentComboStage >= 4)
            {
                Owner.AnimationPresenter.StopAttackAnimation();
                Machine.ChangeState<PlayerIdlingState>();
            }
        }
    }

    /// <summary>
    /// 一定間隔の更新処理を行います。
    /// </summary>
    protected override void OnFixedUpdate()
    {
        
    }
}