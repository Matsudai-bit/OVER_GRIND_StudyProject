using UnityEngine;

/// <summary>
/// ステージ1フェーズ2の経路移動を実行します。
/// </summary>
public sealed class S1P2BossMoveState :
    StateBase<BossController>
{
    // S1P2固有参照
    private S1P2BossReferences m_references;

    // 移動状態参照
    private S1P2MoveStateReferences m_moveReferences;

    // 移動制御
    private S1P2BossMoveController m_moveController;

    private S1P2BossMoveStateParameters m_parameter;

    /// <summary>
    /// 経路移動を開始します。
    /// </summary>
    protected override void OnStartState()
    {
        if (!TryGetReferences())
        {
            SetFailed();

            return;
        }

        if (!m_moveReferences.RoutePlanner.TryCreateRoute(
                m_parameter,
                out BossMoveRoute route))
        {
            Debug.LogError(
                "S1P2ボスの移動経路を生成できませんでした。");

            SetFailed();

            return;
        }

        m_moveController =
            m_moveReferences.MoveController;

        if (!m_moveController.StartMove(
                route,
                m_parameter))
        {
            Debug.LogError(
                "S1P2ボスの経路移動を開始できませんでした。");

            SetFailed();

            return;
        }

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.RUNNING);
    }

    /// <summary>
    /// 経路移動を物理更新します。
    /// </summary>
    protected override void OnFixedUpdate()
    {
        if (Owner.GetStateExecutionStatus() !=
            StateExecutionStatus.RUNNING)
        {
            return;
        }

        if (m_moveController == null)
        {
            SetFailed();

            return;
        }

        m_moveController.FixedUpdateMove(
            Time.fixedDeltaTime);

        if (m_moveController.HasFailed)
        {
            SetFailed();

            return;
        }

        if (!m_moveController.IsCompleted)
        {
            return;
        }

        Owner.Motor?.StopHorizontalMovement();

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.SUCCEEDED);
    }

    /// <summary>
    /// 経路移動を終了します。
    /// </summary>
    protected override void OnExitState()
    {
        m_moveController?.Cancel();

        Owner.Motor?.StopHorizontalMovement();

        if (Owner.GetStateExecutionStatus() ==
            StateExecutionStatus.RUNNING)
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);
        }

        m_moveController = null;
        m_moveReferences = null;
        m_references = null;
    }

    /// <summary>
    /// 移動状態に必要な参照を取得します。
    /// </summary>
    /// <returns>
    /// true：取得しました。
    /// false：取得できませんでした。
    /// </returns>
    private bool TryGetReferences()
    {
        if (Owner == null ||
            Owner.PhaseController == null ||
            Owner.Motor == null)
        {
            return false;
        }

        if (!Owner.PhaseController
                .TryGetCurrentPhaseComponent(
                    out m_references))
        {
            Debug.LogError(
                $"{nameof(S1P2BossReferences)}が" +
                "取得できませんでした。");

            return false;
        }

        m_moveReferences =
            m_references.MoveStateReferences;

        if (m_moveReferences == null)
        {
            Debug.LogError(
                $"{nameof(S1P2MoveStateReferences)}が" +
                "設定されていません。");

            return false;
        }


        m_parameter = m_references.StateParameterAsset.Move;

        if (m_parameter == null)
        {
            Debug.LogError(
                $"{nameof(S1P2BossMoveStateParameters)}が" +
                "設定されていません。");

            return false;
        }

        if (m_moveReferences.RoutePlanner == null)
        {
            Debug.LogError(
                $"{nameof(S1P2BossMoveRoutePlanner)}が" +
                "設定されていません。");

            return false;
        }

        if (m_moveReferences.MoveController == null)
        {
            Debug.LogError(
                $"{nameof(S1P2BossMoveController)}が" +
                "設定されていません。");

            return false;
        }

        return true;
    }

    /// <summary>
    /// Stateを失敗終了します。
    /// </summary>
    private void SetFailed()
    {
        m_moveController?.Cancel();

        Owner?.Motor?.StopHorizontalMovement();

        if (Owner == null)
        {
            return;
        }

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.FAILED);
    }
}