using UnityEngine;

/// <summary>
/// プレイヤーのブーストチャージ状態を管理します。
/// マリオカートのドリフトのように、見た目の向きは
/// 入力方向へ素早く追従する一方、実際の進行方向(慣性)は
/// チャージが進むほど極端にゆっくりとしか変化しなくなり、
/// 操作に強いクセ（曲がりにくさ）が生まれます。
/// </summary>
public sealed class PlayerBoostChargingState
    : StateBase<PlayerStateMachineComponent>
{
    // 最大チャージ時間
    private const float MAX_CHARGE_TIME = 4.0f;

    // ブーストダッシュに必要な最低チャージ割合
    private const float MIN_BOOST_CHARGE_RATE = 0.10f;

    // チャージ中の移動速度倍率（チャージ開始時点の実速度に対する倍率）
    private const float CHARGE_MOVE_SPEED_RATE = 0.75f;

    // チャージ開始直後の、実際の進行方向の旋回速度（度/秒）
    // アセット側のRotationSpeedの値に関わらず、
    // 確実に「素早くは曲がれない」体感を保証するための絶対値
    private const float DRIFT_TURN_SPEED_AT_START = 150.0f;

    // チャージ完了時点の、実際の進行方向の旋回速度（度/秒）
    // かなり小さい値にし、大きく外側へ膨らむ挙動にする
    private const float DRIFT_TURN_SPEED_AT_FULL_CHARGE = 20.0f;

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

    // チャージ開始時点の通常旋回速度（見た目の向き用）
    private float m_normalFacingRotationSpeed;

    // チャージ中に固定して使う移動速度パラメータ
    private PlayerMoveParameters m_moveParameters;

    // 現在の実際の進行方向（ワールド空間、正規化済み）
    // 入力方向へは即座に一致せず、ドリフトのようにゆっくり近づく
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

        // ドリフト開始時点の実際の進行方向を、
        // チャージ開始直前の実速度方向で初期化する
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

        m_chargeTime += Time.fixedDeltaTime;

        if (m_chargeTime >= MAX_CHARGE_TIME)
        {
            m_chargeTime = MAX_CHARGE_TIME;
        }

        // 入力方向を計算する（見た目の向き・進行方向の目標として共通で使用）
        Vector2 normalizedInput =
            Vector2.ClampMagnitude(
                Owner.InputReader.MoveInput,
                1.0f);

        Vector3 inputDirection =
            Owner.Motor.CalculateCameraRelativeDirection(
                normalizedInput);

        // チャージ開始直後から、フルチャージ時と同じ
        // 強いドリフト状態にします。
        // 実際の進行方向は入力方向へゆっくりしか追従せず、
        // プレイヤーの見た目の向きだけが入力方向へ追従します。
        float currentVelocityTurnSpeed =
            DRIFT_TURN_SPEED_AT_FULL_CHARGE;

        UpdateDriftVelocityDirection(
            inputDirection,
            currentVelocityTurnSpeed);

        // 見た目の向きは、実際の進行方向とは切り離し、
        // 入力方向へ通常の旋回速度で素早く追従させる。
        // これにより「車体は曲がりたい方向を向いているのに
        // 実際には外側へ滑っていく」というドリフトの見た目になる
        Owner.Motor.MoveWithDriftAtFixedSpeed(
            m_currentVelocityDirection,
            m_moveParameters.MaxMoveSpeed,
            inputDirection,
            m_normalFacingRotationSpeed,
            Time.fixedDeltaTime);

        if (Owner.VGaugeUI != null)
        {
            Owner.VGaugeUI.SetGaugeRate(ChargeRate);
        }

        m_speedLogElapsedTime += Time.fixedDeltaTime;

        if (m_speedLogElapsedTime >= SPEED_LOG_INTERVAL)
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
    /// 入力方向へ向けて、実際の進行方向を
    /// 指定した速度（度/秒）でゆっくり近づけます（ドリフトの横滑り本体）。
    /// </summary>
    /// <param name="inputDirection">入力方向。</param>
    /// <param name="turnSpeedDegreesPerSecond">1秒間の最大方向転換角度。</param>
    private void UpdateDriftVelocityDirection(
        Vector3 inputDirection,
        float turnSpeedDegreesPerSecond)
    {
        if (inputDirection.sqrMagnitude <= 0.0001f)
        {
            // 入力がほぼ無い場合は、
            // 現在の進行方向をそのまま維持する
            // （ハンドルをニュートラルに戻しても
            // 　滑りは急には止まらない）
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

            Owner.CarriedBoostGaugeRate = ChargeRate;

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