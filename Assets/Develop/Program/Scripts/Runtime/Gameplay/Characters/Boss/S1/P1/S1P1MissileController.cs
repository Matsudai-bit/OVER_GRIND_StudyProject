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
    [SerializeField, Header("ミサイルパラメータ")]
    private MissileParameterAsset m_parameterAsset;

    // ミサイル移動
    private MissileMotor m_motor;

    // 操舵制御
    private MissileSteering m_steering;

    // ホーミング制御
    private MissileHoming m_homing;

    // ホーミング中か
    private bool m_isHoming;

    private void Awake()
    {
        m_motor = GetComponent<MissileMotor>();
        m_steering = GetComponent<MissileSteering>();
        m_homing = GetComponent<MissileHoming>();

        ApplyParameters();
    }

    private void OnEnable()
    {
        m_isHoming = true;

        m_steering.ResetSteering();

        m_motor.Launch(transform.forward);
    }

    private void FixedUpdate()
    {
        if (!m_isHoming || !m_homing.HasTarget)
        {
            return;
        }

        UpdateHoming();
    }

    /// <summary>
    /// 追尾対象を設定します。
    /// </summary>
    public void SetTarget(Transform target)
    {
        m_homing.SetTarget(target);
    }

    /// <summary>
    /// ホーミングを開始します。
    /// </summary>
    public void StartHoming()
    {
        m_isHoming = true;

        m_steering.ResetSteering();
    }

    /// <summary>
    /// ホーミングを停止します。
    /// </summary>
    public void StopHoming()
    {
        m_isHoming = false;

        m_steering.ResetSteering();
    }

    /// <summary>
    /// ミサイルパラメータを各機能へ適用します。
    /// </summary>
    private void ApplyParameters()
    {
        if (m_parameterAsset == null)
        {
            Debug.LogError(
                $"{nameof(MissileParameterAsset)}が設定されていません。",
                this);

            return;
        }

        m_motor.SetParameters(
            m_parameterAsset.MotorParameters);

        m_steering.SetParameters(
            m_parameterAsset.SteeringParameters);
    }

    /// <summary>
    /// ホーミングによる操舵を更新します。
    /// </summary>
    private void UpdateHoming()
    {
        Vector3 desiredDirection =
            m_homing.GetDirection(transform.position);

        Vector3 steeringAcceleration =
            m_steering.CalculateSteering(
                desiredDirection,
                m_motor.CurrentSpeed,
                m_motor.Velocity);

        m_motor.AddAcceleration(
            steeringAcceleration);
    }

    /// <summary>
    /// 指定方向へ飛ばす
    /// </summary>
    /// <param name="direction">飛ばす方向</param>
    public void Launch(Vector3 direction)
    {
        m_motor.Launch(direction);
    }

}