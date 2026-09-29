using UnityEngine;

/// <summary>
/// ミサイルの物理移動を制御します。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class MissileMotor : MonoBehaviour
{
    // 速度ベクトルが有効か判定する閾値
    private const float VELOCITY_SQR_THRESHOLD = 0.0001f;

    // Rigidbody
    private Rigidbody m_rigidbody;

    // 移動パラメータ
    private MissileMotorParameters m_parameters;

    // 現在の目標速度
    private float m_currentSpeed;

    /// <summary>
    /// 現在の速度を取得します。
    /// </summary>
    public Vector3 Velocity => m_rigidbody.linearVelocity;

    /// <summary>
    /// 現在の目標速度を取得します。
    /// </summary>
    public float CurrentSpeed => m_currentSpeed;

    private void Awake()
    {
        m_rigidbody = GetComponent<Rigidbody>();

        // 独自重力を使用する
        m_rigidbody.useGravity = false;
    }

    private void FixedUpdate()
    {
        if (m_parameters == null)
        {
            return;
        }

        UpdateSpeed();
        ApplyGravity();
        LimitSpeed();
        RotateTowardsVelocity();
    }

    /// <summary>
    /// 移動パラメータを設定します。
    /// </summary>
    public void SetParameters(MissileMotorParameters parameters)
    {
        m_parameters = parameters;
        m_currentSpeed = parameters.InitialSpeed;
    }

    /// <summary>
    /// 指定方向へ初速度を与えます。
    /// </summary>
    public void Launch(Vector3 direction)
    {
        if (m_parameters == null)
        {
            return;
        }

        if (direction.sqrMagnitude <= VELOCITY_SQR_THRESHOLD)
        {
            return;
        }

        m_currentSpeed = m_parameters.InitialSpeed;

        m_rigidbody.linearVelocity =
            direction.normalized * m_currentSpeed;
    }

    /// <summary>
    /// 指定された加速度を適用します。
    /// </summary>
    public void AddAcceleration(Vector3 acceleration)
    {
        m_rigidbody.AddForce(
            acceleration,
            ForceMode.Acceleration);
    }

    /// <summary>
    /// 目標速度を最大速度まで加速させます。
    /// </summary>
    private void UpdateSpeed()
    {
        m_currentSpeed = Mathf.MoveTowards(
            m_currentSpeed,
            m_parameters.MaxSpeed,
            m_parameters.Acceleration * Time.fixedDeltaTime);
    }

    /// <summary>
    /// ミサイル固有の重力を適用します。
    /// </summary>
    private void ApplyGravity()
    {
        Vector3 gravityAcceleration =
            Vector3.down * m_parameters.GravityAcceleration;

        AddAcceleration(gravityAcceleration);
    }

    /// <summary>
    /// 最大速度を超えないよう制限します。
    /// </summary>
    private void LimitSpeed()
    {
        Vector3 velocity = m_rigidbody.linearVelocity;
        float maxSpeed = m_parameters.MaxSpeed;

        if (velocity.sqrMagnitude <= maxSpeed * maxSpeed)
        {
            return;
        }

        m_rigidbody.linearVelocity =
            velocity.normalized * maxSpeed;
    }

    /// <summary>
    /// 現在の進行方向へミサイルを回転させます。
    /// </summary>
    private void RotateTowardsVelocity()
    {
        Vector3 velocity = m_rigidbody.linearVelocity;

        if (velocity.sqrMagnitude <= VELOCITY_SQR_THRESHOLD)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(
            velocity.normalized,
            Vector3.up);

        Quaternion nextRotation = Quaternion.RotateTowards(
            m_rigidbody.rotation,
            targetRotation,
            m_parameters.RotationSpeed * Time.fixedDeltaTime);

        m_rigidbody.MoveRotation(nextRotation);
    }
}