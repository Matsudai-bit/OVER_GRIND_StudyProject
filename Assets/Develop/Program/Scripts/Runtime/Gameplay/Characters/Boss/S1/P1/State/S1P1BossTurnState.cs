using UnityEngine;

/// <summary>
/// ステージ1フェーズ1の方向転換を実行します。
/// </summary>
public sealed class S1P1BossTurnState :
    StateBase<BossController>
{
    // Animatorパラメータ名
    private const string TURN_PARAMETER_NAME = "Turn";

    // AnimatorパラメータID
    private static readonly int TURN_PARAMETER_ID =
        Animator.StringToHash(TURN_PARAMETER_NAME);

    // 目標回転
    private Quaternion m_targetRotation;

    // S1P1固有参照
    private S1P1BossReferences m_references;

    // フェーズ共通参照
    private BossPhaseReferences m_commonReferences;

    // 方向転換状態パラメータ
    private S1P1BossTurnStateParameters m_parameters;

    /// <summary>
    /// 方向転換を開始します。
    /// </summary>
    protected override void OnStartState()
    {
        if (!TryGetReferences())
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);

            return;
        }

        Vector3 turnDirection =
            GetTurnDirection();

        if (turnDirection.sqrMagnitude <=
            Mathf.Epsilon)
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);

            return;
        }

        m_targetRotation =
            Quaternion.LookRotation(
                turnDirection,
                Vector3.up);

        Owner.AnimationController?.SetBool(
            TURN_PARAMETER_ID,
            true);

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.RUNNING);
    }

    /// <summary>
    /// 方向転換を更新します。
    /// </summary>
    protected override void OnFixedUpdate()
    {
        if (Owner.Motor == null ||
            m_parameters == null)
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);

            return;
        }

        bool hasReachedTarget =
            Owner.Motor.RotateTowards(
                m_targetRotation,
                m_parameters.RotationSpeed,
                Time.fixedDeltaTime);

        if (!hasReachedTarget)
        {
            return;
        }

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.SUCCEEDED);

        // 方向転換後はそのまま歩行へ移行する
        Machine.ChangeState<S1P1BossWalkState>();
    }

    /// <summary>
    /// 方向転換を終了します。
    /// </summary>
    protected override void OnExitState()
    {
        StartCoolTimeIfSucceeded();

        Owner.AnimationController?.SetBool(
            TURN_PARAMETER_ID,
            false);

        if (Owner.GetStateExecutionStatus() ==
            StateExecutionStatus.RUNNING)
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);
        }

        m_references = null;
        m_commonReferences = null;
        m_parameters = null;
    }

    /// <summary>
    /// 必要な参照とパラメータを取得します。
    /// </summary>
    /// <returns>
    /// true：取得できました。
    /// false：取得できませんでした。
    /// </returns>
    private bool TryGetReferences()
    {
        if (Owner.PhaseController == null)
        {
            return false;
        }

        if (!Owner.PhaseController.TryGetCurrentPhaseComponent(
                out m_references) ||
            !Owner.PhaseController.TryGetCurrentPhaseComponent(
                out m_commonReferences))
        {
            return false;
        }

        if (m_references.StateParameterAsset == null ||
            !m_references.StateParameterAsset.HasRequiredParameters())
        {
            return false;
        }

        m_parameters =
            m_references.StateParameterAsset.Turn;

        return m_parameters != null &&
               m_commonReferences.Origin != null;
    }

    /// <summary>
    /// 正常終了した方向転換のクールタイムを開始します。
    /// </summary>
    private void StartCoolTimeIfSucceeded()
    {
        if (Owner.GetStateExecutionStatus() !=
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

        coolTimeManager.StartCoolTime<S1P1BossTurnState>(
            m_references.DecisionParameterAsset.Turn.CoolTime);
    }

    /// <summary>
    /// 方向転換する方向を取得します。
    /// </summary>
    /// <returns>方向転換する方向。</returns>
    private Vector3 GetTurnDirection()
    {
        BossNavMeshFootprint bossNavMeshFootprint =
            m_commonReferences.NavMeshFootprint;

        Transform playerTransform =
            m_commonReferences.PlayerTransform;

        Transform originTransform =
            m_commonReferences.Origin;

        if (originTransform == null)
        {
            return Vector3.zero;
        }

        if (bossNavMeshFootprint != null &&
            Owner.Navigation != null &&
            !Owner.Navigation.IsFootprintInsideNavMesh(
                bossNavMeshFootprint))
        {
            if (Owner.Navigation.TryGetNavMeshInsideDirection(
                    bossNavMeshFootprint,
                    out Vector3 insideDirection))
            {
                return insideDirection;
            }
        }

        if (playerTransform == null)
        {
            return originTransform.forward;
        }

        Vector3 playerDirection =
            playerTransform.position -
            originTransform.position;

        playerDirection.y = 0.0f;

        if (playerDirection.sqrMagnitude <=
            Mathf.Epsilon)
        {
            return originTransform.forward;
        }

        return playerDirection.normalized;
    }
}
