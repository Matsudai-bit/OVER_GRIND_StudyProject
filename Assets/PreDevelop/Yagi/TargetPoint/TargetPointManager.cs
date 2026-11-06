using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// シーン内のTargetPointと、それぞれを選択可能にする距離を一元管理します。
/// </summary>
[DisallowMultipleComponent]
public sealed class TargetPointManager : MonoBehaviour
{
    /// <summary>TargetPoint単位のターゲット設定です。</summary>
    [Serializable]
    private sealed class TargetPointRegistration
    {
        [SerializeField]
        [Tooltip("管理対象のTargetPointです。")]
        private TargetPoint m_targetPoint;

        [SerializeField, Min(0.1f)]
        [Tooltip("このTargetPointを選択できる最大距離です。")]
        private float m_targetableDistance = 20.0f;

        /// <summary>登録されているTargetPointを取得します。</summary>
        public TargetPoint TargetPoint => m_targetPoint;

        /// <summary>選択可能な最大距離を取得します。</summary>
        public float TargetableDistance => Mathf.Max(0.1f, m_targetableDistance);
    }

    [SerializeField]
    [Tooltip("TargetPointと、そのTargetPointを選択可能にする距離を登録します。")]
    private List<TargetPointRegistration> m_targetPoints = new();

    private readonly HashSet<TargetPoint> m_collectedTargetPoints = new();

    /// <summary>
    /// 指定位置から選択可能なTargetPointを近い順で取得します。
    /// </summary>
    /// <param name="origin">距離判定の基準位置です。</param>
    /// <param name="results">結果を書き込むリストです。</param>
    public void GetTargetablePoints(
        Vector3 origin,
        List<TargetPoint> results)
    {
        if (results == null)
        {
            throw new ArgumentNullException(nameof(results));
        }

        results.Clear();
        m_collectedTargetPoints.Clear();

        foreach (TargetPointRegistration registration in m_targetPoints)
        {
            if (!IsRegistrationTargetable(registration, origin))
            {
                continue;
            }

            TargetPoint targetPoint = registration.TargetPoint;
            if (m_collectedTargetPoints.Add(targetPoint))
            {
                results.Add(targetPoint);
            }
        }

        results.Sort(
            (first, second) => CompareDistance(first, second, origin));
    }

    /// <summary>
    /// 指定したTargetPointが現在の位置から選択可能か確認します。
    /// </summary>
    /// <param name="targetPoint">確認するTargetPointです。</param>
    /// <param name="origin">距離判定の基準位置です。</param>
    /// <returns>true：選択可能。false：未登録、無効、または範囲外。</returns>
    public bool IsTargetable(TargetPoint targetPoint, Vector3 origin)
    {
        if (!IsTargetPointAvailable(targetPoint))
        {
            return false;
        }

        foreach (TargetPointRegistration registration in m_targetPoints)
        {
            if (registration != null &&
                registration.TargetPoint == targetPoint &&
                IsWithinTargetableDistance(registration, origin))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>登録内容が現在選択可能か確認します。</summary>
    /// <param name="registration">確認する登録内容です。</param>
    /// <param name="origin">距離判定の基準位置です。</param>
    /// <returns>true：選択可能。false：選択不可。</returns>
    private static bool IsRegistrationTargetable(
        TargetPointRegistration registration,
        Vector3 origin)
    {
        return registration != null &&
               IsTargetPointAvailable(registration.TargetPoint) &&
               IsWithinTargetableDistance(registration, origin);
    }

    /// <summary>TargetPoint自体が利用可能な状態か確認します。</summary>
    /// <param name="targetPoint">確認するTargetPointです。</param>
    /// <returns>true：利用可能。false：参照切れまたは無効。</returns>
    private static bool IsTargetPointAvailable(TargetPoint targetPoint)
    {
        return targetPoint != null &&
               targetPoint.IsActive &&
               targetPoint.TargetTransform != null &&
               targetPoint.gameObject.activeInHierarchy;
    }

    /// <summary>登録された最大距離内に存在するか確認します。</summary>
    /// <param name="registration">確認する登録内容です。</param>
    /// <param name="origin">距離判定の基準位置です。</param>
    /// <returns>true：距離内。false：距離外。</returns>
    private static bool IsWithinTargetableDistance(
        TargetPointRegistration registration,
        Vector3 origin)
    {
        float targetableDistance = registration.TargetableDistance;
        float sqrDistance =
            (registration.TargetPoint.Position - origin).sqrMagnitude;

        return sqrDistance <= targetableDistance * targetableDistance;
    }

    /// <summary>2つのTargetPointを基準位置からの距離で比較します。</summary>
    /// <param name="first">1つ目のTargetPointです。</param>
    /// <param name="second">2つ目のTargetPointです。</param>
    /// <param name="origin">距離判定の基準位置です。</param>
    /// <returns>1つ目が近い場合は負、同距離は0、遠い場合は正。</returns>
    private static int CompareDistance(
        TargetPoint first,
        TargetPoint second,
        Vector3 origin)
    {
        float firstDistance = (first.Position - origin).sqrMagnitude;
        float secondDistance = (second.Position - origin).sqrMagnitude;
        return firstDistance.CompareTo(secondDistance);
    }

    /// <summary>Inspector設定値を有効範囲へ制限します。</summary>
    private void OnValidate()
    {
        if (m_targetPoints == null)
        {
            m_targetPoints = new List<TargetPointRegistration>();
        }
    }
}
