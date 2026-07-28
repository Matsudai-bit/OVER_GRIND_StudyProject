using UnityEngine;

/// <summary>
/// プレイヤーのジャンプ状態を管理します。
/// </summary>
public sealed class PlayerJumpingState
    : StateBase<PlayerStateMachineComponent>
{

    float elapsedTime;
    protected override void OnStartState()
    {
        elapsedTime = 0.0f;
        Owner.AnimationPresenter.PlayJumpAnimation();
    }

    /// <summary>
    /// 一定間隔の更新処理を行います。
    /// </summary>
    protected override void OnFixedUpdate()
    {
        elapsedTime += Time.fixedDeltaTime;

        if (elapsedTime < 0.5f && Owner.InputReader.HasJumpInput)
        {
             Owner.Motor.Jump(Time.fixedDeltaTime);

        }
        else
        {
            Machine.ChangeState<PlayerIdlingState>();
        }
    }

    protected override void OnExitState()
    {
        Owner.AnimationPresenter.StopJumpAnimation();
    }
}