using Unity.VisualScripting;
using UnityEngine;

public class Wall : MonoBehaviour
{
    // Decal Projector を含むプレハブ
    public GameObject decalPrefab; 

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Hit");

        // 衝突位置を取得
        Vector3 hitPoint = other.ClosestPoint(transform.position);

        // 壁の法線（向き）を取得
        Vector3 hitNormal = transform.forward;

        // デカール生成
        var decal = Instantiate(
            decalPrefab,
            hitPoint,
            Quaternion.LookRotation(hitNormal)
        );

        // 壁に追従させる
        decal.transform.SetParent(transform);
    }
}
