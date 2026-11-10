using UnityEngine;

/// <summary>
/// ミサイルの物理移動を制御するクラスです。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class MissileMotor : MonoBehaviour
{
    private const float VELOCITY_SQR_THRESHOLD = 0.0001f;

    /// <summary>
    /// ミサイル発射時に再生するサウンド ID です。
    /// </summary>
    [Header("Audio Settings")]
    [SerializeField] private AudioID m_launchSoundId;

    /// <summary>
    /// ミサイル飛行中に追従再生するエンジン音のサウンド ID です。
    /// </summary>
    [SerializeField] private AudioID m_engineSoundId;

    /// <summary>
    /// Rigidbody コンポーネントの参照です。
    /// </summary>
    private Rigidbody m_rigidbody;

    /// <summary>
    /// ミサイルの移動パラメータです。
    /// </summary>
    private MissileMotorParameters m_parameters;

    /// <summary>
    /// 現在の目標速度です。
    /// </summary>
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

    private void OnDisable()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.Stop3DFollowing(transform);
        }
    }

    private void OnDestroy()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.Stop3DFollowing(transform);
        }
    }

    /// <summary>
    /// 移動パラメータを設定します。
    /// </summary>
    /// <param name="parameters">ミサイルの移動パラメータ。</param>
    public void SetParameters(MissileMotorParameters parameters)
    {
        m_parameters = parameters;
        m_currentSpeed = parameters.InitialSpeed;
    }

    /// <summary>
    /// 指定方向へ初速度を与え、発射音および飛行エンジン音を再生します。
    /// </summary>
    /// <param name="direction">発射方向。</param>
    public void Launch(Vector3 direction)
    {
        if (m_parameters == null || direction.sqrMagnitude <= VELOCITY_SQR_THRESHOLD)
        {
            return;
        }

        m_currentSpeed = m_parameters.InitialSpeed;
        m_rigidbody.linearVelocity = direction.normalized * m_currentSpeed;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.Play3DAtPosition(m_launchSoundId, transform.position);
            AudioManager.Instance.Play3DFollowing(m_engineSoundId, transform);
        }
    }

    /// <summary>
    /// 指定された加速度を適用します。
    /// </summary>
    /// <param name="acceleration">適用する加速度。</param>
    public void AddAcceleration(Vector3 acceleration)
    {
        m_rigidbody.AddForce(acceleration, ForceMode.Acceleration);
    }

    private void UpdateSpeed()
    {
        m_currentSpeed = Mathf.MoveTowards(
            m_currentSpeed,
            m_parameters.MaxSpeed,
            m_parameters.Acceleration * Time.fixedDeltaTime);
    }

    private void ApplyGravity()
    {
        Vector3 gravityAcceleration = Vector3.down * m_parameters.GravityAcceleration;
        AddAcceleration(gravityAcceleration);
    }

    private void LimitSpeed()
    {
        Vector3 velocity = m_rigidbody.linearVelocity;
        float maxSpeed = m_parameters.MaxSpeed;

        if (velocity.sqrMagnitude <= maxSpeed * maxSpeed)
        {
            return;
        }

        m_rigidbody.linearVelocity = velocity.normalized * maxSpeed;
    }

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