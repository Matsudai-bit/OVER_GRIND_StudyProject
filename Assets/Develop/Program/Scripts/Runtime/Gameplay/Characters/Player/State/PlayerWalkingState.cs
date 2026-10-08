using UnityEngine;

/// <summary>
/// プレイヤーの通常歩行状態を管理します。
/// </summary>
public sealed class PlayerWalkingState
    : StateBase<PlayerStateMachineComponent>
{
    // 通常移動パラメータ
    private PlayerMoveParameters m_moveParameters;
    private Vector3 m_movingChargeStartDirection;
    private bool m_isWaitingForMovingCharge;

    /// <summary>
    /// 状態開始時に呼ばれます。
    /// </summary>
    protected override void OnStartState()
    {
        m_isWaitingForMovingCharge = false;

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
        if (Owner.InputReader.ConsumeAttackInput() && Owner.Monitor.IsGrounded)
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
        if (Owner.Monitor.CanStartJump &&
            Owner.InputReader.HasJumpInput)
        {
            Machine.ChangeState<PlayerJumpingState>();
            return;
        }

        // Vブーストの長押し成立を確認。
        // ただし新規のブースト開始は接地中のみ許可する
        UpdateMovingChargeStartDirection();

        if (Owner.Monitor.IsGrounded &&
            m_isWaitingForMovingCharge &&
            Owner.InputReader.HasVBoostHoldStarted &&
            Owner.BoostChargingParameterAsset.HasLateralChargeInput(
                Owner.InputReader.MoveInput) &&
            Owner.BoostChargingParameterAsset.HasExceededFreeSteeringAngle(
                m_movingChargeStartDirection,
                Owner.Motor.HorizontalDirection) &&
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

    /// <summary>チャージボタン押下時の実移動方向を開始判定用に保持します。</summary>
    private void UpdateMovingChargeStartDirection()
    {
        if (!Owner.InputReader.IsVBoostHeld)
        {
            m_isWaitingForMovingCharge = false;
            return;
        }

        if (m_isWaitingForMovingCharge ||
            !Owner.InputReader.ConsumeVBoostStarted())
        {
            return;
        }

        m_movingChargeStartDirection =
            Owner.Motor.HorizontalDirection;
        m_isWaitingForMovingCharge = true;
    }

    /// <summary>
    /// 状態終了時に呼ばれます。
    /// </summary>
    protected override void OnExitState()
    {
        Owner.AnimationPresenter.StopWalkAnimation();
    }
}
