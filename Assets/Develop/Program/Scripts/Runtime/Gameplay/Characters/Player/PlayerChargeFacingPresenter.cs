using UnityEngine;

/// <summary>
/// Animator更新後に、チャージ中のモデルを指定された進行方向へ向けます。
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerChargeFacingPresenter : MonoBehaviour
{
    private Vector3 m_facingDirection;
    private bool m_isOverrideActive;

    /// <summary>
    /// モデル方向の上書きを開始します。
    /// </summary>
    /// <param name="facingDirection">モデルを向けるワールド方向。</param>
    public void BeginFacingOverride(Vector3 facingDirection)
    {
        m_isOverrideActive = true;
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
    }

    /// <summary>
    /// AnimatorによるTransform更新後にモデルの向きを反映します。
    /// </summary>
    private void LateUpdate()
    {
        if (!m_isOverrideActive ||
            m_facingDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        transform.rotation =
            Quaternion.LookRotation(m_facingDirection, Vector3.up);
    }
}
