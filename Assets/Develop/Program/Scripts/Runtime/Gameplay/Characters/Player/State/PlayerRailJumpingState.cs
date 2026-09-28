using UnityEngine;

/// <summary>レール軌道上のジャンプと、入力方向への飛び降りを管理します。</summary>
public sealed class PlayerRailJumpingState : StateBase<PlayerStateMachineComponent>
{
    private SplineRailInfo m_sourceRail;
    private readonly Vector2 m_moveInput;
    private PlayerRailJumpPath m_path;
    private float m_upSpeed;
    private float m_jumpHeight;
    private float m_elapsedTime;
    private float m_landingDelay;
    private bool m_isFollowingRail;
    private bool m_hasLeftSourceRail;
    private bool m_isTransitionPending;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private const float DIAGNOSTIC_DURATION = 0.2f;
    private const float DIAGNOSTIC_SPEED_RATIO = 0.5f;
    private Vector3 m_diagnosticLaunchVelocity;
    private bool m_hasCheckedFirstStep;
    private bool m_hasReportedSpeedLoss;
    private bool m_isDirectionalJump;
#endif

    /// <summary>ジャンプ入力時点の方向入力を保持します。</summary>
    /// <param name="moveInput">ジャンプ入力時点の方向入力。</param>
    public PlayerRailJumpingState(Vector2 moveInput)
    {
        m_moveInput = moveInput;
    }

    /// <summary>入力に応じた初速を設定し、ジャンプ演出を開始します。</summary>
    protected override void OnStartState()
    {
        PlayerRailJumpParameters parameters = Owner.RailJumpParameters;
        SplineGrindController grind = Owner.GrindController;
        m_sourceRail = grind.CurrentRail;
        m_path = new PlayerRailJumpPath(m_sourceRail, grind.CurrentSpeed,
            grind.CurrentPositionT, grind.Direction, grind.RideHeight);
        // この状態は物理更新の先頭で開始するため、離脱と発射の間に空のフレームを挟みません。
        grind.StopGrind();
        Vector3 exitVelocity = Owner.Monitor.CurrentVelocity;
        m_landingDelay = parameters.LandingDelay;
        Owner.InputReader.SuppressJumpUntilRelease();
        Owner.InputReader.ConsumeAttackInput();
        Owner.AnimationPresenter.PlayJumpAnimation();

        Vector3 launchVelocity;
        if (m_moveInput.sqrMagnitude > parameters.DirectionDeadZone * parameters.DirectionDeadZone)
        {
            grind.BlockRailUntilSeparated(m_sourceRail);
            // 地上移動と同じカメラ基準で、発射時に方向を確定します。
            Vector3 direction = Owner.Motor.CalculateCameraRelativeDirection(m_moveInput);
            launchVelocity = parameters.CalculateLaunchVelocity(direction);
        }
        else
        {
            m_upSpeed = parameters.OnRailUpSpeed;
            m_isFollowingRail = m_path.CanFollow();
            launchVelocity = exitVelocity + Vector3.up * m_upSpeed;
        }
        Owner.Motor.SetWorldVelocity(launchVelocity);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        m_isDirectionalJump = m_moveInput.sqrMagnitude > parameters.DirectionDeadZone * parameters.DirectionDeadZone;
        m_diagnosticLaunchVelocity = launchVelocity;
        Owner.Motor.ResetRailJumpContact();
        if (m_isDirectionalJump)
            Debug.Log($"[RailJump] Launch frame={Time.frameCount}, input={m_moveInput}, velocity={launchVelocity:F3}, {Owner.Motor.RailJumpPhysicsDetails}", Owner);
#endif
        if (m_isFollowingRail) Owner.Motor.BeginRailMotion();
    }

    /// <summary>ジャンプ中の攻撃入力を消費し、着地後へ持ち越さないようにします。</summary>
    protected override void OnUpdate(float deltaTime)
    {
        Owner.InputReader.ConsumeAttackInput();
    }

    /// <summary>軌道追従または慣性移動を続け、下降時に着地と再搭乗を判定します。</summary>
    protected override void OnFixedUpdate()
    {
        if (m_isTransitionPending) return;
        float deltaTime = Time.fixedDeltaTime;
        m_elapsedTime += deltaTime;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        DiagnoseLaunchVelocity(deltaTime);
#endif
        if (!Owner.Monitor.IsRailed || Owner.Monitor.HitRailInfo != m_sourceRail)
            m_hasLeftSourceRail = true;

        if (m_isFollowingRail)
        {
            UpdateRailJump(deltaTime);
            return;
        }

        // 上昇中や離陸直後は、元のレールの検出範囲にいても搭乗しません。
        if (m_elapsedTime < m_landingDelay || Owner.Motor.VerticalVelocity > 0.0f) return;

        SplineRailInfo hitRail = Owner.Monitor.IsRailed ? Owner.Monitor.HitRailInfo : null;
        if ((hitRail != m_sourceRail || m_hasLeftSourceRail) &&
            Owner.GrindController.CanStartGrind(hitRail))
        {
            m_isTransitionPending = true;
            Owner.RequestRailStateChange<PlayerGrindingState>(hitRail);
            return;
        }

        if (Owner.Monitor.IsGrounded)
        {
            m_isTransitionPending = true;
            Owner.RequestRailStateChange<PlayerIdlingState>();
        }
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    /// <summary>発射後の物理更新で初速が大きく失われた場合、一度だけ記録します。</summary>
    private void DiagnoseLaunchVelocity(float deltaTime)
    {
        // 開始と同じFixedUpdateでは、まだ物理シミュレーションされていません。
        if (!m_hasCheckedFirstStep) { m_hasCheckedFirstStep = true; return; }
        if (!m_isDirectionalJump || m_hasReportedSpeedLoss || m_elapsedTime > DIAGNOSTIC_DURATION) return;
        Vector3 expected = m_diagnosticLaunchVelocity + Physics.gravity * (m_elapsedTime - deltaTime);
        Vector3 actual = Owner.Monitor.CurrentVelocity;
        if (Vector3.Dot(actual, expected.normalized) >= expected.magnitude * DIAGNOSTIC_SPEED_RATIO) return;
        m_hasReportedSpeedLoss = true;
        Debug.LogWarning($"[RailJump] SpeedLoss elapsed={m_elapsedTime:F3}, expected={expected:F3}, actual={actual:F3}, {Owner.Motor.RailJumpPhysicsDetails}", Owner);
    }
#endif

    /// <summary>レール上の放物線を進め、障害物や終端では通常の空中移動へ切り替えます。</summary>
    /// <param name="deltaTime">物理更新時間。</param>
    private void UpdateRailJump(float deltaTime)
    {
        m_upSpeed += Physics.gravity.y * deltaTime;
        m_jumpHeight = Mathf.Max(0.0f, m_jumpHeight + m_upSpeed * deltaTime);

        if (!m_path.TryAdvance(deltaTime, m_jumpHeight, out Vector3 targetPosition, out Vector3 forward))
        {
            Owner.GrindController.BlockRailUntilSeparated(m_sourceRail);
            Owner.Motor.EndRailMotion(Owner.Motor.RailVelocity);
            m_isFollowingRail = false;
            return;
        }

        if (!Owner.Motor.TryMoveAlongRail(targetPosition, forward, deltaTime, out _))
        {
            Owner.GrindController.BlockRailUntilSeparated(m_sourceRail);
            Owner.Motor.EndRailMotion(Owner.Motor.RailVelocity);
            m_isFollowingRail = false;
            return;
        }

        if (m_upSpeed <= 0.0f && m_jumpHeight <= 0.0f)
        {
            // 今回の物理移動後、次のFixedUpdateで同じ位置・方向・速度から再開します。
            m_isTransitionPending = true;
            Owner.RequestRailStateChange<PlayerGrindingState>(
                m_sourceRail, m_path.PositionT, m_path.Direction, m_path.Speed);
        }
    }

    /// <summary>被弾による中断時も、ジャンプ演出と溜まった入力を終了します。</summary>
    protected override void OnExitState()
    {
        if (m_isFollowingRail) Owner.Motor.EndRailMotion(Owner.Motor.RailVelocity);
        Owner.AnimationPresenter.StopJumpAnimation();
        Owner.InputReader.ConsumeAttackInput();
        Owner.InputReader.ConsumeJumpPress();
        Owner.InputReader.SuppressJumpUntilRelease();
    }
}
