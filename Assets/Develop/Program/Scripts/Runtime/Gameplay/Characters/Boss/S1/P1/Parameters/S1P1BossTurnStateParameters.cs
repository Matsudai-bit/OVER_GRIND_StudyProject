using System;
using UnityEngine;

/// <summary>
/// S1P1ボスの方向転換状態で使用するパラメータを保持します。
/// </summary>
[Serializable]
public sealed class S1P1BossTurnStateParameters
{
    // 方向転換時の回転速度
    [SerializeField, Header("方向転換"), Min(0.0f)]
    private float m_rotationSpeed = 45.0f;

    /// <summary>
    /// 回転速度を取得します。
    /// </summary>
    public float RotationSpeed =>
        m_rotationSpeed;
}
