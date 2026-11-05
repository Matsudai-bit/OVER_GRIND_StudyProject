using System;
using UnityEngine;

/// <summary>設定範囲内で最も近い攻撃対象を選びます。</summary>
[Serializable]
public sealed class PlayerAttackTargetChecker
{
    private const float DIRECTION_SQR_THRESHOLD = 0.0001f;

    [SerializeField, Tooltip("検索半径・角度の設定。未指定時は向き補正を行いません。")]
    private PlayerAttackAssistParameterAsset m_parameterAsset;

    [SerializeField, Tooltip("弱点など、攻撃を向けたいオブジェクトを必要な数だけ登録します。各Transformの位置を狙います。")]
    private Transform[] m_targets = Array.Empty<Transform>();

    /// <summary>登録対象のうち検索範囲内で最も近い位置への水平な攻撃方向を取得します。</summary>
    /// <param name="owner">攻撃するPlayerのルート。</param>
    /// <param name="forward">攻撃開始前の正面方向。</param>
    /// <param name="direction">選択した対象への水平な方向。</param>
    /// <returns>true：対象あり。false：対象なし、設定なし、または検索方向・半径が無効。</returns>
    public bool TryGetDirection(Transform owner, Vector3 forward, out Vector3 direction)
    {
        direction = Vector3.zero;
        if (owner == null || m_parameterAsset == null || m_targets == null)
            return false;

        float searchRadius = m_parameterAsset.SearchRadius;
        float searchAngle = m_parameterAsset.SearchAngle;
        forward = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < DIRECTION_SQR_THRESHOLD || searchRadius <= 0.0f)
            return false;

        Vector3 origin = owner.position;
        float nearestDistance = float.PositiveInfinity;
        float searchRadiusSquared = searchRadius * searchRadius;
        // シーン内の弱点参照は、共有アセットではなくPlayerのInspectorで保持します。
        foreach (Transform target in m_targets)
        {
            if (target == null || !target.gameObject.activeInHierarchy || target.IsChildOf(owner))
                continue;

            Vector3 targetOffset = target.position - origin;
            float distance = targetOffset.sqrMagnitude;
            if (m_parameterAsset.UseColliderDistance)
            {
                // 大きい弱点は中心が遠くても表面に近ければ対象にします。
                // 子階層の別部位は検索せず、登録された部位自身だけを判定します。
                Collider[] colliders = target.GetComponents<Collider>();
                if (colliders.Length > 0)
                {
                    distance = float.PositiveInfinity;
                    foreach (Collider targetCollider in colliders)
                    {
                        if (!targetCollider.enabled) continue;
                        float surfaceDistance = (targetCollider.ClosestPoint(origin) - origin).sqrMagnitude;
                        distance = Mathf.Min(distance, surfaceDistance);
                    }
                }
            }
            if (distance > searchRadiusSquared || distance >= nearestDistance) continue;

            Vector3 offset = Vector3.ProjectOnPlane(targetOffset, Vector3.up);
            if (offset.sqrMagnitude < DIRECTION_SQR_THRESHOLD ||
                Vector3.Angle(forward, offset) > searchAngle * 0.5f)
                continue;

            nearestDistance = distance;
            direction = offset.normalized;
        }
        return direction.sqrMagnitude > 0.0f;
    }
}
