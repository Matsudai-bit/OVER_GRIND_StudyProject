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

        // ジャンプ開始時に一度だけ上方向へ力を与える
        Owner.Motor.Jump(
            Owner.MovementParameterAsset.JumpPower);
    }

    /// <summary>
    /// 一定間隔の更新処理を行います。
    /// </summary>
    protected override void OnFixedUpdate()
    {
        Debug.Log(
            $"[Jump] y-vel={Owner.Motor.VerticalVelocity:F3}, " +
            $"y-pos={Owner.transform.position.y:F3}, " +
            $"deltaTime={Time.fixedDeltaTime:F4}, " +
            $"gravity.y={Physics.gravity.y:F3}");

        m_elapsedTime += Time.fixedDeltaTime;

        PlayerMovementParameterAsset parameterAsset =
            Owner.MovementParameterAsset;

        bool isJumpHeld =
            m_elapsedTime <
                parameterAsset.JumpInputDuration &&
            Owner.InputReader.HasJumpInput;

        // 空中時の水平方向に空気抵抗を適用する
        Owner.Motor.ApplyAirResistance(
            parameterAsset.AirResistance,
            Time.fixedDeltaTime);

        // 上昇・下降状態に応じた追加重力を適用する
        Owner.Motor.ApplyExtraGravity(
            parameterAsset,
            isJumpHeld,
            Time.fixedDeltaTime);

        // 上昇中はジャンプ状態を継続する。
        // 下降に入ったら、ジャンプ状態を終了する。
        if (Owner.Motor.VerticalVelocity <= 0.0f)
        {
            // Vブーストを中断した状態でジャンプした場合は、
            // 着地を待たずにVブースト状態へ復帰するのではなく、
            // ここでは従来通り下降開始を基準に状態を切り替える。
            //
            // IsBoostSuspendedの場合は、ジャンプによる中断から
            // Vブーストへ復帰する。
            if (Owner.IsBoostSuspended)
            {
                Machine.ChangeState<PlayerVRunningState>();
                return;
            }

            Machine.ChangeState<PlayerIdlingState>();
            return;
        }
    }

    /// <summary>
    /// 状態終了時に呼ばれます。
    /// </summary>
    protected override void OnExitState()
    {
        Owner.AnimationPresenter.StopJumpAnimation();
    }
}