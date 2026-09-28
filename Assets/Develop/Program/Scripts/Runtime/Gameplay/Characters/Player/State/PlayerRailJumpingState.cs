using UnityEngine;

/// <summary>レール軌道上のジャンプと、入力方向への飛び降りを管理します。</summary>
public sealed class PlayerRailJumpingState : StateBase<PlayerStateMachineComponent>
{
    private readonly SplineRailInfo m_sourceRail;
    private readonly Vector2 m_moveInput;
    private readonly Vector3 m_exitVelocity;
    private PlayerRailJumpPath m_path;
    private float m_upSpeed;
    private float m_jumpHeight;
    private float m_elapsedTime;
    private float m_landingDelay;
    private bool m_isFollowingRail;
    private bool m_hasLeftSourceRail;
    private bool m_isTransitionPending;

    /// <summary>ジャンプ入力時点のレール・方向入力・滑走速度を保持します。</summary>
    /// <param name="rail">ジャンプ元のレール。</param>
    /// <param name="moveInput">ジャンプ入力時点の方向入力。</param>
    /// <param name="exitVelocity">グラインド終了時の速度。</param>
    public PlayerRailJumpingState(SplineRailInfo rail, Vector2 moveInput, Vector3 exitVelocity)
    {
        m_sourceRail = rail;
        m_moveInput = moveInput;
        m_exitVelocity = exitVelocity;
    }

    /// <summary>入力に応じた初速を設定し、ジャンプ演出を開始します。</summary>
    protected override void OnStartState()
    {
        PlayerRailJumpParameters parameters = Owner.RailJumpParameters;
        m_landingDelay = parameters.LandingDelay;
        Owner.InputReader.SuppressJumpUntilRelease();
        Owner.InputReader.ConsumeAttackInput();
        Owner.AnimationPresenter.PlayJumpAnimation();

        Vector3 launchVelocity;
        if (m_moveInput.sqrMagnitude > parameters.DirectionDeadZone * parameters.DirectionDeadZone)
        {
            // 地上移動と同じカメラ基準で、発射時に方向を確定します。
            Vector3 direction = Owner.Motor.CalculateCameraRelativeDirection(m_moveInput);
            launchVelocity = parameters.CalculateLaunchVelocity(direction);
        }
        else
        {
            m_upSpeed = parameters.OnRailUpSpeed;
            m_path = new PlayerRailJumpPath(m_sourceRail, m_exitVelocity.magnitude);
            m_isFollowingRail = m_path.TryInitialize(Owner.Motor.Position, Owner.transform.forward);
            launchVelocity = m_exitVelocity + Vector3.up * m_upSpeed;
        }
        Owner.Motor.SetWorldVelocity(launchVelocity);
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
            Machine.ChangeState<PlayerGrindingState>(hitRail);
            return;
        }

        if (Owner.Monitor.IsGrounded)
        {
            m_isTransitionPending = true;
            Machine.ChangeState<PlayerIdlingState>();
        }
    }

    /// <summary>レール上の放物線を進め、障害物や終端では通常の空中移動へ切り替えます。</summary>
    /// <param name="deltaTime">物理更新時間。</param>
    private void UpdateRailJump(float deltaTime)
    {
        m_upSpeed += Physics.gravity.y * deltaTime;
        m_jumpHeight = Mathf.Max(0.0f, m_jumpHeight + m_upSpeed * deltaTime);

        if (!m_path.TryAdvance(deltaTime, m_jumpHeight, out Vector3 targetPosition, out Vector3 forward))
        {
            m_isFollowingRail = false;
            return;
        }

        if (!Owner.Motor.TryMoveRailJump(targetPosition, forward, m_sourceRail, deltaTime))
        {
            m_isFollowingRail = false;
            return;
        }

        if (m_upSpeed <= 0.0f && m_jumpHeight <= 0.0f)
        {
            // FixedUpdate後の物理移動が完了してから、次のUpdateでグラインドを再開します。
            m_isTransitionPending = true;
            Machine.ChangeState<PlayerGrindingState>(m_sourceRail);
        }
    }

    /// <summary>被弾による中断時も、ジャンプ演出と溜まった入力を終了します。</summary>
    protected override void OnExitState()
    {
        Owner.AnimationPresenter.StopJumpAnimation();
        Owner.InputReader.ConsumeAttackInput();
        Owner.InputReader.ConsumeJumpPress();
        Owner.InputReader.SuppressJumpUntilRelease();
    }
}
