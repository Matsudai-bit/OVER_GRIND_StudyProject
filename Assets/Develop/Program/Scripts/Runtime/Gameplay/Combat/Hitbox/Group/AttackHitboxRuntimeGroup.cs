using System;
using System.Collections.Generic;

/// <summary>
/// 実行時に一つの攻撃で使用するAttackHitbox群を保持します。
/// </summary>
public sealed class AttackHitboxRuntimeGroup
{
    // 攻撃ID
    private readonly AttackIdentifier m_attackIdentifier;

    // 攻撃で使用するHitbox一覧
    private readonly IReadOnlyList<AttackHitbox> m_hitboxes;

    /// <summary>
    /// 攻撃IDを取得します。
    /// </summary>
    public AttackIdentifier AttackIdentifier =>
        m_attackIdentifier;

    /// <summary>
    /// 攻撃で使用するHitbox一覧を取得します。
    /// </summary>
    public IReadOnlyList<AttackHitbox> Hitboxes =>
        m_hitboxes;

    /// <summary>
    /// 実行時Hitbox情報を生成します。
    /// </summary>
    /// <param name="attackIdentifier">攻撃ID。</param>
    /// <param name="hitboxes">攻撃で使用するHitbox一覧。</param>
    /// <exception cref="ArgumentNullException">
    /// 必要な情報がnullの場合に発生します。
    /// </exception>
    public AttackHitboxRuntimeGroup(
        AttackIdentifier attackIdentifier,
        IReadOnlyList<AttackHitbox> hitboxes)
    {
        m_attackIdentifier =
            attackIdentifier ??
            throw new ArgumentNullException(
                nameof(attackIdentifier));

        m_hitboxes =
            hitboxes ??
            throw new ArgumentNullException(
                nameof(hitboxes));
    }

    /// <summary>
    /// 登録されているHitboxをすべて有効にします。
    /// </summary>
    public void EnableHitboxes()
    {
        foreach (AttackHitbox hitbox in m_hitboxes)
        {
            if (hitbox == null)
            {
                continue;
            }

            hitbox.EnableHitbox();
        }
    }

    /// <summary>
    /// 登録されているHitboxをすべて無効にします。
    /// </summary>
    public void DisableHitboxes()
    {
        foreach (AttackHitbox hitbox in m_hitboxes)
        {
            if (hitbox == null)
            {
                continue;
            }

            hitbox.DisableHitbox();
        }
    }
}
