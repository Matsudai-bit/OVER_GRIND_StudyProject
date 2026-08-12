using UnityEngine;

/// <summary>
/// ボスの物理移動と回転を実行します。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class BossMotor : MonoBehaviour
{
    // 回転完了判定の許容角度
    private const float ROTATION_ANGLE_THRESHOLD = 0.1f;

    // 移動に使用するRigidbody
    [SerializeField, Header("物理移動")]
    private Rigidbody m_rigidbody;

    /// <summary>
    /// Rigidbodyを取得します。
    /// </summary>
    public Rigidbody Rigidbody => m_rigidbody;

    /// <summary>
    /// 初期化します。
    /// </summary>
    private void Awake()
    {
        if (m_rigidbody == null)
        {
            m_rigidbody = GetComponent<Rigidbody>();
        }
    }

    /// <summary>
    /// ボスを前方向へ移動します。
    /// </summary>
    /// <param name="targetSpeed">目標速度。</param>
    /// <param name="acceleration">加速度。</param>
    /// <param name="deltaTime">物理フレームの経過時間。</param>
    public void MoveForward(
        float targetSpeed,
        float acceleration,
        float deltaTime)
    {
        if (m_rigidbody == null)
        {
            return;
        }

        float safeTargetSpeed = Mathf.Max(0.0f, targetSpeed);
        float safeAcceleration = Mathf.Max(0.0f, acceleration);

        Vector3 targetVelocity =
            transform.forward * safeTargetSpeed;

        Vector3 currentHorizontalVelocity = new Vector3(
            m_rigidbody.linearVelocity.x,
            0.0f,
            m_rigidbody.linearVelocity.z);

        Vector3 newHorizontalVelocity = Vector3.MoveTowards(
            currentHorizontalVelocity,
            targetVelocity,
            safeAcceleration * deltaTime);

        m_rigidbody.linearVelocity = new Vector3(
            newHorizontalVelocity.x,
            m_rigidbody.linearVelocity.y,
            newHorizontalVelocity.z);
    }

    /// <summary>
    /// 指定回転へ向けて回転します。
    /// </summary>
    /// <param name="targetRotation">目標回転。</param>
    /// <param name="rotateSpeed">1秒あたりの回転角度。</param>
    /// <param name="deltaTime">物理フレームの経過時間。</param>
    /// <returns>
    /// true：目標回転へ到達しました。
    /// false：回転中です。
    /// </returns>
    public bool RotateTowards(
        Quaternion targetRotation,
        float rotateSpeed,
        float deltaTime)
    {
        if (m_rigidbody == null)
        {
            return false;
        }

        float currentAngle = Quaternion.Angle(
            m_rigidbody.rotation,
            targetRotation);

        if (currentAngle <= ROTATION_ANGLE_THRESHOLD)
        {
            m_rigidbody.MoveRotation(targetRotation);
            return true;
        }

        Quaternion nextRotation = Quaternion.RotateTowards(
            m_rigidbody.rotation,
            targetRotation,
            Mathf.Max(0.0f, rotateSpeed) * deltaTime);

        m_rigidbody.MoveRotation(nextRotation);

        return Quaternion.Angle(
            nextRotation,
            targetRotation) <= ROTATION_ANGLE_THRESHOLD;
    }

    /// <summary>
    /// 水平方向の移動を停止します。
    /// </summary>
    public void StopHorizontalMovement()
    {
        if (m_rigidbody == null)
        {
            return;
        }

        m_rigidbody.linearVelocity = new Vector3(
            0.0f,
            m_rigidbody.linearVelocity.y,
            0.0f);
    }
}
