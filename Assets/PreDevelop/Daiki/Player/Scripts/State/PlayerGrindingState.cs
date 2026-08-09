using UnityEngine;

/// <summary>
/// プレイヤーのジャンプ状態を管理します。
/// </summary>
public sealed class PlayerGrindingState
    : StateBase<PlayerStateMachineComponent>
{
    protected override void OnStartState()
    {
        Owner.GrindController.StartGrind(Owner.Monitor.HitRailInfo);
    }

    protected override void OnUpdate(float deltaTime)
    {
        if (!Owner.GrindController.IsGrinding)
        {
            Machine.ChangeState<PlayerIdlingState>();
        }
    }
    /// <summary>
    /// 一定間隔の更新処理を行います。
    /// </summary>
    protected override void OnFixedUpdate()
    {
      
    }

    protected override void OnExitState()
    {
    }
}