using UnityEngine;

/// <summary>
/// プレイヤーの通常歩行状態を管理します。
/// </summary>
public sealed class PlayerWalkingState
    : StateBase<PlayerStateMachineComponent>
{
    // 通常移動パラメータ
    private PlayerMoveParameters m_moveParameters;

    /// <summary>
    /// 状態開始時に呼ばれます。
    /// </summary>
    protected override void OnStartState()
    {
        m_moveParameters =
            Owner.MovementParameterAsset.CreateMoveParameters();

        Owner.AnimationPresenter.PlayWalkAnimation();
    }

    /// <summary>
    /// 一定間隔の更新処理を行います。
    /// </summary>
    protected override void OnFixedUpdate()
    {
        // 攻撃入力を確認
        if (Owner.InputReader.ConsumeAttackInput())
        {
            Machine.ChangeState<PlayerAttackingState>();
            return;
        }

        // 移動入力がなければ待機状態へ遷移
        if (!Owner.InputReader.HasMoveInput)
        {
            // Vブースト入力が残らないように消費
            Owner.InputReader.ConsumeVBoostInput();

            Machine.ChangeState<PlayerIdlingState>();
            return;
        }

        // ジャンプ入力を確認
        if (Owner.Monitor.IsGrounded &&
            Owner.InputReader.HasJumpInput)
        {
            Machine.ChangeState<PlayerJumpingState>();
            return;
        }

        // Vブーストの長押し成立を確認。
        // ただし新規のブースト開始は接地中のみ許可する
        if (Owner.Monitor.IsGrounded &&
            Owner.InputReader.ConsumeVBoostHoldStarted())
        {
            Machine.ChangeState<PlayerBoostChargingState>();
            return;
        }

        // 通常移動
        Owner.Motor.Move(
            Owner.InputReader.MoveInput,
            m_moveParameters,
            Time.fixedDeltaTime);
    }

    /// <summary>
    /// 状態終了時に呼ばれます。
    /// </summary>
    protected override void OnExitState()
    {
        Owner.AnimationPresenter.StopWalkAnimation();
    }
}