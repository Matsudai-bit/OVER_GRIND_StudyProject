using UnityEngine;

/// <summary>
/// Animator更新後に、チャージ中のモデルを指定された進行方向へ向けます。
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerChargeFacingPresenter : MonoBehaviour
{
    private const float ROTATION_COMPLETE_ANGLE = 0.1f;

    private Vector3 m_facingDirection;
    private bool m_isOverrideActive;
    private bool m_isReturningToDefault;
    private float m_returnRotationSpeed;
    private Quaternion m_displayedWorldRotation;
    private Quaternion m_defaultLocalRotation;

    /// <summary>
    /// モデル方向の上書きを開始します。
    /// </summary>
    /// <param name="facingDirection">モデルを向けるワールド方向。</param>
    public void BeginFacingOverride(Vector3 facingDirection)
    {
        m_isOverrideActive = true;
        m_isReturningToDefault = false;
        m_displayedWorldRotation = transform.rotation;
        SetFacingDirection(facingDirection);
    }

    /// <summary>
    /// モデルを向けるワールド方向を更新します。
    /// </summary>
    /// <param name="facingDirection">モデルを向けるワールド方向。</param>
    public void SetFacingDirection(Vector3 facingDirection)
    {
        facingDirection.y = 0.0f;
        if (facingDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        m_facingDirection = facingDirection.normalized;
    }

    /// <summary>
    /// モデル方向の上書きを終了します。
    /// </summary>
    public void EndFacingOverride()
    {
        m_isOverrideActive = false;
        m_isReturningToDefault = false;
    }

    /// <summary>
    /// チャージ中の表示角度からチャージ開始前の通常角度へ滑らかに戻します。
    /// </summary>
    /// <param name="defaultLocalRotation">チャージ開始前の基準ローカル角度。</param>
    /// <param name="rotationSpeed">通常角度へ戻す回転速度（度/秒）。</param>
    public void EndFacingOverrideSmoothly(
        Quaternion defaultLocalRotation,
        float rotationSpeed)
    {
        m_isOverrideActive = false;
        m_defaultLocalRotation = defaultLocalRotation;
        m_returnRotationSpeed = Mathf.Max(0.0f, rotationSpeed);

        if (Mathf.Approximately(m_returnRotationSpeed, 0.0f))
        {
            m_isReturningToDefault = false;
            transform.localRotation = m_defaultLocalRotation;
            m_displayedWorldRotation = transform.rotation;
            return;
        }

        m_isReturningToDefault = true;
    }

    /// <summary>
    /// AnimatorによるTransform更新後にモデルの向きを反映します。
    /// </summary>
    private void LateUpdate()
    {
        if (m_isOverrideActive)
        {
            if (m_facingDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            m_displayedWorldRotation =
                Quaternion.LookRotation(m_facingDirection, Vector3.up);
            transform.rotation = m_displayedWorldRotation;
            return;
        }

        if (!m_isReturningToDefault)
        {
            return;
        }

        Quaternion defaultWorldRotation =
            transform.parent != null
                ? transform.parent.rotation * m_defaultLocalRotation
                : m_defaultLocalRotation;

        m_displayedWorldRotation =
            Quaternion.RotateTowards(
                m_displayedWorldRotation,
                defaultWorldRotation,
                m_returnRotationSpeed * Time.deltaTime);

        transform.rotation = m_displayedWorldRotation;

        if (Quaternion.Angle(
                m_displayedWorldRotation,
                defaultWorldRotation) <= ROTATION_COMPLETE_ANGLE)
        {
            m_isReturningToDefault = false;
            transform.localRotation = m_defaultLocalRotation;
        }
    }
}
