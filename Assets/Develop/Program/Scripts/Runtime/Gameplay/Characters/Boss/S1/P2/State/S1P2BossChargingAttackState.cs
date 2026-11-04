using UnityEngine;

/// <summary>
/// ステージ1フェーズ2の突進攻撃を実行します。
/// </summary>
public sealed class S1P2BossChargingAttackState :
    StateBase<BossController>
{
    // プレイヤー
    private Transform m_playerTransform;

    // 直線突進実行機構
    private StraightChargeExecutor m_chargeExecutor;

    // S1P2固有参照
    private S1P2BossReferences m_references;

    // 突進攻撃パラメータ
    private S1BossChargeAttackParameters
        m_chargeParameters;

    // 突進攻撃中Bool ID
    private int m_animationActiveBoolID;

    // 突進攻撃フェーズInt ID
    private int m_animationPhaseIntID;

    // Animation設定を初期化済みか
    private bool m_isAnimationInitialized;

    /// <summary>
    /// 突進攻撃を開始します。
    /// </summary>
    protected override void OnStartState()
    {
        if (Owner == null ||
            Owner.PhaseController == null)
        {
            SetFailed();

            return;
        }

        if (!Owner.PhaseController
                .TryGetCurrentPhaseComponent(
                    out m_references))
        {
            SetFailed();

            return;
        }

        BossPhaseParameters phaseParameters =
            Owner.PhaseParameters;

        if (phaseParameters == null ||
            phaseParameters.ChargeAttack == null)
        {
            Debug.LogError(
                $"[{nameof(S1P2BossChargingAttackState)}] " +
                "突進攻撃パラメータを取得できませんでした。");

            SetFailed();

            return;
        }

        m_chargeParameters =
            phaseParameters.ChargeAttack;

        if (!InitializeAnimationParameters())
        {
            Debug.LogError(
                $"[{nameof(S1P2BossChargingAttackState)}] " +
                "Animation設定を初期化できませんでした。");

            SetFailed();

            return;
        }

        GameObject player =
            GameObject.FindGameObjectWithTag(
                "Player");

        if (player == null)
        {
            Debug.LogError(
                $"[{nameof(S1P2BossChargingAttackState)}] " +
                "Playerを取得できませんでした。");

            SetFailed();

            return;
        }

        m_playerTransform =
            player.transform;

        m_chargeExecutor =
            new StraightChargeExecutor(
                Owner);

        m_chargeExecutor.PhaseChanged +=
            HandleChargePhaseChanged;

        SetAnimationPhase(
            BossActionPhase.PREPARATION);

        Owner.AnimationController.SetBool(
            m_animationActiveBoolID,
            true);

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.RUNNING);

        if (!m_chargeExecutor.Start(
                m_chargeParameters,
                GetPlayerPosition))
        {
            SetFailed();
        }
    }

    /// <summary>
    /// 突進攻撃を物理更新します。
    /// </summary>
    protected override void OnFixedUpdate()
    {
        if (Owner == null ||
            Owner.GetStateExecutionStatus() !=
                StateExecutionStatus.RUNNING)
        {
            return;
        }

        if (m_chargeExecutor == null)
        {
            SetFailed();

            return;
        }

        m_chargeExecutor.FixedUpdate(
            Time.fixedDeltaTime);

        if (m_chargeExecutor.HasFailed)
        {
            SetFailed();

            return;
        }

        if (!m_chargeExecutor.IsCompleted)
        {
            return;
        }

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.SUCCEEDED);
    }

    /// <summary>
    /// 突進攻撃を終了します。
    /// </summary>
    protected override void OnExitState()
    {
        StartCoolTimeIfSucceeded();

        ReleaseChargeExecutor();

        ResetAnimationParameters();

        m_chargeParameters =
            null;

        m_playerTransform =
            null;

        m_references =
            null;

        if (Owner != null &&
            Owner.GetStateExecutionStatus() ==
                StateExecutionStatus.RUNNING)
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);
        }
    }

    /// <summary>
    /// Executorの行動フェーズ変更を処理します。
    /// </summary>
    private void HandleChargePhaseChanged(
        BossActionPhase phase)
    {
        SetAnimationPhase(
            phase);
    }

    /// <summary>
    /// Animatorへ行動フェーズを設定します。
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
    /// Animationパラメータを初期化します。
    /// </summary>
    private bool InitializeAnimationParameters()
    {
        m_isAnimationInitialized =
            false;

        if (Owner.AnimationController == null ||
            m_chargeParameters == null ||
            string.IsNullOrEmpty(
                m_chargeParameters
                    .AnimationActiveBoolName) ||
            string.IsNullOrEmpty(
                m_chargeParameters
                    .AnimationPhaseIntName))
        {
            return false;
        }

        m_animationActiveBoolID =
            Animator.StringToHash(
                m_chargeParameters
                    .AnimationActiveBoolName);

        m_animationPhaseIntID =
            Animator.StringToHash(
                m_chargeParameters
                    .AnimationPhaseIntName);

        m_isAnimationInitialized =
            true;

        return true;
    }

    /// <summary>
    /// 突進Animationパラメータを初期状態へ戻します。
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
    /// Playerの現在位置を取得します。
    /// </summary>
    private Vector3 GetPlayerPosition()
    {
        if (m_playerTransform == null)
        {
            return Owner.transform.position;
        }

        return m_playerTransform.position;
    }

    /// <summary>
    /// Executorとの接続を解除します。
    /// </summary>
    private void ReleaseChargeExecutor()
    {
        if (m_chargeExecutor == null)
        {
            return;
        }

        m_chargeExecutor.PhaseChanged -=
            HandleChargePhaseChanged;

        m_chargeExecutor.Cancel();

        m_chargeExecutor =
            null;
    }

    /// <summary>
    /// 正常終了した突進攻撃のクールタイムを開始します。
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
                S1P2BossChargingAttackState>(
                m_references
                    .DecisionParameterAsset
                    .Charge
                    .CoolTime);
    }

    /// <summary>
    /// Stateを失敗状態にします。
    /// </summary>
    private void SetFailed()
    {
        ReleaseChargeExecutor();

        ResetAnimationParameters();

        if (Owner == null)
        {
            return;
        }

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.FAILED);
    }
}