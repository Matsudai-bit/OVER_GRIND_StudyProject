using UnityEngine;

/// <summary>空中攻撃中、体の中心を軸にモデルを傾けて両足のチェーンソーを対象へ向けます。</summary>
[DisallowMultipleComponent]
public sealed class PlayerAirAttackPose : MonoBehaviour
{
    [SerializeField] private Transform m_model;
    [SerializeField] private CapsuleCollider m_body;
    [SerializeField] private Transform m_leftChain;
    [SerializeField] private Transform m_rightChain;
    private Transform m_target;
    private Collider m_surface;
    private Vector3 m_basePosition;
    private Quaternion m_baseRotation;
    private float m_maxPitch;
    private bool m_isAiming;
    private bool m_useSurface;

    /// <summary>攻撃直前の姿勢と、攻撃補正が選んだ弱点を保持します。</summary>
    public void BeginAim(Transform target, Collider surface, float maxPitch)
    {
        RestorePose();
        if (!isActiveAndEnabled || target == null || m_model == null || m_body == null ||
            m_leftChain == null || m_rightChain == null || maxPitch <= 0f) return;

        m_target = target;
        m_surface = surface;
        m_useSurface = surface != null;
        m_maxPitch = maxPitch;
        m_basePosition = m_model.localPosition;
        m_baseRotation = m_model.localRotation;
        m_isAiming = true;
        UpdateAim();
    }

    /// <summary>現在の足の位置から必要な角度を求め、姿勢補正の累積を防ぎます。</summary>
    public void UpdateAim()
    {
        if (!m_isAiming) return;
        if (m_target == null || !m_target.gameObject.activeInHierarchy ||
            (m_useSurface && (m_surface == null || !m_surface.enabled)))
        {
            RestorePose();
            return;
        }

        m_model.SetLocalPositionAndRotation(m_basePosition, m_baseRotation);
        Vector3 pivot = m_body.transform.TransformPoint(m_body.center);
        Vector3 feet = (m_leftChain.position + m_rightChain.position) * 0.5f;
        Vector3 aim = m_useSurface ? m_surface.ClosestPoint(pivot) : m_target.position;
        Vector3 axis = transform.right;
        Vector3 from = Vector3.ProjectOnPlane(feet - pivot, axis);
        Vector3 to = Vector3.ProjectOnPlane(aim - pivot, axis);
        // Collider内ではClosestPointが照会位置を返すため、中心方向を使います。
        if (to == Vector3.zero && m_useSurface)
            to = Vector3.ProjectOnPlane(m_surface.bounds.center - pivot, axis);
        if (from == Vector3.zero || to == Vector3.zero) return;

        float pitch = Mathf.Clamp(Vector3.SignedAngle(from, to, axis), -m_maxPitch, m_maxPitch);
        m_model.RotateAround(pivot, axis, pitch);
    }

    /// <summary>着地・攻撃終了時にモデルのローカル位置と回転を復元します。</summary>
    public void RestorePose()
    {
        if (!m_isAiming) return;
        if (m_model != null) m_model.SetLocalPositionAndRotation(m_basePosition, m_baseRotation);
        m_isAiming = false;
        m_target = null;
        m_surface = null;
    }

    /// <summary>無効化による中断でも補正を解除します。</summary>
    private void OnDisable() => RestorePose();
}
