using UnityEngine;

/// <summary>
/// レール軌道上のジャンプと、入力方向への飛び降りを管理します。
/// </summary>
public sealed class PlayerRailJumpingState : StateBase<PlayerStateMachineComponent>
{
    // 攻撃開始可能な最低速度
    private const float ATTACK_SPEED_EPSILON = 0.01f;

    // ダッシュ終了判定に使用する距離の許容値
    private const float DASH_DISTANCE_EPSILON = 0.001f;

    // 衝突によるダッシュ停止判定に使用する速度比率
    private const float DASH_BLOCKED_SPEED_RATIO = 0.5f;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // 発射速度診断を行う時間
    private const float DIAGNOSTIC_DURATION = 0.2f;

    // 発射速度低下と判定する速度比率
    private const float DIAGNOSTIC_SPEED_RATIO = 0.5f;
#endif

    // ジャンプ入力時点の移動入力
    private readonly Vector2 m_moveInput;

    // ジャンプ元のレール
    private SplineRailInfo m_sourceRail;

    // レール沿いジャンプ経路
    private PlayerRailJumpPath m_path;

    // レールジャンプ時の上方向速度
    private float m_upSpeed;

    // レールからの高さ
    private float m_jumpHeight;

    // State開始からの経過時間
    private float m_elapsedTime;

    // 着地・再搭乗判定を開始するまでの待機時間
    private float m_landingDelay;

    // レール軌道を追従しているか
    private bool m_isFollowingRail;

    // ジャンプ元レールから一度離れたか
    private bool m_hasLeftSourceRail;

    // State遷移要求済みか
    private bool m_isTransitionPending;

    // 空中攻撃
    private PlayerContinuousAttack m_airAttack;

    // 空中攻撃中に使用する音声ActionID
    private PlayerAudioActionID m_attackAudioActionID =
        PlayerAudioActionID.NONE;

    // 同じ長押し入力による空中攻撃の再開始を防止する
    private bool m_hasStartedAttackWhileHeld;

    // 方向入力ジャンプ中のダッシュ状態
    private bool m_isDashing;

    // ダッシュ開始後に1回以上物理更新されたか
    private bool m_hasSimulatedDash;

    // 前回物理更新時の位置
    private Vector3 m_dashPreviousPosition;

    // ダッシュ開始時の水平速度
    private Vector3 m_dashHorizontalVelocity;

    // 現在Motorへ指示しているダッシュ速度
    private Vector3 m_dashCommandedVelocity;

    // ダッシュ残り距離
    private float m_dashRemainingDistance;

    // ダッシュ終了時の速度倍率
    private float m_dashEndSpeedRate;

    // ダッシュ継続可能時間
    private float m_dashTimeLimit;

    // ダッシュ経過時間
    private float m_dashElapsedTime;

    private EffectHandle m_hitEffectHandle;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // 発射時の速度
    private Vector3 m_diagnosticLaunchVelocity;

    // 発射後最初の物理更新を確認したか
    private bool m_hasCheckedFirstStep;

    // 速度低下警告を出力済みか
    private bool m_hasReportedSpeedLoss;

    // 方向入力ジャンプか
    private bool m_isDirectionalJump;
#endif

    /// <summary>
    /// ジャンプ入力時点の方向入力を保持します。
    /// </summary>
    /// <param name="moveInput">ジャンプ入力時点の方向入力。</param>
    public PlayerRailJumpingState(Vector2 moveInput)
    {
        m_moveInput = moveInput;
    }

    /// <summary>
    /// 入力に応じた初速を設定し、ジャンプを開始します。
    /// </summary>
    protected override void OnStartState()
    {
        PlayerRailJumpParameters parameters = Owner.RailJumpParameters;
        SplineGrindController grindController = Owner.GrindController;

        PlayerAudioActionUtility.PlaySequential(
            Owner,
            PlayerAudioActionID.RAIL_JUMPING);

        InitializeRailJump(grindController, parameters);

        Vector3 launchVelocity = HasDirectionalInput(parameters)
            ? StartDirectionalJump(grindController, parameters)
            : StartRailFollowingJump(parameters);

        ApplyLaunchVelocity(launchVelocity);
        InitializeLaunchDiagnostics(parameters, launchVelocity);

        if (m_isFollowingRail)
        {
            Owner.Motor.BeginRailMotion();
        }
    }

    /// <summary>
    /// 攻撃入力の解除状態を更新します。
    /// </summary>
    /// <param name="deltaTime">フレーム経過時間。</param>
    protected override void OnUpdate(float deltaTime)
    {
        if (Owner.InputReader.IsAttackHeld)
        {
            return;
        }

        m_hasStartedAttackWhileHeld = false;
        StopAirAttack(
            resumeRailJumpAudio: true);
    }

    /// <summary>
    /// レールジャンプ、ダッシュ、着地、レール再搭乗を更新します。
    /// </summary>
    protected override void OnFixedUpdate()
    {
        if (m_isTransitionPending)
        {
            return;
        }

        float deltaTime = Time.fixedDeltaTime;

        m_elapsedTime += deltaTime;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        DiagnoseLaunchVelocity(deltaTime);
#endif

        UpdateSourceRailSeparation();
        UpdateAttackInput();

        bool hasLanded = HasLanded();

        if (m_airAttack != null &&
            hasLanded &&
            Owner.Motor.VerticalVelocity <= 0.0f)
        {
            StopAirAttack(
                resumeRailJumpAudio: false);
        }

        TryStartAirAttack();

        if (m_airAttack != null &&
            !m_airAttack.UpdateAttack(deltaTime))
        {
            StopAirAttack(
                resumeRailJumpAudio: true);
        }

        if (Owner.AttackController.IsHittingAnyTarget())
        {
            if (!m_hitEffectHandle.IsValid)
                m_hitEffectHandle = Owner.EffectEmitter.Play(EffectId.PLAYER_ATTACK_NORMAL_L);
        }
        else
        {
            if (m_hitEffectHandle.IsValid)
                m_hitEffectHandle.Stop();
        }

        if (m_isFollowingRail)
        {
            UpdateRailJump(deltaTime);
            return;
        }

        if (m_isDashing &&
            UpdateDirectionalDash(deltaTime))
        {
            return;
        }

        if (!CanCheckLanding())
        {
            return;
        }

        if (TryReturnToRail())
        {
            return;
        }

        if (hasLanded)
        {
            TransitionToGroundState();
        }
    }

    /// <summary>
    /// State終了時にジャンプ中の状態を解除します。
    /// </summary>
    protected override void OnExitState()
    {
        StopAirAttack(
            resumeRailJumpAudio: false);

        PlayerAudioActionUtility.StopSequential(
            Owner,
            PlayerAudioActionID.RAIL_JUMPING);

        // Rail Collision IgnoreはGuardが分離確認後に復元する。

        if (m_isFollowingRail)
        {
            Owner.Motor.EndRailMotion(Owner.Motor.RailVelocity);
        }

        Owner.AnimationPresenter.StopJumpAnimation();

        Owner.InputReader.ConsumeAttackInput();
        Owner.InputReader.ConsumeJumpPress();
        Owner.InputReader.SuppressJumpUntilRelease();
    }

    /// <summary>
    /// レールジャンプ開始時の共通状態を初期化します。
    /// </summary>
    /// <param name="grindController">レール移動制御。</param>
    /// <param name="parameters">レールジャンプパラメータ。</param>
    private void InitializeRailJump(
        SplineGrindController grindController,
        PlayerRailJumpParameters parameters)
    {
        m_sourceRail = grindController.CurrentRail;

        m_path = new PlayerRailJumpPath(
            m_sourceRail,
            grindController.CurrentSpeed,
            grindController.CurrentPositionT,
            grindController.Direction,
            grindController.RideHeight);

        // 離脱と発射の間に物理更新の空白を発生させない
        grindController.StopGrind();

        m_landingDelay = parameters.LandingDelay;

        Owner.InputReader.SuppressJumpUntilRelease();
        Owner.InputReader.ConsumeAttackInput();

        Owner.AnimationPresenter.PlayJumpAnimation(
            PlayerJumpAnimationType.RAIL);
    }

    /// <summary>
    /// 方向入力を伴うジャンプか判定します。
    /// </summary>
    /// <param name="parameters">レールジャンプパラメータ。</param>
    /// <returns>方向入力を伴う場合はtrue。</returns>
    private bool HasDirectionalInput(PlayerRailJumpParameters parameters)
    {
        float deadZone = parameters.DirectionDeadZone;

        return m_moveInput.sqrMagnitude > deadZone * deadZone;
    }

    /// <summary>
    /// 入力方向へのダッシュジャンプを開始します。
    /// </summary>
    /// <param name="grindController">レール移動制御。</param>
    /// <param name="parameters">レールジャンプパラメータ。</param>
    /// <returns>発射速度。</returns>
    private Vector3 StartDirectionalJump(
        SplineGrindController grindController,
        PlayerRailJumpParameters parameters)
    {
        grindController.BlockRailUntilSeparated(m_sourceRail);

        // StopGrind()で既にRelease要求済みなので、
        // 方向入力ジャンプでは最低Ignore時間だけ延長する。
        Owner.Motor.RequestRailCollisionRelease(
            parameters.RailCollisionIgnoreDuration);

        Vector3 direction =
            Owner.Motor.CalculateCameraRelativeDirection(m_moveInput);

        Vector3 launchVelocity =
            parameters.CalculateLaunchVelocity(direction);

        Vector3 horizontalVelocity =
            Vector3.ProjectOnPlane(launchVelocity, Vector3.up);

        m_isDashing = true;
        m_dashPreviousPosition = Owner.Motor.Position;
        m_dashHorizontalVelocity = horizontalVelocity;
        m_dashCommandedVelocity = horizontalVelocity;
        m_dashRemainingDistance = parameters.DashDistance;
        m_dashEndSpeedRate = parameters.DashEndSpeedRate;

        float dashSpeed =
            Mathf.Max(
                DASH_DISTANCE_EPSILON,
                horizontalVelocity.magnitude);

        // 衝突などで距離を稼げない場合でもダッシュを終了できるようにする
        m_dashTimeLimit =
            parameters.DashDistance / dashSpeed +
            Time.fixedDeltaTime * 2.0f;

        return launchVelocity;
    }

    /// <summary>
    /// レール軌道を追従するジャンプを開始します。
    /// </summary>
    /// <param name="parameters">レールジャンプパラメータ。</param>
    /// <returns>発射速度。</returns>
    private Vector3 StartRailFollowingJump(
        PlayerRailJumpParameters parameters)
    {
        m_upSpeed = parameters.OnRailUpSpeed;
        m_isFollowingRail = m_path.CanFollow();

        return Owner.Monitor.CurrentVelocity +
               Vector3.up * m_upSpeed;
    }

    /// <summary>
    /// ジャンプ初速をMotorへ適用します。
    /// </summary>
    /// <param name="launchVelocity">発射速度。</param>
    private void ApplyLaunchVelocity(Vector3 launchVelocity)
    {
        Owner.Motor.SetWorldVelocity(launchVelocity);
        Owner.Motor.AlignFacingToDirection(launchVelocity);
    }

    /// <summary>
    /// ジャンプ元レールから離れたかを更新します。
    /// </summary>
    private void UpdateSourceRailSeparation()
    {
        if (!Owner.Monitor.IsRailed ||
            Owner.Monitor.HitRailInfo != m_sourceRail)
        {
            m_hasLeftSourceRail = true;
        }
    }

    /// <summary>
    /// 空中攻撃入力状態を更新します。
    /// </summary>
    private void UpdateAttackInput()
    {
        Owner.InputReader.ConsumeAttackInput();

        if (!Owner.InputReader.IsAttackHeld)
        {
            m_hasStartedAttackWhileHeld = false;
        }
    }

    /// <summary>
    /// 空中攻撃を開始可能な場合に開始します。
    /// </summary>
    private void TryStartAirAttack()
    {
        if (m_hasStartedAttackWhileHeld ||
            m_airAttack != null ||
            Owner.Monitor.IsGrounded ||
            !Owner.InputReader.IsAttackHeld)
        {
            return;
        }

        ExitControlledJumpMotion();

        Owner.GrindController.BlockRailUntilSeparated(m_sourceRail);
        Owner.AnimationPresenter.StopJumpAnimation();

        m_hasStartedAttackWhileHeld = true;

        // レールジャンプ音から攻撃音へ切り替えるため、
        // レールジャンプ側の終了音は鳴らしません。
        PlayerAudioActionUtility.StopSequential(
            Owner,
            PlayerAudioActionID.RAIL_JUMPING,
            true);

        bool isBoostAttack =
            Owner.IsBoostSuspended &&
            Owner.SuspendedBoostGaugeRate > 0.0f;

        m_attackAudioActionID =
            PlayerAudioActionUtility.GetAttackActionID(
                isBoostAttack);

        PlayerAudioActionUtility.PlaySequential(
            Owner,
            m_attackAudioActionID);

        m_airAttack =
            new PlayerContinuousAttack(
                Owner,
                true);

        m_airAttack.StartAttack();
    }

    /// <summary>
    /// レール追従またはダッシュによる制御移動を終了します。
    /// </summary>
    private void ExitControlledJumpMotion()
    {
        if (m_isFollowingRail)
        {
            Owner.Motor.EndRailMotion(
                Owner.Motor.RailVelocity);

            m_isFollowingRail = false;
        }

        m_isDashing = false;
    }

    /// <summary>
    /// 接地しているか判定します。
    /// </summary>
    /// <returns>着地状態の場合はtrue。</returns>
    private bool HasLanded()
    {
        if (!Owner.Monitor.IsGrounded)
        {
            return false;
        }

        return m_airAttack == null ||
               Owner.Monitor.HasGroundSupport();
    }

    /// <summary>
    /// 着地および再搭乗判定を開始可能か判定します。
    /// </summary>
    /// <returns>判定可能な場合はtrue。</returns>
    private bool CanCheckLanding()
    {
        if (m_elapsedTime < m_landingDelay)
        {
            return false;
        }

        return Owner.Motor.VerticalVelocity <= 0.0f;
    }

    /// <summary>
    /// レールへ再搭乗可能な場合にGrindingStateへ遷移します。
    /// </summary>
    /// <returns>遷移要求を行った場合はtrue。</returns>
    private bool TryReturnToRail()
    {
        if (m_airAttack != null)
        {
            return false;
        }

        SplineRailInfo hitRail =
            Owner.Monitor.IsRailed
                ? Owner.Monitor.HitRailInfo
                : null;

        bool canReturnFromSourceRail =
            hitRail != m_sourceRail ||
            m_hasLeftSourceRail;

        if (!canReturnFromSourceRail ||
            !Owner.GrindController.CanStartGrind(hitRail))
        {
            return false;
        }

        StopAirAttack(
            resumeRailJumpAudio: false);

        m_isTransitionPending = true;

        Owner.RequestRailStateChange<PlayerGrindingState>(
            hitRail);

        return true;
    }

    /// <summary>
    /// 接地時の移動状態へ遷移します。
    /// </summary>
    private void TransitionToGroundState()
    {
        StopAirAttack(
            resumeRailJumpAudio: false);

        m_isTransitionPending = true;

        // 攻撃入力を着地後へ持ち越さない
        Owner.InputReader.SuppressAttackUntilRelease();

        if (Owner.Motor.IsHorizontallyStopped)
        {
            Owner.RequestRailStateChange<PlayerIdlingState>();
            return;
        }

        Owner.RequestRailStateChange<PlayerWalkingState>();
    }

    /// <summary>
    /// 実際の水平移動距離を基準にダッシュを更新します。
    /// </summary>
    /// <param name="deltaTime">物理更新間隔。</param>
    /// <returns>ダッシュを継続する場合はtrue。</returns>
    private bool UpdateDirectionalDash(float deltaTime)
    {
        Vector3 currentPosition = Owner.Motor.Position;
        Vector3 currentVelocity = Owner.Monitor.CurrentVelocity;

        if (m_hasSimulatedDash)
        {
            UpdateDashTravelDistance(
                currentPosition,
                deltaTime);

            if (m_dashRemainingDistance <=
                DASH_DISTANCE_EPSILON)
            {
                FinishDirectionalDash(
                    currentVelocity,
                    true);

                return false;
            }

            if (ShouldStopDirectionalDash(currentVelocity))
            {
                FinishDirectionalDash(
                    currentVelocity,
                    false);

                return false;
            }
        }

        m_hasSimulatedDash = true;
        m_dashPreviousPosition = currentPosition;

        Vector3 horizontalVelocity =
            Vector3.ProjectOnPlane(
                currentVelocity,
                Vector3.up);

        float maxSpeed =
            m_dashRemainingDistance / deltaTime;

        m_dashCommandedVelocity =
            Vector3.ClampMagnitude(
                horizontalVelocity,
                maxSpeed);

        Owner.Motor.SetWorldVelocity(
            m_dashCommandedVelocity +
            Vector3.up * currentVelocity.y);

        return true;
    }

    /// <summary>
    /// ダッシュで実際に移動した距離を残り距離から減算します。
    /// </summary>
    /// <param name="currentPosition">現在位置。</param>
    /// <param name="deltaTime">物理更新間隔。</param>
    private void UpdateDashTravelDistance(
        Vector3 currentPosition,
        float deltaTime)
    {
        m_dashElapsedTime += deltaTime;

        Vector3 displacement =
            Vector3.ProjectOnPlane(
                currentPosition - m_dashPreviousPosition,
                Vector3.up);

        m_dashRemainingDistance -=
            displacement.magnitude;
    }

    /// <summary>
    /// ダッシュを中断すべきか判定します。
    /// </summary>
    /// <param name="currentVelocity">現在速度。</param>
    /// <returns>中断する場合はtrue。</returns>
    private bool ShouldStopDirectionalDash(
        Vector3 currentVelocity)
    {
        float horizontalSpeed =
            m_dashHorizontalVelocity.magnitude;

        if (horizontalSpeed <= DASH_DISTANCE_EPSILON)
        {
            return true;
        }

        Vector3 dashDirection =
            m_dashHorizontalVelocity / horizontalSpeed;

        float forwardSpeed =
            Vector3.Dot(
                currentVelocity,
                dashDirection);

        bool isBlocked =
            forwardSpeed <
            m_dashCommandedVelocity.magnitude *
            DASH_BLOCKED_SPEED_RATIO;

        bool hasTimedOut =
            m_dashElapsedTime >= m_dashTimeLimit;

        bool hasLanded =
            m_elapsedTime >= m_landingDelay &&
            Owner.Monitor.IsGrounded &&
            currentVelocity.y <= 0.0f;

        return isBlocked ||
               hasTimedOut ||
               hasLanded;
    }

    /// <summary>
    /// 方向入力ジャンプのダッシュを終了します。
    /// </summary>
    /// <param name="velocity">現在速度。</param>
    /// <param name="completed">
    /// 指定距離まで正常に到達した場合はtrue。
    /// </param>
    private void FinishDirectionalDash(
        Vector3 velocity,
        bool completed)
    {
        m_isDashing = false;

        // 衝突終了時は物理演算によって得られた速度を維持する
        if (!completed)
        {
            return;
        }

        Vector3 horizontalVelocity =
            m_dashHorizontalVelocity *
            m_dashEndSpeedRate;

        float verticalVelocity =
            Mathf.Min(0.0f, velocity.y);

        Owner.Motor.SetWorldVelocity(
            horizontalVelocity +
            Vector3.up * verticalVelocity);
    }

    /// <summary>
    /// レール上の放物線ジャンプを更新します。
    /// </summary>
    /// <param name="deltaTime">物理更新間隔。</param>
    private void UpdateRailJump(float deltaTime)
    {
        m_upSpeed += Physics.gravity.y * deltaTime;

        m_jumpHeight =
            Mathf.Max(
                0.0f,
                m_jumpHeight + m_upSpeed * deltaTime);

        if (!m_path.TryAdvance(
                deltaTime,
                m_jumpHeight,
                out Vector3 targetPosition,
                out Vector3 forward))
        {
            ExitRailFollowingJump();
            return;
        }

        if (!Owner.Motor.TryMoveAlongRail(
                targetPosition,
                forward,
                deltaTime,
                m_sourceRail.ForcesEscape,
                out _))
        {
            ExitRailFollowingJump();
            return;
        }

        if (m_upSpeed > 0.0f ||
            m_jumpHeight > 0.0f)
        {
            return;
        }

        m_isTransitionPending = true;

        Owner.RequestRailStateChange<PlayerGrindingState>(
            m_sourceRail,
            m_path.PositionT,
            m_path.Direction,
            m_path.Speed);
    }

    /// <summary>
    /// レール追従ジャンプを終了し、通常の空中物理へ移行します。
    /// </summary>
    private void ExitRailFollowingJump()
    {
        Owner.GrindController.BlockRailUntilSeparated(
            m_sourceRail);

        Owner.Motor.EndRailMotion(
            Owner.Motor.RailVelocity);

        m_isFollowingRail = false;
    }

    /// <summary>
    /// 空中攻撃を終了します。
    /// </summary>
    private void StopAirAttack(
        bool resumeRailJumpAudio)
    {
        if (m_airAttack == null)
        {
            return;
        }

        if (m_hitEffectHandle.IsValid)
        {
            m_hitEffectHandle.Stop();
        }

        m_airAttack.StopAttack();
        m_airAttack = null;

        if (m_attackAudioActionID !=
            PlayerAudioActionID.NONE)
        {
            PlayerAudioActionUtility.StopSequential(
                Owner,
                m_attackAudioActionID);

            m_attackAudioActionID =
                PlayerAudioActionID.NONE;
        }

        if (resumeRailJumpAudio &&
            !m_isTransitionPending)
        {
            PlayerAudioActionUtility.PlaySequential(
                Owner,
                PlayerAudioActionID.RAIL_JUMPING);
        }

        Owner.AnimationPresenter.PlayJumpAnimation(
            PlayerJumpAnimationType.RAIL);
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD

    /// <summary>
    /// 発射速度診断を初期化します。
    /// </summary>
    /// <param name="parameters">ジャンプパラメータ。</param>
    /// <param name="launchVelocity">発射速度。</param>
    private void InitializeLaunchDiagnostics(
        PlayerRailJumpParameters parameters,
        Vector3 launchVelocity)
    {
        m_isDirectionalJump =
            HasDirectionalInput(parameters);

        m_diagnosticLaunchVelocity =
            launchVelocity;

        Owner.Motor.ResetRailJumpContact();

        if (!m_isDirectionalJump)
        {
            return;
        }

        Debug.Log(
            $"[RailJump] Launch frame={Time.frameCount}, " +
            $"input={m_moveInput}, " +
            $"velocity={launchVelocity:F3}, " +
            $"{Owner.Motor.RailJumpPhysicsDetails}",
            Owner);
    }

    /// <summary>
    /// 発射後に初速が大きく失われた場合に警告します。
    /// </summary>
    /// <param name="deltaTime">物理更新間隔。</param>
    private void DiagnoseLaunchVelocity(float deltaTime)
    {
        if (!m_hasCheckedFirstStep)
        {
            m_hasCheckedFirstStep = true;
            return;
        }

        if (!m_isDirectionalJump ||
            !m_isDashing ||
            m_hasReportedSpeedLoss ||
            m_elapsedTime > DIAGNOSTIC_DURATION)
        {
            return;
        }

        Vector3 expectedVelocity =
            m_dashCommandedVelocity +
            Vector3.up *
            (
                m_diagnosticLaunchVelocity.y +
                Physics.gravity.y *
                (m_elapsedTime - deltaTime)
            );

        Vector3 actualVelocity =
            Owner.Monitor.CurrentVelocity;

        if (expectedVelocity.sqrMagnitude <=
            DASH_DISTANCE_EPSILON * DASH_DISTANCE_EPSILON)
        {
            return;
        }

        float speedAlongExpectedDirection =
            Vector3.Dot(
                actualVelocity,
                expectedVelocity.normalized);

        if (speedAlongExpectedDirection >=
            expectedVelocity.magnitude *
            DIAGNOSTIC_SPEED_RATIO)
        {
            return;
        }

        m_hasReportedSpeedLoss = true;

        Debug.LogWarning(
            $"[RailJump] SpeedLoss " +
            $"elapsed={m_elapsedTime:F3}, " +
            $"expected={expectedVelocity:F3}, " +
            $"actual={actualVelocity:F3}, " +
            $"{Owner.Motor.RailJumpPhysicsDetails}",
            Owner);
    }

#else

    /// <summary>
    /// Development Build以外では診断処理を行いません。
    /// </summary>
    /// <param name="parameters">ジャンプパラメータ。</param>
    /// <param name="launchVelocity">発射速度。</param>
    private void InitializeLaunchDiagnostics(
        PlayerRailJumpParameters parameters,
        Vector3 launchVelocity)
    {
    }

#endif
}
