using UnityEngine;

/// <summary>
/// プレイヤーの歩行状態を管理します。
/// </summary>
public sealed class PlayerWalkingState
    : StateBase<PlayerStateMachineComponent>
{
    /// <summary>
    /// 一定間隔の更新処理を行います。
    /// </summary>
    protected override void OnFixedUpdate()
    {
        // 移動入力がなくなったら待機状態へ遷移
        if (!Owner.InputReader.HasMoveInput)
        {
            Machine.ChangeState<PlayerIdlingState>();
            return;
        }

        // 入力方向へプレイヤーを移動
        Owner.Motor.Move(
            Owner.InputReader.MoveInput,
            Time.fixedDeltaTime);
    }
}