
using UnityEngine;

/// <summary>
/// プレイヤーのブーストチャージ状態を管理します。
///
/// チャージ中は現在の移動方向を維持しながら、
/// スティック入力によって移動方向を変更できます。
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

    // チャージ中に使用する移動速度パラメータ
    private PlayerMoveParameters m_moveParameters;

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

        if (!m_startedFromStationary)
        {
            // 移動中開始の場合はドリフトする
            UpdateDriftVelocityDirection(
                inputDirection,
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

        // 停止中開始の場合は移動しない
        if (!m_startedFromStationary)
        {
            Owner.Motor.MoveWithDriftAtFixedSpeed(
                m_currentVelocityDirection,
                m_moveParameters.MaxMoveSpeed,
                m_currentFacingDirection,
                m_parameterAsset.FacingRotationSpeed,
                Time.fixedDeltaTime,
                true,
                false);
        }

        // --------------------------------------------------------
        // カメラを、開始時の向きと実際の移動方向の中間へ追従
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
    /// 左右のスティック入力に応じてプレイヤーをその場で回転させます。
    /// 回転速度はチャージ率によって変化せず、常に一定です。
    /// </summary>
    /// <param name="input">正規化された移動入力。</param>
    private void UpdateStationaryChargeRotation(
        Vector2 input)
    {
        if (Mathf.Abs(input.x) <=
            m_parameterAsset.SteeringDeadZone)
        {
            return;
        }

        float rotationAmount =
            input.x *
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

    /// <summary>
    /// チャージ中の移動方向を更新します。
    ///
    /// スティック入力に対して移動方向を徐々に追従させます。
    /// </summary>
    /// <param name="inputDirection">カメラ基準の入力方向。</param>
    /// <param name="turnSpeedDegreesPerSecond">
    /// 1秒間の最大方向転換角度。
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

    /// <summary>
    /// 移動中開始のチャージ中、モデルオブジェクトの見た目の向きを
    /// 移動方向に対して横向きへ変更します。
    ///
    /// スティックを倒している間は、現在の移動方向に対して
    /// MovingChargeSidewaysLookAngleで指定された角度を常に維持します。
    ///
    /// そのため、スティックを倒した瞬間にモデルが横を向き、
    /// その後移動方向が変化しても横向きの関係を維持したまま追従します。
    ///
    /// Player本体のTransformは変更せず、ModelTransformのみを変更します。
    /// </summary>
    /// <param name="input">正規化された移動入力。</param>
    private void UpdateModelSidewaysFacing(
        Vector2 input)
    {
        bool isSteering =
            Mathf.Abs(input.x) >
            m_parameterAsset.SteeringDeadZone;

        if (isSteering)
        {
            // 左右入力の方向に応じて、移動方向から左右へ角度を付ける。
            float sideSign =
                Mathf.Sign(input.x);

            Vector3 targetDirection =
                Quaternion.AngleAxis(
                    sideSign *
                    m_parameterAsset.MovingChargeSidewaysLookAngle,
                    Vector3.up) *
                m_currentVelocityDirection;

            // 進行方向に対する横向きをモデルへ反映する。
            // 物理ボディは回転させず、ダッシュ開始時だけこの向きを引き継ぐ。
            m_currentModelFacingDirection =
                targetDirection;
        }
        else
        {
            // スティックを離した場合のみ、
            // 通常の移動方向へ滑らかに戻します。
            float maxRadiansDelta =
                m_parameterAsset.FacingRotationSpeed *
                Mathf.Deg2Rad *
                Time.fixedDeltaTime;

            m_currentModelFacingDirection =
                Vector3.RotateTowards(
                    m_currentModelFacingDirection,
                    m_currentVelocityDirection,
                    maxRadiansDelta,
                    0.0f);
        }

        m_currentModelFacingDirection.y = 0.0f;

        if (m_currentModelFacingDirection.sqrMagnitude >
            0.0001f)
        {
            m_currentModelFacingDirection.Normalize();
        }

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
    /// チャージ終了時点のモデルが向いている方向を
    /// ダッシュ方向として使用します。
    /// </summary>
    private void ReleaseCharge()
    {
        if (ChargeRate >=
            m_parameterAsset.MinBoostChargeRate)
        {
            // チャージ中にモデルへ反映した横向き（旋回側への90度オフセット）を
            // そのままダッシュ方向へ引き継ぐ。
            Vector3 dashDirection =
                m_currentModelFacingDirection;

            dashDirection.y = 0.0f;

            if (dashDirection.sqrMagnitude >
                0.0001f)
            {
                dashDirection.Normalize();
            }

            Owner.BoostDashDirection =
                dashDirection;

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

        if (Owner.VGaugeUI != null)
        {
            Owner.VGaugeUI.SetGaugeRate(0.0f);
            Owner.VGaugeUI.SetCharging(false);
        }

        Machine.ChangeState<PlayerWalkingState>();
    }
}
