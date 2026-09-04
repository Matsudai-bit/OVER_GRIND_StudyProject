using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ボスが使用する攻撃Hitboxを一元管理します。
/// </summary>
[DisallowMultipleComponent]
public sealed class AttackHitboxRegistry : MonoBehaviour
{
    /// <summary>
    /// 攻撃IDとHitboxの対応情報です。
    /// </summary>
    [Serializable]
    private sealed class HitboxEntry
    {
        // 攻撃ID
        [SerializeField]
        private AttackIdentifier m_attackIdentifier;

        // 対応するHitbox
        [SerializeField]
        private AttackHitbox m_hitbox;

        /// <summary>
        /// 攻撃IDを取得します。
        /// </summary>
        public AttackIdentifier AttackIdentifier => m_attackIdentifier;

        /// <summary>
        /// Hitboxを取得します。
        /// </summary>
        public AttackHitbox Hitbox => m_hitbox;
    }

    // Inspectorで設定するHitbox一覧
    [SerializeField, Header("攻撃Hitbox")]
    private List<HitboxEntry> m_hitboxes = new();


    // 実行時に使用する検索テーブル
    private readonly Dictionary<AttackIdentifier, AttackHitbox>
        m_hitboxMap = new();

    /// <summary>
    /// Hitbox検索テーブルを構築します。
    /// </summary>
    private void Awake()
    {
        m_hitboxMap.Clear();

        foreach (HitboxEntry entry in m_hitboxes)
        {
            if (entry == null ||
                entry.AttackIdentifier == null ||
                entry.Hitbox == null)
            {
                continue;
            }

            if (!m_hitboxMap.TryAdd(
                    entry.AttackIdentifier,
                    entry.Hitbox))
            {
                Debug.LogWarning(
                    $"{entry.AttackIdentifier.name}が重複しています。",
                    this);
            }
        }
    }

    /// <summary>
    /// 指定攻撃のHitboxを有効にします。
    /// </summary>
    /// <param name="attackIdentifier">攻撃ID。</param>
    public void EnableHitbox(AttackIdentifier attackIdentifier, AttackDamageControllerBase attackDamage)
    {
        if (!TryGetHitbox(attackIdentifier, out AttackHitbox hitbox))
        {
            return;
        }

       
        attackDamage.ApplyDamageParameters(attackIdentifier);

        hitbox.EnableHitbox();
    }

    /// <summary>
    /// 指定攻撃のHitboxを無効にします。
    /// </summary>
    /// <param name="attackIdentifier">攻撃ID。</param>
    public void DisableHitbox(AttackIdentifier attackIdentifier)
    {
        if (!TryGetHitbox(attackIdentifier, out AttackHitbox hitbox))
        {
            return;
        }

        hitbox.DisableHitbox();
        
    }

    /// <summary>
    /// 指定攻撃のHitboxを取得します。
    /// </summary>
    /// <param name="attackIdentifier">攻撃ID。</param>
    /// <param name="hitbox">取得したHitbox。</param>
    /// <returns>
    /// true：取得できました。
    /// false：取得できませんでした。
    /// </returns>
    public bool TryGetHitbox(
        AttackIdentifier attackIdentifier,
        out AttackHitbox hitbox)
    {
        hitbox = null;

        if (attackIdentifier == null)
        {
            return false;
        }

        if (m_hitboxMap.TryGetValue(
                attackIdentifier,
                out hitbox))
        {
            return true;
        }

        Debug.LogWarning(
            $"{attackIdentifier.name}に対応するHitboxがありません。",
            this);

        return false;
    }
}
