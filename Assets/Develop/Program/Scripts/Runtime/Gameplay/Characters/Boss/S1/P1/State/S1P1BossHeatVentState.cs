using UnityEngine;

/// <summary>
/// ステージ1フェーズ1の排熱攻撃を実行します。
/// </summary>
public sealed class S1P1BossHeatVentState :
    StateBase<BossController>
{
    /// <summary>
    /// 排熱攻撃の進行状態です。
    /// </summary>
    private enum HeatExhaustState
    {
        NONE,
        PREPARATION,    // 待機状態
        EXHAUSTING,     // 攻撃状態
        ENDING          // 終了状態
    }

    // 攻撃ID
    private AttackIdentifier m_attackIdentifier;

    // Animator Trigger ID
    private int m_animationTriggerID;

    // 現在の排熱攻撃状態
    private HeatExhaustState m_currentState;

    /// <summary>
    /// 排熱攻撃を開始します。
    /// </summary>
    protected override void OnStartState()
    {
        m_currentState =
            HeatExhaustState.NONE;

        Owner.Motor?.StopHorizontalMovement();

        if (!ApplyAttackSetting())
        {
            Debug.LogError(
                "排熱攻撃を開始できませんでした。");

            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);

            return;
        }

        // アニメーション開始時点では予備動作
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

        if (Owner.AnimationController != null &&
            Owner.AnimationController.CurrentAnimationEventReceiver != null)
        {
            Owner.AnimationController
                .CurrentAnimationEventReceiver
                .AttackEventReceived -=
                HandleAttackEvent;
        }

        // State途中で終了した場合でもHitboxを残さない
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
    }

    /// <summary>
    /// 正常終了した排熱攻撃のクールタイムを開始します。
    /// </summary>
    private void StartCoolTimeIfSucceeded()
    {
        if (Owner.GetStateExecutionStatus() !=
            StateExecutionStatus.SUCCEEDED ||
            Owner.PhaseController == null)
        {
            return;
        }

        if (!Owner.PhaseController.TryGetCurrentPhaseComponent(
                out S1P1BossReferences references) ||
            references.DecisionParameterAsset == null)
        {
            return;
        }

        BossStateCoolTimeManager coolTimeManager =
            Owner.GetComponent<BossStateCoolTimeManager>();

        if (coolTimeManager == null)
        {
            return;
        }

        coolTimeManager.StartCoolTime<S1P1BossHeatVentState>(
            references.DecisionParameterAsset.HeatExhaust.CoolTime);
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
                S1P1BossAttackType.HEAT_VENT,
                out m_attackIdentifier,
                out string animationTriggerName))
        {
            Debug.LogError(
                $"{S1P1BossAttackType.HEAT_VENT}" +
                "の攻撃設定がありません。");

            return false;
        }

        if (string.IsNullOrEmpty(animationTriggerName) ||
            m_attackIdentifier == null ||
            Owner.AnimationController == null ||
            Owner.AnimationController
                .CurrentAnimationEventReceiver == null)
        {
            return false;
        }

        m_animationTriggerID =
            Animator.StringToHash(
                animationTriggerName);

        Owner.AnimationController
            .CurrentAnimationEventReceiver
            .AttackEventReceived +=
            HandleAttackEvent;

        Owner.AnimationController.SetTrigger(
            m_animationTriggerID);

        return true;
    }

    /// <summary>
    /// 排熱攻撃のAnimationEventを処理します。
    /// </summary>
    /// <param name="attackEventData">
    /// 攻撃イベント情報。
    /// </param>
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
    /// 排熱を終了して終了動作へ移行します。
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
        // AnimationEventの設定ミスでも
        // Hitboxが残らないよう明示的に無効化する
        Owner.AttackHitboxRegistry?.DisableHitboxes(
            m_attackIdentifier);

        m_currentState =
            HeatExhaustState.NONE;

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.SUCCEEDED);
    }
}