using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ƒ{ƒX‚ÌUŒ‚İ’è‚ğŠÇ—‚µ‚Ü‚·B
/// </summary>
/// <typeparam name="TAttackType">UŒ‚í—ŞB</typeparam>
public abstract class BossAttackSettings<TAttackType> : MonoBehaviour
    where TAttackType : Enum
{
    /// <summary>
    /// UŒ‚‚²‚Æ‚Ìİ’è‚Å‚·B
    /// </summary>
    [Serializable]
    protected sealed class AttackSetting
    {
        // UŒ‚í—Ş
        [SerializeField]
        private TAttackType m_attackType;

        // UŒ‚ID
        [SerializeField]
        private AttackIdentifier m_attackIdentifier;

        // Animator Trigger–¼
        [SerializeField]
        private string m_animationTriggerName;

        /// <summary>
        /// UŒ‚í—Ş‚ğæ“¾‚µ‚Ü‚·B
        /// </summary>
        public TAttackType AttackType => m_attackType;

        /// <summary>
        /// UŒ‚ID‚ğæ“¾‚µ‚Ü‚·B
        /// </summary>
        public AttackIdentifier AttackIdentifier =>
            m_attackIdentifier;

        /// <summary>
        /// Animator Trigger–¼‚ğæ“¾‚µ‚Ü‚·B
        /// </summary>
        public string AnimationTriggerName =>
            m_animationTriggerName;
    }

    // UŒ‚İ’èˆê——
    [SerializeField]
    private List<AttackSetting> m_attackSettings = new();

    /// <summary>
    /// w’èUŒ‚‚Ìİ’è‚ğæ“¾‚µ‚Ü‚·B
    /// </summary>
    public bool TryGetAttackSetting(
        TAttackType attackType,
        out AttackIdentifier attackIdentifier,
        out string animationTriggerName)
    {
        EqualityComparer<TAttackType> comparer =
            EqualityComparer<TAttackType>.Default;

        foreach (AttackSetting attackSetting in m_attackSettings)
        {
            if (attackSetting == null ||
                !comparer.Equals(
                    attackSetting.AttackType,
                    attackType))
            {
                continue;
            }

            attackIdentifier =
                attackSetting.AttackIdentifier;

            animationTriggerName =
                attackSetting.AnimationTriggerName;

            return attackIdentifier != null &&
                   !string.IsNullOrEmpty(
                       animationTriggerName);
        }

        attackIdentifier = null;
        animationTriggerName = string.Empty;

        return false;
    }
}