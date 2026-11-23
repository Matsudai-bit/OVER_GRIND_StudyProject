using UnityEngine;

/// <summary>ダッシュ後と中断復帰後の通常ブースト移動を管理します。</summary>
public sealed class PlayerVRunningState : PlayerBoostRunningStateBase
{
    private readonly bool m_isDashHandoff;
    private readonly EffectHandle m_inheritedLoopEffect;
    private PlayerMoveParameters m_moveParameters;
    private Vector3 m_movingChargeStartDirection;
    private bool m_isWaitingForMovingCharge;

    /// <summary>中断したブースト移動を再開します。</summary>
    public PlayerVRunningState() { }

    /// <summary>ダッシュから演出を引き継いで通常移動を開始します。</summary>
    /// <param name="loopEffect">ダッシュ中の継続エフェクト。</param>
    public PlayerVRunningState(EffectHandle loopEffect)
    {
        m_isDashHandoff = true;
        m_inheritedLoopEffect = loopEffect;
    }

    /// <summary>通常移動では移動入力がなくなったら停止します。</summary>
    protected override bool StopsWithoutMoveInput => true;

    /// <summary>通常ブースト移動のパラメータと演出を準備します。</summary>
    protected override void OnStartState()
    {
        m_isWaitingForMovingCharge = false;
        m_moveParameters = Owner.VBoostMovementParameterAsset.CreateStableBoostParameters();

        if (Owner.IsBoostSuspended)
            Owner.IsBoostSuspended = false;

        StartBoost(m_isDashHandoff, m_inheritedLoopEffect);
    }

    /// <summary>再チャージ入力を確認し、通常ブースト移動を行います。</summary>
    protected override void UpdateBoostMovement()
    {
        UpdateMovingChargeStartDirection();

        if (Owner.Monitor.IsGrounded &&
            m_isWaitingForMovingCharge &&
            Owner.InputReader.HasVBoostHoldStarted &&
            Owner.BoostChargingParameterAsset.CanStartMovingCharge(
                Owner.Motor.HorizontalSpeed) &&
            Owner.BoostChargingParameterAsset.HasLateralChargeInput(
                Owner.InputReader.MoveInput) &&
            Owner.BoostChargingParameterAsset.HasExceededFreeSteeringAngle(
                m_movingChargeStartDirection,
                Owner.Motor.HorizontalDirection) &&
            Owner.InputReader.ConsumeVBoostHoldStarted())
        {
            PlayerAudioActionUtility.StopSequential(
                Owner, PlayerAudioActionID.V_RUNNING, true);
            Machine.ChangeState<PlayerDynamicChargeState>();
            return;
        }

        Owner.Motor.Move(
            Owner.InputReader.MoveInput,
            m_moveParameters,
            Time.fixedDeltaTime);
    }

    /// <summary>移動チャージ開始時の進行方向を保存します。</summary>
    private void UpdateMovingChargeStartDirection()
    {
        if (!Owner.InputReader.IsVBoostHeld)
        {
            m_isWaitingForMovingCharge = false;
            return;
        }

        if (m_isWaitingForMovingCharge ||
            !Owner.InputReader.ConsumeVBoostStarted())
            return;

        m_movingChargeStartDirection = Owner.Motor.HorizontalDirection;
        m_isWaitingForMovingCharge = true;
    }
}
