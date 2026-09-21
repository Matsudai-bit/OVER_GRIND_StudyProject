using UnityEngine;

/// <summary>
/// ミサイルのホーミング対象を管理します。
/// </summary>
[DisallowMultipleComponent]
public sealed class MissileHoming : MonoBehaviour
{
    [SerializeField, Header("追尾対象")]
    private Transform m_target;

    /// <summary>
    /// 追尾対象が存在するか取得します。
    /// </summary>
    public bool HasTarget => m_target != null;

    /// <summary>
    /// 追尾対象を設定します。
    /// </summary>
    public void SetTarget(Transform target)
    {
        m_target = target;
    }

    /// <summary>
    /// 追尾対象を解除します。
    /// </summary>
    public void ClearTarget()
    {
        m_target = null;
    }

    /// <summary>
    /// 指定位置から追尾対象への方向を取得します。
    /// </summary>
    public Vector3 GetDirection(Vector3 currentPosition)
    {
        if (m_target == null)
        {
            return Vector3.zero;
        }

        return m_target.position - currentPosition;
    }
}