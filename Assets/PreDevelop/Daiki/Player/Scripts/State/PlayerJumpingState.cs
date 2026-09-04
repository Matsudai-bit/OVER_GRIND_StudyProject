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

        // ジャンプ初速は開始時に一度だけ与える
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

        Owner.Motor.ApplyExtraGravity(
            parameterAsset,
            isJumpHeld,
            Time.fixedDeltaTime);

        // 頂点を過ぎて落下に転じたら状態遷移
        if (Owner.Motor.VerticalVelocity <= 0.0f)
        {
            Machine.ChangeState<PlayerIdlingState>();
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