using UnityEngine;
/// <summary>通常ジャンプの下降と、外力による滞空を管理します。</summary>
public sealed class PlayerJumpAirborneState : StateBase<PlayerStateMachineComponent>
{
    private bool m_isTransitionPending;
    private readonly bool m_inheritJumpAnimation;
    /// <summary>外力による滞空状態を生成します。</summary>
    public PlayerJumpAirborneState() { }
    /// <summary>ジャンプ開始時のアニメーションを引き継ぎます。</summary>
    /// <param name="inheritJumpAnimation">trueならAnimatorの状態を維持します。</param>
    public PlayerJumpAirborneState(bool inheritJumpAnimation)
    {
        m_inheritJumpAnimation = inheritJumpAnimation;
    }
    /// <summary>開始トリガーを発行せず滞空表示を継続します。</summary>
    protected override void OnStartState()
    {
        if (!m_inheritJumpAnimation)
            Owner.AnimationPresenter.ResumeAirborneAnimation(PlayerJumpAnimationType.MOVING);
    }
    /// <summary>着地・レール搭乗と空中の物理移動を更新します。</summary>
    protected override void OnFixedUpdate()
    {
        if (m_isTransitionPending) return;
        Owner.InputReader.ConsumeAttackInput();
        if (Owner.GrindController.CanStartGrind(Owner.Monitor.IsRailed ? Owner.Monitor.HitRailInfo : null))
        {
            m_isTransitionPending = true;
            Machine.ChangeState<PlayerGrindingState>(Owner.Monitor.HitRailInfo);
            return;
        }
        if (Owner.Motor.VerticalVelocity <= 0.0f && Owner.Monitor.IsGrounded && Owner.Monitor.HasGroundSupport())
        {
            m_isTransitionPending = true;
            if (Owner.IsBoostSuspended) Machine.ChangeState<PlayerVRunningState>();
            else Machine.ChangeState<PlayerIdlingState>();
            return;
        }
        var parameters = Owner.MovementParameterAsset;
        Owner.Motor.ApplyAirResistance(parameters.AirResistance, Time.fixedDeltaTime);
        Owner.Motor.ApplyExtraGravity(parameters, false, Time.fixedDeltaTime);
    }
    /// <summary>着地または中断時に滞空表示を終了します。</summary>
    protected override void OnExitState() { Owner.AnimationPresenter.StopJumpAnimation(); }
}
