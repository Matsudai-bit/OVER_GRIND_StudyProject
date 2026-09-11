using UnityEngine;

/// <summary>
/// プレイヤーのブーストチャージ状態を管理します。
///
/// チャージ中は現在の移動方向を維持しながら、
/// スティック入力によって少しずつ移動方向を変更できます。
///
/// チャージ開始時に移動していたか停止していたかによって、
/// チャージ時間とステアリングの挙動を変更します。
/// </summary>
public sealed class PlayerBoostChargingState
    : StateBase<PlayerStateMachineComponent>
{
    // 停止状態と判定する速度のしきい値
    private const float STATIONARY_SPEED_THRESHOLD = 0.01f;

    // 状態

    // 使用するパラメータアセット
    private PlayerBoostChargingParameterAsset m_parameterAsset;

    // チャージ経過時間
    private float m_chargeTime;

    // 速度ログの経過時間
    private float m_speedLogElapsedTime;

    // 移動入力が無い状態が続いている時間
    private float m_noMoveInputElapsedTime;

    // チャージ中に使用する移動速度パラメータ
    private PlayerMoveParameters m_moveParameters;

    // 現在のチャージ中の移動方向
    private Vector3 m_currentVelocityDirection;

    // 現在のプレイヤーの向き
    private Vector3 m_currentFacingDirection;

    // 停止中にチャージを開始したか
    private bool m_startedFromStationary;

    // 現在使用している最大チャージ時間
    private float m_currentMaxChargeTime;


    /// <summary>
    /// 現在のチャージ割合を取得します。
    /// </summary>
    private float ChargeRate =>
        Mathf.Clamp01(
            m_chargeTime /
            m_currentMaxChargeTime);


    /// <summary>
    /// 状態開始時に呼ばれます。
    /// </summary>
    protected override void OnStartState()
    {
        Owner.InputReader.ConsumeVBoostStarted();

        m_parameterAsset =
            Owner.BoostChargingParameterAsset;

        m_chargeTime = 0.0f;
        m_speedLogElapsedTime = 0.0f;
        m_noMoveInputElapsedTime = 0.0f;

        PlayerMoveParameters normalParameters =
            Owner.MovementParameterAsset
                .CreateMoveParameters();

        float currentSpeedAtChargeStart =
            Owner.Motor.HorizontalSpeed;

        // チャージ開始時に停止していたか判定
        m_startedFromStationary =
            currentSpeedAtChargeStart <=
            STATIONARY_SPEED_THRESHOLD;

        // 開始状態に応じてチャージ時間を選択
        m_currentMaxChargeTime =
            m_startedFromStationary
                ? m_parameterAsset.StationaryStartChargeTime
                : m_parameterAsset.MovingStartChargeTime;

        // 停止中から開始した場合は通常の最大速度を基準にする
        float chargeBaseSpeed =
            m_startedFromStationary
                ? normalParameters.MaxMoveSpeed
                : currentSpeedAtChargeStart;

        float chargeMoveSpeed =
            chargeBaseSpeed *
            m_parameterAsset.ChargeMoveSpeedRate;

        m_moveParameters =
            new PlayerMoveParameters(
                chargeMoveSpeed,
                normalParameters.TimeToMaxSpeed,
                normalParameters.TimeToStop,
                m_parameterAsset.FacingRotationSpeed);


        // --------------------------------------------------------
        // チャージ開始時の移動方向を取得
        // --------------------------------------------------------

        m_currentVelocityDirection =
            Owner.Motor.HorizontalDirection;

        m_currentVelocityDirection.y = 0.0f;

        if (m_currentVelocityDirection.sqrMagnitude <= 0.0001f)
        {
            m_currentVelocityDirection =
                Owner.transform.forward;

            m_currentVelocityDirection.y = 0.0f;
        }

        m_currentVelocityDirection.Normalize();


        // --------------------------------------------------------
        // プレイヤーの向きを初期化
        // --------------------------------------------------------

        m_currentFacingDirection =
            m_currentVelocityDirection;


        Debug.Log(
            $"[PlayerBoostChargingState] チャージ開始 " +
            $"開始時実速度={currentSpeedAtChargeStart:F2} " +
            $"停止開始={m_startedFromStationary} " +
            $"最大チャージ時間={m_currentMaxChargeTime:F2}秒 " +
            $"チャージ速度={m_moveParameters.MaxMoveSpeed:F2}",
            Owner);


        if (Owner.VGaugeUI != null)
        {
            Owner.VGaugeUI.SetGaugeRate(0.0f);
            Owner.VGaugeUI.SetCharging(true);
        }

        if (Owner.PlayerCamera != null)
        {
            Owner.PlayerCamera.BeginDriftLookOverride();
        }

        Owner.AnimationPresenter.PlayWalkAnimation();
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
            // チャージを中断するため、保持しているゲージを破棄する
            Owner.CarriedBoostGaugeRate = 0.0f;
            Owner.SuspendedBoostGaugeRate = 0.0f;

            // UIのチャージゲージも0にする
            if (Owner.VGaugeUI != null)
            {
                Owner.VGaugeUI.SetGaugeRate(0.0f);
                Owner.VGaugeUI.SetCharging(false);
            }

            Debug.Log(
                "[PlayerBoostChargingState] " +
                "ジャンプによりチャージを中断しました。チャージゲージを0%にします。",
                Owner);

            Machine.ChangeState<PlayerJumpingState>();
            return;
        }


        if (Owner.InputReader.ConsumeVBoostReleased())
        {
            Debug.Log(
                $"[PlayerBoostChargingState] " +
                $"チャージ解除 " +
                $"経過時間={m_chargeTime:F2}秒 " +
                $"チャージ率={ChargeRate:P1} " +
                $"実速度={Owner.Motor.HorizontalSpeed:F2}",
                Owner);

            ReleaseCharge();
            return;
        }


        // --------------------------------------------------------
        // 移動入力チェック
        // --------------------------------------------------------

        // 歩き開始のチャージ中に移動入力がなくなった場合、
        // 立ちチャージへ移行してチャージを継続する
        if (!m_startedFromStationary)
        {
            if (Owner.InputReader.HasMoveInput)
            {
                m_noMoveInputElapsedTime = 0.0f;
            }
            else
            {
                m_noMoveInputElapsedTime +=
                    Time.fixedDeltaTime;

                if (m_noMoveInputElapsedTime >=
                    m_parameterAsset.NoMoveInputGraceTime)
                {
                    // 立ちチャージへ移行するが、
                    // m_chargeTimeはリセットせず現在のチャージ率を維持する
                    m_startedFromStationary = true;

                    Debug.Log(
                        $"[PlayerBoostChargingState] " +
                        $"移動入力がなくなったため立ちチャージへ移行します。 " +
                        $"現在のチャージ率={ChargeRate:P1}",
                        Owner);
                }
            }
        }


        // --------------------------------------------------------
        // チャージ時間更新
        // --------------------------------------------------------

        m_chargeTime += Time.fixedDeltaTime;

        if (m_chargeTime >= m_currentMaxChargeTime)
        {
            m_chargeTime =
                m_currentMaxChargeTime;
        }


        // --------------------------------------------------------
        // 入力方向取得
        // --------------------------------------------------------

        Vector2 normalizedInput =
            Vector2.ClampMagnitude(
                Owner.InputReader.MoveInput,
                1.0f);

        Vector3 inputDirection =
            Owner.Motor.CalculateCameraRelativeDirection(
                normalizedInput);


        // --------------------------------------------------------
        // チャージ率に応じた、現在の曲がりやすさを計算
        // --------------------------------------------------------

        float currentDriftTurnSpeed =
            Mathf.Lerp(
                m_parameterAsset.DriftTurnSpeedAtChargeStart,
                m_parameterAsset.DriftTurnSpeedAtFullCharge,
                ChargeRate);


        // --------------------------------------------------------
        // チャージ中の移動方向を更新
        // --------------------------------------------------------

        // 停止中開始の場合はチャージ開始時の方向を維持する
        if (!m_startedFromStationary)
        {
            UpdateDriftVelocityDirection(
                inputDirection,
                currentDriftTurnSpeed);

            UpdateFacingDirection();
        }


        // --------------------------------------------------------
        // チャージ中の移動
        // --------------------------------------------------------

        if (!m_startedFromStationary)
        {
            Owner.Motor.MoveWithDriftAtFixedSpeed(
                m_currentVelocityDirection,
                m_moveParameters.MaxMoveSpeed,
                m_currentFacingDirection,
                m_parameterAsset.FacingRotationSpeed,
                Time.fixedDeltaTime);
        }


        // --------------------------------------------------------
        // カメラを、開始時の向きとプレイヤーの向きの中間へ追従
        // --------------------------------------------------------

        if (Owner.PlayerCamera != null)
        {
            Owner.PlayerCamera.UpdateDriftLookDirection(
                m_currentFacingDirection,
                m_parameterAsset.CameraDriftLookBlendRate,
                m_parameterAsset.CameraDriftLookTurnSpeed,
                Time.fixedDeltaTime);
        }


        // --------------------------------------------------------
        // ゲージ更新
        // --------------------------------------------------------

        if (Owner.VGaugeUI != null)
        {
            Owner.VGaugeUI.SetGaugeRate(ChargeRate);
        }


        // --------------------------------------------------------
        // デバッグログ
        // --------------------------------------------------------

        m_speedLogElapsedTime +=
            Time.fixedDeltaTime;

        if (m_speedLogElapsedTime >=
            m_parameterAsset.SpeedLogInterval)
        {
            m_speedLogElapsedTime = 0.0f;

            Debug.Log(
                $"[PlayerBoostChargingState] " +
                $"チャージ={ChargeRate:P1} " +
                $"実速度={Owner.Motor.HorizontalSpeed:F2} " +
                $"固定速度={m_moveParameters.MaxMoveSpeed:F2} " +
                $"曲がりやすさ={currentDriftTurnSpeed:F1}deg/s",
                Owner);
        }
    }


    /// <summary>
    /// チャージ中の移動方向を更新します。
    ///
    /// スティック入力に対して移動方向を徐々に追従させます。
    /// turnSpeedDegreesPerSecondが小さいほど曲がりにくく、
    /// 大きいほど曲がりやすくなります。
    /// </summary>
    /// <param name="inputDirection">カメラ基準の入力方向。</param>
    /// <param name="turnSpeedDegreesPerSecond">
    /// 1秒間の最大方向転換角度（曲がりやすさ）。
    /// </param>
    private void UpdateDriftVelocityDirection(
        Vector3 inputDirection,
        float turnSpeedDegreesPerSecond)
    {
        inputDirection.y = 0.0f;

        float deadZone =
            m_parameterAsset.SteeringDeadZone;

        if (inputDirection.sqrMagnitude <=
            deadZone * deadZone)
        {
            return;
        }

        inputDirection.Normalize();


        // --------------------------------------------------------
        // 入力方向へ徐々に移動方向を変更
        // --------------------------------------------------------

        float maxRadiansDelta =
            turnSpeedDegreesPerSecond *
            Mathf.Deg2Rad *
            Time.fixedDeltaTime;

        m_currentVelocityDirection =
            Vector3.RotateTowards(
                m_currentVelocityDirection,
                inputDirection,
                maxRadiansDelta,
                0.0f);

        m_currentVelocityDirection.y = 0.0f;

        if (m_currentVelocityDirection.sqrMagnitude >
            0.0001f)
        {
            m_currentVelocityDirection.Normalize();
        }
    }


    /// <summary>
    /// プレイヤーの向きを現在の移動方向へ徐々に変更します。
    /// </summary>
    private void UpdateFacingDirection()
    {
        float maxRadiansDelta =
            m_parameterAsset.FacingRotationSpeed *
            Mathf.Deg2Rad *
            Time.fixedDeltaTime;

        m_currentFacingDirection =
            Vector3.RotateTowards(
                m_currentFacingDirection,
                m_currentVelocityDirection,
                maxRadiansDelta,
                0.0f);

        m_currentFacingDirection.y = 0.0f;

        if (m_currentFacingDirection.sqrMagnitude >
            0.0001f)
        {
            m_currentFacingDirection.Normalize();
        }
    }


    /// <summary>
    /// 状態終了時に呼ばれます。
    /// </summary>
    protected override void OnExitState()
    {
        Debug.Log(
            "[PlayerBoostChargingState] チャージ状態終了",
            Owner);

        if (Owner.PlayerCamera != null)
        {
            Owner.PlayerCamera.EndDriftLookOverride();
        }

        Owner.AnimationPresenter.StopWalkAnimation();
    }


    /// <summary>
    /// チャージを解除したときの遷移を行います。
    /// </summary>
    private void ReleaseCharge()
    {
        if (ChargeRate >=
            m_parameterAsset.MinBoostChargeRate)
        {
            // ----------------------------------------------------
            // チャージ終了時のプレイヤーの向きを保存
            // ----------------------------------------------------

            Owner.BoostDashDirection =
                m_currentFacingDirection;

            Debug.Log(
                $"[PlayerBoostChargingState] " +
                $"チャージ率{ChargeRate:P1} → Vブーストへ遷移 " +
                $"ダッシュ方向={m_currentFacingDirection}",
                Owner);

            Owner.CarriedBoostGaugeRate =
                ChargeRate;

            Machine.ChangeState<PlayerVRunningState>();
            return;
        }


        Debug.Log(
            $"[PlayerBoostChargingState] " +
            $"チャージ率{ChargeRate:P1} → 通常歩行へ遷移",
            Owner);


        if (Owner.VGaugeUI != null)
        {
            Owner.VGaugeUI.SetGaugeRate(0.0f);
            Owner.VGaugeUI.SetCharging(false);
        }

        Machine.ChangeState<PlayerWalkingState>();
    }
}