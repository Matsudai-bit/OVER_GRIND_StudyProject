using UnityEngine;

/// <summary>
/// プレイヤーの待機状態を管理します。
/// </summary>
public sealed class PlayerIdlingState
    : StateBase<PlayerStateMachineComponent>
{
    /// <summary>
    /// 一定間隔の更新処理を行います。
    /// </summary>
    protected override void OnFixedUpdate()
    {
        // 移動入力があれば歩行状態へ遷移
        if (Owner.InputReader.HasMoveInput)
        {
            Machine.ChangeState<PlayerWalkingState>();
            return;
        }

        if (Owner.InputReader.ConsumeAttackInput())
        {
            Machine.ChangeState<PlayerAttackingState>();
            return;
        }

        if (Owner.Monitor.IsGrounded && Owner.InputReader.HasJumpInput)
        {
            Machine.ChangeState<PlayerJumpingState>();
            return;
        }

        if (Owner.Monitor.IsRailed)
        {
            Machine.ChangeState<PlayerGrindingState>();
            return;
        }

        // 入力がない間は水平速度を減速
        Owner.Motor.Decelerate(Time.fixedDeltaTime);
    }
}