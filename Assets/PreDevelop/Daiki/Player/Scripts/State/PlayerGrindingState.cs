using UnityEngine;

/// <summary>
/// プレイヤーのグラインド状態と離脱先への遷移を管理します。
/// </summary>
public sealed class PlayerGrindingState
    : StateBase<PlayerStateMachineComponent>
{
    /// <summary>
    /// 接触中のレールでグラインドを開始します。
    /// </summary>
    protected override void OnStartState()
    {
        Owner.GrindController.StartGrind(Owner.Monitor.HitRailInfo);
    }

    /// <summary>
    /// 衝突による離脱、通常終了、ジャンプの遷移を処理します。
    /// </summary>
    protected override void OnUpdate(float deltaTime)
    {
        if (!Owner.GrindController.IsGrinding)
        {
            if (Owner.GrindController.IsCollisionExiting)
            {
                Machine.ChangeState<PlayerRailExitingState>();
            }
            else
            {
                Machine.ChangeState<PlayerIdlingState>();
            }
            return;
        }

        if (Owner.InputReader.HasJumpInput)
        {
            Owner.GrindController.StopGrind();
            Machine.ChangeState<PlayerJumpingState>();
        }
    }
    /// <summary>
    /// 一定間隔の更新処理を行います。
    /// </summary>
    protected override void OnFixedUpdate()
    {
      
    }

    /// <summary>
    /// グラインド状態の終了処理を行います。
    /// </summary>
    protected override void OnExitState()
    {
    }
}