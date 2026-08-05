using System;
using System.Collections;
using UnityEngine;

public class P1BossAttackState : StateBase<P1BossController>
{
    private string m_attackAnimationTriggerName;
    private P1AttackType m_attackType;

    private Action<AttackEventData> m_animationEndedAction;
    private Action<AttackEventData> m_enableHitBox;
    private Action<AttackEventData> m_disableHitBox;

    public P1BossAttackState(string attackAnimationTriggerName, P1AttackType attackType)
    {
        m_attackAnimationTriggerName = attackAnimationTriggerName;
        m_attackType = attackType;
        m_animationEndedAction = (AttackEventData data) =>
        {
            if (data.AttackType== attackType && data.AttackEventType == AttackEventType.ANIMATION_END)
                Owner.SetStateExecutionStatus(StateExecutionStatus.SUCCEEDED);
        };

        m_enableHitBox = (AttackEventData data) =>
        {
            if (data.AttackType == attackType && data.AttackEventType == AttackEventType.HITBOX_ENABLE)
            {
                Debug.Log(m_attackAnimationTriggerName + "のヒットボックス有効化");
                Owner.attackHitBox[m_attackType].EnableHitbox();

            }
                
        };
        m_disableHitBox = (AttackEventData data) =>
        {
            if (data.AttackType == attackType && data.AttackEventType == AttackEventType.HITBOX_DISABLE)
            {
                Debug.Log(m_attackAnimationTriggerName + "のヒットボックス無効か");
                Owner.attackHitBox[m_attackType].DisableHitbox();

            }

        };

    }
    protected override void OnStartState()
    {
        Owner.stateText.text = "Attack_" + m_attackType.ToString();

        Debug.Log(m_attackAnimationTriggerName + "の開始");
 
        Owner.SetStateExecutionStatus(StateExecutionStatus.RUNNING);


        Owner.Animator.SetTrigger(m_attackAnimationTriggerName);

        Owner.AnimationEventReceiver.AttackEventReceived+= m_animationEndedAction;
        Owner.AnimationEventReceiver.AttackEventReceived+= m_enableHitBox;
        Owner.AnimationEventReceiver.AttackEventReceived+= m_disableHitBox;
    }



    protected override void OnUpdate(float deltaTime)
    {
        
    }


    protected override void OnExitState()
    {
        Owner.AnimationEventReceiver.AttackEventReceived -= m_animationEndedAction;
        Owner.AnimationEventReceiver.AttackEventReceived -= m_enableHitBox;
        Owner.AnimationEventReceiver.AttackEventReceived -= m_disableHitBox;

        Debug.Log(m_attackAnimationTriggerName + "の終了");
    }



}
