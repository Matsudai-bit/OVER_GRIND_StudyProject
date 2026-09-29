using UnityEngine;

/// <summary>
/// ステージ1フェーズ2の排熱攻撃を実行します。
/// </summary>
public sealed class S1P2BossHeatVentState :
    StateBase<BossController>
{
    /// <summary>
    /// 排熱攻撃の進行状態です。
    /// </summary>
    private enum HeatExhaustState
    {
        NONE,
        PREPARATION,
        EXHAUSTING,
        ENDING
    }

    // 攻撃ID
    private AttackIdentifier m_attackIdentifier;

    // Animator Trigger ID
    private int m_animationTriggerID;

    // 現在の排熱攻撃状態
    private HeatExhaustState m_currentState;

    // S1P2固有参照
    private S1P2BossReferences m_references;

    // 排熱状態パラメータ
    private S1P2BossHeatVentStateParameters m_parameters;

    // AnimationEvent受信
    private AnimationEventReceiver m_animationEventReceiver;

    /// <summary>
    /// 排熱攻撃を開始します。
    /// </summary>
    protected override void OnStartState()
    {
        m_currentState =
            HeatExhaustState.NONE;

        Owner.Motor?.StopHorizontalMovement();

        if (!TryGetParameters())
        {
            Debug.LogError(
                "排熱状態のパラメータを取得できませんでした。");

            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);

            return;
        }

        if (!ApplyAttackSetting())
        {
            Debug.LogError(
                "排熱攻撃を開始できませんでした。");

            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);

            return;
        }

        m_currentState =
            HeatExhaustState.PREPARATION;

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.RUNNING);
    }

    /// <summary>
    /// 排熱攻撃を終了します。
    /// </summary>
    protected override void OnExitState()
    {
        StartCoolTimeIfSucceeded();

        if (m_animationEventReceiver != null)
        {
            m_animationEventReceiver.AttackEventReceived -=
                HandleAttackEvent;
        }

        if (m_attackIdentifier != null)
        {
            Owner.AttackHitboxRegistry?.DisableHitboxes(
                m_attackIdentifier);
        }

        m_currentState =
            HeatExhaustState.NONE;

        if (Owner.GetStateExecutionStatus() ==
            StateExecutionStatus.RUNNING)
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);
        }

        m_references = null;
        m_parameters = null;
        m_animationEventReceiver = null;
    }

    /// <summary>
    /// 現在フェーズの排熱状態パラメータを取得します。
    /// </summary>
    /// <returns>
    /// true：取得できました。
    /// false：取得できませんでした。
    /// </returns>
    private bool TryGetParameters()
    {
        if (Owner.PhaseController == null ||
            !Owner.PhaseController.TryGetCurrentPhaseComponent(
                out m_references) ||
            m_references.StateParameterAsset == null)
        {
            return false;
        }

        m_parameters =
            m_references.StateParameterAsset.HeatVent;

        return m_parameters != null;
    }

    /// <summary>
    /// 正常終了した排熱攻撃のクールタイムを開始します。
    /// </summary>
    private void StartCoolTimeIfSucceeded()
    {
        //if (Owner.GetStateExecutionStatus() !=
        //    StateExecutionStatus.SUCCEEDED ||
        //    m_references == null ||
        //    m_references.DecisionParameterAsset == null)
        //{
        //    return;
        //}

        //BossStateCoolTimeManager coolTimeManager =
        //    Owner.GetComponent<BossStateCoolTimeManager>();

        //if (coolTimeManager == null)
        //{
        //    return;
        //}

        //coolTimeManager.StartCoolTime<S1P2BossHeatVentState>(
        //    m_references.DecisionParameterAsset.HeatExhaust.CoolTime);
    }

    /// <summary>
    /// 排熱攻撃の設定を適用します。
    /// </summary>
    /// <returns>
    /// true：設定できました。
    /// false：設定できませんでした。
    /// </returns>
    private bool ApplyAttackSetting()
    {
        S1P2BossAttackSettings attackSettings =
            Owner.GetComponentInChildren<
                S1P2BossAttackSettings>(true);

        if (attackSettings == null)
        {
            return false;
        }

        if (!attackSettings.TryGetAttackSetting(
                S1P2BossAttackType.HEAT_VENT,
                out m_attackIdentifier,
                out string animationTriggerName))
        {
            return false;
        }

        if (string.IsNullOrEmpty(animationTriggerName) ||
            m_attackIdentifier == null ||
            Owner.AnimationController == null)
        {
            return false;
        }

        m_animationEventReceiver =
            Owner.AnimationController.CurrentAnimationEventReceiver;

        if (m_animationEventReceiver == null)
        {
            return false;
        }

        m_animationTriggerID =
            Animator.StringToHash(
                animationTriggerName);

        m_animationEventReceiver.AttackEventReceived +=
            HandleAttackEvent;

        Owner.AnimationController.SetTrigger(
            m_animationTriggerID);

        return true;
    }

    /// <summary>
    /// 排熱攻撃のAnimationEventを処理します。
    /// </summary>
    /// <param name="attackEventData">攻撃イベント情報。</param>
    private void HandleAttackEvent(
        AttackEventData attackEventData)
    {
        switch (attackEventData.AttackEventType)
        {
            case AttackEventType.HITBOX_ENABLE:
                StartHeatExhaust();
                break;

            case AttackEventType.HITBOX_DISABLE:
                EndHeatExhaust();
                break;

            case AttackEventType.ANIMATION_END:
                FinishAttack();
                break;
        }
    }

    /// <summary>
    /// 排熱を開始します。
    /// </summary>
    private void StartHeatExhaust()
    {
        if (m_currentState !=
            HeatExhaustState.PREPARATION)
        {
            return;
        }

        Owner.EnableAttackHitboxes(
            m_attackIdentifier);

        m_currentState =
            HeatExhaustState.EXHAUSTING;
    }

    /// <summary>
    /// 排熱を終了します。
    /// </summary>
    private void EndHeatExhaust()
    {
        if (m_currentState !=
            HeatExhaustState.EXHAUSTING)
        {
            return;
        }

        Owner.AttackHitboxRegistry?.DisableHitboxes(
            m_attackIdentifier);

        m_currentState =
            HeatExhaustState.ENDING;
    }

    /// <summary>
    /// 排熱攻撃を完了します。
    /// </summary>
    private void FinishAttack()
    {
        Owner.AttackHitboxRegistry?.DisableHitboxes(
            m_attackIdentifier);

        m_currentState =
            HeatExhaustState.NONE;

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.SUCCEEDED);
    }
}
