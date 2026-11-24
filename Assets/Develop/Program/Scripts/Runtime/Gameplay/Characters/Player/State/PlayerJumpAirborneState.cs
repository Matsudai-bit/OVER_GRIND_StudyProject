using UnityEngine;

/// <summary>
/// 通常ジャンプの下降と、外力による滞空を管理します。
/// </summary>
public sealed class PlayerJumpAirborneState
    : StateBase<PlayerStateMachineComponent>
{
    // 状態遷移要求済みか
    private bool m_isTransitionPending;

    // 前Stateのジャンプアニメーションを引き継ぐか
    private readonly bool m_inheritJumpAnimation;

    // 滑空・下降中の水平操作パラメータ
    private PlayerMoveParameters m_airMoveParameters;

    /// <summary>
    /// 外力による滞空状態を生成します。
    /// </summary>
    public PlayerJumpAirborneState()
    {
    }

    /// <summary>
    /// ジャンプ開始時のアニメーションを引き継ぐ滞空状態を生成します。
    /// </summary>
    /// <param name="inheritJumpAnimation">
    /// true：前Stateのジャンプアニメーションを維持します。
    /// </param>
    public PlayerJumpAirborneState(
        bool inheritJumpAnimation)
    {
        m_inheritJumpAnimation =
            inheritJumpAnimation;
    }

    /// <summary>
    /// 滑空・下降状態を開始します。
    /// </summary>
    protected override void OnStartState()
    {
        m_isTransitionPending = false;

        m_airMoveParameters =
            Owner.MovementParameterAsset
                .CreateAirMoveParameters();

        if (!m_inheritJumpAnimation)
        {
            Owner.AnimationPresenter
                .ResumeAirborneAnimation(
                    PlayerJumpAnimationType.MOVING);
        }
    }

    /// <summary>
    /// 着地・レール搭乗と空中の物理移動を更新します。
    /// </summary>
    protected override void OnFixedUpdate()
    {
        if (m_isTransitionPending)
        {
            return;
        }

        // 通常滞空中の攻撃入力を着地後へ持ち越しません。
        Owner.InputReader.ConsumeAttackInput();

        if (Owner.GrindController.CanStartGrind(
                Owner.Monitor.IsRailed
                    ? Owner.Monitor.HitRailInfo
                    : null))
        {
            m_isTransitionPending = true;

            Machine.ChangeState<PlayerGrindingState>(
                Owner.Monitor.HitRailInfo);

            return;
        }

        if (Owner.Motor.VerticalVelocity <= 0.0f &&
            Owner.Monitor.IsGrounded &&
            Owner.Monitor.HasGroundSupport())
        {
            m_isTransitionPending = true;

            if (Owner.IsBoostSuspended)
            {
                Machine.ChangeState<PlayerVRunningState>();
            }
            else
            {
                Machine.ChangeState<PlayerIdlingState>();
            }

            return;
        }

        PlayerMovementParameterAsset parameterAsset =
            Owner.MovementParameterAsset;

        // 滑空中も従来どおり空気抵抗を適用します。
        Owner.Motor.ApplyAirResistance(
            parameterAsset.AirResistance,
            Time.fixedDeltaTime);

        // 移動入力があれば、カメラ基準方向へ水平操作します。
        if (Owner.InputReader.HasMoveInput)
        {
            Owner.Motor.MoveAir(
                Owner.InputReader.MoveInput,
                m_airMoveParameters,
                Time.fixedDeltaTime);
        }

        Owner.Motor.ApplyExtraGravity(
            parameterAsset,
            false,
            Time.fixedDeltaTime);
    }

    /// <summary>
    /// 着地または中断時に滞空表示を終了します。
    /// </summary>
    protected override void OnExitState()
    {
        Owner.AnimationPresenter
            .StopJumpAnimation();
    }
}
