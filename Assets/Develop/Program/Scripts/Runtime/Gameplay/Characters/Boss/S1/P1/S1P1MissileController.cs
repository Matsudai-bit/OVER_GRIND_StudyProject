using UnityEngine;

/// <summary>
/// S1P1で使用するミサイルを制御します。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(MissileMotor))]
[RequireComponent(typeof(MissileSteering))]
[RequireComponent(typeof(MissileHoming))]
public sealed class S1P1MissileController : MonoBehaviour
{
    private enum MissileState
    {
        NONE,
        LAUNCH_UPWARD,
        HOMING
    }

    [SerializeField, Header("発射直後の上昇時間")]
    private float m_upwardDuration = 0.5f;

    // ミサイル移動
    private MissileMotor m_motor;

    // 操舵制御
    private MissileSteering m_steering;

    // ホーミング制御
    private MissileHoming m_homing;

    // 現在の状態
    private MissileState m_currentState;

    // 上昇経過時間
    private float m_upwardElapsedTime;

    // 初期化済みか
    private bool m_isInitialized;

    private MissileExplosion m_explosion;


    private void Awake()
    {
        m_motor = GetComponent<MissileMotor>();
        m_steering = GetComponent<MissileSteering>();
        m_homing = GetComponent<MissileHoming>();
        m_explosion = GetComponent<MissileExplosion>();

    }

    private void FixedUpdate()
    {
        if (!m_isInitialized)
        {
            return;
        }

        switch (m_currentState)
        {
            case MissileState.LAUNCH_UPWARD:
                UpdateUpwardLaunch();
                break;

            case MissileState.HOMING:
                UpdateHoming();
                break;
        }
    }

    /// <summary>
    /// ミサイルを初期化します。
    /// </summary>
    public void Initialize(
        MissileParameterAsset parameterAsset,
        Transform target,
        float upwardDuration)
    {
        if (parameterAsset == null)
        {
            Debug.LogError(
                $"{nameof(MissileParameterAsset)}がnullです。",
                this);

            return;
        }

        m_motor.SetParameters(
            parameterAsset.MotorParameters);

        m_steering.SetParameters(
            parameterAsset.SteeringParameters);

        m_homing.SetTarget(target);

        m_upwardDuration = upwardDuration;

        m_isInitialized = true;
    }

    /// <summary>
    /// ミサイルを発射します。
    /// </summary>
    public void Launch()
    {
        if (!m_isInitialized)
        {
            Debug.LogError(
                "ミサイルが初期化されていません。",
                this);

            return;
        }

        m_upwardElapsedTime = 0.0f;

        m_steering.ResetSteering();

        // 発射時に一度だけ上方向へ初速度を与える
        m_motor.Launch(Vector3.up);

        m_currentState =
            MissileState.LAUNCH_UPWARD;
    }

    /// <summary>
    /// 発射直後の上昇状態を更新します。
    /// </summary>
    private void UpdateUpwardLaunch()
    {
        m_upwardElapsedTime +=
            Time.fixedDeltaTime;

        if (m_upwardElapsedTime < m_upwardDuration)
        {
            return;
        }

        StartHoming();
    }

    /// <summary>
    /// ホーミングを開始します。
    /// </summary>
    private void StartHoming()
    {
        m_steering.ResetSteering();

        m_currentState =
            MissileState.HOMING;
    }

    /// <summary>
    /// ホーミングを更新します。
    /// </summary>
    private void UpdateHoming()
    {
        if (!m_homing.HasTarget)
        {
            return;
        }

        Vector3 desiredDirection =
            m_homing.GetDirection(
                transform.position);

        Vector3 steeringAcceleration =
            m_steering.CalculateSteering(
                desiredDirection,
                m_motor.CurrentSpeed,
                m_motor.Velocity);

        m_motor.AddAcceleration(
            steeringAcceleration);
    }

    private void OnCollisionEnter(Collision collision)
    {
        Explode();
    }

    private void Explode()
    {
        m_explosion.Explode(transform.parent);
    }
}