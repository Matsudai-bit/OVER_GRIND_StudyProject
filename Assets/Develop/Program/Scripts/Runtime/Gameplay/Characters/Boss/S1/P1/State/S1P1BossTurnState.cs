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

    /// <summary>
    /// 方向転換を開始します。
    /// </summary>
    protected override void OnStartState()
    {
        m_targetRotation =
            Owner.transform.rotation *
            Quaternion.Euler(0.0f, ROTATE_ANGLE, 0.0f);

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
}
