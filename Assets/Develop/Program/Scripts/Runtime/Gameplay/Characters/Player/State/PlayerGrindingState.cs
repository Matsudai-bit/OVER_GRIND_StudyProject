using UnityEngine;

/// <summary>
/// グラインド移動と離脱先への遷移を物理更新に合わせて管理します。
/// </summary>
public sealed class PlayerGrindingState
    : StateBase<PlayerStateMachineComponent>
{
    // 搭乗中のレール
    private SplineRailInfo m_rail;

    // レールジャンプからの復帰か
    private readonly bool m_isResuming;

    // 復帰時のレール上位置
    private readonly float m_positionT;

    // 復帰時の進行方向
    private readonly int m_direction;

    // 復帰時の移動速度
    private readonly float m_speed;

    // グラインド開始済みか
    private bool m_hasStarted;

    // レール上で進行中のチャージ。
    private PlayerRailGrindCharge m_charge;
    private bool m_hasHandledChargePress;
    private int m_lastChargePressId;
    private bool m_isRailDashActive;
    private float m_speedBeforeCharge;

    // ForcesEscapeが無効なレール搭乗中に一時停止したPlayer CapsuleCollider
    private CapsuleCollider m_disabledPlayerCapsuleCollider;

    /// <summary>
    /// 通常のレール検出からグラインドを開始します。
    /// </summary>
    public PlayerGrindingState()
    {
    }

    /// <summary>
    /// 搭乗するレールを指定してグラインドを開始します。
    /// </summary>
    /// <param name="rail">搭乗するレール。</param>
    public PlayerGrindingState(
        SplineRailInfo rail)
    {
        m_rail = rail;
    }

    /// <summary>
    /// レールジャンプの着地点・方向・速度を引き継いで
    /// グラインドを再開します。
    /// </summary>
    /// <param name="rail">搭乗するレール。</param>
    /// <param name="positionT">レール上の位置。</param>
    /// <param name="direction">進行方向。</param>
    /// <param name="speed">グラインド速度。</param>
    public PlayerGrindingState(
        SplineRailInfo rail,
        float positionT,
        int direction,
        float speed)
    {
        m_rail = rail;
        m_positionT = positionT;
        m_direction = direction;
        m_speed = speed;
        m_isResuming = true;
    }

    /// <summary>
    /// 搭乗先を確定し、グラインドアニメーションを開始します。
    /// </summary>
    protected override void OnStartState()
    {
        if (m_rail == null &&
            !m_isResuming)
        {
            m_rail =
                Owner.Monitor.HitRailInfo;
        }

        Owner.InputReader.ConsumeJumpPress();

        // 通常移動音からレールグラインド音へ切り替えます。
        PlayerAudioActionUtility.StopSequential(
            Owner,
            PlayerAudioActionID.WALKING,
            true);

        PlayerAudioActionUtility.PlaySequential(
            Owner,
            PlayerAudioActionID.RAIL_GRINDING);

        Owner.AnimationPresenter
            .PlayRailGrindAnimation();
    }

    /// <summary>
    /// 物理更新ごとにグラインド移動を更新し、
    /// 終了状態に応じて次のStateへ遷移します。
    /// </summary>
    protected override void OnFixedUpdate()
    {
        if (!m_hasStarted)
        {
            // 障害物との衝突で強制離脱しないレールでは、
            // グラインド中だけPlayerのCapsuleColliderを無効化します。
            if (m_rail != null &&
                !m_rail.ForcesEscape)
            {
                DisablePlayerCapsuleCollider();
            }

            if (m_isResuming)
            {
                Owner.GrindController.StartGrindAt(
                    m_rail,
                    m_positionT,
                    m_direction,
                    m_speed);
            }
            else
            {
                Owner.GrindController.StartGrind(
                    m_rail);
            }

            m_hasStarted = true;
        }

        UpdateRailCharge();
        UpdateRailDashGauge();

        // レール終端で離脱した場合も接線方向を引き継ぎます。
        Vector3 travelDirection = Owner.GrindController.GetTravelDirection();
        Owner.GrindController.UpdateGrind(
            Time.fixedDeltaTime);

        if (Owner.GrindController.IsGrinding)
        {
            return;
        }

        RestorePlayerCapsuleCollider();

        if (m_charge != null && !Owner.GrindController.IsCollisionExiting)
        {
            CompleteChargeDash(travelDirection);
            Owner.RequestRailStateChange<PlayerBoostDashingState>();
            return;
        }

        // 障害物との衝突によるレール離脱
        if (Owner.GrindController.IsCollisionExiting)
        {
            Owner.RequestRailStateChange<
                PlayerRailExitingState>();

            return;
        }

        // Return to regular movement at a rail endpoint without starting a jump.
        if (Owner.GrindController.DidReachRailEnd)
        {
            if (Owner.Monitor.IsGrounded)
            {
                Owner.RequestRailStateChange<PlayerIdlingState>();
            }
            else
            {
                Owner.RequestRailStateChange<PlayerJumpAirborneState>();
            }

            return;
        }

        // 攻撃入力を維持した状態でレールを抜けた場合
        if (Owner.InputReader.IsAttackHeld)
        {
            Owner.RequestRailStateChange<
                PlayerRailExitingState>(
                true);

            return;
        }

        // 通常のレール終了
        // 通常のレール終了
        Owner.RequestRailStateChange<
            PlayerRailExitingState>();
    }

    /// <summary>
    /// グラインドState終了時に
    /// グラインドアニメーションを停止します。
    /// </summary>
    protected override void OnExitState()
    {
        if (m_charge != null)
            Owner.VGaugePlaceModel.SetGaugeRate(0.0f);
        m_isRailDashActive = false;
        m_charge = null;
        RestorePlayerCapsuleCollider();

        PlayerAudioActionUtility.StopSequential(
            Owner,
            PlayerAudioActionID.RAIL_GRINDING);

        Owner.AnimationPresenter
            .StopRailGrindAnimation();
    }

    /// <summary>Cancel the current charge and restore normal rail-jump speed.</summary>
    public void CancelChargeForRailJump()
    {
        if (m_charge == null) return;

        Owner.GrindController.SetGrindSpeed(m_charge.StartSpeed);
        m_charge = null;
        Owner.IsBoostSuspended = false;
        Owner.CarriedBoostGaugeRate = 0.0f;
        Owner.SuspendedBoostGaugeRate = 0.0f;
        Owner.SetBoostGaugeDepletionRate(0.0f);
        Owner.VGaugePlaceModel.SetGaugeRate(0.0f);
        Owner.InputReader.SuppressRailChargeUntilRelease();
        Owner.InputReader.DiscardVBoostPendingInput();
    }

    /// <summary>入力とチャージ量を更新し、解放時はレール速度をダッシュ速度へ変更します。</summary>
    private void UpdateRailCharge()
    {
        var controller = Owner.GrindController;
        var input = Owner.InputReader;
        var parameters = controller.ChargeParameterAsset;
        if (parameters == null || !controller.IsGrinding) return;

        // 搭乗前のStateが開始フラグを消費していても、押下IDは維持されます。
        if (m_charge == null && input.CanStartRailCharge &&
            (!m_hasHandledChargePress || m_lastChargePressId != input.VBoostPressId))
        {
            m_hasHandledChargePress = true;
            m_lastChargePressId = input.VBoostPressId;
            input.ConsumeVBoostStarted();
            Owner.IsBoostSuspended = false;
            m_isRailDashActive = false;
            m_charge = new PlayerRailGrindCharge(parameters, controller.CurrentSpeed);
        }

        if (m_charge == null) return;
        if (!input.IsVBoostHeld)
        {
            input.ConsumeVBoostReleased();
            m_speedBeforeCharge = m_charge.StartSpeed;
            controller.SetGrindSpeed(parameters.DashGrindSpeed);
            Owner.CarriedBoostGaugeRate = m_charge.ChargeRate;
            Owner.SuspendedBoostGaugeRate = m_charge.ChargeRate;
            Owner.SetBoostGaugeDepletionRate(1.0f / parameters.DashDuration);
            Owner.VGaugePlaceModel.SetGaugeRate(m_charge.ChargeRate);
            Owner.IsBoostSuspended = true;
            m_isRailDashActive = true;
            m_charge = null;
            return;
        }

        m_charge.UpdateCharge(controller, Time.fixedDeltaTime);
        Owner.VGaugePlaceModel.SetGaugeRate(m_charge.ChargeRate);
    }

    /// <summary>レールダッシュの残量を消費し、終了時に開始前の速度へ戻します。</summary>
    private void UpdateRailDashGauge()
    {
        if (!m_isRailDashActive || !Owner.GrindController.IsGrinding) return;

        // 残量はPlayerStateMachineComponentの物理更新で消費します。
        Owner.CarriedBoostGaugeRate = Owner.SuspendedBoostGaugeRate;
        if (Owner.SuspendedBoostGaugeRate > 0.0f) return;

        m_isRailDashActive = false;
        Owner.GrindController.SetGrindSpeed(m_speedBeforeCharge);
    }

    /// <summary>蓄えたチャージ量をレール接線方向のブーストダッシュへ渡します。</summary>
    /// <param name="direction">レール上の進行方向。</param>
    private void CompleteChargeDash(Vector3 direction)
    {
        Owner.BoostDashDirection = direction;
        Owner.CarriedBoostGaugeRate = m_charge.ChargeRate;
        Owner.IsBoostSuspended = false;
        Owner.InputReader.ConsumeVBoostReleased();
        m_charge = null;
    }

    /// <summary>
    /// ForcesEscapeが無効なレール搭乗中だけ、
    /// PlayerのCapsuleColliderを一時的に無効化します。
    /// </summary>
    private void DisablePlayerCapsuleCollider()
    {
        if (m_disabledPlayerCapsuleCollider != null)
        {
            return;
        }

        if (!Owner.gameObject.TryGetComponent(
                out CapsuleCollider capsuleCollider) ||
            !capsuleCollider.enabled)
        {
            return;
        }

        m_disabledPlayerCapsuleCollider =
            capsuleCollider;

        m_disabledPlayerCapsuleCollider.enabled =
            false;
    }

    /// <summary>
    /// グラインド中に無効化したPlayer CapsuleColliderを復元します。
    /// </summary>
    private void RestorePlayerCapsuleCollider()
    {
        if (m_disabledPlayerCapsuleCollider == null)
        {
            return;
        }

        m_disabledPlayerCapsuleCollider.enabled =
            true;

        m_disabledPlayerCapsuleCollider =
            null;
    }
}
