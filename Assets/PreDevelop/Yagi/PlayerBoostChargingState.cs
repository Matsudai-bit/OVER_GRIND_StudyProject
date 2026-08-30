using UnityEngine;

/// <summary>
/// プレイヤーのブーストチャージ状態を管理します。
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

    // チャージ完了時点での旋回速度倍率（曲がりにくさの最大値）
    private const float MIN_CHARGE_ROTATION_SPEED_RATE = 0.15f;

    // 速度ログの出力間隔
    private const float SPEED_LOG_INTERVAL = 0.25f;

    // 移動入力が瞬間的に途切れてもIdlingへ遷移しない猶予時間
    // （逆入力時にスティックが0を通過する誤検知対策）
    private const float NO_MOVE_INPUT_GRACE_TIME = 0.15f;

    // チャージ経過時間
    private float m_chargeTime;

    // 速度ログの経過時間
    private float m_speedLogElapsedTime;

    // 移動入力が無い状態が続いている時間
    private float m_noMoveInputElapsedTime;

    // チャージ開始時点の通常旋回速度（補間の基準値）
    private float m_normalRotationSpeed;

    // チャージ中に固定して使う移動速度・加減速パラメータ
    private PlayerMoveParameters m_moveParameters;

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
        m_chargeTime = 0.0f;
        m_speedLogElapsedTime = 0.0f;
        m_noMoveInputElapsedTime = 0.0f;

        PlayerMoveParameters normalParameters =
            Owner.MovementParameterAsset
                .CreateMoveParameters();

        // 通常の最高速度ではなく、
        // チャージ開始時点の「実際の速度」を基準にする
        float currentSpeedAtChargeStart =
            Owner.Motor.HorizontalSpeed;

        float chargeMoveSpeed =
            currentSpeedAtChargeStart *
            CHARGE_MOVE_SPEED_RATE;

        // 旋回補間の基準値として、通常旋回速度を保存
        m_normalRotationSpeed =
            normalParameters.RotationSpeed;

        m_moveParameters =
            new PlayerMoveParameters(
                chargeMoveSpeed,
                normalParameters.TimeToMaxSpeed,
                normalParameters.TimeToStop,
                m_normalRotationSpeed);

        Debug.Log(
            $"[PlayerBoostChargingState] チャージ開始 " +
            $"開始時実速度={currentSpeedAtChargeStart:F2} " +
            $"チャージ固定速度={m_moveParameters.MaxMoveSpeed:F2} " +
            $"通常旋回速度={m_normalRotationSpeed:F2}",
            Owner);

        // ゲージ演出を0からチャージ中表示へ切り替える
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
        // 攻撃入力
        if (Owner.InputReader.ConsumeAttackInput())
        {
            Machine.ChangeState<PlayerAttackingState>();
            return;
        }

        // ジャンプ入力
        if (Owner.Monitor.IsGrounded &&
            Owner.InputReader.HasJumpInput)
        {
            Machine.ChangeState<PlayerJumpingState>();
            return;
        }

        // Vブースト入力を離したか確認
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

        // 移動入力の有無で、無入力の継続時間を更新
        if (Owner.InputReader.HasMoveInput)
        {
            m_noMoveInputElapsedTime = 0.0f;
        }
        else
        {
            m_noMoveInputElapsedTime += Time.fixedDeltaTime;

            // 猶予時間を超えて無入力が続いた場合のみ待機状態へ遷移
            // （逆入力時の瞬間的な0通過は無視する）
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

        // チャージ時間を進める
        m_chargeTime += Time.fixedDeltaTime;

        // 最大チャージ到達
        if (m_chargeTime >= MAX_CHARGE_TIME)
        {
            m_chargeTime = MAX_CHARGE_TIME;
        }

        // チャージ率に応じて、通常旋回速度から
        // 最も曲がりにくい旋回速度まで徐々に補間する
        float currentRotationSpeed =
            Mathf.Lerp(
                m_normalRotationSpeed,
                m_normalRotationSpeed *
                    MIN_CHARGE_ROTATION_SPEED_RATE,
                ChargeRate);

        Owner.Motor.MoveAtFixedSpeed(
            Owner.InputReader.MoveInput,
            m_moveParameters.MaxMoveSpeed,
            currentRotationSpeed,
            Time.fixedDeltaTime);

        // ゲージ表示をチャージ率に合わせて更新する
        if (Owner.VGaugeUI != null)
        {
            Owner.VGaugeUI.SetGaugeRate(ChargeRate);
        }

        // 速度ログ
        m_speedLogElapsedTime += Time.fixedDeltaTime;

        if (m_speedLogElapsedTime >= SPEED_LOG_INTERVAL)
        {
            m_speedLogElapsedTime = 0.0f;

            Debug.Log(
                $"[PlayerBoostChargingState] " +
                $"チャージ={ChargeRate:P1} " +
                $"実速度={Owner.Motor.HorizontalSpeed:F2} " +
                $"固定速度={m_moveParameters.MaxMoveSpeed:F2} " +
                $"旋回速度={currentRotationSpeed:F2}",
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

        // ゲージ表示・演出を通常状態へ戻す
        if (Owner.VGaugeUI != null)
        {
            Owner.VGaugeUI.SetGaugeRate(0.0f);
            Owner.VGaugeUI.SetCharging(false);
        }

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

            // チャージ率をVRunningStateへ引き継ぐ
            Owner.LastBoostChargeRate = ChargeRate;

            Machine.ChangeState<PlayerVRunningState>();
            return;
        }

        Debug.Log(
            $"[PlayerBoostChargingState] " +
            $"チャージ率{ChargeRate:P1} → 通常歩行へ遷移",
            Owner);

        Machine.ChangeState<PlayerWalkingState>();
    }
}