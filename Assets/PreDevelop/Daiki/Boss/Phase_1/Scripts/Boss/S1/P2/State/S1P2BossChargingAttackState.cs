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
                out S1P2BossChargeSettings chargeSettings))
        {
            SetFailed();
            return;
        }

        GameObject player =
            GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            SetFailed();
            return;
        }

        m_playerTransform = player.transform;
        m_chargeExecutor = new StraightChargeExecutor(Owner);

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.RUNNING);

        if (!m_chargeExecutor.Start(
                chargeSettings.ChargeSettings,
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
        m_chargeExecutor?.Cancel();
        m_chargeExecutor = null;
        m_playerTransform = null;

        if (Owner != null &&
            Owner.GetStateExecutionStatus() ==
            StateExecutionStatus.RUNNING)
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);
        }
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
