using System;
using UnityEngine;

/// <summary>
/// S1P1ボスの歩行状態で使用するパラメータを保持します。
/// </summary>
[Serializable]
public sealed class S1P1BossWalkStateParameters
{
    // 歩行を継続する時間
    [SerializeField, Header("歩行"), Min(0.0f)]
    private float m_walkDuration = 3.0f;

    // 前方の通行可能判定を行う距離
    [SerializeField, Min(0.0f)]
    private float m_forwardCheckDistance = 5.0f;

    // 通行不能時に移行する停止状態の継続時間
    [SerializeField, Header("停止"), Min(0.0f)]
    private float m_blockedIdleDuration = 3.0f;

    /// <summary>
    /// 歩行時間を取得します。
    /// </summary>
    public float WalkDuration =>
        m_walkDuration;

    /// <summary>
    /// 前方確認距離を取得します。
    /// </summary>
    public float ForwardCheckDistance =>
        m_forwardCheckDistance;

    /// <summary>
    /// 通行不能時の停止時間を取得します。
    /// </summary>
    public float BlockedIdleDuration =>
        m_blockedIdleDuration;
}
