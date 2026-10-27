using UnityEngine;

/// <summary>
/// ステージ1フェーズ3のエネルギー砲攻撃を実行します。
/// </summary>
public sealed class S1P3BossEnergyCannonState :
    StateBase<BossController>
{
    /// <summary>
    /// エネルギー砲攻撃の進行状態です。
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

    // フェーズ3全体の参照
    private S1P3BossReferences m_references;

    // ボス共通参照
    private BossPhaseReferences m_commonReferences;

    // エネルギー砲固有参照
    private S1P3EnergyCannonStateReferences
        m_stateReferences;

    // エネルギー砲パラメータ
    private S1P3BossEnergyCannonStateParameters
        m_parameters;

    // 攻撃ID
    private AttackIdentifier m_attackIdentifier;

    // 攻撃中Animator Bool ID
    private int m_animationActiveBoolID;

    // 攻撃フェーズAnimator Int ID
    private int m_animationPhaseIntID;

    // Animation設定を初期化済みか
    private bool m_isAnimationInitialized;

    // 現在の進行状態
    private EnergyCannonExecutionState
        m_executionState;

    // 現在状態の経過時間
    private float m_elapsedTime;

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
                "エネルギー砲の攻撃設定を取得できませんでした.");

            SetFailed();

            return;
        }

        if (!InitializeAnimationParameters())
        {
            Debug.LogError(
                $"[{nameof(S1P3BossEnergyCannonState)}] " +
                "Animation設定を初期化できませんでした.");

            SetFailed();

            return;
        }

        // 攻撃中はボス本体を停止する
        Owner.Motor?
            .StopHorizontalMovement();

        // 開始時点では攻撃判定を無効化する
        Owner.AttackHitboxRegistry?
            .DisableHitboxes(
                m_attackIdentifier);

        /*
         * BoolをONにする前にPhaseを設定し、
         * 前回値による誤遷移を防ぎます。
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

        switch (m_executionState)
        {
            case EnergyCannonExecutionState.PREPARATION:
                UpdatePreparation(
                    deltaTime);
                break;

            case EnergyCannonExecutionState.STRAIGHT_FIRE:
                UpdateStraightFire(
                    deltaTime);
                break;

            case EnergyCannonExecutionState.TRACKING:
                UpdateTracking(
                    deltaTime);
                break;

            case EnergyCannonExecutionState.ENDING:
                UpdateEnding(
                    deltaTime);
                break;
        }
    }

    /// <summary>
    /// エネルギー砲攻撃を終了します。
    /// </summary>
    protected override void OnExitState()
    {
        StartCoolTimeIfSucceeded();

        Owner.Motor?
            .StopHorizontalMovement();

        if (m_attackIdentifier != null)
        {
            Owner.AttackHitboxRegistry?
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
    }

    /// <summary>
    /// 予備動作を更新します。
    /// </summary>
    private void UpdatePreparation(
        float deltaTime)
    {
        m_elapsedTime +=
            deltaTime;

        if (m_elapsedTime <
            m_parameters.PreparationDuration)
        {
            return;
        }

        StartStraightFire();
    }

    /// <summary>
    /// 直線発射を開始します。
    /// </summary>
    private void StartStraightFire()
    {
        m_elapsedTime =
            0.0f;

        /*
         * Animator上ではここから
         * 発射アニメーションへ移行します。
         */
        SetAnimationPhase(
            BossActionPhase.EXECUTION);

        Owner.EnableAttackHitboxes(
            m_attackIdentifier);

        m_executionState =
            EnergyCannonExecutionState.STRAIGHT_FIRE;
    }

    /// <summary>
    /// 直線発射状態を更新します。
    /// </summary>
    private void UpdateStraightFire(
        float deltaTime)
    {
        m_elapsedTime +=
            deltaTime;

        if (m_elapsedTime <
            m_parameters.StraightFireDuration)
        {
            return;
        }

        StartTracking();
    }

    /// <summary>
    /// Player追従を開始します。
    /// </summary>
    private void StartTracking()
    {
        m_elapsedTime =
            0.0f;

        /*
         * STRAIGHT_FIREとTRACKINGは、
         * Animator上ではどちらも発射中です。
         * そのためPhaseはEXECUTIONのまま変更しません。
         */
        m_executionState =
            EnergyCannonExecutionState.TRACKING;
    }

    /// <summary>
    /// Player追従状態を更新します。
    /// </summary>
    private void UpdateTracking(
        float deltaTime)
    {
        TrackPlayer(
            deltaTime);

        m_elapsedTime +=
            deltaTime;

        if (m_elapsedTime <
            m_parameters.TrackingDuration)
        {
            return;
        }

        StartEnding();
    }

    /// <summary>
    /// 終了動作を開始します。
    /// </summary>
    private void StartEnding()
    {
        if (m_executionState ==
                EnergyCannonExecutionState.ENDING ||
            m_executionState ==
                EnergyCannonExecutionState.FINISHED)
        {
            return;
        }

        m_elapsedTime =
            0.0f;

        // 終了動作開始時に攻撃判定を停止する
        Owner.AttackHitboxRegistry?
            .DisableHitboxes(
                m_attackIdentifier);

        SetAnimationPhase(
            BossActionPhase.RECOVERY);

        m_executionState =
            EnergyCannonExecutionState.ENDING;

        if (m_parameters.EndDuration <=
            0.0f)
        {
            FinishAttack();
        }
    }

    /// <summary>
    /// 終了動作を更新します。
    /// </summary>
    private void UpdateEnding(
        float deltaTime)
    {
        m_elapsedTime +=
            deltaTime;

        if (m_elapsedTime <
            m_parameters.EndDuration)
        {
            return;
        }

        FinishAttack();
    }

    /// <summary>
    /// エネルギー砲をPlayer方向へ追従させます。
    /// </summary>
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

        // 上下には追従しない
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
    /// エネルギー砲攻撃を正常終了します。
    /// </summary>
    private void FinishAttack()
    {
        if (m_executionState ==
            EnergyCannonExecutionState.FINISHED)
        {
            return;
        }

        m_executionState =
            EnergyCannonExecutionState.FINISHED;

        Owner.AttackHitboxRegistry?.DisableHitboxes(
            m_attackIdentifier);

            ;

        if (!Owner.PhaseController.TryGetCurrentPhaseComponent(out S1P3BossStateTransitionRequest transitionRequest))
        {
            Debug.LogError(
                $"[{nameof(S1P3BossEnergyCannonState)}] " +
                $"{nameof(S1P3BossStateTransitionRequest)}が設定されていません。");

            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);

            return;
        }

        // エネルギー砲の正常終了後はスタン状態へ確定遷移する
        transitionRequest.RequestStunned();

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.SUCCEEDED);
    }

    /// <summary>
    /// 必要な参照を取得します。
    /// </summary>
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
                "見つかりません.");

            return false;
        }

        if (!Owner.PhaseController
                .TryGetCurrentPhaseComponent(
                    out m_commonReferences))
        {
            Debug.LogError(
                $"{nameof(BossPhaseReferences)}が" +
                "見つかりません.");

            return false;
        }

        m_stateReferences =
            m_references
                .EnergyCannonStateReferences;

        if (m_stateReferences == null)
        {
            Debug.LogError(
                $"{nameof(S1P3EnergyCannonStateReferences)}が" +
                "設定されていません.");

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
                "CannonPivotが設定されていません.");

            return false;
        }

        if (m_commonReferences.PlayerTransform ==
            null)
        {
            Debug.LogError(
                "PlayerTransformが設定されていません.");

            return false;
        }

        return true;
    }

    /// <summary>
    /// エネルギー砲の攻撃設定を取得します。
    /// </summary>
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
                "見つかりません.");

            return false;
        }

        /*
         * AnimationはBool + Phase Intで管理するため、
         * 従来のTrigger名は使用しません。
         */
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
    /// Animatorへ攻撃中フラグを設定します。
    /// </summary>
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
    /// Animatorへ現在の行動フェーズを設定します。
    /// </summary>
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
        m_elapsedTime =
            0.0f;

        m_executionState =
            EnergyCannonExecutionState.NONE;
    }

    /// <summary>
    /// 正常終了した行動のクールタイムを開始します。
    /// </summary>
    private void StartCoolTimeIfSucceeded()
    {
        if (Owner == null ||
            Owner.GetStateExecutionStatus() !=
                StateExecutionStatus.SUCCEEDED ||
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
    /// Stateを失敗状態にします。
    /// </summary>
    private void SetFailed()
    {
        if (m_attackIdentifier != null)
        {
            Owner?.AttackHitboxRegistry?
                .DisableHitboxes(
                    m_attackIdentifier);
        }

        ResetAnimationParameters();

        if (Owner == null)
        {
            return;
        }

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.FAILED);
    }
}