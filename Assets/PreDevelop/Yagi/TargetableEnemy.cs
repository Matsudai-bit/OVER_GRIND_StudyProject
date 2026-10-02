using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ロックオン対象となる敵と、そのターゲット可能な部位を管理します。
/// </summary>
[DisallowMultipleComponent]
public sealed class TargetableEnemy : MonoBehaviour
{
    [Tooltip("ロックオン可能な部位を登録します。上から順番に切り替わります。")]
    [SerializeField]
    private List<TargetPoint> m_targetPoints = new();

    /// <summary>
    /// この敵が持つターゲット可能な部位一覧を取得します。
    /// </summary>
    public IReadOnlyList<TargetPoint> TargetPoints
    {
        get
        {
            return m_targetPoints;
        }
    }

    /// <summary>
    /// 有効なターゲット部位を持っているか確認します。
    /// </summary>
    public bool HasTargetPoint
    {
        get
        {
            foreach (TargetPoint targetPoint in m_targetPoints)
            {
                if (targetPoint != null &&
                    targetPoint.TargetTransform != null)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// 指定されたインデックスのターゲット部位を取得します。
    /// </summary>
    /// <param name="index">取得する部位のインデックスです。</param>
    /// <returns>指定されたターゲット部位です。</returns>
    public TargetPoint GetTargetPoint(int index)
    {
        if (index < 0 || index >= m_targetPoints.Count)
        {
            return null;
        }

        return m_targetPoints[index];
    }
}