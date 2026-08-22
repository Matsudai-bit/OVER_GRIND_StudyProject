using System.Collections.Generic;
using UnityEngine;

public class Wall : MonoBehaviour
{
    [SerializeField] private GameObject m_decalPrefab;
    [SerializeField] private float m_surfaceOffset = 0.02f;
    [SerializeField] private string m_weaponTag = "Chainsaw";
    [SerializeField] private bool m_parentDecalToSelf = true;
    [SerializeField] private float m_hitCooldown = 0.15f;

    public GameObject DecalPrefab => m_decalPrefab;
    public float SurfaceOffset => m_surfaceOffset;
    public string WeaponTag => m_weaponTag;

    // コライダーごとの直前位置と最終ヒット時刻（ジッター対策 & 振り方向推定用）
    private readonly Dictionary<int, Vector3> m_lastPositions = new();
    private readonly Dictionary<int, float> m_lastHitTime = new();

    private void OnTriggerEnter(Collider other)
    {
        if (other == null || !other.CompareTag(m_weaponTag))
        {
            return;
        }

        int id = other.GetInstanceID();

        // クールダウン判定（ジッターによる連続ヒット防止）
        if (m_lastHitTime.TryGetValue(id, out float lastTime) &&
            Time.time - lastTime < m_hitCooldown)
        {
            return;
        }
        m_lastHitTime[id] = Time.time;

        SpawnDecalFromTrigger(other, id);
    }

    private void SpawnDecalFromTrigger(Collider other, int id)
    {
        if (m_decalPrefab == null)
        {
            Debug.LogWarning($"{name}: DecalPrefab が設定されていません。", this);
            return;
        }

        Collider wallCollider = GetComponent<Collider>();
        Vector3 approxPoint = wallCollider != null
            ? wallCollider.ClosestPoint(other.bounds.center)
            : other.ClosestPoint(transform.position);

        // 直前フレームの武器位置（壁の外側にいるはず）から衝突点へ向けてRayを飛ばす
        Vector3 rayOrigin = m_lastPositions.TryGetValue(id, out Vector3 prevPos)
            ? prevPos
            : other.bounds.center;

        Vector3 rayDir = (approxPoint - rayOrigin).normalized;
        float rayDistance = Vector3.Distance(rayOrigin, approxPoint) + 0.3f;

        Vector3 hitPoint = approxPoint;
        Vector3 hitNormal = -rayDir; // フォールバック（Rayが外れた場合の保険）

        if (rayDir.sqrMagnitude > 0.0001f &&
            Physics.Raycast(rayOrigin, rayDir, out RaycastHit hit, rayDistance))
        {
            if (wallCollider == null || hit.collider == wallCollider)
            {
                hitPoint = hit.point;
                hitNormal = hit.normal;
            }
        }

        // 振り方向の推定は既存のまま
        Vector3 swingDirection = Vector3.up;
        if (m_lastPositions.TryGetValue(id, out Vector3 lastPos))
        {
            Vector3 delta = other.transform.position - lastPos;
            if (delta.sqrMagnitude > 0.0001f)
            {
                swingDirection = delta.normalized;
            }
        }
        m_lastPositions[id] = other.transform.position;

        SpawnDecal(hitPoint, hitNormal, swingDirection);
    }

    private void SpawnDecal(Vector3 hitPoint, Vector3 hitNormal, Vector3 swingDirection)
    {
        Vector3 spawnPosition = hitPoint + hitNormal * m_surfaceOffset;

        Vector3 projectedSwing = Vector3.ProjectOnPlane(swingDirection, hitNormal).normalized;
        if (projectedSwing.sqrMagnitude < 0.001f)
        {
            projectedSwing = Vector3.up;
        }

        Quaternion decalRotation = Quaternion.LookRotation(-hitNormal, projectedSwing);
        GameObject decal = Instantiate(m_decalPrefab, spawnPosition, decalRotation);

        if (m_parentDecalToSelf)
        {
            decal.transform.SetParent(transform);
        }
    }
}