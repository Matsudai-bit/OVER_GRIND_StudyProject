using UnityEngine;

/// <summary>
/// プレイヤーのジャンプ状態を管理します。
/// </summary>
public sealed class PlayerJumpingState
    : StateBase<PlayerStateMachineComponent>
{
    // ジャンプ開始からの経過時間
    private float m_elapsedTime;

    /// <summary>
    /// 状態開始時に呼ばれます。
    /// </summary>
    protected override void OnStartState()
    {
        m_elapsedTime = 0.0f;

        Owner.AnimationPresenter.PlayJumpAnimation();
    }

    /// <summary>
    /// 一定間隔の更新処理を行います。
    /// </summary>
    protected override void OnFixedUpdate()
    {
        m_elapsedTime += Time.fixedDeltaTime;

        PlayerMovementParameterAsset parameterAsset =
            Owner.MovementParameterAsset;

        if (m_elapsedTime <
                parameterAsset.JumpInputDuration &&
            Owner.InputReader.HasJumpInput)
        {
            Owner.Motor.Jump(
                parameterAsset.JumpPower,
                Time.fixedDeltaTime);

            return;
        }

        // ジャンプ前がVブースト状態だった場合は、
        // 待機状態を経由せず直接Vブーストへ復帰する
        if (Owner.IsBoostSuspended)
        {
            Machine.ChangeState<PlayerVRunningState>();
            return;
        }

        Machine.ChangeState<PlayerIdlingState>();
    }

    /// <summary>
    /// 状態終了時に呼ばれます。
    /// </summary>
    protected override void OnExitState()
    {
        Owner.AnimationPresenter.StopJumpAnimation();
    }
}