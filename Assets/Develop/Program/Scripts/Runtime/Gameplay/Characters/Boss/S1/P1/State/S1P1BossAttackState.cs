using NUnit.Framework.Internal.Commands;
using UnityEngine;

/// <summary>
/// ステージ1フェーズ1のアニメーション攻撃を実行します。
/// </summary>
public sealed class S1P1BossAttackState : StateBase<BossController>
{
    // Animator Trigger名

    // 攻撃ID
    private  AttackIdentifier m_attackIdentifier;

    // Animator Trigger ID
    private int m_animationTriggerID;

    // 使用中のAnimationEventReceiver
    private AnimationEventReceiver m_animationEventReceiver;

    private S1P1BossReferences m_references;

    /// <summary>
    /// 攻撃を開始します。
    /// </summary>
    protected override void OnStartState()
    {
        if (!Owner.PhaseController.TryGetCurrentPhaseComponent(
                     out m_references))
        {
            Debug.LogError("リファレンスが取得できません");
        }
        // 攻撃の適用
        ApplyAttackSetting(GetAttackType());

        if (m_attackIdentifier == null ||
            Owner.AnimationController == null)
        {
            Debug.LogError("設定ミス");
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);
            return;
        }


        m_animationEventReceiver =
            Owner.AnimationController.CurrentAnimationEventReceiver;

        if (m_animationEventReceiver == null)
        {
            Debug.LogError("設定ミス");

            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);
            return;
        }
        Owner.SetStateExecutionStatus(
            StateExecutionStatus.RUNNING);

        Owner.AnimationController.SetTrigger(
            m_animationTriggerID);
    }

    /// <summary>
    /// 攻撃AnimationEventを処理します。
    /// </summary>
    /// <param name="attackEventData">攻撃イベント情報。</param>
    private void HandleAttackEvent(AttackEventData attackEventData)
    {


        switch (attackEventData.AttackEventType)
        {
            case AttackEventType.HITBOX_ENABLE:
                Owner.EnableAttackHitboxes(m_attackIdentifier);
                break;

            case AttackEventType.HITBOX_DISABLE:
                Owner.AttackHitboxRegistry?.DisableHitboxes(
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

        Owner.AttackHitboxRegistry?.DisableHitboxes(
            m_attackIdentifier);

        if (Owner.GetStateExecutionStatus() ==
            StateExecutionStatus.RUNNING)
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);
        }
    }

    private S1P1BossAttackType GetAttackType()
    {
        if (
            Vector3.Distance(m_references.LeftLegTransform.position, m_references.PlayerTransform.position) 
            >
            Vector3.Distance(m_references.RightLegTransform.position, m_references.PlayerTransform.position))
        {
            return S1P1BossAttackType.RIGHT_LEG;
        }
        return S1P1BossAttackType.LEFT_LEG;
    }

    private bool ApplyAttackSetting(S1P1BossAttackType attackType)
    {
        S1P1BossAttackSettings attackSettings =
          Owner.GetComponentInChildren<
              S1P1BossAttackSettings>(true);

        if (attackSettings == null)
        {
            Debug.LogError(
                $"{nameof(S1P1BossAttackSettings)}が見つかりません。");

            return false;
        }

        if (!attackSettings.TryGetAttackSetting(
                attackType,
                out m_attackIdentifier,
                out string animationTriggerName))
        {
            Debug.LogError(
                $"{attackType}の攻撃設定がありません。");

            return false;
        }


        if (string.IsNullOrEmpty(animationTriggerName) ||
           m_attackIdentifier == null ||
           Owner.AnimationController == null)
        {

            return false;
        }
        m_animationTriggerID =
            Animator.StringToHash(animationTriggerName);

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.RUNNING);

        Owner.AnimationController.CurrentAnimationEventReceiver.AttackEventReceived +=
            HandleAttackEvent;

        Owner.AnimationController.SetTrigger(
            m_animationTriggerID);

        return true;

    }
}

