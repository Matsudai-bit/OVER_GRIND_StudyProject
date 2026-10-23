using System;
using UnityEngine;

/// <summary>
/// 1回分の直線突進を実行します。
/// </summary>
public sealed class StraightChargeExecutor
{
    /// <summary>
    /// 直線突進の内部実行状態です。
    /// </summary>
    private enum ExecutionPhase
    {
        IDLE,
        PREPARING,
        CHARGING,
        ENDING,
        COMPLETED,
        FAILED
    }

    // 方向ベクトルの有効判定閾値
    private const float DIRECTION_SQR_THRESHOLD =
        0.0001f;

    // 到達判定距離
    private const float REACH_DISTANCE =
        0.05f;

    // 所有するボス
    private readonly BossController m_owner;

    // 突進攻撃パラメータ
    private S1BossChargeAttackParameters m_parameters;

    // 目標位置取得処理
    private Func<Vector3> m_targetPositionProvider;

    // 現在の実行状態
    private ExecutionPhase m_executionPhase;

    // 現在フェーズの経過時間
    private float m_elapsedTime;

    // 突進方向
    private Vector3 m_chargeDirection;

    // 突進開始位置
    private Vector3 m_chargeStartPosition;

    // 突進終了位置
    private Vector3 m_chargeEndPosition;

    // 実際の突進距離
    private float m_chargeDistance;

    /// <summary>
    /// 行動フェーズが変更されたときに通知されます。
    /// </summary>
    public event Action<BossActionPhase>
        PhaseChanged;

    /// <summary>
    /// 突進が完了したか取得します。
    /// </summary>
    public bool IsCompleted =>
        m_executionPhase ==
        ExecutionPhase.COMPLETED;

    /// <summary>
    /// 突進が失敗したか取得します。
    /// </summary>
    public bool HasFailed =>
        m_executionPhase ==
        ExecutionPhase.FAILED;

    /// <summary>
    /// 直線突進実行機構を生成します。
    /// </summary>
    /// <param name="owner">
    /// 実行対象のボス。
    /// </param>
    public StraightChargeExecutor(
        BossController owner)
    {
        m_owner =
            owner;

        m_executionPhase =
            ExecutionPhase.IDLE;
    }

    /// <summary>
    /// 直線突進を開始します。
    /// </summary>
    public bool Start(
        S1BossChargeAttackParameters parameters,
        Func<Vector3> targetPositionProvider)
    {
        Cancel();

        if (m_owner == null ||
            m_owner.Motor == null ||
            m_owner.Navigation == null ||
            parameters == null ||
            targetPositionProvider == null)
        {
            m_executionPhase =
                ExecutionPhase.FAILED;

            return false;
        }

        if (!ValidateParameters(
                parameters))
        {
            m_executionPhase =
                ExecutionPhase.FAILED;

            return false;
        }

        m_parameters =
            parameters;

        m_targetPositionProvider =
            targetPositionProvider;

        ResetRuntimeValues();

        m_owner.Motor
            .StopHorizontalMovement();

        m_executionPhase =
            ExecutionPhase.PREPARING;

        NotifyPhaseChanged(
            BossActionPhase.PREPARATION);

        return true;
    }

    /// <summary>
    /// 直線突進を物理更新します。
    /// </summary>
    public void FixedUpdate(
        float deltaTime)
    {
        switch (m_executionPhase)
        {
            case ExecutionPhase.PREPARING:
                UpdatePreparation(
                    deltaTime);
                break;

            case ExecutionPhase.CHARGING:
                UpdateCharge(
                    deltaTime);
                break;

            case ExecutionPhase.ENDING:
                UpdateEnding(
                    deltaTime);
                break;
        }
    }

    /// <summary>
    /// 実行中の突進を中止します。
    /// </summary>
    public void Cancel()
    {
        if (m_owner != null)
        {
            m_owner.Motor?
                .StopHorizontalMovement();

            DisableHitbox();
        }

        m_parameters =
            null;

        m_targetPositionProvider =
            null;

        ResetRuntimeValues();

        m_executionPhase =
            ExecutionPhase.IDLE;
    }

    /// <summary>
    /// 予備動作を更新します。
    /// </summary>
    private void UpdatePreparation(
        float deltaTime)
    {
        if (!TryGetTargetDirection(
                out Vector3 direction))
        {
            SetFailed();

            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(
                direction,
                Vector3.up);

        m_owner.Motor.RotateTowards(
            targetRotation,
            m_parameters.RotationSpeed,
            deltaTime);

        m_elapsedTime +=
            deltaTime;

        if (m_elapsedTime <
            m_parameters.PreparationDuration)
        {
            return;
        }

        StartCharge();
    }

    /// <summary>
    /// 突進移動を開始します。
    /// </summary>
    private void StartCharge()
    {
        Vector3 startPosition =
            m_owner.transform.position;

        Vector3 targetPosition =
            m_targetPositionProvider.Invoke();

        /*
         * 実際の突進方向は予備動作終了時に
         * ボスが向いている方向で固定します。
         */
        m_chargeDirection =
            m_owner.transform.forward;

        m_chargeDirection.y =
            0.0f;

        if (m_chargeDirection.sqrMagnitude <=
            DIRECTION_SQR_THRESHOLD)
        {
            SetFailed();

            return;
        }

        m_chargeDirection.Normalize();

        Vector3 targetDifference =
            targetPosition -
            startPosition;

        targetDifference.y =
            0.0f;

        /*
         * Player位置を突進方向へ投影し、
         * その位置よりさらに指定距離先を理想停止地点とします。
         */
        float targetDistance =
            Vector3.Dot(
                targetDifference,
                m_chargeDirection);

        targetDistance =
            Mathf.Max(
                0.0f,
                targetDistance);

        float desiredChargeDistance =
            targetDistance +
            m_parameters.TargetOverrunDistance;

        // 攻撃そのものの最大距離で制限
        desiredChargeDistance =
            Mathf.Min(
                desiredChargeDistance,
                m_parameters.MaxChargeDistance);

        /*
         * NavMesh境界・移動可能範囲を考慮し、
         * 実際に進める距離まで短縮します。
         */
        float availableChargeDistance =
            m_owner.Navigation
                .GetMaxStraightMoveDistance(
                    m_chargeDirection,
                    desiredChargeDistance,
                    m_parameters.StopMargin);

        if (availableChargeDistance <=
            REACH_DISTANCE)
        {
            StartEnding();

            return;
        }

        m_chargeStartPosition =
            startPosition;

        m_chargeDistance =
            availableChargeDistance;

        m_chargeEndPosition =
            startPosition +
            m_chargeDirection *
            availableChargeDistance;

        m_elapsedTime =
            0.0f;

        EnableHitbox();

        m_executionPhase =
            ExecutionPhase.CHARGING;

        NotifyPhaseChanged(
            BossActionPhase.EXECUTION);
    }

    /// <summary>
    /// 突進移動を更新します。
    /// </summary>
    private void UpdateCharge(
        float deltaTime)
    {
        Vector3 currentPosition =
            m_owner.transform.position;

        float remainingDistance =
            GetHorizontalDistance(
                currentPosition,
                m_chargeEndPosition);

        if (remainingDistance <=
            REACH_DISTANCE)
        {
            CompleteChargeMovement();

            return;
        }

        m_elapsedTime +=
            deltaTime;

        float currentSpeed =
            CalculateChargeSpeed(
                remainingDistance);

        float moveDistance =
            currentSpeed *
            deltaTime;

        /*
         * 終点を超えないよう、
         * 1フレームの移動量を残距離までに制限します。
         */
        moveDistance =
            Mathf.Min(
                moveDistance,
                remainingDistance);

        Vector3 nextPosition =
            currentPosition +
            m_chargeDirection *
            moveDistance;

        m_owner.Motor.MovePosition(
            nextPosition);

        if (moveDistance >=
            remainingDistance)
        {
            CompleteChargeMovement();
        }
    }

    /// <summary>
    /// 現在の突進速度を計算します。
    /// </summary>
    private float CalculateChargeSpeed(
        float remainingDistance)
    {
        /*
         * 終点付近では加速状態よりも
         * 減速を優先します。
         */
        if (m_parameters.DecelerationDistance >
                0.0f &&
            remainingDistance <=
                m_parameters.DecelerationDistance)
        {
            return CalculateDecelerationSpeed(
                remainingDistance);
        }

        if (m_parameters.AccelerationDuration >
                0.0f &&
            m_elapsedTime <
                m_parameters.AccelerationDuration)
        {
            return CalculateAccelerationSpeed();
        }

        return m_parameters.ChargeSpeed;
    }

    /// <summary>
    /// 加速区間の速度を計算します。
    /// </summary>
    private float CalculateAccelerationSpeed()
    {
        if (m_parameters.AccelerationDuration <=
            0.0f)
        {
            return m_parameters.ChargeSpeed;
        }

        float normalizedTime =
            Mathf.Clamp01(
                m_elapsedTime /
                m_parameters.AccelerationDuration);

        float speedRate =
            EvaluateCurve(
                m_parameters.AccelerationCurve,
                normalizedTime);

        return m_parameters.ChargeSpeed *
               speedRate;
    }

    /// <summary>
    /// 減速区間の速度を計算します。
    /// </summary>
    private float CalculateDecelerationSpeed(
        float remainingDistance)
    {
        if (m_parameters.DecelerationDistance <=
            0.0f)
        {
            return m_parameters.ChargeSpeed;
        }

        /*
         * 減速開始地点で0、
         * 停止地点で1となる進捗率。
         */
        float normalizedProgress =
            1.0f -
            Mathf.Clamp01(
                remainingDistance /
                m_parameters.DecelerationDistance);

        float decelerationRate =
            EvaluateCurve(
                m_parameters.DecelerationCurve,
                normalizedProgress);

        return m_parameters.ChargeSpeed *
               (1.0f - decelerationRate);
    }

    /// <summary>
    /// 突進移動を終了します。
    /// </summary>
    private void CompleteChargeMovement()
    {
        m_owner.Motor.MovePosition(
            m_chargeEndPosition);

        StartEnding();
    }

    /// <summary>
    /// 終了処理を開始します。
    /// </summary>
    private void StartEnding()
    {
        m_owner.Motor
            .StopHorizontalMovement();

        DisableHitbox();

        m_elapsedTime =
            0.0f;

        m_executionPhase =
            ExecutionPhase.ENDING;

        NotifyPhaseChanged(
            BossActionPhase.RECOVERY);

        if (m_parameters.EndDuration <=
            0.0f)
        {
            SetCompleted();
        }
    }

    /// <summary>
    /// 終了状態を更新します。
    /// </summary>
    private void UpdateEnding(
        float deltaTime)
    {
        m_elapsedTime +=
            deltaTime;

        if (m_elapsedTime <
            m_parameters.EndDuration)
        {
            return;
        }

        SetCompleted();
    }

    /// <summary>
    /// 現在の目標方向を取得します。
    /// </summary>
    private bool TryGetTargetDirection(
        out Vector3 direction)
    {
        direction =
            m_targetPositionProvider.Invoke() -
            m_owner.transform.position;

        direction.y =
            0.0f;

        if (direction.sqrMagnitude <=
            DIRECTION_SQR_THRESHOLD)
        {
            direction =
                Vector3.zero;

            return false;
        }

        direction.Normalize();

        return true;
    }

    /// <summary>
    /// AnimationCurveを評価します。
    /// </summary>
    private float EvaluateCurve(
        AnimationCurve curve,
        float normalizedTime)
    {
        if (curve == null)
        {
            return normalizedTime;
        }

        return Mathf.Clamp01(
            curve.Evaluate(
                Mathf.Clamp01(
                    normalizedTime)));
    }

    /// <summary>
    /// 水平方向の距離を取得します。
    /// </summary>
    private float GetHorizontalDistance(
        Vector3 from,
        Vector3 to)
    {
        Vector3 difference =
            to - from;

        difference.y =
            0.0f;

        return difference.magnitude;
    }

    /// <summary>
    /// パラメータが有効か確認します。
    /// </summary>
    private bool ValidateParameters(
        S1BossChargeAttackParameters parameters)
    {
        return
            parameters.ChargeSpeed > 0.0f &&
            parameters.MaxChargeDistance > 0.0f &&
            parameters.PreparationDuration >= 0.0f &&
            parameters.RotationSpeed >= 0.0f &&
            parameters.StopMargin >= 0.0f &&
            parameters.EndDuration >= 0.0f &&
            parameters.AccelerationDuration >= 0.0f &&
            parameters.DecelerationDistance >= 0.0f &&
            parameters.TargetOverrunDistance >= 0.0f;
    }

    /// <summary>
    /// Runtime値を初期化します。
    /// </summary>
    private void ResetRuntimeValues()
    {
        m_elapsedTime =
            0.0f;

        m_chargeDirection =
            Vector3.zero;

        m_chargeStartPosition =
            Vector3.zero;

        m_chargeEndPosition =
            Vector3.zero;

        m_chargeDistance =
            0.0f;
    }

    /// <summary>
    /// 突進Hitboxを有効にします。
    /// </summary>
    private void EnableHitbox()
    {
        if (m_parameters?.AttackIdentifier ==
            null)
        {
            return;
        }

        m_owner.EnableAttackHitboxes(
            m_parameters.AttackIdentifier);
    }

    /// <summary>
    /// 突進Hitboxを無効にします。
    /// </summary>
    private void DisableHitbox()
    {
        if (m_parameters?.AttackIdentifier ==
            null)
        {
            return;
        }

        m_owner.AttackHitboxRegistry?
            .DisableHitboxes(
                m_parameters.AttackIdentifier);
    }

    /// <summary>
    /// 行動フェーズ変更を通知します。
    /// </summary>
    private void NotifyPhaseChanged(
        BossActionPhase phase)
    {
        PhaseChanged?.Invoke(
            phase);
    }

    /// <summary>
    /// 突進を完了状態にします。
    /// </summary>
    private void SetCompleted()
    {
        m_executionPhase =
            ExecutionPhase.COMPLETED;
    }

    /// <summary>
    /// 突進を失敗状態にします。
    /// </summary>
    private void SetFailed()
    {
        m_owner?.Motor?
            .StopHorizontalMovement();

        DisableHitbox();

        m_executionPhase =
            ExecutionPhase.FAILED;
    }
}