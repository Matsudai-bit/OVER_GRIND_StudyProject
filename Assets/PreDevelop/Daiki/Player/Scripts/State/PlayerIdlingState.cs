using UnityEngine;

/// <summary>
/// プレイヤーの待機状態を管理します。
/// </summary>
public sealed class PlayerIdlingState
    : StateBase<PlayerStateMachineComponent>
{
    // 通常移動パラメータ
    private PlayerMoveParameters m_moveParameters;

    /// <summary>
    /// 待機開始時に呼ばれます。
    /// </summary>
    protected override void OnStartState()
    {
        m_moveParameters =
            Owner.MovementParameterAsset.CreateMoveParameters();
    }

    /// <summary>
    /// 一定間隔の更新処理を行います。
    /// </summary>
    protected override void OnFixedUpdate()
    {
        if (Owner.InputReader.ConsumeAttackInput())
        {
            Machine.ChangeState<PlayerAttackingState>();
            return;
        }

        if (Owner.Monitor.IsGrounded &&
            Owner.InputReader.HasJumpInput)
        {
            Machine.ChangeState<PlayerJumpingState>();
            return;
        }

        if (Owner.Monitor.IsRailed)
        {
            Machine.ChangeState<PlayerGrindingState>();
            return;
        }

        // 移動入力があれば移動状態へ遷移
        if (Owner.InputReader.HasMoveInput)
        {
            // Vブーストが中断中であれば、接地状態に関わらず
            // 通常歩行ではなくVブースト状態へ復帰する
            // （これはジャンプ等で中断したものの再開であり、
            // 　新規開始ではないため接地条件の対象外とする）
            if (Owner.IsBoostSuspended)
            {
                Machine.ChangeState<PlayerVRunningState>();
                return;
            }

            // Vブースト入力が開始されたら
            // ブーストチャージ状態へ遷移する。
            // ただし新規のブースト開始は接地中のみ許可する
            if (Owner.Monitor.IsGrounded &&
                Owner.InputReader.ConsumeVBoostStarted())
            {
                Machine.ChangeState<PlayerBoostChargingState>();
                return;
            }

            Machine.ChangeState<PlayerWalkingState>();
            return;
        }

        // 移動入力がなくても残っている
        // Vブースト開始入力を消費する
        Owner.InputReader.ConsumeVBoostStarted();

        // 通常移動パラメータで停止
        Owner.Motor.Decelerate(
            m_moveParameters,
            Time.fixedDeltaTime);
    }
}