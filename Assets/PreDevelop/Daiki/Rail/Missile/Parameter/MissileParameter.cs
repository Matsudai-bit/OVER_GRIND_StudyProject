
using System;

using UnityEngine;

/// <summary>
/// ミサイルの移動パラメータです。
/// </summary>
[Serializable]
public sealed class MissileMotorParameters
{
    [SerializeField, Min(0.0f)]
    private float m_initialSpeed = 5.0f;

    [SerializeField, Min(0.0f)]
    private float m_maxSpeed = 25.0f;

    [SerializeField, Min(0.0f)]
    private float m_acceleration = 10.0f;

    [SerializeField, Min(0.0f)]
    private float m_gravityAcceleration = 0.6f;

    [SerializeField, Min(0.0f)]
    private float m_rotationSpeed = 360.0f;

    public float InitialSpeed => m_initialSpeed;
    public float MaxSpeed => m_maxSpeed;
    public float Acceleration => m_acceleration;
    public float GravityAcceleration => m_gravityAcceleration;
    public float RotationSpeed => m_rotationSpeed;
}

/// <summary>
/// ミサイルの操舵パラメータです。
/// </summary>
[Serializable]
public sealed class MissileSteeringParameters
{
    [SerializeField, Min(0.0f)]
    private float m_minSteeringAcceleration = 10.0f;

    [SerializeField, Min(0.0f)]
    private float m_maxSteeringAcceleration = 100.0f;

    [SerializeField, Min(0.0f)]
    private float m_steeringAccelerationRate = 9.0f;

    [SerializeField, Min(0.0f)]
    private float m_steeringDecelerationRate = 20.0f;

    [SerializeField, Range(-1.0f, 1.0f)]
    private float m_sameDirectionThreshold = 0.8f;

    [SerializeField, Range(0.0f, 1.0f)]
    private float m_verticalSteeringRatio = 1.0f;

    public float MinSteeringAcceleration =>
        m_minSteeringAcceleration;

    public float MaxSteeringAcceleration =>
        m_maxSteeringAcceleration;

    public float SteeringAccelerationRate =>
        m_steeringAccelerationRate;

    public float SteeringDecelerationRate =>
        m_steeringDecelerationRate;

    public float SameDirectionThreshold =>
        m_sameDirectionThreshold;

    public float VerticalSteeringRatio =>
        m_verticalSteeringRatio;
}