using System;
using UnityEngine;

/// <summary>
/// ボスがNavMesh上で占有する範囲を表します。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public sealed class BossNavMeshFootprint :
    MonoBehaviour
{
    // 判定範囲として使用するBoxCollider
    [SerializeField]
    private BoxCollider m_footprintCollider;

    /// <summary>
    /// Inspector設定時に初期化します。
    /// </summary>
    private void Reset()
    {
        m_footprintCollider =
            GetComponent<BoxCollider>();

        if (m_footprintCollider != null)
        {
            m_footprintCollider.isTrigger =
                true;
        }
    }

    /// <summary>
    /// 現在位置での底面4隅を取得します。
    /// </summary>
    /// <returns>底面4隅のワールド座標。</returns>
    public Vector3[] GetWorldCorners()
    {
        if (m_footprintCollider == null)
        {
            return Array.Empty<Vector3>();
        }

        Vector3[] localCorners =
            CreateLocalBottomCorners();

        Vector3[] worldCorners =
            new Vector3[localCorners.Length];

        for (int i = 0;
             i < localCorners.Length;
             i++)
        {
            worldCorners[i] =
                m_footprintCollider.transform.TransformPoint(
                    localCorners[i]);
        }

        return worldCorners;
    }

    /// <summary>
    /// 移動基準が指定位置・回転にある場合の底面4隅を取得します。
    /// </summary>
    /// <param name="poseOrigin">移動基準Transform。</param>
    /// <param name="position">予測位置。</param>
    /// <param name="rotation">予測回転。</param>
    /// <returns>予測した底面4隅のワールド座標。</returns>
    public Vector3[] GetWorldCorners(
        Transform poseOrigin,
        Vector3 position,
        Quaternion rotation)
    {
        if (m_footprintCollider == null ||
            poseOrigin == null)
        {
            return Array.Empty<Vector3>();
        }

        Vector3[] currentWorldCorners =
            GetWorldCorners();

        Vector3[] predictedWorldCorners =
            new Vector3[currentWorldCorners.Length];

        Matrix4x4 predictedOriginMatrix =
            Matrix4x4.TRS(
                position,
                rotation,
                poseOrigin.lossyScale);

        for (int i = 0;
             i < currentWorldCorners.Length;
             i++)
        {
            Vector3 originLocalPosition =
                poseOrigin.InverseTransformPoint(
                    currentWorldCorners[i]);

            predictedWorldCorners[i] =
                predictedOriginMatrix.MultiplyPoint3x4(
                    originLocalPosition);
        }

        return predictedWorldCorners;
    }

    /// <summary>
    /// BoxCollider底面のローカル4隅を生成します。
    /// </summary>
    /// <returns>底面4隅のローカル座標。</returns>
    private Vector3[] CreateLocalBottomCorners()
    {
        Vector3 center =
            m_footprintCollider.center;

        Vector3 halfSize =
            m_footprintCollider.size *
            0.5f;

        float bottomY =
            center.y -
            halfSize.y;

        return new Vector3[]
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
    }
}
