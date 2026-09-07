using UnityEngine;

/// <summary>
/// プレイヤーのブーストチャージ状態を管理します。
///
/// チャージ中は現在の移動方向を維持しながら、
/// スティック入力によって少しずつ移動方向を変更できます。
///
/// チャージが進むほど移動方向の曲がりやすさが増していきます
/// （PlayerBoostChargingParameterAssetで調整可能）。
///
/// プレイヤーの向きは、現在の移動方向へ
/// ゆっくり追従します。
///
/// また、カメラはマリオカートのドリフトのように、
/// プレイヤーの見た目の向きの一部を向くようになります。
/// </summary>
public sealed class PlayerBoostChargingState
    : StateBase<PlayerStateMachineComponent>
{
    // ============================================================
    // 状態
    // ============================================================

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


    /// <summary>
    /// 現在のチャージ割合を取得します。
    /// </summary>
    private float ChargeRate =>
        Mathf.Clamp01(
            m_chargeTime / m_parameterAsset.MaxChargeTime);


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

        float chargeMoveSpeed =
            currentSpeedAtChargeStart *
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

        if (Owner.InputReader.HasMoveInput)
        {
            m_noMoveInputElapsedTime = 0.0f;
        }
        else
        {
            m_noMoveInputElapsedTime += Time.fixedDeltaTime;

            if (m_noMoveInputElapsedTime >=
                m_parameterAsset.NoMoveInputGraceTime)
            {
                Debug.Log(
                    "[PlayerBoostChargingState] " +
                    "移動入力がなくなったため待機状態へ遷移します。",
                    Owner);

                Machine.ChangeState<PlayerIdlingState>();
                return;
            }
        }


        // --------------------------------------------------------
        // チャージ時間更新
        // --------------------------------------------------------

        m_chargeTime += Time.fixedDeltaTime;

        if (m_chargeTime >= m_parameterAsset.MaxChargeTime)
        {
            m_chargeTime = m_parameterAsset.MaxChargeTime;
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

        UpdateDriftVelocityDirection(
            inputDirection,
            currentDriftTurnSpeed);


        // --------------------------------------------------------
        // プレイヤーの向きを移動方向へ追従させる
        // --------------------------------------------------------

        UpdateFacingDirection();


        // --------------------------------------------------------
        // チャージ中の移動
        // --------------------------------------------------------

        Owner.Motor.MoveWithDriftAtFixedSpeed(
            m_currentVelocityDirection,
            m_moveParameters.MaxMoveSpeed,
            m_currentFacingDirection,
            m_parameterAsset.FacingRotationSpeed,
            Time.fixedDeltaTime);


        // --------------------------------------------------------
        // カメラを、開始時の向きとプレイヤーの向きの中間へ追従させる
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

        m_speedLogElapsedTime += Time.fixedDeltaTime;

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
    ///
    /// 現在の移動方向より後ろ側への入力は無視します。
    /// これにより、チャージ中に後退することを防ぎます。
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
        // 後ろ方向への入力を禁止
        // --------------------------------------------------------

        float forwardDot =
            Vector3.Dot(
                m_currentVelocityDirection,
                inputDirection);

        // 現在の移動方向より後ろを向いている入力は無視
        if (forwardDot <= 0.0f)
        {
            return;
        }


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
        if (ChargeRate >= m_parameterAsset.MinBoostChargeRate)
        {
            // ----------------------------------------------------
            // チャージ終了時のプレイヤーの向きを保存
            // ----------------------------------------------------
            // この方向がVRunningStateのブーストダッシュ中、
            // 固定のダッシュ方向として使用されます。
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