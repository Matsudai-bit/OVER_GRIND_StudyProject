using UnityEngine;

/// <summary>
/// プレイヤーのブーストチャージ状態を管理します。
/// チャージ中はマリオカートのドリフトのように、
/// キャラクターの向きと実際の進行方向を分離します。
///
/// チャージが進むほど進行方向の旋回性能が低下し、
/// 徐々に強い横滑りが発生します。
/// </summary>
public sealed class PlayerBoostChargingState
    : StateBase<PlayerStateMachineComponent>
{
    // 最大チャージ時間
    private const float MAX_CHARGE_TIME = 4.0f;

    // ブーストダッシュに必要な最低チャージ割合
    private const float MIN_BOOST_CHARGE_RATE = 0.10f;

    // チャージ中の移動速度倍率
    private const float CHARGE_MOVE_SPEED_RATE = 0.75f;


    // ============================================================
    // ドリフト設定
    // ============================================================

    // 軽いドリフトへ移行するチャージ率
    private const float DRIFT_STAGE_LIGHT_END = 0.25f;

    // 中程度のドリフトへ移行するチャージ率
    private const float DRIFT_STAGE_MEDIUM_END = 0.65f;

    // チャージ開始時の進行方向旋回速度
    private const float DRIFT_TURN_SPEED_LIGHT = 120.0f;

    // 中程度のドリフト時の進行方向旋回速度
    private const float DRIFT_TURN_SPEED_MEDIUM = 65.0f;

    // 最大チャージ時の進行方向旋回速度
    private const float DRIFT_TURN_SPEED_DEEP = 25.0f;


    // ============================================================
    // その他
    // ============================================================

    // 速度ログの出力間隔
    private const float SPEED_LOG_INTERVAL = 0.25f;

    // 移動入力が瞬間的に途切れてもIdlingへ遷移しない猶予時間
    private const float NO_MOVE_INPUT_GRACE_TIME = 0.15f;


    // チャージ経過時間
    private float m_chargeTime;

    // 速度ログの経過時間
    private float m_speedLogElapsedTime;

    // 移動入力が無い状態が続いている時間
    private float m_noMoveInputElapsedTime;

    // チャージ開始時点の通常旋回速度
    private float m_normalFacingRotationSpeed;

    // チャージ中に固定して使う移動速度パラメータ
    private PlayerMoveParameters m_moveParameters;

    // 現在の実際の進行方向
    private Vector3 m_currentVelocityDirection;


    /// <summary>
    /// 現在のチャージ割合を取得します。
    /// </summary>
    private float ChargeRate =>
        Mathf.Clamp01(
            m_chargeTime / MAX_CHARGE_TIME);


    /// <summary>
    /// 状態開始時に呼ばれます。
    /// </summary>
    protected override void OnStartState()
    {
        Owner.InputReader.ConsumeVBoostStarted();

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
            CHARGE_MOVE_SPEED_RATE;

        m_normalFacingRotationSpeed =
            normalParameters.RotationSpeed;

        m_moveParameters =
            new PlayerMoveParameters(
                chargeMoveSpeed,
                normalParameters.TimeToMaxSpeed,
                normalParameters.TimeToStop,
                m_normalFacingRotationSpeed);

        // チャージ開始時の実際の進行方向を保存
        m_currentVelocityDirection =
            Owner.Motor.HorizontalDirection;

        Debug.Log(
            $"[PlayerBoostChargingState] チャージ開始 " +
            $"開始時実速度={currentSpeedAtChargeStart:F2} " +
            $"チャージ固定速度={m_moveParameters.MaxMoveSpeed:F2}",
            Owner);

        if (Owner.VGaugeUI != null)
        {
            Owner.VGaugeUI.SetGaugeRate(0.0f);
            Owner.VGaugeUI.SetCharging(true);
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
                NO_MOVE_INPUT_GRACE_TIME)
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

        if (m_chargeTime >= MAX_CHARGE_TIME)
        {
            m_chargeTime = MAX_CHARGE_TIME;
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
        // ドリフト旋回速度を計算
        // --------------------------------------------------------

        float currentVelocityTurnSpeed =
            CalculateDriftTurnSpeed(ChargeRate);


        // --------------------------------------------------------
        // 実際の進行方向を更新
        // --------------------------------------------------------

        UpdateDriftVelocityDirection(
            inputDirection,
            currentVelocityTurnSpeed);


        // --------------------------------------------------------
        // ドリフト移動
        // --------------------------------------------------------

        Owner.Motor.MoveWithDriftAtFixedSpeed(
            m_currentVelocityDirection,
            m_moveParameters.MaxMoveSpeed,
            inputDirection,
            m_normalFacingRotationSpeed,
            Time.fixedDeltaTime);


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
            SPEED_LOG_INTERVAL)
        {
            m_speedLogElapsedTime = 0.0f;

            Debug.Log(
                $"[PlayerBoostChargingState] " +
                $"チャージ={ChargeRate:P1} " +
                $"実速度={Owner.Motor.HorizontalSpeed:F2} " +
                $"固定速度={m_moveParameters.MaxMoveSpeed:F2} " +
                $"進行方向旋回速度={currentVelocityTurnSpeed:F1}deg/s",
                Owner);
        }
    }


    /// <summary>
    /// チャージ率からドリフト中の
    /// 実際の進行方向旋回速度を計算します。
    ///
    /// チャージ開始時は軽く曲がれますが、
    /// チャージが進むほど徐々に曲がりにくくなります。
    /// </summary>
    /// <param name="chargeRate">チャージ割合。</param>
    /// <returns>進行方向の旋回速度。</returns>
    private float CalculateDriftTurnSpeed(
        float chargeRate)
    {
        if (chargeRate <= DRIFT_STAGE_LIGHT_END)
        {
            float stageRate =
                Mathf.InverseLerp(
                    0.0f,
                    DRIFT_STAGE_LIGHT_END,
                    chargeRate);

            return Mathf.Lerp(
                DRIFT_TURN_SPEED_LIGHT,
                DRIFT_TURN_SPEED_MEDIUM,
                stageRate);
        }

        if (chargeRate <= DRIFT_STAGE_MEDIUM_END)
        {
            float stageRate =
                Mathf.InverseLerp(
                    DRIFT_STAGE_LIGHT_END,
                    DRIFT_STAGE_MEDIUM_END,
                    chargeRate);

            return Mathf.Lerp(
                DRIFT_TURN_SPEED_MEDIUM,
                DRIFT_TURN_SPEED_DEEP,
                stageRate);
        }

        return DRIFT_TURN_SPEED_DEEP;
    }


    /// <summary>
    /// 入力方向へ向けて、実際の進行方向を
    /// 指定した速度でゆっくり近づけます。
    /// </summary>
    /// <param name="inputDirection">入力方向。</param>
    /// <param name="turnSpeedDegreesPerSecond">
    /// 1秒間の最大方向転換角度。
    /// </param>
    private void UpdateDriftVelocityDirection(
        Vector3 inputDirection,
        float turnSpeedDegreesPerSecond)
    {
        if (inputDirection.sqrMagnitude <= 0.0001f)
        {
            // 入力がなくなっても、
            // 現在の進行方向を維持します。
            return;
        }

        float maxRadiansDelta =
            turnSpeedDegreesPerSecond *
            Mathf.Deg2Rad *
            Time.fixedDeltaTime;

        m_currentVelocityDirection =
            Vector3.RotateTowards(
                m_currentVelocityDirection,
                inputDirection,
                maxRadiansDelta,
                0.0f)
            .normalized;
    }


    /// <summary>
    /// 状態終了時に呼ばれます。
    /// </summary>
    protected override void OnExitState()
    {
        Debug.Log(
            "[PlayerBoostChargingState] チャージ状態終了",
            Owner);

        Owner.AnimationPresenter.StopWalkAnimation();
    }


    /// <summary>
    /// チャージを解除したときの遷移を行います。
    /// </summary>
    private void ReleaseCharge()
    {
        if (ChargeRate >= MIN_BOOST_CHARGE_RATE)
        {
            Debug.Log(
                $"[PlayerBoostChargingState] " +
                $"チャージ率{ChargeRate:P1} → Vブーストへ遷移",
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