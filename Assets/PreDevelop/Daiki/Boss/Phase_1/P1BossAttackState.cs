using System;
using System.Collections;
using UnityEngine;

public class P1BossAttackState : StateBase<P1BossController>
{
    private string m_attackAnimationTriggerName;
    private P1AttackType m_attackType;

    private Action<AttackEventData> m_animationEndedAction;
    private Action<AttackEventData> m_animationAttackInteractAction;

    public P1BossAttackState(string attackAnimationTriggerName, P1AttackType attackType)
    {
        m_attackAnimationTriggerName = attackAnimationTriggerName;
        m_attackType = attackType;
        m_animationEndedAction = (AttackEventData data) =>
        {
            if (data.AttackType== attackType && data.AttackEventType == AttackEventType.ANIMATION_END)
                Owner.SetStateExecutionStatus(StateExecutionStatus.SUCCEEDED);
        };

        m_animationAttackInteractAction = (AttackEventData data) =>
        {
            if (data.AttackType == attackType && data.AttackEventType == AttackEventType.INTERACT)
                Debug.Log(m_attackAnimationTriggerName + "の攻撃インタラクト");
        };

    }
    protected override void OnStartState()
    {
        Debug.Log(m_attackAnimationTriggerName + "の開始");
 
        Owner.SetStateExecutionStatus(StateExecutionStatus.RUNNING);


        Owner.Animator.SetTrigger(m_attackAnimationTriggerName);

        Owner.AnimationEventReceiver.AttackEventReceived+= m_animationEndedAction;
        Owner.AnimationEventReceiver.AttackEventReceived+= m_animationAttackInteractAction;
    }



    protected override void OnUpdate(float deltaTime)
    {
        
    }


    protected override void OnExitState()
    {
        Owner.AnimationEventReceiver.AttackEventReceived -= m_animationEndedAction;
        Owner.AnimationEventReceiver.AttackEventReceived -= m_animationAttackInteractAction;

        Debug.Log(m_attackAnimationTriggerName + "の終了");
    }



}
