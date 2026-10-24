using System;
using UnityEngine;

/// <summary>
/// S1P3ボスのスタン状態で使用するパラメータを保持します。
/// </summary>
[Serializable]
public sealed class S1P3BossStunnedStateParameters
{
    // スタンを継続する時間
    [SerializeField, Header("スタン"), Min(0.0f)]
    private float m_duration = 10.0f;

    /// <summary>
    /// スタン時間を取得します。
    /// </summary>
    public float Duration =>
        m_duration;
}
