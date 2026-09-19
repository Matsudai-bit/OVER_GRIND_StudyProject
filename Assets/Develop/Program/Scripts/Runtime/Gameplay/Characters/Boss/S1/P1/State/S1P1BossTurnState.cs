using UnityEngine;

/// <summary>
/// ステージ1フェーズ1の方向転換を実行します。
/// </summary>
public sealed class S1P1BossTurnState : StateBase<BossController>
{
    // 回転角度
    private const float ROTATE_ANGLE = 90.0f;

    // 回転時間
    private const float ROTATE_DURATION = 2.0f;

    // Animatorパラメータ名
    private const string TURN_PARAMETER_NAME = "Turn";

    // AnimatorパラメータID
    private static readonly int TURN_PARAMETER_ID =
        Animator.StringToHash(TURN_PARAMETER_NAME);

    // 目標回転
    private Quaternion m_targetRotation;

    // 回転速度
    private float m_rotateSpeed;

    S1P1BossReferences m_reference;
    BossPhaseReferences m_commonReference;

    /// <summary>
    /// 方向転換を開始します。
    /// </summary>
    protected override void OnStartState()
    {

        if (!Owner.PhaseController.TryGetCurrentPhaseComponent(out m_reference))
        {
            Debug.LogError("リファレンスが取得できません");
        }
        if (!Owner.PhaseController.TryGetCurrentPhaseComponent(out m_commonReference))
        {
            Debug.LogError("リファレンスが取得できません");
        }

        Vector3 turnDirection = GetTurnDirection();

        if (turnDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        m_targetRotation = Quaternion.LookRotation(
            turnDirection,
            Vector3.up);
        m_rotateSpeed = ROTATE_ANGLE / ROTATE_DURATION;

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
        if (Owner.Motor == null)
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);
            return;
        }

        bool hasReachedTarget = Owner.Motor.RotateTowards(
            m_targetRotation,
            m_rotateSpeed,
            Time.fixedDeltaTime);

        if (!hasReachedTarget)
        {
            return;
        }

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.SUCCEEDED);
    }

    /// <summary>
    /// 方向転換を終了します。
    /// </summary>
    protected override void OnExitState()
    {
        Owner.AnimationController?.SetBool(
            TURN_PARAMETER_ID,
            false);

        if (Owner.GetStateExecutionStatus() ==
            StateExecutionStatus.RUNNING)
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);
        }
    }

    /// <summary>
    /// 方向転換する方向を取得します。
    /// </summary>
    /// <returns>方向転換する方向。</returns>
    private Vector3 GetTurnDirection()
    {
    
  

        BossNavMeshFootprint bossNavMeshFootprint = m_reference.BossNavMeshFootprint;
        if (bossNavMeshFootprint == null)
        {
            Debug.LogError("bossNavMeshFootprintが取得できません");

        }

        var playerTransform = m_reference.PlayerTransform;
        var originTransform = m_commonReference.Origin;

        // ボスがNavMeshからはみ出している場合は内側を向く
        if (!Owner.Navigation.IsFootprintInsideNavMesh(
            bossNavMeshFootprint))
        {
            if (Owner.Navigation.TryGetNavMeshInsideDirection(
                    bossNavMeshFootprint,
                    out Vector3 insideDirection))
            {
                return insideDirection;
            }
        }
        
        // 通常時はPlayerの方向を向く
        if (playerTransform == null)
        {
            return originTransform.forward;
        }

        Vector3 playerDirection =
            playerTransform.position - originTransform.position;

        playerDirection.y = 0.0f;

        if (playerDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            return originTransform.forward;
        }

        return playerDirection.normalized;
    }
}
