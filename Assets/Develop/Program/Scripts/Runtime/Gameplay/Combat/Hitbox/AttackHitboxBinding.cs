using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hitbox IDと複数のAttackHitboxの対応を保持します。
/// </summary>
/// <typeparam name="THitboxId">Hitboxを識別する列挙型。</typeparam>
[Serializable]
public sealed class AttackHitboxBinding<THitboxId>
    where THitboxId : struct, Enum
{
    // Hitboxを識別するID
    [SerializeField, Header("Hitbox")]
    private THitboxId m_hitboxId;

    // IDに対応するAttackHitbox
    [SerializeField]
    private List<AttackHitbox> m_attackHitboxes = new();

    /// <summary>
    /// Hitbox IDを取得します。
    /// </summary>
    public THitboxId HitboxId => m_hitboxId;

    /// <summary>
    /// IDに対応するAttackHitbox一覧を取得します。
    /// </summary>
    public IReadOnlyList<AttackHitbox> AttackHitboxes =>
        m_attackHitboxes;
}