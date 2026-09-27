using System;
using UnityEngine;

/// <summary>
/// S1P3ボスのエネルギー砲状態で使用するパラメータを保持します。
/// </summary>
[Serializable]
public sealed class S1P3BossEnergyCannonStateParameters
{
    [SerializeField, Header("予備動作"), Min(0.0f)]
    private float m_preparationDuration = 5.0f;

    [SerializeField, Header("発射"), Min(0.0f)]
    private float m_straightFireDuration = 2.0f;

    [SerializeField, Header("追従"), Min(0.0f)]
    private float m_trackingDuration = 5.0f;

    [SerializeField, Min(0.0f)]
    private float m_trackingSpeed = 0.0f;

    /// <summary>
    /// 予備動作時間を取得します。
    /// </summary>
    public float PreparationDuration => m_preparationDuration;

    /// <summary>
    /// 一直線に放つ時間を取得します。
    /// </summary>
    public float StraightFireDuration => m_straightFireDuration;

    /// <summary>
    /// Player方向へ動かす時間を取得します。
    /// </summary>
    public float TrackingDuration => m_trackingDuration;

    /// <summary>
    /// Player方向へ動かす速度を取得します。
    /// </summary>
    public float TrackingSpeed => m_trackingSpeed;
}
