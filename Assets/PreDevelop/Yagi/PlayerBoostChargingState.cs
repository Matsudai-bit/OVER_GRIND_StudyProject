using UnityEngine;

/// <summary>
/// プレイヤーのブーストチャージ状態を管理します。
///
/// チャージ中は現在の移動方向を維持しながら、
/// スティック入力によって少しずつ移動方向を変更できます。
///
/// 移動方向の変更速度は通常移動より遅く、
/// 後ろ方向への移動はできません。
///
/// プレイヤーの向きは、現在の移動方向へ
/// ゆっくり追従します。
/// </summary>
public sealed class PlayerBoostChargingState
    : StateBase<PlayerStateMachineComponent>
{
    // ============================================================
    // チャージ設定
    // ============================================================

    // 最大チャージ時間
    private const float MAX_CHARGE_TIME = 4.0f;

    // ブーストダッシュに必要な最低チャージ割合
    private const float MIN_BOOST_CHARGE_RATE = 0.10f;

    // チャージ中の移動速度倍率
    private const float CHARGE_MOVE_SPEED_RATE = 0.75f;


    // ============================================================
    // ドリフト設定
    // ============================================================

    // チャージ中に移動方向を変更する速度（度/秒）
    //
    // 小さくするほど曲がりにくくなります。
    private const float CHARGE_DRIFT_TURN_SPEED = 40.0f;

    // チャージ中にプレイヤーの向きを変更する速度（度/秒）
    //
    // 移動方向よりさらに遅くすることで、
    // 「滑りながら徐々に向きが変わる」感覚を作ります。
    private const float CHARGE_FACING_ROTATION_SPEED = 50.0f;

    // スティック入力のデッドゾーン
    private const float STEERING_DEAD_ZONE = 0.1f;


    // ============================================================
    // その他
    // ============================================================

    // 速度ログの出力間隔
    private const float SPEED_LOG_INTERVAL = 0.25f;

    // 移動入力が瞬間的に途切れてもIdlingへ遷移しない猶予時間
    private const float NO_MOVE_INPUT_GRACE_TIME = 0.15f;


    // ============================================================
    // 状態
    // ============================================================

    // チャージ経過時間
    private float m_chargeTime;

    // 速度ログの経過時間
    private float m_speedLogElapsedTime;

    // 移動入力が無い状態が続いている時間
    private float m_noMoveInputElapsedTime;

    // チャージ開始時点の通常旋回速度
    private float m_normalFacingRotationSpeed;

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


        // --------------------------------------------------------
        // チャージ開始時の移動方向を取得
        // --------------------------------------------------------

        m_currentVelocityDirection =
            Owner.Motor.HorizontalDirection;

        // 水平方向だけを使用
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
        // チャージ中の移動方向を更新
        // --------------------------------------------------------

        UpdateDriftVelocityDirection(
            inputDirection);


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
            CHARGE_FACING_ROTATION_SPEED,
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
                $"固定速度={m_moveParameters.MaxMoveSpeed:F2}",
                Owner);
        }
    }


    /// <summary>
    /// チャージ中の移動方向を更新します。
    ///
    /// スティック入力に対して移動方向を徐々に追従させます。
    /// そのため、通常移動よりも曲がりにくいドリフトになります。
    ///
    /// 現在の移動方向より後ろ側への入力は無視します。
    /// これにより、チャージ中に後退することを防ぎます。
    /// </summary>
    /// <param name="inputDirection">カメラ基準の入力方向。</param>
    private void UpdateDriftVelocityDirection(
        Vector3 inputDirection)
    {
        inputDirection.y = 0.0f;

        if (inputDirection.sqrMagnitude <=
            STEERING_DEAD_ZONE * STEERING_DEAD_ZONE)
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
        //
        // これにより、チャージ中にスティックを
        // 真後ろへ倒しても後退しません。
        if (forwardDot <= 0.0f)
        {
            return;
        }


        // --------------------------------------------------------
        // 入力方向へ徐々に移動方向を変更
        // --------------------------------------------------------

        float maxRadiansDelta =
            CHARGE_DRIFT_TURN_SPEED *
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
    ///
    /// 移動方向とプレイヤーの向きを完全に同期させるのではなく、
    /// 一定速度で追従させることでドリフト感を出します。
    /// </summary>
    private void UpdateFacingDirection()
    {
        float maxRadiansDelta =
            CHARGE_FACING_ROTATION_SPEED *
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