using UnityEngine;

/// <summary>
/// ステージ1フェーズ1の歩行を実行します。
/// </summary>
public sealed class S1P1BossWalkState :
    StateBase<BossController>
{
    // 歩行時間
    private const float WALK_DURATION = 3.0f;

    // 前方の通行可能判定を行う距離
    private const float FORWARD_CHECK_DISTANCE = 5.0f;

    // 通行不能時に移行する停止時間
    private const float IDLE_DURATION = 3.0f;

    // Animatorパラメータ名
    private const string WALK_PARAMETER_NAME = "Walk";

    // AnimatorパラメータID
    private static readonly int WALK_PARAMETER_ID =
        Animator.StringToHash(WALK_PARAMETER_NAME);

    // 歩行経過時間
    private float m_elapsedTime;

    // 停止状態への変更を要求したか
    private bool m_isIdleRequested;

    /// <summary>
    /// 歩行を開始します。
    /// </summary>
    protected override void OnStartState()
    {
        m_elapsedTime = 0.0f;
        m_isIdleRequested = false;

        if (Owner.Navigation == null ||
            Owner.Motor == null)
        {
            RequestIdleState();
            return;
        }

        // 開始時点で前方へ進めるか確認します。
        if (!CanMoveForward())
        {
            RequestIdleState();
            return;
        }

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
        if (m_isIdleRequested)
        {
            return;
        }

        m_elapsedTime += deltaTime;

        if (m_elapsedTime < WALK_DURATION)
        {
            return;
        }

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.SUCCEEDED);
    }

    /// <summary>
    /// 前方への物理移動を実行します。
    /// </summary>
    protected override void OnFixedUpdate()
    {
        if (m_isIdleRequested)
        {
            return;
        }

        // 前方へ直進できなくなった場合は停止状態へ移行します。
        if (!CanMoveForward())
        {
            RequestIdleState();
            return;
        }

        Owner.Motor.MoveForward(
            Time.fixedDeltaTime);
    }

    /// <summary>
    /// 歩行を終了します。
    /// </summary>
    protected override void OnExitState()
    {
        Owner.Motor?.StopHorizontalMovement();

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

    /// <summary>
    /// ボスの前方へ直進できるか確認します。
    /// </summary>
    /// <returns>
    /// true：前方へ直進できます。
    /// false：前方へ直進できません。
    /// </returns>
    private bool CanMoveForward()
    {
        if (Owner.Navigation == null)
        {
            return false;
        }

        return Owner.Navigation.CanMoveStraight(
            Owner.transform.forward,
            FORWARD_CHECK_DISTANCE);
    }

    /// <summary>
    /// 停止状態への変更を要求します。
    /// </summary>
    private void RequestIdleState()
    {
        if (m_isIdleRequested)
        {
            return;
        }

        m_isIdleRequested = true;

        // State切り替えまでの間も進まないよう即座に停止します。
        Owner.Motor?.StopHorizontalMovement();

        Machine.ChangeState<BossIdleState>(
            IDLE_DURATION);
    }
}