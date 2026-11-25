using System;
using UnityEngine;

/// <summary>
/// S1P1ボスの方向転換状態で使用するパラメータを保持します。
/// </summary>
[Serializable]
public sealed class S1P1BossTurnStateParameters
{
    // 右方向転換Animation
    [SerializeField, Header("Animation")]
    private AnimationClip m_rightTurnAnimationClip;

    // 左方向転換Animation
    [SerializeField]
    private AnimationClip m_leftTurnAnimationClip;

    // Animation進行率に対する回転進行率
    [SerializeField, Header("回転")]
    private AnimationCurve m_rotationEasingCurve =
        AnimationCurve.EaseInOut(
            0.0f,
            0.0f,
            1.0f,
            1.0f);

    /// <summary>
    /// 右方向転換Animationを取得します。
    /// </summary>
    public AnimationClip RightTurnAnimationClip =>
        m_rightTurnAnimationClip;

    /// <summary>
    /// 左方向転換Animationを取得します。
    /// </summary>
    public AnimationClip LeftTurnAnimationClip =>
        m_leftTurnAnimationClip;

    /// <summary>
    /// 回転イージングを取得します。
    /// </summary>
    public AnimationCurve RotationEasingCurve =>
        m_rotationEasingCurve;

    /// <summary>
    /// 必要なパラメータが設定されているか確認します。
    /// </summary>
    public bool HasRequiredParameters()
    {
        return m_rightTurnAnimationClip != null &&
               m_leftTurnAnimationClip != null &&
               m_rotationEasingCurve != null &&
               m_rotationEasingCurve.length > 0;
    }
}