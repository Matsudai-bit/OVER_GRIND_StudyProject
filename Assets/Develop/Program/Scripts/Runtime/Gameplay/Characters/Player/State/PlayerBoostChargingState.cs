
using UnityEngine;

/// <summary>
/// プレイヤーのブーストチャージ状態を管理します。
///
    /// チャージ中は現在の移動方向を維持しながら、
    /// 入力角度で直進減速と左右ドリフトを切り替えます。
    /// 固定した旋回方向への通常入力と、低倍率の逆入力を別に扱います。
///
/// 移動中開始のチャージでは、実際のPlayer本体は回転させず、
/// ModelTransformのみを移動方向に対して横向きにします。
/// これにより、移動処理へ影響を与えずにドリフト中の見た目を表現します。
/// </summary>
public sealed class PlayerBoostChargingState
    : StateBase<PlayerStateMachineComponent>
{
    // 停止状態と判定する速度のしきい値
    private const float STATIONARY_SPEED_THRESHOLD = 0.01f;

    // 使用するパラメータアセット
    private PlayerBoostChargingParameterAsset m_parameterAsset;

    // チャージ経過時間
    private float m_chargeTime;

    // 速度ログの経過時間
    private float m_speedLogElapsedTime;

    // 開始時の実速度から徐々に減少するチャージ中の速度
    private float m_chargeSpeed;

    // 現在のチャージ中の移動方向
    private Vector3 m_currentVelocityDirection;

    // 現在のプレイヤーの向き
    // 実際のPlayer本体は回転させず、移動方向・カメラ等の基準として使用します。
    private Vector3 m_currentFacingDirection;

    // チャージ中のモデルの見た目の向き
    // 実際の移動方向とは独立して、ドリフト中の見た目を表現するために使用します。
    private Vector3 m_currentModelFacingDirection;

    // チャージ開始前のモデルのローカル回転
    // チャージ終了時に、Prefabで設定された本来の姿勢へ正確に戻すために保持する。
    private Quaternion m_defaultModelLocalRotation;

    // 停止中にチャージを開始したか
    private bool m_startedFromStationary;

    // 現在使用している最大チャージ時間
    private float m_currentMaxChargeTime;

    // 最初に確定した旋回方向。チャージが終わるまで変更しません。
    private int m_driftSide;

    // 入力の急変を緩和した旋回量と、モデルの横滑り角度。
    private float m_currentDriftSteering;
    private float m_modelSlipAngle;
    private float m_driftElapsedTime;
    private bool m_isDashRequested;

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
        m_driftSide = 0;
        m_currentDriftSteering = 0.0f;
        m_modelSlipAngle = 0.0f;
        m_driftElapsedTime = 0.0f;
        m_isDashRequested = false;



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

        m_chargeSpeed = currentSpeedAtChargeStart;

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

        // モデルの見た目の向きも移動方向で初期化する
        m_currentModelFacingDirection =
            m_currentVelocityDirection;

        // モデルは物理ボディとは独立して回転させる。基準姿勢を保持しておくことで、
        // Prefab側で設定された回転を終了時に壊さない。
        if (Owner.ModelTransform != null)
        {
            m_defaultModelLocalRotation =
                Owner.ModelTransform.localRotation;
        }

        Debug.Log(
            $"[PlayerBoostChargingState] チャージ開始 " +
            $"開始時実速度={currentSpeedAtChargeStart:F2} " +
            $"停止開始={m_startedFromStationary} " +
            $"最大チャージ時間={m_currentMaxChargeTime:F2}秒 " +
            $"チャージ速度={m_chargeSpeed:F2}",
            Owner);

        Owner.VGaugePlaceModel.SetGaugeRate(0.0f);

        if (Owner.VGaugeUI != null)
        {
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
        // Updateで遷移するまで、解除時に確定した向きとダッシュ方向を保持します。
        if (m_isDashRequested) return;
        if (Owner.InputReader.ConsumeAttackInput() && Owner.Monitor.IsGrounded)
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
            Owner.VGaugePlaceModel.SetGaugeRate(0.0f);

            if (Owner.VGaugeUI != null)
            {
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

        if (!m_startedFromStationary)
        {
            // 移動中開始の場合はドリフトする
            UpdateDriftVelocityDirection(
                normalizedInput,
                currentDriftTurnSpeed);

            // Player本体の回転方向を内部情報として更新するだけで、
            // Owner.transform自体は回転させません。
            UpdateFacingDirection();

            // モデルだけを移動方向に対して横向きにする
            UpdateModelSidewaysFacing(normalizedInput);
        }
        else
        {
            // 停止中開始の場合は左右入力によってその場で回転する
            UpdateStationaryChargeRotation(normalizedInput);
        }

        // --------------------------------------------------------
        // チャージ中の移動
        // --------------------------------------------------------

        // 実速度には接触摩擦や物理の押し戻しも含まれるため、減速計算へ毎回取り込みません。
        // 開始時の速度から設定した減速度だけで目標速度を減らし、障害物はMotor側で制限します。
        m_chargeSpeed = Mathf.MoveTowards(m_chargeSpeed,
            0.0f, m_parameterAsset.ChargeDeceleration * Time.fixedDeltaTime);

        // 正面・ニュートラルでも速度を保持し、上記の減速だけを適用します。
        // 停止中開始の場合は移動しない
        if (!m_startedFromStationary)
        {
            Owner.Motor.MoveWithDriftAtFixedSpeed(
                m_currentVelocityDirection,
                m_chargeSpeed,
                m_currentFacingDirection,
                m_parameterAsset.FacingRotationSpeed,
                Time.fixedDeltaTime,
                true,
                false);
        }

        // --------------------------------------------------------
        // カメラをチャージ中の入力側へ追従
        // --------------------------------------------------------

        UpdateChargeCamera();

        // --------------------------------------------------------
        // ゲージ更新
        // --------------------------------------------------------

        Owner.VGaugePlaceModel.SetGaugeRate(ChargeRate);

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
                $"減速後速度={m_chargeSpeed:F2} " +
                $"減速度={m_parameterAsset.ChargeDeceleration:F3} " +
                $"設定={m_parameterAsset.name} " +
                $"曲がりやすさ={currentDriftTurnSpeed:F1}deg/s",
                Owner);
        }
    }

    /// <summary>チャージ中のモデルの向きにカメラを滑らかに追従させます。</summary>
    private void UpdateChargeCamera()
    {
        if (Owner.PlayerCamera == null) return;

        if (!m_startedFromStationary)
        {
            // モデルへ適用する向きを共有し、横滑り中も体の正面へ徐々に追従します。
            Owner.PlayerCamera.UpdateDriftLookDirectionOverTime(m_currentModelFacingDirection,
                m_parameterAsset.MovingChargeCameraLookDuration, Time.fixedDeltaTime);
            return;
        }

        // 停止中も開始時のカメラ角度と混ぜず、体の正面を追従先にします。
        Owner.PlayerCamera.UpdateDriftLookDirection(m_currentModelFacingDirection,
            1.0f,
            m_parameterAsset.CameraDriftLookTurnSpeed, Time.fixedDeltaTime);
    }

    /// <summary>
    /// 描画フレームごとにモデルの回転を適用します。
    /// 物理更新で決めた進行方向とは別に、Animator更新後にも見た目を維持します。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過時間。</param>
    protected override void OnUpdate(float deltaTime)
    {
        ApplyModelFacing();
    }

    /// <summary>
    /// 停止中チャージ時の回転処理を行います。
    ///
    /// 最初に確定した左右入力に応じてプレイヤーをその場で回転させます。
    /// 回転速度はチャージ率によって変化せず、常に一定です。
    /// </summary>
    /// <param name="input">正規化された移動入力。</param>
    private void UpdateStationaryChargeRotation(
        Vector2 input)
    {
        float steeringInput = GetLockedSteeringInput(input);
        if (Mathf.Approximately(steeringInput, 0.0f))
        {
            return;
        }

        float rotationAmount =
            steeringInput *
            m_parameterAsset.StationaryChargeRotationSpeed *
            Time.fixedDeltaTime;

        Owner.transform.Rotate(
            0.0f,
            rotationAmount,
            0.0f,
            Space.World);

        m_currentFacingDirection =
            Owner.transform.forward;

        m_currentFacingDirection.y = 0.0f;

        if (m_currentFacingDirection.sqrMagnitude >
            0.0001f)
        {
            m_currentFacingDirection.Normalize();
        }

        m_currentVelocityDirection =
            m_currentFacingDirection;

        // 停止中開始ではPlayer本体を回転させる既存仕様を維持するため、
        // モデルもPlayer本体の正面へ合わせます。
        m_currentModelFacingDirection =
            m_currentFacingDirection;

        ApplyModelFacing();
    }

    /// <summary>スティックの正面からの角度で左右を判定します。</summary>
    /// <param name="input">スティック入力。</param>
    /// <returns>左は-1、右は1、正面・後方・ニュートラルは0。</returns>
    private int ClassifyChargeInput(Vector2 input)
    {
        float deadZone = m_parameterAsset.SteeringDeadZone;
        if (input.sqrMagnitude <= deadZone * deadZone) return 0;
        float angle = Mathf.Atan2(input.x, input.y) * Mathf.Rad2Deg;
        float absoluteAngle = Mathf.Abs(angle);
        if (absoluteAngle <= m_parameterAsset.ForwardInputHalfAngle ||
            absoluteAngle >= 180.0f - m_parameterAsset.ForwardInputHalfAngle) return 0;
        return angle > 0.0f ? 1 : -1;
    }

    /// <summary>最初の有効な左右入力を固定し、逆入力・無入力は低い倍率で同じ軌道を返します。</summary>
    /// <param name="input">スティック入力。</param>
    /// <returns>旋回方向と倍率を反映した符号付き入力。</returns>
    private float GetLockedSteeringInput(Vector2 input)
    {
        int inputSide = ClassifyChargeInput(input);
        if (m_driftSide == 0 && inputSide != 0)
        {
            m_driftSide = inputSide;
            m_driftElapsedTime = 0.0f;
            m_currentDriftSteering = 0.0f;
        }

        if (inputSide == m_driftSide)
        {
            return m_driftSide * input.magnitude;
        }

        // 逆入力とスティックを離した状態は、通常入力と同じ旋回計算を使い、
        // 旋回方向だけ固定したまま入力の強さを下げます。
        float steeringMagnitude = input.sqrMagnitude <=
            m_parameterAsset.SteeringDeadZone * m_parameterAsset.SteeringDeadZone
            ? 1.0f
            : input.magnitude;

        return m_driftSide *
            steeringMagnitude *
            m_parameterAsset.DriftCounterTurnRate;
    }

    /// <summary>入力と反対側へ膨らんだ後、入力側へ旋回する軌道を計算します。</summary>
    /// <param name="input">スティック入力。</param>
    /// <param name="turnSpeedDegreesPerSecond">最大旋回速度（度/秒）。</param>
    private void UpdateDriftVelocityDirection(Vector2 input, float turnSpeedDegreesPerSecond)
    {
        float steeringInput = GetLockedSteeringInput(input);
        if (Mathf.Approximately(steeringInput, 0.0f))
        {
            m_currentDriftSteering = 0.0f;
        }
        else
        {
            m_currentDriftSteering = Mathf.MoveTowards(m_currentDriftSteering, steeringInput,
                Time.fixedDeltaTime / m_parameterAsset.DriftSteeringResponseTime);
        }

        // 通常入力・逆入力・スティックを離した状態で同じ軌道計算を使用します。
        // 逆入力と無入力だけ、GetLockedSteeringInput側で旋回量を下げています。
        float progress = Mathf.Clamp01(m_driftElapsedTime / m_parameterAsset.DriftOutwardDuration);
        float turnRate = Mathf.Lerp(-m_parameterAsset.DriftOutwardTurnRate, 1.0f,
            Mathf.SmoothStep(0.0f, 1.0f, progress));
        float turnInput = m_currentDriftSteering;
        float turnAngle = turnInput * turnSpeedDegreesPerSecond * turnRate * Time.fixedDeltaTime;
        m_currentVelocityDirection = (Quaternion.AngleAxis(turnAngle, Vector3.up)
            * m_currentVelocityDirection).normalized;
        m_driftElapsedTime += Time.fixedDeltaTime;
    }

    /// <summary>
    /// プレイヤーの向きを現在の移動方向へ徐々に変更するための
    /// 内部向き情報を更新します。
    ///
    /// 実際のPlayer本体のTransformは回転させません。
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

    /// <summary>旋回の強さに応じた横滑り角度へ、モデルを滑らかに傾けます。</summary>
    private void UpdateModelSidewaysFacing(Vector2 input)
    {
        // 旋回の弱さと体の横向き角度を分離し、逆入力・無入力でも姿勢を残します。
        float lookRate = ClassifyChargeInput(input) == m_driftSide
            ? input.magnitude
            : m_parameterAsset.CounterSteeringLookRate;
        float targetAngle = m_driftSide * lookRate * m_parameterAsset.MovingChargeSidewaysLookAngle;
        m_modelSlipAngle = Mathf.MoveTowards(m_modelSlipAngle, targetAngle,
            m_parameterAsset.FacingRotationSpeed * Time.fixedDeltaTime);
        m_currentModelFacingDirection = Quaternion.AngleAxis(m_modelSlipAngle, Vector3.up)
            * m_currentVelocityDirection;
    }

    /// <summary>
    /// モデルだけを指定方向へ向けます。
    /// Player本体、Collider、RigidbodyのTransformは一切変更しません。
    /// </summary>
    private void ApplyModelFacing()
    {
        if (Owner.ModelTransform == null ||
            m_currentModelFacingDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Owner.ModelTransform.rotation =
            Quaternion.LookRotation(
                m_currentModelFacingDirection,
                Vector3.up);
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

        // 見た目の横滑り角度を移動へ持ち越さず、終了時の進行方向へ本体を合わせます。
        if (!m_startedFromStationary)
            Owner.Motor.AlignFacingToDirection(m_isDashRequested
                ? Owner.BoostDashDirection : m_currentVelocityDirection);

        // モデルオブジェクトの回転をPrefabで設定された元の姿勢へ戻す
        if (Owner.ModelTransform != null)
        {
            Owner.ModelTransform.localRotation =
                m_defaultModelLocalRotation;
        }

        Owner.AnimationPresenter.StopWalkAnimation();
    }

    /// <summary>
    /// チャージを解除したときの遷移を行います。
    ///
    /// ダッシュ可能なチャージ量の場合は、
    /// チャージ終了時点の実際の進行方向を
    /// ダッシュ方向として使用します。
    /// </summary>
    private void ReleaseCharge()
    {
        if (ChargeRate >=
            m_parameterAsset.MinBoostChargeRate)
        {
            // 解除直前に表示されている体の正面をダッシュ方向として確定します。
            Vector3 dashDirection =
                Owner.ModelTransform != null ? Owner.ModelTransform.forward : m_currentModelFacingDirection;

            dashDirection.y = 0.0f;

            if (dashDirection.sqrMagnitude >
                0.0001f)
            {
                dashDirection.Normalize();
            }

            Owner.BoostDashDirection =
                dashDirection;
            m_isDashRequested = true;

            Debug.Log(
                $"[PlayerBoostChargingState] " +
                $"チャージ率{ChargeRate:P1} → Vブーストへ遷移 " +
                $"ダッシュ方向={dashDirection}",
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

        Owner.VGaugePlaceModel.SetGaugeRate(0.0f);

        if (Owner.VGaugeUI != null)
        {
            Owner.VGaugeUI.SetCharging(false);
        }

        Machine.ChangeState<PlayerWalkingState>();
    }
}
