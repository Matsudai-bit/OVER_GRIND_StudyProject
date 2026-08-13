using Unity.VisualScripting;
using UnityEngine;

public class Wall : MonoBehaviour
{
    // Decal Projector を含むプレハブ
    public GameObject decalPrefab;

    // 接地面からのオフセット距離
    public float surfaceOffset = 0.01f;

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Hit");

        Vector3 hitPoint = other.ClosestPoint(transform.position);
        Vector3 hitNormal = transform.forward;

        // ★法線方向に少し手前にずらす
        Vector3 spawnPosition = hitPoint + hitNormal * surfaceOffset;

        Vector3 swingDirection = Vector3.right;
        Vector3 projectedSwing = Vector3.ProjectOnPlane(swingDirection, hitNormal).normalized;

        if (projectedSwing.sqrMagnitude < 0.001f)
        {
            projectedSwing = Vector3.up;
        }

        Quaternion decalRotation = Quaternion.LookRotation(hitNormal, projectedSwing);

        var decal = Instantiate(decalPrefab, spawnPosition, decalRotation);
        decal.transform.SetParent(transform);
    }
}
