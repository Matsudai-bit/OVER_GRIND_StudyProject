using System;
using UnityEngine;

/// <summary>
/// Hitbox IDとAttackHitboxの対応を保持します。
/// </summary>
/// <typeparam name="THitboxId">Hitboxを識別する列挙型。</typeparam>
[Serializable]
public class AttackHitboxBinding<THitboxId>
    where THitboxId : struct, Enum
{
    // Hitboxを識別するID
    [SerializeField, Header("Hitbox")]
    private THitboxId m_hitboxId;

    // IDに対応するAttackHitbox
    [SerializeField]
    private AttackHitbox m_attackHitbox;

    /// <summary>
    /// Hitbox IDを取得します。
    /// </summary>
    public THitboxId HitboxId => m_hitboxId;

    /// <summary>
    /// AttackHitboxを取得します。
    /// </summary>
    public AttackHitbox AttackHitbox => m_attackHitbox;
}
