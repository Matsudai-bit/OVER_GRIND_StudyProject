using System;
using UnityEngine;

/// <summary>
/// S1P3ボスのエネルギー砲状態で使用する
/// パラメータを保持します。
/// </summary>
[Serializable]
public sealed class S1P3BossEnergyCannonStateParameters
{
    // Player追従速度
    [SerializeField, Header("追従"), Min(0.0f)]
    private float m_trackingSpeed =
        0.0f;

    // エネルギー砲攻撃中を示すAnimator Bool名
    [SerializeField, Header("Animation")]
    private string m_animationActiveBoolName =
        "EnergyCannon";

    // エネルギー砲の段階を示すAnimator Int名
    [SerializeField]
    private string m_animationPhaseIntName =
        "EnergyCannonPhase";

    /// <summary>
    /// Player方向への追従速度を取得します。
    /// </summary>
    public float TrackingSpeed =>
        m_trackingSpeed;

    /// <summary>
    /// エネルギー砲攻撃中を示す
    /// Animator Bool名を取得します。
    /// </summary>
    public string AnimationActiveBoolName =>
        m_animationActiveBoolName;

    /// <summary>
    /// エネルギー砲の段階を示す
    /// Animator Int名を取得します。
    /// </summary>
    public string AnimationPhaseIntName =>
        m_animationPhaseIntName;
}