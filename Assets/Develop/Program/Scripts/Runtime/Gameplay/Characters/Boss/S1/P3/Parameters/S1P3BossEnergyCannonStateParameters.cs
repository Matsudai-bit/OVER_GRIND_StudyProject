using System;
using UnityEngine;

/// <summary>
/// S1P3ボスのエネルギー砲状態で使用するパラメータを保持します。
/// </summary>
[Serializable]
public sealed class S1P3BossEnergyCannonStateParameters
{
    // 予備動作時間
    [SerializeField, Header("予備動作"), Min(0.0f)]
    private float m_preparationDuration =
        5.0f;

    // 直線発射時間
    [SerializeField, Header("発射"), Min(0.0f)]
    private float m_straightFireDuration =
        2.0f;

    // Player追従時間
    [SerializeField, Header("追従"), Min(0.0f)]
    private float m_trackingDuration =
        5.0f;

    // Player追従速度
    [SerializeField, Min(0.0f)]
    private float m_trackingSpeed =
        0.0f;

    // 終了動作時間
    [SerializeField, Header("終了動作"), Min(0.0f)]
    private float m_endDuration =
        1.0f;

    // エネルギー砲攻撃中を示すAnimator Bool名
    [SerializeField, Header("Animation")]
    private string m_animationActiveBoolName =
        "EnergyCannon";

    // エネルギー砲の段階を示すAnimator Int名
    [SerializeField]
    private string m_animationPhaseIntName =
        "EnergyCannonPhase";

    /// <summary>
    /// 予備動作時間を取得します。
    /// </summary>
    public float PreparationDuration =>
        m_preparationDuration;

    /// <summary>
    /// 一直線に放つ時間を取得します。
    /// </summary>
    public float StraightFireDuration =>
        m_straightFireDuration;

    /// <summary>
    /// Player方向へ追従する時間を取得します。
    /// </summary>
    public float TrackingDuration =>
        m_trackingDuration;

    /// <summary>
    /// Player方向への追従速度を取得します。
    /// </summary>
    public float TrackingSpeed =>
        m_trackingSpeed;

    /// <summary>
    /// 終了動作時間を取得します。
    /// </summary>
    public float EndDuration =>
        m_endDuration;

    /// <summary>
    /// エネルギー砲攻撃中を示すAnimator Bool名を取得します。
    /// </summary>
    public string AnimationActiveBoolName =>
        m_animationActiveBoolName;

    /// <summary>
    /// エネルギー砲の段階を示すAnimator Int名を取得します。
    /// </summary>
    public string AnimationPhaseIntName =>
        m_animationPhaseIntName;
}