using System;
using UnityEngine;

/// <summary>
/// S1P1ボスのミサイル攻撃状態で使用するパラメータを保持します。
/// </summary>
[Serializable]
public sealed class S1P1BossMissileStateParameters
{
    // ミサイルを順番に発射する間隔
    [SerializeField, Header("発射"), Min(0.0f)]
    private float m_launchInterval = 0.5f;

    // 発射直後に上方向へ進む時間
    [SerializeField, Min(0.0f)]
    private float m_missileUpwardDuration = 1.5f;

    /// <summary>
    /// 発射間隔を取得します。
    /// </summary>
    public float LaunchInterval =>
        m_launchInterval;

    /// <summary>
    /// ミサイルの上昇時間を取得します。
    /// </summary>
    public float MissileUpwardDuration =>
        m_missileUpwardDuration;
}
