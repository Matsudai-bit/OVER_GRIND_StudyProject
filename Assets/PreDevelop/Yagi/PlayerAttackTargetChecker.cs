using System;
using UnityEngine;

/// <summary>近距離かつ正面180度内の攻撃対象を選びます。</summary>
[Serializable]
public sealed class PlayerAttackTargetChecker
{
    [SerializeField, Min(0.0f), Tooltip("攻撃開始時に対象を探す距離です。")]
    private float m_searchRadius = 2.0f;

    [SerializeField, Tooltip("攻撃を向ける対象のColliderに設定したLayerです。")]
    private LayerMask m_targetLayers;

    /// <summary>正面半円内で最も近いColliderへの水平な攻撃方向を取得します。</summary>
    /// <param name="owner">攻撃するPlayerのルート。</param>
    /// <param name="forward">攻撃開始前の正面方向。</param>
    /// <param name="direction">選択した対象への水平な方向。</param>
    /// <returns>対象が見つかった場合はtrue。</returns>
    public bool TryGetDirection(Transform owner, Vector3 forward, out Vector3 direction)
    {
        direction = Vector3.zero;
        forward = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
        if (owner == null || forward.sqrMagnitude < 0.0001f || m_searchRadius <= 0.0f)
            return false;

        Vector3 origin = owner.position;
        float nearestDistance = float.PositiveInfinity;
        // 攻撃開始時だけ検索します。配列上限で近い対象を取り逃がさないよう全候補を取得します。
        Collider[] candidates = Physics.OverlapSphere(
            origin, m_searchRadius, m_targetLayers, QueryTriggerInteraction.Collide);
        foreach (Collider candidate in candidates)
        {
            if (candidate.transform.IsChildOf(owner)) continue;
            Vector3 offset = Vector3.ProjectOnPlane(candidate.bounds.center - origin, Vector3.up);
            if (offset.sqrMagnitude < 0.0001f || Vector3.Dot(forward, offset) < 0.0f)
                continue;

            float distance = (candidate.ClosestPoint(origin) - origin).sqrMagnitude;
            if (distance >= nearestDistance) continue;
            nearestDistance = distance;
            direction = offset.normalized;
        }
        return direction.sqrMagnitude > 0.0f;
    }
}
