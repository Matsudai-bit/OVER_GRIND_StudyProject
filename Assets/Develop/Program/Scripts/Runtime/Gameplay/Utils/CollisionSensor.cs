using System;
using UnityEngine;

/// <summary>
/// 指定Layerとの衝突を検知して通知します。
/// </summary>
[DisallowMultipleComponent]
public sealed class CollisionSensor : MonoBehaviour
{
    // 衝突を検知するLayer
    [SerializeField]
    private LayerMask m_targetLayerMask;

    /// <summary>
    /// 対象との衝突を通知します。
    /// </summary>
    public event Action<Vector3> CollisionDetected;

    /// <summary>
    /// 衝突を検知します。
    /// </summary>
    /// <param name="collision">衝突情報。</param>
    private void OnCollisionEnter(Collision collision)
    {
        if (!IsTargetLayer(collision.gameObject.layer))
        {
            return;
        }

        if (collision.contactCount <= 0)
        {
            return;
        }

        ContactPoint contactPoint =
            collision.GetContact(0);

        CollisionDetected?.Invoke(
            contactPoint.point);
    }

    /// <summary>
    /// 対象Layerか確認します。
    /// </summary>
    /// <param name="layer">確認するLayer。</param>
    /// <returns>
    /// true：対象Layerです。
    /// false：対象Layerではありません。
    /// </returns>
    private bool IsTargetLayer(int layer)
    {
        return (m_targetLayerMask.value &
                (1 << layer)) != 0;
    }
}