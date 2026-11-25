using System;
using UnityEngine;

/// <summary>
/// S1P1ボスの歩行状態で使用するパラメータを保持します。
/// </summary>
[Serializable]
public sealed class S1P1BossWalkStateParameters
{
    // 歩行Animation
    [SerializeField, Header("Animation")]
    private AnimationClip m_walkAnimationClip;

    // Animation進行率に対する移動速度倍率
    [SerializeField, Header("移動")]
    private AnimationCurve m_moveEasingCurve =
        new AnimationCurve(
            new Keyframe(0.0f, 0.0f),
            new Keyframe(0.2f, 1.0f),
            new Keyframe(0.8f, 1.0f),
            new Keyframe(1.0f, 0.0f));

    // 前方の通行可能判定を行う距離
    [SerializeField, Min(0.0f)]
    private float m_forwardCheckDistance = 5.0f;

    // 通行不能時に移行する停止状態の継続時間
    [SerializeField, Header("停止"), Min(0.0f)]
    private float m_blockedIdleDuration = 3.0f;

    /// <summary>
    /// 歩行Animationを取得します。
    /// </summary>
    public AnimationClip WalkAnimationClip =>
        m_walkAnimationClip;

    /// <summary>
    /// Animation進行率に対する移動速度倍率を取得します。
    /// </summary>
    public AnimationCurve MoveEasingCurve =>
        m_moveEasingCurve;

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

    /// <summary>
    /// 必要なパラメータが設定されているか確認します。
    /// </summary>
    /// <returns>
    /// true：必要なパラメータが設定されています。
    /// false：パラメータが不足しています。
    /// </returns>
    public bool HasRequiredParameters()
    {
        return m_walkAnimationClip != null &&
               m_walkAnimationClip.length > Mathf.Epsilon &&
               m_moveEasingCurve != null &&
               m_moveEasingCurve.length > 0;
    }
}
