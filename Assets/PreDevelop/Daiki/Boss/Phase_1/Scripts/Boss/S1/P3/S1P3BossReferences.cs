using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ステージ1フェーズ3固有の参照を保持します。
/// </summary>
[DisallowMultipleComponent]
public sealed class S1P3BossReferences : MonoBehaviour
{
    // 3回目の突進で使用する目的地候補
    [SerializeField, Header("突進目的地")]
    private List<Transform> m_chargeDestinationPoints = new();

    /// <summary>
    /// 突進目的地候補を取得します。
    /// </summary>
    public IReadOnlyList<Transform> ChargeDestinationPoints =>
        m_chargeDestinationPoints;
}