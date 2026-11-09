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

    /// <summary>現在の検索設定で許可する空中攻撃の傾きを取得します。</summary>
    public float AirMaxPitch => m_parameterAsset != null ? m_parameterAsset.AirMaxPitch : 0f;

    /// <summary>登録対象のうち検索範囲内で最も近い位置への水平な攻撃方向を取得します。</summary>
    /// <param name="owner">攻撃するPlayerのルート。</param>
    /// <param name="forward">攻撃開始前の正面方向。</param>
    /// <param name="direction">選択した対象への水平な方向。</param>
    /// <param name="isAirAttack">true：空中攻撃設定。false：地上攻撃設定。</param>
    /// <returns>true：対象あり。false：対象なし、設定なし、または検索方向・半径が無効。</returns>
    public bool TryGetDirection(Transform owner, Vector3 forward, out Vector3 direction, bool isAirAttack = false)
    {
        return TryGetTarget(owner, forward, isAirAttack, out direction, out _, out _);
    }

    /// <summary>同じ検索結果から水平進行方向と姿勢補正用の対象を取得します。</summary>
    public bool TryGetTarget(Transform owner, Vector3 forward, bool isAirAttack, out Vector3 direction,
        out Transform selectedTarget, out Collider selectedCollider)
    {
        direction = Vector3.zero;
        selectedTarget = null;
        selectedCollider = null;
        if (owner == null || m_parameterAsset == null || m_targets == null)
            return false;

        float searchRadius = m_parameterAsset.GetSearchRadius(isAirAttack);
        float searchAngle = m_parameterAsset.GetSearchAngle(isAirAttack);
        bool useColliderDistance = m_parameterAsset.GetUseColliderDistance(isAirAttack);
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
            Collider closestCollider = null;
            if (useColliderDistance)
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
                        if (surfaceDistance >= distance) continue;
                        distance = surfaceDistance;
                        closestCollider = targetCollider;
                    }
                }
            }
            if (distance > searchRadiusSquared || distance >= nearestDistance) continue;

            Vector3 offset = Vector3.ProjectOnPlane(targetOffset, Vector3.up);
            if (offset.sqrMagnitude < DIRECTION_SQR_THRESHOLD)
            {
                if (!isAirAttack || targetOffset.sqrMagnitude < DIRECTION_SQR_THRESHOLD) continue;
                offset = forward;
            }
            if (Vector3.Angle(forward, offset) > searchAngle * 0.5f)
                continue;

            nearestDistance = distance;
            direction = offset.normalized;
            selectedTarget = target;
            selectedCollider = closestCollider;
        }
        return direction.sqrMagnitude > 0.0f;
    }
}
