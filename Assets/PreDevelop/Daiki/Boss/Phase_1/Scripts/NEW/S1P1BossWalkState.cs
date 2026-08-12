using UnityEngine;

/// <summary>
/// ステージ1フェーズ1の歩行を実行します。
/// </summary>
public sealed class S1P1BossWalkState : StateBase<BossController>
{
    // 歩行時間
    private const float WALK_DURATION = 3.0f;

    // 目標速度
    private const float TARGET_SPEED = 100.0f;

    // 加速度
    private const float ACCELERATION = 100.0f;

    // Animatorパラメータ名
    private const string WALK_PARAMETER_NAME = "Walk";

    // AnimatorパラメータID
    private static readonly int WALK_PARAMETER_ID =
        Animator.StringToHash(WALK_PARAMETER_NAME);

    // 経過時間
    private float m_elapsedTime;

    /// <summary>
    /// 歩行を開始します。
    /// </summary>
    protected override void OnStartState()
    {
        m_elapsedTime = 0.0f;

        Owner.AnimationController?.SetBool(
            WALK_PARAMETER_ID,
            true);

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.RUNNING);
    }

    /// <summary>
    /// 歩行時間を更新します。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過時間。</param>
    protected override void OnUpdate(float deltaTime)
    {
        m_elapsedTime += deltaTime;

        if (m_elapsedTime < WALK_DURATION)
        {
            return;
        }

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.SUCCEEDED);
    }

    /// <summary>
    /// 物理移動を実行します。
    /// </summary>
    protected override void OnFixedUpdate()
    {
        Owner.Motor?.MoveForward(
            TARGET_SPEED,
            ACCELERATION,
            Time.fixedDeltaTime);
    }

    /// <summary>
    /// 歩行を終了します。
    /// </summary>
    protected override void OnExitState()
    {
        Owner.AnimationController?.SetBool(
            WALK_PARAMETER_ID,
            false);

        if (Owner.GetStateExecutionStatus() ==
            StateExecutionStatus.RUNNING)
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);
        }
    }
}
