using UnityEngine;

/// <summary>
/// ステージ1フェーズ1の排熱攻撃を実行します。
/// </summary>
public sealed class S1P1BossHeatVentState :
    StateBase<BossController>
{
    // AnimationEvent待機による永久停止を防ぐ上限時間
    private const float MAX_STATE_DURATION = 15.0f;

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

    // S1P1固有参照
    private S1P1BossReferences m_references;

    // 排熱状態パラメータ
    private S1P1BossHeatVentStateParameters m_parameters;

    // AnimationEvent受信
    private AnimationEventReceiver m_animationEventReceiver;

    // State開始からの経過時間
    private float m_stateElapsedTime;

    // AnimationEvent購読中か
    private bool m_isAnimationEventRegistered;

    /// <summary>
    /// 排熱攻撃を開始します。
    /// </summary>
    protected override void OnStartState()
    {
        ResetRuntimeState();

        Owner?.Motor?
            .StopHorizontalMovement();

        if (!TryGetParameters())
        {
            Debug.LogError(
                $"[{nameof(S1P1BossHeatVentState)}] " +
                "排熱状態のパラメータを取得できませんでした。");

            SetFailed();

            return;
        }

        if (!TryGetAttackSetting())
        {
            Debug.LogError(
                $"[{nameof(S1P1BossHeatVentState)}] " +
                "排熱攻撃の設定を取得できませんでした。");

            SetFailed();

            return;
        }

        RegisterAnimationEvent();

        m_currentState =
            HeatExhaustState.PREPARATION;

        /*
         * AnimationEvent側ではRUNNINGのみ処理するため、
         * Triggerを発火する前にRUNNINGへ変更します。
         */
        Owner.SetStateExecutionStatus(
            StateExecutionStatus.RUNNING);

        Owner.AnimationController.SetTrigger(
            m_animationTriggerID);
    }

    /// <summary>
    /// AnimationEvent待機のタイムアウトを監視します。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過時間。</param>
    protected override void OnUpdate(
        float deltaTime)
    {
        if (Owner == null ||
            Owner.GetStateExecutionStatus() !=
                StateExecutionStatus.RUNNING)
        {
            return;
        }

        m_stateElapsedTime +=
            Mathf.Max(
                0.0f,
                deltaTime);

        if (m_stateElapsedTime <
            MAX_STATE_DURATION)
        {
            return;
        }

        Debug.LogError(
            $"[{nameof(S1P1BossHeatVentState)}] " +
            "AnimationEvent待機がタイムアウトしました。");

        SetFailed();
    }

    /// <summary>
    /// 排熱攻撃を終了します。
    /// </summary>
    protected override void OnExitState()
    {
        UnregisterAnimationEvent();

        DisableAttackHitboxes();

        m_currentState =
            HeatExhaustState.NONE;

        if (Owner != null &&
            Owner.GetStateExecutionStatus() ==
                StateExecutionStatus.RUNNING)
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);
        }

        m_references =
            null;

        m_parameters =
            null;

        m_animationEventReceiver =
            null;

        m_attackIdentifier =
            null;
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
        if (Owner == null ||
            Owner.PhaseController == null ||
            !Owner.PhaseController.TryGetCurrentPhaseComponent(
                out m_references) ||
            m_references == null ||
            m_references.StateParameterAsset == null)
        {
            return false;
        }

        m_parameters =
            m_references.StateParameterAsset.HeatVent;

        return m_parameters != null;
    }

    /// <summary>
    /// 排熱攻撃の設定を取得します。
    /// </summary>
    /// <returns>
    /// true：設定できました。
    /// false：設定できませんでした。
    /// </returns>
    private bool TryGetAttackSetting()
    {
        if (Owner == null ||
            Owner.AnimationController == null)
        {
            return false;
        }

        S1P1BossAttackSettings attackSettings =
            Owner.GetComponentInChildren<
                S1P1BossAttackSettings>(
                    true);

        if (attackSettings == null)
        {
            return false;
        }

        if (!attackSettings.TryGetAttackSetting(
                S1P1BossAttackType.HEAT_VENT,
                out m_attackIdentifier,
                out string animationTriggerName))
        {
            return false;
        }

        if (string.IsNullOrEmpty(
                animationTriggerName) ||
            m_attackIdentifier == null)
        {
            return false;
        }

        m_animationEventReceiver =
            Owner.AnimationController
                .CurrentAnimationEventReceiver;

        if (m_animationEventReceiver == null)
        {
            return false;
        }

        m_animationTriggerID =
            Animator.StringToHash(
                animationTriggerName);

        return true;
    }

    /// <summary>
    /// 排熱攻撃のAnimationEventを処理します。
    /// </summary>
    /// <param name="attackEventData">攻撃イベント情報。</param>
    private void HandleAttackEvent(
        AttackEventData attackEventData)
    {
        if (attackEventData == null ||
            Owner == null ||
            Owner.GetStateExecutionStatus() !=
                StateExecutionStatus.RUNNING)
        {
            return;
        }

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

        DisableAttackHitboxes();

        m_currentState =
            HeatExhaustState.ENDING;
    }

    /// <summary>
    /// 排熱攻撃を完了します。
    /// </summary>
    private void FinishAttack()
    {
        if (Owner == null ||
            Owner.GetStateExecutionStatus() !=
                StateExecutionStatus.RUNNING)
        {
            return;
        }

        DisableAttackHitboxes();

        m_currentState =
            HeatExhaustState.NONE;

        /*
         * SUCCEEDED設定後に同じ行動が再評価される可能性があるため、
         * 必ず先にクールタイムを登録します。
         */
        StartCoolTime();

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.SUCCEEDED);
    }

    /// <summary>
    /// 排熱攻撃のクールタイムを開始します。
    /// </summary>
    private void StartCoolTime()
    {
        if (Owner == null ||
            m_references == null ||
            m_references.DecisionParameterAsset == null)
        {
            return;
        }

        BossStateCoolTimeManager coolTimeManager =
            Owner.GetComponent<
                BossStateCoolTimeManager>();

        if (coolTimeManager == null)
        {
            return;
        }

        coolTimeManager
            .StartCoolTime<
                S1P1BossHeatVentState>(
                m_references
                    .DecisionParameterAsset
                    .HeatExhaust
                    .CoolTime);
    }

    /// <summary>
    /// AnimationEventを購読します。
    /// </summary>
    private void RegisterAnimationEvent()
    {
        if (m_isAnimationEventRegistered ||
            m_animationEventReceiver == null)
        {
            return;
        }

        m_animationEventReceiver.AttackEventReceived +=
            HandleAttackEvent;

        m_isAnimationEventRegistered =
            true;
    }

    /// <summary>
    /// AnimationEventの購読を解除します。
    /// </summary>
    private void UnregisterAnimationEvent()
    {
        if (!m_isAnimationEventRegistered ||
            m_animationEventReceiver == null)
        {
            return;
        }

        m_animationEventReceiver.AttackEventReceived -=
            HandleAttackEvent;

        m_isAnimationEventRegistered =
            false;
    }

    /// <summary>
    /// 排熱攻撃判定を無効化します。
    /// </summary>
    private void DisableAttackHitboxes()
    {
        if (Owner == null ||
            m_attackIdentifier == null)
        {
            return;
        }

        Owner.AttackHitboxRegistry?
            .DisableHitboxes(
                m_attackIdentifier);
    }

    /// <summary>
    /// Stateを失敗状態にします。
    /// </summary>
    private void SetFailed()
    {
        DisableAttackHitboxes();

        UnregisterAnimationEvent();

        if (Owner == null)
        {
            return;
        }

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.FAILED);
    }

    /// <summary>
    /// Runtime状態を初期化します。
    /// </summary>
    private void ResetRuntimeState()
    {
        m_currentState =
            HeatExhaustState.NONE;

        m_stateElapsedTime =
            0.0f;

        m_attackIdentifier =
            null;

        m_animationEventReceiver =
            null;

        m_animationTriggerID =
            0;

        m_isAnimationEventRegistered =
            false;
    }
}
