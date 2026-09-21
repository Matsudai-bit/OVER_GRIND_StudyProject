using UnityEngine;

/// <summary>
/// S1P1で使用するホーミングミサイルを制御します。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class S1P1MissileController : MonoBehaviour
{
    // 方向ベクトルが有効か判定する閾値
    private const float DIRECTION_SQR_THRESHOLD = 0.0001f;

    [SerializeField, Header("追尾対象")]
    private Transform m_target;

    [SerializeField, Header("移動")]
    private float m_initialSpeed = 5.0f;

    [SerializeField]
    private float m_maxSpeed = 20.0f;

    [SerializeField, Min(0.0f)]
    private float m_acceleration = 10.0f;

    [SerializeField, Header("操舵")]
    private float m_minSteeringAcceleration = 3.0f;

    [SerializeField]
    private float m_maxSteeringAcceleration = 20.0f;

    [SerializeField, Min(0.0f)]
    private float m_steeringAccelerationRate = 8.0f;

    [SerializeField, Min(0.0f)]
    private float m_steeringDecelerationRate = 20.0f;

    [SerializeField, Range(-1.0f, 1.0f)]
    private float m_sameSteeringDirectionThreshold = 0.8f;

    [SerializeField, Range(0.0f, 1.0f)]
    private float m_verticalSteeringRatio = 0.3f;

    [SerializeField, Header("重力")]
    private float m_gravityAcceleration = 3.0f;

    [SerializeField, Header("回転")]
    private float m_rotationSpeed = 360.0f;

    // Rigidbody
    private Rigidbody m_rigidbody;

    // 現在の推進目標速度
    private float m_currentSpeed;

    // 現在の操舵加速度
    private float m_currentSteeringAcceleration;

    // 前回の操舵方向
    private Vector3 m_previousSteeringDirection;

    // ホーミング中か
    private bool m_isHoming;

    private void Awake()
    {
        m_rigidbody = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        // Unity標準の重力は使用しない
        m_rigidbody.useGravity = false;

        // 推進速度を初期化
        m_currentSpeed = m_initialSpeed;

        // 操舵力を初期化
        m_currentSteeringAcceleration =
            m_minSteeringAcceleration;

        m_previousSteeringDirection = Vector3.zero;

        // 発射方向へ初速度を与える
        m_rigidbody.linearVelocity =
            transform.forward * m_initialSpeed;

        m_isHoming = true;
    }

    private void FixedUpdate()
    {
        ApplyGravity();
        UpdateSpeed();

        if (!m_isHoming || m_target == null)
        {
            RotateTowardsVelocity();
            return;
        }

        ApplySteering();
        RotateTowardsVelocity();
    }

    /// <summary>
    /// 追尾対象を設定します。
    /// </summary>
    public void SetTarget(Transform target)
    {
        m_target = target;
    }

    /// <summary>
    /// ホーミングを開始します。
    /// </summary>
    public void StartHoming()
    {
        m_isHoming = true;

        ResetSteeringAcceleration();
    }

    /// <summary>
    /// ホーミングを停止します。
    /// </summary>
    public void StopHoming()
    {
        m_isHoming = false;

        ResetSteeringAcceleration();
    }

    /// <summary>
    /// 推進速度を最大速度まで加速させます。
    /// </summary>
    private void UpdateSpeed()
    {
        m_currentSpeed = Mathf.MoveTowards(
            m_currentSpeed,
            m_maxSpeed,
            m_acceleration * Time.fixedDeltaTime);
    }

    /// <summary>
    /// ミサイル固有の重力加速度を適用します。
    /// </summary>
    private void ApplyGravity()
    {
        Vector3 gravityAcceleration =
            Vector3.down * m_gravityAcceleration;

        m_rigidbody.AddForce(
            gravityAcceleration,
            ForceMode.Acceleration);
    }

    /// <summary>
    /// ターゲットへ向かう操舵力を計算して適用します。
    /// </summary>
    private void ApplySteering()
    {
        Vector3 toTarget =
            m_target.position - m_rigidbody.position;

        if (toTarget.sqrMagnitude <= DIRECTION_SQR_THRESHOLD)
        {
            return;
        }

        // ターゲットへ向かう理想速度
        Vector3 desiredVelocity =
            toTarget.normalized * m_currentSpeed;

        // 現在速度との差から必要な操舵方向を求める
        Vector3 steering =
            desiredVelocity - m_rigidbody.linearVelocity;

        if (steering.sqrMagnitude <= DIRECTION_SQR_THRESHOLD)
        {
            DecelerateSteering();
            return;
        }

        Vector3 steeringDirection =
            steering.normalized;

        // 同じ方向への旋回継続時間に応じて操舵力を更新
        UpdateSteeringAcceleration(steeringDirection);

        // 現在の操舵力を使用して操舵加速度を生成
        Vector3 steeringAcceleration =
            steeringDirection * m_currentSteeringAcceleration;

        // 上下方向の操舵性能を調整
        steeringAcceleration =
            AdjustVerticalSteering(steeringAcceleration);

        m_rigidbody.AddForce(
            steeringAcceleration,
            ForceMode.Acceleration);

        m_previousSteeringDirection =
            steeringDirection;
    }

    /// <summary>
    /// 操舵方向に応じて現在の操舵加速度を更新します。
    /// </summary>
    private void UpdateSteeringAcceleration(
        Vector3 steeringDirection)
    {
        if (m_previousSteeringDirection.sqrMagnitude
            <= DIRECTION_SQR_THRESHOLD)
        {
            m_currentSteeringAcceleration =
                m_minSteeringAcceleration;

            return;
        }

        float directionDot = Vector3.Dot(
            m_previousSteeringDirection,
            steeringDirection);

        // 同じ方向へ旋回し続けている場合
        if (directionDot >= m_sameSteeringDirectionThreshold)
        {
            AccelerateSteering();
            return;
        }

        // 旋回方向が変化した場合
        DecelerateSteering();
    }

    /// <summary>
    /// 操舵力を最大値まで増加させます。
    /// </summary>
    private void AccelerateSteering()
    {
        m_currentSteeringAcceleration = Mathf.MoveTowards(
            m_currentSteeringAcceleration,
            m_maxSteeringAcceleration,
            m_steeringAccelerationRate * Time.fixedDeltaTime);
    }

    /// <summary>
    /// 操舵力を最小値まで減少させます。
    /// </summary>
    private void DecelerateSteering()
    {
        m_currentSteeringAcceleration = Mathf.MoveTowards(
            m_currentSteeringAcceleration,
            m_minSteeringAcceleration,
            m_steeringDecelerationRate * Time.fixedDeltaTime);
    }

    /// <summary>
    /// 上下方向の操舵力を調整します。
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

        verticalSteering *= m_verticalSteeringRatio;

        return horizontalSteering + verticalSteering;
    }

    /// <summary>
    /// 現在の進行方向へミサイルを回転させます。
    /// </summary>
    private void RotateTowardsVelocity()
    {
        Vector3 velocity = m_rigidbody.linearVelocity;

        if (velocity.sqrMagnitude <= DIRECTION_SQR_THRESHOLD)
        {
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(
                velocity.normalized,
                Vector3.up);

        Quaternion nextRotation =
            Quaternion.RotateTowards(
                m_rigidbody.rotation,
                targetRotation,
                m_rotationSpeed * Time.fixedDeltaTime);

        m_rigidbody.MoveRotation(nextRotation);
    }

    /// <summary>
    /// 操舵状態を初期化します。
    /// </summary>
    private void ResetSteeringAcceleration()
    {
        m_currentSteeringAcceleration =
            m_minSteeringAcceleration;

        m_previousSteeringDirection =
            Vector3.zero;
    }
}