using UnityEngine;

/// <summary>
/// レールでPlayerとの物理衝突制御に使用するCollider参照を保持します。
/// </summary>
[DisallowMultipleComponent]
public sealed class SplineRailCollisionSettings : MonoBehaviour
{
    [SerializeField, Header("衝突対象")]
    private Collider[] m_collisionColliders;

    /// <summary>
    /// Playerとの衝突制御に使用するColliderを取得します。
    /// </summary>
    public Collider[] CollisionColliders =>
        m_collisionColliders;

    /// <summary>
    /// 有効なColliderが1つ以上設定されているか確認します。
    /// </summary>
    /// <returns>true：1つ以上設定されています。</returns>
    public bool HasCollisionCollider()
    {
        if (m_collisionColliders == null ||
            m_collisionColliders.Length == 0)
        {
            return false;
        }

        foreach (Collider collisionCollider in
                 m_collisionColliders)
        {
            if (collisionCollider != null)
            {
                return true;
            }
        }

        return false;
    }
}
