using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ステージ1フェーズ1の攻撃設定を保持します。
/// </summary>
public sealed class S1P1BossAttackSettings : MonoBehaviour
{
    /// <summary>
    /// 攻撃ごとの設定です。
    /// </summary>
    [Serializable]
    private sealed class AttackSetting
    {
        // 攻撃種類
        [SerializeField]
        private S1P1BossAttackType m_attackType;

        // 攻撃ID
        [SerializeField]
        private AttackIdentifier m_attackIdentifier;

        // Animator Trigger名
        [SerializeField]
        private string m_animationTriggerName;

        /// <summary>
        /// 攻撃種類を取得します。
        /// </summary>
        public S1P1BossAttackType AttackType => m_attackType;

        /// <summary>
        /// 攻撃IDを取得します。
        /// </summary>
        public AttackIdentifier AttackIdentifier => m_attackIdentifier;

        /// <summary>
        /// Animator Trigger名を取得します。
        /// </summary>
        public string AnimationTriggerName => m_animationTriggerName;
    }

    // 攻撃設定一覧
    [SerializeField, Header("フェーズ1攻撃設定")]
    private List<AttackSetting> m_attackSettings = new();

    /// <summary>
    /// 指定攻撃の設定を取得します。
    /// </summary>
    /// <param name="attackType">攻撃種類。</param>
    /// <param name="attackIdentifier">取得した攻撃ID。</param>
    /// <param name="animationTriggerName">取得したTrigger名。</param>
    /// <returns>
    /// true：取得できました。
    /// false：取得できませんでした。
    /// </returns>
    public bool TryGetAttackSetting(
        S1P1BossAttackType attackType,
        out AttackIdentifier attackIdentifier,
        out string animationTriggerName)
    {
        foreach (AttackSetting attackSetting in m_attackSettings)
        {
            if (attackSetting == null ||
                attackSetting.AttackType != attackType)
            {
                continue;
            }

            attackIdentifier = attackSetting.AttackIdentifier;
            animationTriggerName = attackSetting.AnimationTriggerName;

            return attackIdentifier != null &&
                   !string.IsNullOrEmpty(animationTriggerName);
        }

        attackIdentifier = null;
        animationTriggerName = string.Empty;
        return false;
    }
}
