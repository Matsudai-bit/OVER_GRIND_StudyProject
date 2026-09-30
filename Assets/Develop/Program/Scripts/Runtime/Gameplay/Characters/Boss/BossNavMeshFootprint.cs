using UnityEngine;

/// <summary>
/// ボスがNavMesh上で占有する範囲を表します。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public sealed class BossNavMeshFootprint : MonoBehaviour
{
    // 判定範囲として使用するBoxCollider
    [SerializeField]
    private BoxCollider m_footprintCollider;

    /// <summary>
    /// 初期化します。
    /// </summary>
    private void Reset()
    {
        m_footprintCollider = GetComponent<BoxCollider>();

        if (m_footprintCollider != null)
        {
            m_footprintCollider.isTrigger = true;
        }
    }

    /// <summary>
    /// NavMesh判定に使用する底面4隅のワールド座標を取得します。
    /// </summary>
    /// <returns>底面4隅のワールド座標。</returns>
    public Vector3[] GetWorldCorners()
    {
        if (m_footprintCollider == null)
        {
            return System.Array.Empty<Vector3>();
        }

        Vector3 center = m_footprintCollider.center;
        Vector3 halfSize = m_footprintCollider.size * 0.5f;

        float bottomY = center.y - halfSize.y;

        Vector3[] localCorners =
        {
            new(
                center.x - halfSize.x,
                bottomY,
                center.z + halfSize.z),

            new(
                center.x + halfSize.x,
                bottomY,
                center.z + halfSize.z),

            new(
                center.x + halfSize.x,
                bottomY,
                center.z - halfSize.z),

            new(
                center.x - halfSize.x,
                bottomY,
                center.z - halfSize.z)
        };

        Vector3[] worldCorners =
            new Vector3[localCorners.Length];

        for (int i = 0; i < localCorners.Length; i++)
        {
            worldCorners[i] =
                transform.TransformPoint(localCorners[i]);
        }

        return worldCorners;
    }
}