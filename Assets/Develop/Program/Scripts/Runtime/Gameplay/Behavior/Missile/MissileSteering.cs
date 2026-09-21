using UnityEngine;

/// <summary>
/// ミサイルの操舵挙動を計算します。
/// </summary>
[DisallowMultipleComponent]
public sealed class MissileSteering : MonoBehaviour
{
    private const float DIRECTION_SQR_THRESHOLD = 0.0001f;

    // 使用する操舵パラメータ
    [SerializeField]
    private MissileSteeringParameters m_parameters;

    // 現在の操舵加速度
    private float m_currentSteeringAcceleration;

    // 前回の操舵方向
    private Vector3 m_previousSteeringDirection;

    /// <summary>
    /// 操舵パラメータを設定します。
    /// </summary>
    public void SetParameters(
        MissileSteeringParameters parameters)
    {
        m_parameters = parameters;

        ResetSteering();
    }

    /// <summary>
    /// 指定方向へ向かうための操舵加速度を計算します。
    /// </summary>
    public Vector3 CalculateSteering(
        Vector3 desiredDirection,
        float desiredSpeed,
        Vector3 currentVelocity)
    {
        if (m_parameters == null)
        {
            return Vector3.zero;
        }

        if (desiredDirection.sqrMagnitude <= DIRECTION_SQR_THRESHOLD)
        {
            DecelerateSteering();
            return Vector3.zero;
        }

        Vector3 desiredVelocity =
            desiredDirection.normalized * desiredSpeed;

        Vector3 steering =
            desiredVelocity - currentVelocity;

        if (steering.sqrMagnitude <= DIRECTION_SQR_THRESHOLD)
        {
            DecelerateSteering();
            return Vector3.zero;
        }

        Vector3 steeringDirection =
            steering.normalized;

        UpdateSteeringAcceleration(steeringDirection);

        Vector3 steeringAcceleration =
            steeringDirection * m_currentSteeringAcceleration;

        steeringAcceleration =
            AdjustVerticalSteering(steeringAcceleration);

        m_previousSteeringDirection =
            steeringDirection;

        return steeringAcceleration;
    }

    /// <summary>
    /// 操舵状態を初期化します。
    /// </summary>
    public void ResetSteering()
    {
        if (m_parameters == null)
        {
            return;
        }

        m_currentSteeringAcceleration =
            m_parameters.MinSteeringAcceleration;

        m_previousSteeringDirection =
            Vector3.zero;
    }

    /// <summary>
    /// 操舵方向に応じて操舵力を更新します。
    /// </summary>
    private void UpdateSteeringAcceleration(
        Vector3 steeringDirection)
    {
        if (m_previousSteeringDirection.sqrMagnitude
            <= DIRECTION_SQR_THRESHOLD)
        {
            m_currentSteeringAcceleration =
                m_parameters.MinSteeringAcceleration;

            return;
        }

        float directionDot = Vector3.Dot(
            m_previousSteeringDirection,
            steeringDirection);

        if (directionDot >= m_parameters.SameDirectionThreshold)
        {
            AccelerateSteering();
            return;
        }

        DecelerateSteering();
    }

    /// <summary>
    /// 操舵力を増加させます。
    /// </summary>
    private void AccelerateSteering()
    {
        m_currentSteeringAcceleration = Mathf.MoveTowards(
            m_currentSteeringAcceleration,
            m_parameters.MaxSteeringAcceleration,
            m_parameters.SteeringAccelerationRate
                * Time.fixedDeltaTime);
    }

    /// <summary>
    /// 操舵力を減少させます。
    /// </summary>
    private void DecelerateSteering()
    {
        m_currentSteeringAcceleration = Mathf.MoveTowards(
            m_currentSteeringAcceleration,
            m_parameters.MinSteeringAcceleration,
            m_parameters.SteeringDecelerationRate
                * Time.fixedDeltaTime);
    }

    /// <summary>
    /// 垂直方向の操舵力を調整します。
    /// </summary>
    private Vector3 AdjustVerticalSteering(
        Vector3 steeringAcceleration)
    {
        Vector3 verticalSteering =
            Vector3.Project(
                steeringAcceleration,
                Vector3.up);

        Vector3 horizontalSteering =
            steeringAcceleration - verticalSteering;

        verticalSteering *=
            m_parameters.VerticalSteeringRatio;

        return horizontalSteering + verticalSteering;
    }
}