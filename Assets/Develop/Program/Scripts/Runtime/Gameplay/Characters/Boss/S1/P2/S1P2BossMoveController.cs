using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// S1P2ボスの生成済み経路に沿った操舵移動を制御します。
/// </summary>
[DisallowMultipleComponent]
public sealed class S1P2BossMoveController :
    MonoBehaviour
{
    // 移動基準
    [SerializeField, Header("参照")]
    private Transform m_movementRoot;

    // ボス移動
    [SerializeField]
    private BossMotor m_motor;

    // 使用中の経路
    private BossMoveRoute m_route;

    // 移動パラメータ
    private S1P2BossMoveStateParameters m_parameters;

    // 現在追跡している経路点Index
    private int m_currentSampleIndex;

    // 移動中か
    private bool m_isMoving;

    // 初期旋回中か
    private bool m_isInitialTurning;

    /// <summary>
    /// 移動が完了したか取得します。
    /// </summary>
    public bool IsCompleted { get; private set; }

    /// <summary>
    /// 移動に失敗したか取得します。
    /// </summary>
    public bool HasFailed { get; private set; }

    /// <summary>
    /// 移動中か取得します。
    /// </summary>
    public bool IsMoving =>
        m_isMoving;

    /// <summary>
    /// 経路移動を開始します。
    /// </summary>
    /// <param name="route">使用する移動経路。</param>
    /// <param name="parameters">移動パラメータ。</param>
    /// <returns>
    /// true：移動を開始しました。
    /// false：開始できませんでした。
    /// </returns>
    public bool StartMove(
     BossMoveRoute route,
     S1P2BossMoveStateParameters parameters)
    {
        ResetRuntimeState();

        if (m_movementRoot == null ||
            m_motor == null)
        {
            Debug.LogError(
                "ボス移動に必要な参照が設定されていません。",
                this);

            HasFailed = true;

            return false;
        }

        if (route == null ||
            !route.IsValid)
        {
            Debug.LogError(
                "有効な移動経路が設定されていません。",
                this);

            HasFailed = true;

            return false;
        }

        if (parameters == null)
        {
            Debug.LogError(
                "移動パラメータが設定されていません。",
                this);

            HasFailed = true;

            return false;
        }

        m_route =
            route;

        m_parameters =
            parameters;

        m_currentSampleIndex =
            Mathf.Min(
                1,
                m_route.SamplePoints.Count - 1);

        // 前Stateの速度を引き継がない
        m_motor.StopHorizontalMovement();

        // 最初はその場旋回から開始する
        m_isInitialTurning = true;

        m_isMoving = true;

        return true;
    }

    /// <summary>
    /// 経路移動を物理更新します。
    /// </summary>
    /// <param name="deltaTime">物理フレームの経過時間。</param>
    public void FixedUpdateMove(
     float deltaTime)
    {
        if (!m_isMoving ||
            IsCompleted ||
            HasFailed)
        {
            return;
        }

        if (!ValidateRuntimeState())
        {
            FailMove();

            return;
        }

        if (HasReachedDestination())
        {
            CompleteMove();

            return;
        }

        UpdateCurrentSampleIndex();

        Vector3 lookAheadPosition =
            GetLookAheadPosition();

        Vector3 desiredDirection =
            lookAheadPosition -
            m_movementRoot.position;

        desiredDirection.y =
            0.0f;

        if (desiredDirection.sqrMagnitude <=
            Mathf.Epsilon)
        {
            return;
        }

        desiredDirection.Normalize();

        Quaternion targetRotation =
            Quaternion.LookRotation(
                desiredDirection,
                Vector3.up);

        // 移動開始時だけ、その場で進行方向を向く
        if (m_isInitialTurning)
        {
            UpdateInitialTurn(
                targetRotation,
                deltaTime);

            return;
        }

        // 移動中の通常旋回
        m_motor.RotateTowards(
            targetRotation,
            m_parameters.RotationSpeed,
            deltaTime);

        float targetSpeed =
            CalculateTargetSpeed(
                desiredDirection);

        float speedChangeRate =
            targetSpeed <
            m_motor.HorizontalSpeed
                ? m_parameters.Deceleration
                : m_parameters.Acceleration;

        m_motor.MoveDirection(
            m_movementRoot.forward,
            targetSpeed,
            speedChangeRate,
            deltaTime);
    }

    /// <summary>
    /// 経路移動をキャンセルします。
    /// </summary>
    public void Cancel()
    {
        if (!m_isMoving)
        {
            return;
        }

        m_motor?.StopHorizontalMovement();

        m_isMoving = false;
        m_isInitialTurning = false;

        m_route = null;
        m_parameters = null;
    }

    /// <summary>
    /// 現在位置に応じて追跡中の経路点を前方へ進めます。
    /// </summary>
    private void UpdateCurrentSampleIndex()
    {
        IReadOnlyList<Vector3> samplePoints =
            m_route.SamplePoints;

        if (samplePoints == null ||
            samplePoints.Count == 0)
        {
            return;
        }

        /*
         * 現在Indexと次のIndexを比較し、
         * 次の点の方が近くなった場合のみ進めます。
         *
         * 経路の大きく先までIndexが飛ぶことを防ぎます。
         */
        while (m_currentSampleIndex <
               samplePoints.Count - 1)
        {
            float currentDistanceSqr =
                GetHorizontalDistanceSqr(
                    m_movementRoot.position,
                    samplePoints[m_currentSampleIndex]);

            float nextDistanceSqr =
                GetHorizontalDistanceSqr(
                    m_movementRoot.position,
                    samplePoints[m_currentSampleIndex + 1]);

            if (nextDistanceSqr >=
                currentDistanceSqr)
            {
                break;
            }

            m_currentSampleIndex++;
        }
    }

    /// <summary>
    /// 現在の経路位置から先読み地点を取得します。
    /// </summary>
    /// <returns>先読み地点。</returns>
    private Vector3 GetLookAheadPosition()
    {
        IReadOnlyList<Vector3> samplePoints =
            m_route.SamplePoints;

        float remainingLookAhead =
            m_parameters.LookAheadDistance;

        Vector3 previousPosition =
            m_movementRoot.position;

        for (int i = m_currentSampleIndex;
             i < samplePoints.Count;
             i++)
        {
            Vector3 currentPosition =
                samplePoints[i];

            float segmentLength =
                GetHorizontalDistance(
                    previousPosition,
                    currentPosition);

            if (segmentLength >=
                remainingLookAhead)
            {
                if (segmentLength <=
                    Mathf.Epsilon)
                {
                    return currentPosition;
                }

                float t =
                    remainingLookAhead /
                    segmentLength;

                return Vector3.Lerp(
                    previousPosition,
                    currentPosition,
                    t);
            }

            remainingLookAhead -=
                segmentLength;

            previousPosition =
                currentPosition;
        }

        return m_route.FinalPosition;
    }

    /// <summary>
    /// 終点までの残り経路長を取得します。
    /// </summary>
    /// <returns>残り経路長。</returns>
    private float CalculateRemainingRouteDistance()
    {
        IReadOnlyList<Vector3> samplePoints =
            m_route.SamplePoints;

        if (m_currentSampleIndex >=
            samplePoints.Count)
        {
            return 0.0f;
        }

        float remainingDistance =
            GetHorizontalDistance(
                m_movementRoot.position,
                samplePoints[m_currentSampleIndex]);

        for (int i = m_currentSampleIndex + 1;
             i < samplePoints.Count;
             i++)
        {
            remainingDistance +=
                GetHorizontalDistance(
                    samplePoints[i - 1],
                    samplePoints[i]);
        }

        return remainingDistance;
    }

    /// <summary>
    /// 終点までの距離と旋回角度から目標速度を計算します。
    /// </summary>
    /// <param name="desiredDirection">
    /// 経路追従で向かいたい方向。
    /// </param>
    /// <returns>現在の目標速度。</returns>
    private float CalculateTargetSpeed(
        Vector3 desiredDirection)
    {
        float remainingDistance =
            CalculateRemainingRouteDistance();

        // 到着範囲内では停止する
        if (remainingDistance <=
            m_parameters.ArrivalDistance)
        {
            return 0.0f;
        }

        float distanceTargetSpeed =
            CalculateDistanceTargetSpeed(
                remainingDistance);

        float turnSpeedMultiplier =
            CalculateTurnSpeedMultiplier(
                desiredDirection);

        return distanceTargetSpeed *
               turnSpeedMultiplier;
    }

    /// <summary>
    /// 最終地点へ到達したか確認します。
    /// </summary>
    /// <returns>
    /// true：到達しました。
    /// false：まだ到達していません。
    /// </returns>
    private bool HasReachedDestination()
    {
        float distanceSqr =
            GetHorizontalDistanceSqr(
                m_movementRoot.position,
                m_route.FinalPosition);

        float arrivalDistance =
            m_parameters.ArrivalDistance;

        return distanceSqr <=
            arrivalDistance *
            arrivalDistance;
    }

    /// <summary>
    /// 移動を正常終了します。
    /// </summary>
    private void CompleteMove()
    {
        m_motor.StopHorizontalMovement();

        m_isMoving = false;
        IsCompleted = true;
    }

    /// <summary>
    /// 移動を失敗終了します。
    /// </summary>
    private void FailMove()
    {
        m_motor?.StopHorizontalMovement();

        m_isMoving = false;
        HasFailed = true;
    }

    /// <summary>
    /// 実行中に必要な情報が有効か確認します。
    /// </summary>
    /// <returns>使用可能か。</returns>
    private bool ValidateRuntimeState()
    {
        return m_movementRoot != null &&
               m_motor != null &&
               m_route != null &&
               m_route.IsValid &&
               m_parameters != null;
    }

    /// <summary>
    /// 水平距離を取得します。
    /// </summary>
    private static float GetHorizontalDistance(
        Vector3 from,
        Vector3 to)
    {
        return Mathf.Sqrt(
            GetHorizontalDistanceSqr(
                from,
                to));
    }

    /// <summary>
    /// 水平距離の2乗を取得します。
    /// </summary>
    private static float GetHorizontalDistanceSqr(
        Vector3 from,
        Vector3 to)
    {
        float x =
            to.x - from.x;

        float z =
            to.z - from.z;

        return x * x +
               z * z;
    }

    /// <summary>
    /// 実行時状態を初期化します。
    /// </summary>
    private void ResetRuntimeState()
    {
        m_route = null;
        m_parameters = null;

        m_currentSampleIndex = 0;

        m_isMoving = false;
        m_isInitialTurning = false;

        IsCompleted = false;
        HasFailed = false;
    }

    /// <summary>
    /// Inspector設定時に参照を取得します。
    /// </summary>
    private void Reset()
    {
        BossController bossController =
            GetComponentInParent<BossController>();

        if (bossController == null)
        {
            return;
        }

        m_movementRoot =
            bossController.transform;

        m_motor =
            bossController.GetComponent<BossMotor>();
    }

    /// <summary>
    /// 終点までの残り距離から目標速度を計算します。
    /// </summary>
    /// <param name="remainingDistance">
    /// 終点までの残り経路長。
    /// </param>
    /// <returns>距離に応じた目標速度。</returns>
    private float CalculateDistanceTargetSpeed(
        float remainingDistance)
    {
        float deceleration =
            m_parameters.Deceleration;

        if (deceleration <=
            Mathf.Epsilon)
        {
            return m_parameters.MaxMoveSpeed;
        }

        /*
         * 最高速度から停止するために必要な距離。
         *
         * d = v^2 / 2a
         */
        float brakingDistance =
            m_parameters.MaxMoveSpeed *
            m_parameters.MaxMoveSpeed /
            (2.0f * deceleration);

        // まだ減速開始地点より遠ければ最高速度
        if (remainingDistance >
            brakingDistance +
            m_parameters.ArrivalDistance)
        {
            return m_parameters.MaxMoveSpeed;
        }

        float availableDistance =
            Mathf.Max(
                0.0f,
                remainingDistance -
                m_parameters.ArrivalDistance);

        float targetSpeed =
            Mathf.Sqrt(
                2.0f *
                deceleration *
                availableDistance);

        return Mathf.Clamp(
            targetSpeed,
            0.0f,
            m_parameters.MaxMoveSpeed);
    }

    /// <summary>
    /// 現在の向きと目標方向の角度から移動速度倍率を計算します。
    /// </summary>
    /// <param name="desiredDirection">
    /// 経路追従で向かいたい方向。
    /// </param>
    /// <returns>
    /// 0～1の移動速度倍率。
    /// </returns>
    private float CalculateTurnSpeedMultiplier(
        Vector3 desiredDirection)
    {
        Vector3 currentForward =
            new(
                m_movementRoot.forward.x,
                0.0f,
                m_movementRoot.forward.z);

        Vector3 horizontalDesiredDirection =
            new(
                desiredDirection.x,
                0.0f,
                desiredDirection.z);

        if (currentForward.sqrMagnitude <=
                Mathf.Epsilon ||
            horizontalDesiredDirection.sqrMagnitude <=
                Mathf.Epsilon)
        {
            return 1.0f;
        }

        currentForward.Normalize();
        horizontalDesiredDirection.Normalize();

        float turnAngle =
            Vector3.Angle(
                currentForward,
                horizontalDesiredDirection);

        float slowdownStartAngle =
            m_parameters.TurnSlowdownStartAngle;

        float turnInPlaceAngle =
            Mathf.Max(
                slowdownStartAngle,
                m_parameters.TurnInPlaceAngle);

        // 小さい旋回では通常速度を維持する
        if (turnAngle <=
            slowdownStartAngle)
        {
            return 1.0f;
        }

        // 大きく向きがズレている場合はその場で旋回する
        if (turnAngle >=
            turnInPlaceAngle)
        {
            return 0.0f;
        }

        float turnRate =
            Mathf.InverseLerp(
                slowdownStartAngle,
                turnInPlaceAngle,
                turnAngle);

        /*
         * 旋回角度が大きくなるほど
         * 1 → 0へ滑らかに速度を下げる。
         */
        return Mathf.SmoothStep(
            1.0f,
            0.0f,
            turnRate);
    }

    /// <summary>
    /// 移動開始時のその場旋回を更新します。
    /// </summary>
    /// <param name="targetRotation">最初の進行方向。</param>
    /// <param name="deltaTime">物理フレームの経過時間。</param>
    private void UpdateInitialTurn(
        Quaternion targetRotation,
        float deltaTime)
    {
        // 初期旋回中は必ず停止する
        m_motor.StopHorizontalMovement();

        bool hasCompletedTurn =
            m_motor.RotateTowards(
                targetRotation,
                m_parameters.RotationSpeed,
                deltaTime);

        if (!hasCompletedTurn)
        {
            return;
        }

        // 次のFixedUpdateから移動開始
        m_isInitialTurning = false;
    }
}