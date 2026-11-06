using UnityEngine;

/// <summary>
/// ステージ1フェーズ3のエネルギー砲攻撃を実行します。
/// AnimationEventに合わせて照射開始・終了を制御します。
/// </summary>
public sealed class S1P3BossEnergyCannonState :
    StateBase<BossController>
{
    /// <summary>
    /// エネルギー砲攻撃の実行状態です。
    /// </summary>
    private enum EnergyCannonExecutionState
    {
        NONE,
        PREPARATION,
        STRAIGHT_FIRE,
        TRACKING,
        ENDING,
        FINISHED
    }

    // フェーズ3固有参照
    private S1P3BossReferences m_references;

    // ボス共通参照
    private BossPhaseReferences m_commonReferences;

    // エネルギー砲固有参照
    private S1P3EnergyCannonStateReferences m_stateReferences;

    // エネルギー砲パラメータ
    private S1P3BossEnergyCannonStateParameters m_parameters;

    // 攻撃ID
    private AttackIdentifier m_attackIdentifier;

    // 攻撃中Animator Bool ID
    private int m_animationActiveBoolID;

    // 攻撃フェーズAnimator Int ID
    private int m_animationPhaseIntID;

    // Animation設定を初期化済みか
    private bool m_isAnimationInitialized;

    // AnimationEvent受信
    private AnimationEventReceiver m_animationEventReceiver;

    // 現在の進行状態
    private EnergyCannonExecutionState m_executionState;

    /// <summary>
    /// エネルギー砲攻撃を開始します。
    /// </summary>
    protected override void OnStartState()
    {
        ResetRuntimeState();

        if (!TryGetReferences())
        {
            SetFailed();

            return;
        }

        if (!TryGetAttackSetting())
        {
            Debug.LogError(
                $"[{nameof(S1P3BossEnergyCannonState)}] " +
                "エネルギー砲の攻撃設定を取得できませんでした。");

            SetFailed();

            return;
        }

        if (!InitializeAnimationParameters())
        {
            Debug.LogError(
                $"[{nameof(S1P3BossEnergyCannonState)}] " +
                "Animation設定を初期化できませんでした。");

            SetFailed();

            return;
        }

        if (!RegisterAnimationEvent())
        {
            Debug.LogError(
                $"[{nameof(S1P3BossEnergyCannonState)}] " +
                "AnimationEventを購読できませんでした。");

            SetFailed();

            return;
        }

        // 攻撃中はボス本体を停止させます。
        Owner.Motor?
            .StopHorizontalMovement();

        // 前回の攻撃判定が残らないよう初期状態では無効化します。
        Owner.AttackHitboxRegistry?
            .DisableHitboxes(
                m_attackIdentifier);

        /*
         * Animation開始前にPhaseを設定します。
         * HITBOX_ENABLEが来るまでは予備動作状態です。
         */
        SetAnimationPhase(
            BossActionPhase.PREPARATION);

        SetAnimationActive(
            true);

        m_executionState =
            EnergyCannonExecutionState.PREPARATION;

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.RUNNING);
    }

    /// <summary>
    /// エネルギー砲攻撃を更新します。
    /// </summary>
    /// <param name="deltaTime">
    /// 前フレームからの経過時間。
    /// </param>
    protected override void OnUpdate(
        float deltaTime)
    {
        if (Owner == null ||
            Owner.GetStateExecutionStatus() !=
                StateExecutionStatus.RUNNING)
        {
            return;
        }

        if (m_executionState !=
            EnergyCannonExecutionState.TRACKING)
        {
            return;
        }

        TrackPlayer(
            deltaTime);
    }

    /// <summary>
    /// エネルギー砲攻撃を終了します。
    /// </summary>
    protected override void OnExitState()
    {
        UnregisterAnimationEvent();

        Owner?.Motor?
            .StopHorizontalMovement();

        if (m_attackIdentifier != null)
        {
            Owner?.AttackHitboxRegistry?
                .DisableHitboxes(
                    m_attackIdentifier);
        }

        ResetAnimationParameters();

        if (Owner != null &&
            Owner.GetStateExecutionStatus() ==
                StateExecutionStatus.RUNNING)
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);
        }

        ResetRuntimeState();

        m_references =
            null;

        m_commonReferences =
            null;

        m_stateReferences =
            null;

        m_parameters =
            null;

        m_attackIdentifier =
            null;

        m_animationEventReceiver =
            null;
    }

    /// <summary>
    /// エネルギー砲AnimationEventを処理します。
    /// </summary>
    /// <param name="attackEventData">
    /// 攻撃イベント情報。
    /// </param>
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
                StartEnergyCannon();
                break;

            case AttackEventType.IMPACT_START:
                StartTracking();
                break;

            case AttackEventType.HITBOX_DISABLE:
                StopEnergyCannon();
                break;

            case AttackEventType.ANIMATION_END:
                FinishAttack();
                break;
        }
    }

    /// <summary>
    /// AnimationEventを起点に
    /// エネルギー砲の照射を開始します。
    /// </summary>
    private void StartEnergyCannon()
    {
        if (m_executionState !=
            EnergyCannonExecutionState.PREPARATION)
        {
            return;
        }

        SetAnimationPhase(
            BossActionPhase.EXECUTION);

        Owner.EnableAttackHitboxes(
            m_attackIdentifier);

        /*
         * IMPACT_STARTが来るまでは
         * CannonPivotを回転させず正面へ照射します。
         */
        m_executionState =
            EnergyCannonExecutionState.STRAIGHT_FIRE;
    }


    /// <summary>
    /// AnimationEventを起点に
    /// Player追従状態を開始します。
    /// </summary>
    private void StartTracking()
    {
        if (m_executionState !=
            EnergyCannonExecutionState.STRAIGHT_FIRE)
        {
            return;
        }

        m_executionState =
            EnergyCannonExecutionState.TRACKING;
    }

    /// <summary>
    /// Player追従状態を更新します。
    /// </summary>
    /// <param name="deltaTime">
    /// 前フレームからの経過時間。
    /// </param>
    private void UpdateTracking(
        float deltaTime)
    {
        TrackPlayer(
            deltaTime);
    }

    /// <summary>
    /// AnimationEventを起点に
    /// エネルギー砲の照射を終了します。
    /// </summary>
    private void StopEnergyCannon()
    {
        if (m_executionState !=
                EnergyCannonExecutionState.STRAIGHT_FIRE &&
            m_executionState !=
                EnergyCannonExecutionState.TRACKING)
        {
            return;
        }

        Owner.AttackHitboxRegistry?
            .DisableHitboxes(
                m_attackIdentifier);

        SetAnimationPhase(
            BossActionPhase.RECOVERY);

        m_executionState =
            EnergyCannonExecutionState.ENDING;
    }

    /// <summary>
    /// エネルギー砲をPlayer方向へ追従させます。
    /// </summary>
    /// <param name="deltaTime">
    /// 前フレームからの経過時間。
    /// </param>
    private void TrackPlayer(
        float deltaTime)
    {
        if (m_stateReferences?.CannonPivot == null ||
            m_commonReferences?.PlayerTransform == null)
        {
            return;
        }

        Transform cannonPivot =
            m_stateReferences.CannonPivot;

        Transform playerTransform =
            m_commonReferences.PlayerTransform;

        Vector3 direction =
            playerTransform.position -
            cannonPivot.position;

        // 水平方向のみ追従します。
        direction.y =
            0.0f;

        if (direction.sqrMagnitude <=
            Mathf.Epsilon)
        {
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(
                direction.normalized,
                Vector3.up);

        cannonPivot.rotation =
            Quaternion.RotateTowards(
                cannonPivot.rotation,
                targetRotation,
                m_parameters.TrackingSpeed *
                deltaTime);
    }

    /// <summary>
    /// Animation終了時にエネルギー砲攻撃を完了します。
    /// </summary>
    private void FinishAttack()
    {
        if (m_executionState ==
            EnergyCannonExecutionState.FINISHED)
        {
            return;
        }

        /*
         * HITBOX_DISABLEを取りこぼした場合でも
         * Animation終了時には必ず攻撃判定を停止します。
         */
        Owner.AttackHitboxRegistry?
            .DisableHitboxes(
                m_attackIdentifier);

        m_executionState =
            EnergyCannonExecutionState.FINISHED;

        if (!Owner.PhaseController
                .TryGetCurrentPhaseComponent(
                    out S1P3BossStateTransitionRequest transitionRequest))
        {
            Debug.LogError(
                $"[{nameof(S1P3BossEnergyCannonState)}] " +
                $"{nameof(S1P3BossStateTransitionRequest)}が" +
                "設定されていません。");

            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);

            return;
        }

        /*
         * SUCCEEDEDにすると次の行動判定が開始される可能性があるため、
         * 必ず先にクールタイムを登録します。
         */
        StartCoolTime();

        // エネルギー砲正常終了後はスタンへ遷移させます。
        transitionRequest.RequestStunned();

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.SUCCEEDED);
    }

    /// <summary>
    /// 必要な参照を取得します。
    /// </summary>
    /// <returns>
    /// true：取得できました。
    /// false：取得できませんでした。
    /// </returns>
    private bool TryGetReferences()
    {
        if (Owner == null ||
            Owner.PhaseController == null)
        {
            return false;
        }

        if (!Owner.PhaseController
                .TryGetCurrentPhaseComponent(
                    out m_references))
        {
            Debug.LogError(
                $"{nameof(S1P3BossReferences)}が" +
                "見つかりません。");

            return false;
        }

        if (!Owner.PhaseController
                .TryGetCurrentPhaseComponent(
                    out m_commonReferences))
        {
            Debug.LogError(
                $"{nameof(BossPhaseReferences)}が" +
                "見つかりません。");

            return false;
        }

        m_stateReferences =
            m_references
                .EnergyCannonStateReferences;

        if (m_stateReferences == null)
        {
            Debug.LogError(
                $"{nameof(S1P3EnergyCannonStateReferences)}が" +
                "設定されていません。");

            return false;
        }

        if (m_references.StateParameterAsset ==
            null)
        {
            return false;
        }

        m_parameters =
            m_references
                .StateParameterAsset
                .EnergyCannon;

        if (m_parameters == null)
        {
            return false;
        }

        if (m_stateReferences.CannonPivot == null)
        {
            Debug.LogError(
                "CannonPivotが設定されていません。");

            return false;
        }

        if (m_commonReferences.PlayerTransform ==
            null)
        {
            Debug.LogError(
                "PlayerTransformが設定されていません。");

            return false;
        }

        return true;
    }

    /// <summary>
    /// エネルギー砲の攻撃設定を取得します。
    /// </summary>
    /// <returns>
    /// true：取得できました。
    /// false：取得できませんでした。
    /// </returns>
    private bool TryGetAttackSetting()
    {
        S1P3BossAttackSettings attackSettings =
            Owner.GetComponentInChildren<
                S1P3BossAttackSettings>(
                    true);

        if (attackSettings == null)
        {
            Debug.LogError(
                $"{nameof(S1P3BossAttackSettings)}が" +
                "見つかりません。");

            return false;
        }

        if (!attackSettings.TryGetAttackSetting(
                S1P3BossAttackType.ENERGY_CANNON,
                out m_attackIdentifier,
                out _))
        {
            return false;
        }

        return m_attackIdentifier != null;
    }

    /// <summary>
    /// Animatorパラメータを初期化します。
    /// </summary>
    /// <returns>
    /// true：初期化できました。
    /// false：初期化できませんでした。
    /// </returns>
    private bool InitializeAnimationParameters()
    {
        m_isAnimationInitialized =
            false;

        if (Owner.AnimationController == null ||
            m_parameters == null ||
            string.IsNullOrEmpty(
                m_parameters.AnimationActiveBoolName) ||
            string.IsNullOrEmpty(
                m_parameters.AnimationPhaseIntName))
        {
            return false;
        }

        m_animationActiveBoolID =
            Animator.StringToHash(
                m_parameters
                    .AnimationActiveBoolName);

        m_animationPhaseIntID =
            Animator.StringToHash(
                m_parameters
                    .AnimationPhaseIntName);

        m_isAnimationInitialized =
            true;

        return true;
    }

    /// <summary>
    /// AnimationEventを購読します。
    /// </summary>
    /// <returns>
    /// true：購読できました。
    /// false：購読できませんでした。
    /// </returns>
    private bool RegisterAnimationEvent()
    {
        if (Owner?.AnimationController == null)
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

        m_animationEventReceiver.AttackEventReceived +=
            HandleAttackEvent;

        return true;
    }

    /// <summary>
    /// AnimationEventの購読を解除します。
    /// </summary>
    private void UnregisterAnimationEvent()
    {
        if (m_animationEventReceiver == null)
        {
            return;
        }

        m_animationEventReceiver.AttackEventReceived -=
            HandleAttackEvent;
    }

    /// <summary>
    /// Animatorへ攻撃中フラグを設定します。
    /// </summary>
    /// <param name="isActive">
    /// 攻撃中か。
    /// </param>
    private void SetAnimationActive(
        bool isActive)
    {
        if (!m_isAnimationInitialized ||
            Owner?.AnimationController == null)
        {
            return;
        }

        Owner.AnimationController.SetBool(
            m_animationActiveBoolID,
            isActive);
    }

    /// <summary>
    /// Animatorへ攻撃フェーズを設定します。
    /// </summary>
    /// <param name="phase">
    /// 攻撃フェーズ。
    /// </param>
    private void SetAnimationPhase(
        BossActionPhase phase)
    {
        if (!m_isAnimationInitialized ||
            Owner?.AnimationController == null)
        {
            return;
        }

        Owner.AnimationController.SetInteger(
            m_animationPhaseIntID,
            (int)phase);
    }

    /// <summary>
    /// Animatorパラメータを初期状態へ戻します。
    /// </summary>
    private void ResetAnimationParameters()
    {
        if (!m_isAnimationInitialized ||
            Owner?.AnimationController == null)
        {
            return;
        }

        Owner.AnimationController.SetInteger(
            m_animationPhaseIntID,
            (int)BossActionPhase.PREPARATION);

        Owner.AnimationController.SetBool(
            m_animationActiveBoolID,
            false);

        m_isAnimationInitialized =
            false;
    }

    /// <summary>
    /// Runtime状態を初期化します。
    /// </summary>
    private void ResetRuntimeState()
    {
 
        m_executionState =
            EnergyCannonExecutionState.NONE;
    }

    /// <summary>
    /// エネルギー砲のクールタイムを開始します。
    /// </summary>
    private void StartCoolTime()
    {
        if (Owner == null ||
            m_references == null ||
            m_references.DecisionParameterAsset ==
                null)
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
                S1P3BossEnergyCannonState>(
                m_references
                    .DecisionParameterAsset
                    .EnergyCannon
                    .CoolTime);
    }

    /// <summary>
    /// Stateを失敗終了します。
    /// </summary>
    private void SetFailed()
    {
        if (m_attackIdentifier != null)
        {
            Owner?.AttackHitboxRegistry?
                .DisableHitboxes(
                    m_attackIdentifier);
        }

        UnregisterAnimationEvent();

        ResetAnimationParameters();

        if (Owner == null)
        {
            return;
        }

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.FAILED);
    }
}