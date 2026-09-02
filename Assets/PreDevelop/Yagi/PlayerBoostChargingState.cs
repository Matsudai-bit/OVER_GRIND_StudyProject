using UnityEngine;

/// <summary>
/// プレイヤーのブーストチャージ状態を管理します。
///
/// チャージ開始時の移動方向を固定し、
/// チャージ中はその方向へ進み続けます。
///
/// プレイヤーの向きはチャージ開始方向を基準にして、
/// スティック入力された方向へ一定角度まで変更できます。
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

    // チャージ中のプレイヤーの向きを変更できる最大角度
    private const float MAX_CHARGE_FACING_ANGLE = 1.0f;

    // プレイヤーの向きが目標方向へ変化する速度
    private const float CHARGE_FACING_ROTATION_SPEED = 180.0f;

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

    // チャージ中に固定して使う移動速度パラメータ
    private PlayerMoveParameters m_moveParameters;

    // チャージ中の固定された移動方向
    private Vector3 m_currentVelocityDirection;

    // チャージ開始時のプレイヤーの向き
    private Vector3 m_chargeStartFacingDirection;

    // 現在のプレイヤーの向き
    private Vector3 m_chargeFacingDirection;


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
        // チャージ開始時の移動方向を保存
        // --------------------------------------------------------

        m_currentVelocityDirection =
            Owner.Motor.HorizontalDirection;

        // 念のため水平化
        m_currentVelocityDirection.y = 0.0f;

        if (m_currentVelocityDirection.sqrMagnitude <= 0.0001f)
        {
            m_currentVelocityDirection =
                Owner.transform.forward;
        }

        m_currentVelocityDirection.Normalize();


        // --------------------------------------------------------
        // チャージ開始時のプレイヤーの向きを保存
        // --------------------------------------------------------

        m_chargeStartFacingDirection =
            m_currentVelocityDirection;

        m_chargeFacingDirection =
            m_chargeStartFacingDirection;


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
        // プレイヤーの向きを更新
        // --------------------------------------------------------

        UpdateChargeFacingDirection(
            inputDirection);


        // --------------------------------------------------------
        // チャージ中の移動
        // --------------------------------------------------------
        //
        // m_currentVelocityDirection は
        // チャージ開始時に保存した方向から変更しません。
        //
        // そのため、チャージ中に後ろや横へ入力しても
        // 移動方向は変わりません。
        //

        Owner.Motor.MoveWithDriftAtFixedSpeed(
            m_currentVelocityDirection,
            m_moveParameters.MaxMoveSpeed,
            m_chargeFacingDirection,
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
    /// チャージ中のプレイヤーの向きを更新します。
    ///
    /// 移動方向そのものは変更せず、
    /// チャージ開始時の方向を基準にして
    /// プレイヤーの向きだけを変更します。
    ///
    /// チャージ開始方向からの角度は
    /// 最大角度を超えないように制限します。
    /// </summary>
    /// <param name="inputDirection">入力方向。</param>
    private void UpdateChargeFacingDirection(
        Vector3 inputDirection)
    {
        if (inputDirection.sqrMagnitude <=
            STEERING_DEAD_ZONE * STEERING_DEAD_ZONE)
        {
            return;
        }


        // --------------------------------------------------------
        // チャージ開始方向から見た入力方向の角度を取得
        // --------------------------------------------------------

        float signedAngle =
            Vector3.SignedAngle(
                m_chargeStartFacingDirection,
                inputDirection.normalized,
                Vector3.up);


        // --------------------------------------------------------
        // 最大角度を超えないように制限
        // --------------------------------------------------------

        signedAngle =
            Mathf.Clamp(
                signedAngle,
                -MAX_CHARGE_FACING_ANGLE,
                MAX_CHARGE_FACING_ANGLE);


        // --------------------------------------------------------
        // チャージ開始方向を基準に
        // 制限された角度だけ回転した方向を作る
        // --------------------------------------------------------

        Vector3 targetFacingDirection =
            Quaternion.AngleAxis(
                signedAngle,
                Vector3.up) *
            m_chargeStartFacingDirection;


        // --------------------------------------------------------
        // 現在の向きを目標方向へ徐々に変更
        // --------------------------------------------------------

        float maxRadiansDelta =
            CHARGE_FACING_ROTATION_SPEED *
            Mathf.Deg2Rad *
            Time.fixedDeltaTime;

        m_chargeFacingDirection =
            Vector3.RotateTowards(
                m_chargeFacingDirection,
                targetFacingDirection,
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