using UnityEngine;

/// <summary>移動中に開始するブーストチャージのドリフトと対象旋回を管理します。</summary>
public sealed class PlayerDynamicChargeState : PlayerBoostChargingState
{
    // 移動チャージ開始時の進行方向と固定した旋回方向。
    private Vector3 m_chargeStartDirection;
    private int m_driftSide;

    // ドリフト時の旋回量とモデルの横滑り角度。
    private float m_currentDriftSteering;
    private float m_modelSlipAngle;
    private float m_driftElapsedTime;

    // 対象を中心に旋回するときの状態。
    private Transform m_targetOrbitTransform;
    private int m_targetOrbitSide;
    private float m_currentTargetOrbitTurnRate;

    /// <summary>移動開始のチャージであることを示します。</summary>
    protected override bool IsStaticCharge => false;

    /// <summary>ドリフトと対象旋回の開始状態を初期化します。</summary>
    protected override void InitializeChargeMode()
    {
        m_chargeStartDirection = m_currentVelocityDirection;
        m_driftSide = 0;
        m_currentDriftSteering = 0.0f;
        m_modelSlipAngle = 0.0f;
        m_driftElapsedTime = 0.0f;
        m_targetOrbitTransform = null;
        m_targetOrbitSide = 0;
        m_currentTargetOrbitTurnRate = 0.0f;
    }

    /// <summary>移動チャージと歩行のアニメーションを開始します。</summary>
    protected override void PlayChargeAnimation()
    {
        Owner.AnimationPresenter.PlayChargeMovingAnimation();
        Owner.AnimationPresenter.PlayWalkAnimation();
    }

    /// <summary>移動チャージ中の歩行アニメーションを終了します。</summary>
    protected override void StopChargeAnimation()
    {
        Owner.AnimationPresenter.StopWalkAnimation();
    }

    /// <summary>左右入力を移動チャージのアニメーションへ反映します。</summary>
    /// <param name="input">移動入力。</param>
    protected override void UpdateChargeDirectionAnimation(Vector2 input)
    {
        Owner.AnimationPresenter.SetMoveChargeDirection(ClassifyChargeInput(input));
    }

    /// <summary>既存の対象旋回またはドリフト条件で方向を更新し、固定速度で移動します。</summary>
    /// <param name="input">移動入力。</param>
    /// <param name="turnSpeed">チャージ率に応じた旋回速度。</param>
    /// <param name="target">現在のターゲット。</param>
    protected override void UpdateChargeMode(Vector2 input, float turnSpeed, Transform target)
    {
        if (target != null)
        {
            UpdateTargetOrbitVelocityDirection(target, input, turnSpeed);
            UpdateTargetFacingDirection(target);
        }
        else
        {
            ResetTargetOrbit();
            UpdateDriftVelocityDirection(input, turnSpeed);
            UpdateFacingDirection();
            UpdateModelSidewaysFacing(input);
        }

        Vector3 movementDirection = target != null
            ? m_currentVelocityDirection
            : GetChargeMovementDirection(input);

        Owner.Motor.MoveWithDriftAtFixedSpeed(
            movementDirection,
            m_chargeSpeed,
            m_currentFacingDirection,
            m_parameterAsset.FacingRotationSpeed,
            Time.fixedDeltaTime,
            true,
            false);
    }

    /// <summary>チャージ終了時の移動方向へ本体を合わせます。</summary>
    protected override void AlignFacingOnExit()
    {
        Owner.Motor.AlignFacingToDirection(
            m_isDashRequested ? Owner.BoostDashDirection : m_currentVelocityDirection);
    }

    /// <summary>
    /// ターゲットを中心とする接線方向へ進行方向を旋回させます。
    /// 前入力・無入力・後ろ入力に応じたターゲット専用の旋回倍率を使用します。
    /// </summary>
    /// <param name="targetTransform">旋回中心となるターゲット。</param>
    /// <param name="input">正規化された移動入力。</param>
    /// <param name="turnSpeedDegreesPerSecond">基準旋回速度（度/秒）。</param>
    private void UpdateTargetOrbitVelocityDirection(
        Transform targetTransform,
        Vector2 input,
        float turnSpeedDegreesPerSecond)
    {
        Vector3 outwardDirection =
            Owner.transform.position - targetTransform.position;
        outwardDirection.y = 0.0f;

        if (outwardDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        float targetDistance = outwardDirection.magnitude;
        outwardDirection /= targetDistance;

        if (m_targetOrbitTransform != targetTransform ||
            m_targetOrbitSide == 0)
        {
            m_targetOrbitTransform = targetTransform;

            Vector3 clockwiseTangent =
                Quaternion.AngleAxis(90.0f, Vector3.up) *
                outwardDirection;

            // 現在の進行方向に近い側を選び、ターゲット開始時の急反転を防ぐ。
            m_targetOrbitSide =
                Vector3.Dot(
                    m_currentVelocityDirection,
                    clockwiseTangent) >= 0.0f
                    ? 1
                    : -1;
        }

        Vector3 tangentOrbitDirection =
            Quaternion.AngleAxis(
                90.0f * m_targetOrbitSide,
                Vector3.up) *
            outwardDirection;

        Vector3 orbitDirection = tangentOrbitDirection;

        // 設定半径より外側では接線方向を内側へ傾ける。
        // 半径へ近づくほど角度を小さくし、到達後は接線方向だけで距離を維持する。
        float approachRate =
            Mathf.InverseLerp(
                m_parameterAsset.TargetOrbitRadius,
                m_parameterAsset.TargetOrbitRadius * 2.0f,
                targetDistance);

        float inwardInputRate =
            GetTargetOrbitInwardInputRate(input);

        float inwardAngle =
            m_parameterAsset.TargetOrbitMaxInwardAngle *
            approachRate *
            inwardInputRate;

        orbitDirection =
            Quaternion.AngleAxis(
                inwardAngle * m_targetOrbitSide,
                Vector3.up) *
            orbitDirection;

        float targetTurnRate =
            GetTargetOrbitTurnRate(input);

        m_currentTargetOrbitTurnRate =
            Mathf.MoveTowards(
                m_currentTargetOrbitTurnRate,
                targetTurnRate,
                Time.fixedDeltaTime /
                m_parameterAsset.DriftSteeringResponseTime);

        float maxRadiansDelta =
            turnSpeedDegreesPerSecond *
            m_currentTargetOrbitTurnRate *
            Mathf.Deg2Rad *
            Time.fixedDeltaTime;

        m_currentVelocityDirection =
            Vector3.RotateTowards(
                m_currentVelocityDirection,
                orbitDirection,
                maxRadiansDelta,
                0.0f);

        m_currentVelocityDirection.y = 0.0f;
        m_currentVelocityDirection.Normalize();

        bool hasReachedOrbitRadius =
            targetDistance <= m_parameterAsset.TargetOrbitRadius;
        bool shouldMaintainDistance =
            hasReachedOrbitRadius ||
            Mathf.Approximately(inwardInputRate, 0.0f);

        float outwardVelocityRate =
            Vector3.Dot(
                m_currentVelocityDirection,
                outwardDirection);

        // 旋回開始前の進行方向に外向き成分が残っていても、
        // ターゲット旋回中に対象から意図せず離れないよう除去する。
        // 接近入力がない場合と設定半径到達後は、内外両方の半径成分を除去する。
        if (shouldMaintainDistance ||
            outwardVelocityRate > 0.0f)
        {
            Vector3 tangentVelocityDirection =
                m_currentVelocityDirection -
                outwardDirection * outwardVelocityRate;

            Vector3 fallbackDirection =
                shouldMaintainDistance
                    ? tangentOrbitDirection
                    : orbitDirection;

            m_currentVelocityDirection =
                tangentVelocityDirection.sqrMagnitude > 0.0001f
                    ? tangentVelocityDirection.normalized
                    : fallbackDirection.normalized;
        }
    }

    /// <summary>
    /// 前後入力からターゲット方向へ接近する強さを取得します。
    /// 前入力で最大、後ろ入力で0となり、前後方向への直接移動には使用しません。
    /// </summary>
    /// <param name="input">正規化された移動入力。</param>
    /// <returns>内向き角度へ適用する0～1の割合。</returns>
    private float GetTargetOrbitInwardInputRate(Vector2 input)
    {
        float verticalInput = input.y;
        float deadZone = m_parameterAsset.SteeringDeadZone;
        float neutralRate =
            m_parameterAsset.TargetOrbitNeutralInwardRate;

        if (verticalInput > deadZone)
        {
            float inputRate = Mathf.InverseLerp(
                deadZone,
                1.0f,
                verticalInput);

            return Mathf.Lerp(
                neutralRate,
                1.0f,
                inputRate);
        }

        if (verticalInput < -deadZone)
        {
            return 0.0f;
        }

        return neutralRate;
    }

    /// <summary>
    /// ターゲット旋回で使用する前後入力別の旋回倍率を取得します。
    /// 前後入力は移動方向へ反映せず、旋回倍率の補間だけに使用します。
    /// </summary>
    /// <param name="input">正規化された移動入力。</param>
    /// <returns>前入力・無入力・後ろ入力に対応する旋回倍率。</returns>
    private float GetTargetOrbitTurnRate(Vector2 input)
    {
        float verticalInput = input.y;
        float deadZone = m_parameterAsset.SteeringDeadZone;

        if (verticalInput > deadZone)
        {
            float inputRate = Mathf.InverseLerp(
                deadZone,
                1.0f,
                verticalInput);

            return Mathf.Lerp(
                m_parameterAsset.TargetDriftNeutralTurnRate,
                m_parameterAsset.TargetDriftForwardTurnRate,
                inputRate);
        }

        if (verticalInput < -deadZone)
        {
            float inputRate = Mathf.InverseLerp(
                deadZone,
                1.0f,
                Mathf.Abs(verticalInput));

            return Mathf.Lerp(
                m_parameterAsset.TargetDriftNeutralTurnRate,
                m_parameterAsset.TargetDriftCounterTurnRate,
                inputRate);
        }

        return m_parameterAsset.TargetDriftNeutralTurnRate;
    }

    /// <summary>ターゲット旋回専用の状態を初期化します。</summary>
    private void ResetTargetOrbit()
    {
        m_targetOrbitTransform = null;
        m_targetOrbitSide = 0;
        m_currentTargetOrbitTurnRate = 0.0f;
    }
    /// <summary>現在のスティック横入力の左右を判定します。</summary>
    /// <param name="input">スティック入力。</param>
    /// <returns>左は-1、右は1、正面・後方・ニュートラルは0。</returns>
    private int ClassifyChargeInput(Vector2 input)
    {
        float steeringInput =
            m_parameterAsset.GetSmoothedSteeringInput(input);

        if (Mathf.Approximately(steeringInput, 0.0f))
        {
            return 0;
        }

        return steeringInput > 0.0f ? 1 : -1;
    }

    /// <summary>自由旋回中は左右入力を直接返し、角度超過後は最初の有効な方向へ固定します。</summary>
    /// <param name="input">スティック入力。</param>
    /// <returns>旋回方向と倍率を反映した符号付き入力。</returns>
    private float GetLockedSteeringInput(Vector2 input)
    {
        int inputSide = ClassifyChargeInput(input);
        if (m_driftSide == 0)
        {
            float currentAngleFromStart =
                Vector3.SignedAngle(
                    m_chargeStartDirection,
                    m_currentVelocityDirection,
                    Vector3.up);

            if (Mathf.Abs(currentAngleFromStart) <
                m_parameterAsset.FreeSteeringAngle)
            {
                return GetFreeSteeringInput(input);
            }

            m_driftSide = currentAngleFromStart > 0.0f ? 1 : -1;
            m_driftElapsedTime = 0.0f;
            m_currentDriftSteering = 0.0f;
        }

        if (inputSide == m_driftSide)
        {
            return m_driftSide *
                input.magnitude *
                m_parameterAsset.DriftForwardTurnRate;
        }

        if (inputSide == -m_driftSide)
        {
            // 逆入力でも確定済みの旋回方向は維持し、旋回量だけを弱めます。
            return m_driftSide *
                input.magnitude *
                m_parameterAsset.DriftCounterTurnRate;
        }

        // 無入力では、従来の逆入力・無入力共通だった内向きの緩い旋回を維持します。
        return m_driftSide *
            m_parameterAsset.DriftNeutralTurnRate;
    }

    /// <summary>方向固定前の範囲内で、スティックの横方向傾斜に応じた自由旋回入力を取得します。</summary>
    /// <param name="input">スティック入力。</param>
    /// <returns>左は負、右は正となる自由旋回入力。</returns>
    private float GetFreeSteeringInput(Vector2 input)
    {
        return
            m_parameterAsset.GetSmoothedSteeringInput(input) *
            m_parameterAsset.DriftForwardTurnRate;
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

        // 自由旋回中は入力方向へ直接曲がり、方向固定後のみ従来の膨らむ軌道を使用します。
        float turnRate = 1.0f;
        if (m_driftSide != 0)
        {
            float progress = Mathf.Clamp01(
                m_driftElapsedTime / m_parameterAsset.DriftOutwardDuration);
            turnRate = Mathf.Lerp(
                -m_parameterAsset.DriftOutwardTurnRate,
                1.0f,
                Mathf.SmoothStep(0.0f, 1.0f, progress));
            m_driftElapsedTime += Time.fixedDeltaTime;
        }

        float turnInput = m_currentDriftSteering;
        float turnAngle = turnInput * turnSpeedDegreesPerSecond * turnRate * Time.fixedDeltaTime;
        m_currentVelocityDirection = (Quaternion.AngleAxis(turnAngle, Vector3.up)
            * m_currentVelocityDirection).normalized;
    }

    /// <summary>
    /// 入力状態に応じたチャージ中の実移動方向を取得します。
    /// 逆入力中は旋回方向を変えず、移動方向だけを旋回外側へ傾けます。
    /// </summary>
    /// <param name="input">正規化された移動入力。</param>
    /// <returns>チャージ中の実移動に使用する正規化済み方向。</returns>
    private Vector3 GetChargeMovementDirection(Vector2 input)
    {
        int inputSide = ClassifyChargeInput(input);
        bool isCounterSteering =
            m_driftSide != 0 &&
            inputSide == -m_driftSide;

        if (!isCounterSteering)
        {
            return m_currentVelocityDirection;
        }

        // 右旋回中は左、左旋回中は右へ傾け、常にカーブ外側へ大きく移動させます。
        float outwardAngle =
            -m_driftSide *
            m_parameterAsset.CounterInputMovementAngle;

        return (Quaternion.AngleAxis(outwardAngle, Vector3.up) *
            m_currentVelocityDirection).normalized;
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
        if (m_driftSide == 0)
        {
            float freeTargetAngle =
                m_currentDriftSteering *
                m_parameterAsset.MovingChargeSidewaysLookAngle;
            m_modelSlipAngle = Mathf.MoveTowards(m_modelSlipAngle, freeTargetAngle,
                m_parameterAsset.FacingRotationSpeed * Time.fixedDeltaTime);
            m_currentModelFacingDirection = Quaternion.AngleAxis(m_modelSlipAngle, Vector3.up)
                * m_currentVelocityDirection;
            return;
        }

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
}
