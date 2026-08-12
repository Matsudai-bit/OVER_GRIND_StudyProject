using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// フェーズごとのAnimatorとAnimationEventReceiverを管理します。
/// </summary>
[DisallowMultipleComponent]
public sealed class BossAnimationController : MonoBehaviour
{
    /// <summary>
    /// フェーズごとのアニメーション参照です。
    /// </summary>
    [Serializable]
    private sealed class PhaseAnimationEntry
    {
        // 対象フェーズ
        [SerializeField]
        private BossPhaseID m_phaseID;

        // 対象Animator
        [SerializeField]
        private Animator m_animator;

        // AnimationEvent受信コンポーネント
        [SerializeField]
        private AnimationEventReceiver m_animationEventReceiver;

        /// <summary>
        /// 対象フェーズを取得します。
        /// </summary>
        public BossPhaseID PhaseID => m_phaseID;

        /// <summary>
        /// Animatorを取得します。
        /// </summary>
        public Animator Animator => m_animator;

        /// <summary>
        /// AnimationEventReceiverを取得します。
        /// </summary>
        public AnimationEventReceiver AnimationEventReceiver => m_animationEventReceiver;
    }

    // フェーズごとのアニメーション参照
    [SerializeField, Header("フェーズ別アニメーション")]
    private List<PhaseAnimationEntry> m_phaseAnimations = new();

    // 現在使用しているAnimator
    private Animator m_currentAnimator;

    // 現在使用しているAnimationEventReceiver
    private AnimationEventReceiver m_currentAnimationEventReceiver;

    /// <summary>
    /// 現在使用しているAnimatorを取得します。
    /// </summary>
    public Animator CurrentAnimator => m_currentAnimator;

    /// <summary>
    /// 現在使用しているAnimationEventReceiverを取得します。
    /// </summary>
    public AnimationEventReceiver CurrentAnimationEventReceiver => m_currentAnimationEventReceiver;

    /// <summary>
    /// 使用するフェーズのアニメーションを設定します。
    /// </summary>
    /// <param name="phaseID">使用するフェーズ。</param>
    /// <returns>
    /// true：設定できました。
    /// false：対象フェーズの設定がありません。
    /// </returns>
    public bool SetPhase(BossPhaseID phaseID)
    {
        foreach (PhaseAnimationEntry entry in m_phaseAnimations)
        {
            if (entry == null || entry.PhaseID != phaseID)
            {
                continue;
            }

            m_currentAnimator = entry.Animator;
            m_currentAnimationEventReceiver = entry.AnimationEventReceiver;
            return true;
        }

        m_currentAnimator = null;
        m_currentAnimationEventReceiver = null;

        Debug.LogWarning(
            $"{phaseID}のアニメーション設定が見つかりません。",
            this);

        return false;
    }

    /// <summary>
    /// Triggerパラメータを設定します。
    /// </summary>
    /// <param name="parameterID">AnimatorパラメータID。</param>
    public void SetTrigger(int parameterID)
    {
        if (m_currentAnimator == null)
        {
            return;
        }

        m_currentAnimator.SetTrigger(parameterID);
    }

    /// <summary>
    /// Boolパラメータを設定します。
    /// </summary>
    /// <param name="parameterID">AnimatorパラメータID。</param>
    /// <param name="value">設定する値。</param>
    public void SetBool(int parameterID, bool value)
    {
        if (m_currentAnimator == null)
        {
            return;
        }

        m_currentAnimator.SetBool(parameterID, value);
    }
}
