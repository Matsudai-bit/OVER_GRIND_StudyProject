using UnityEngine;

/// <summary>
/// ボスの実際の移動・回転から
/// 左右キャタピラーAnimationの速度を制御します。
/// </summary>
[DisallowMultipleComponent]
public sealed class BossCaterpillarAnimationPresenter :
    MonoBehaviour
{
    private const string LEFT_SPEED_PARAMETER_NAME =
        "Caterpillar_L_Speed";

    private const string RIGHT_SPEED_PARAMETER_NAME =
        "Caterpillar_R_Speed";

    private static readonly int LEFT_SPEED_PARAMETER_ID =
        Animator.StringToHash(
            LEFT_SPEED_PARAMETER_NAME);

    private static readonly int RIGHT_SPEED_PARAMETER_ID =
        Animator.StringToHash(
            RIGHT_SPEED_PARAMETER_NAME);

    [SerializeField, Header("参照")]
    private Animator m_animator;

    [SerializeField]
    private Transform m_motionOrigin;

    [SerializeField, Header("基準速度"), Min(0.01f)]
    private float m_referenceMoveSpeed = 5.0f;

    [SerializeField, Min(0.01f)]
    private float m_referenceTurnSpeed = 90.0f;

    [SerializeField, Header("Animation")]
    private float m_dampTime = 0.1f;

    // 前回位置
    private Vector3 m_previousPosition;

    // 前回回転
    private Quaternion m_previousRotation;

    // 初期化済みか
    private bool m_isInitialized;

    private void OnEnable()
    {
        InitializeMotionState();
    }

    private void FixedUpdate()
    {
        if (m_animator == null ||
            m_motionOrigin == null)
        {
            return;
        }

        if (!m_isInitialized)
        {
            InitializeMotionState();
            return;
        }

        UpdateCaterpillarAnimation(
            Time.fixedDeltaTime);

        CacheCurrentMotionState();
    }

    /// <summary>
    /// キャタピラーAnimationを更新します。
    /// </summary>
    /// <param name="deltaTime">
    /// 前回更新からの経過時間。
    /// </param>
    private void UpdateCaterpillarAnimation(
        float deltaTime)
    {
        if (deltaTime <= Mathf.Epsilon)
        {
            return;
        }

        float forwardSpeed =
            CalculateForwardSpeed(
                deltaTime);

        float turnSpeed =
            CalculateTurnSpeed(
                deltaTime);

        float normalizedForwardSpeed =
            Mathf.Clamp(
                forwardSpeed /
                m_referenceMoveSpeed,
                -1.0f,
                1.0f);

        float normalizedTurnSpeed =
            Mathf.Clamp(
                turnSpeed /
                m_referenceTurnSpeed,
                -1.0f,
                1.0f);

        float leftSpeed =
            Mathf.Clamp(
                normalizedForwardSpeed +
                normalizedTurnSpeed,
                -1.0f,
                1.0f);

        float rightSpeed =
            Mathf.Clamp(
                normalizedForwardSpeed -
                normalizedTurnSpeed,
                -1.0f,
                1.0f);

        ApplyAnimatorParameters(
            leftSpeed,
            rightSpeed,
            deltaTime);
    }

    /// <summary>
    /// 前後方向の移動速度を計算します。
    /// </summary>
    private float CalculateForwardSpeed(
        float deltaTime)
    {
        Vector3 displacement =
            m_motionOrigin.position -
            m_previousPosition;

        Vector3 velocity =
            displacement /
            deltaTime;

        return Vector3.Dot(
            velocity,
            m_motionOrigin.forward);
    }

    /// <summary>
    /// Y軸方向の回転速度を計算します。
    /// </summary>
    private float CalculateTurnSpeed(
        float deltaTime)
    {
        Vector3 previousForward =
            m_previousRotation *
            Vector3.forward;

        Vector3 currentForward =
            m_motionOrigin.forward;

        previousForward.y = 0.0f;
        currentForward.y = 0.0f;

        if (previousForward.sqrMagnitude <=
                Mathf.Epsilon ||
            currentForward.sqrMagnitude <=
                Mathf.Epsilon)
        {
            return 0.0f;
        }

        float angleDelta =
            Vector3.SignedAngle(
                previousForward,
                currentForward,
                Vector3.up);

        return angleDelta /
               deltaTime;
    }

    /// <summary>
    /// Animatorへ左右キャタピラー速度を適用します。
    /// </summary>
    private void ApplyAnimatorParameters(
        float leftSpeed,
        float rightSpeed,
        float deltaTime)
    {
        m_animator.SetFloat(
            LEFT_SPEED_PARAMETER_ID,
            leftSpeed,
            m_dampTime,
            deltaTime);

        m_animator.SetFloat(
            RIGHT_SPEED_PARAMETER_ID,
            rightSpeed,
            m_dampTime,
            deltaTime);
    }

    /// <summary>
    /// 現在のTransform状態を保持します。
    /// </summary>
    private void CacheCurrentMotionState()
    {
        m_previousPosition =
            m_motionOrigin.position;

        m_previousRotation =
            m_motionOrigin.rotation;
    }

    /// <summary>
    /// 移動監視状態を初期化します。
    /// </summary>
    private void InitializeMotionState()
    {
        if (m_motionOrigin == null)
        {
            m_isInitialized = false;
            return;
        }

        CacheCurrentMotionState();

        m_isInitialized = true;
    }
}