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

        if (!Owner.PhaseController.TryGetCurrentPhaseComponent(
                out m_references))
        {
            SetFailed();
            return;
        }

        // 現在フェーズの突進パラメータを取得する
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

        S1BossChargeAttackParameters chargeParameters =
            phaseParameters.ChargeAttack;

        GameObject player =
            GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            SetFailed();
            return;
        }

        m_playerTransform = player.transform;
        m_chargeExecutor =
            new StraightChargeExecutor(Owner);

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.RUNNING);

        if (!m_chargeExecutor.Start(
                chargeParameters,
                () => m_playerTransform.position))
        {
            SetFailed();
        }
    }

    /// <summary>
    /// 突進攻撃を物理更新します。
    /// </summary>
    protected override void OnFixedUpdate()
    {
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

        m_chargeExecutor?.Cancel();

        m_chargeExecutor = null;
        m_playerTransform = null;
        m_references = null;

        if (Owner != null &&
            Owner.GetStateExecutionStatus() ==
            StateExecutionStatus.RUNNING)
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);
        }
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
            m_references.DecisionParameterAsset == null)
        {
            return;
        }

        BossStateCoolTimeManager coolTimeManager =
            Owner.GetComponent<BossStateCoolTimeManager>();

        if (coolTimeManager == null)
        {
            return;
        }

        coolTimeManager.StartCoolTime<S1P2BossChargingAttackState>(
            m_references.DecisionParameterAsset.Charge.CoolTime);
    }

    /// <summary>
    /// Stateを失敗状態にします。
    /// </summary>
    private void SetFailed()
    {
        if (Owner == null)
        {
            return;
        }

        m_chargeExecutor?.Cancel();

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.FAILED);
    }
}
