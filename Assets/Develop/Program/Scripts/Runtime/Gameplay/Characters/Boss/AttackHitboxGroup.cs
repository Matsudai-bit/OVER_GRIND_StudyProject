using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一つの攻撃で使用するHitbox ID群を保持します。
/// </summary>
/// <typeparam name="THitboxId">Hitboxを識別する列挙型。</typeparam>
[Serializable]
public sealed class AttackHitboxGroup<THitboxId>
    where THitboxId : struct, Enum
{
    // 攻撃ID
    [SerializeField, Header("攻撃")]
    private AttackIdentifier m_attackIdentifier;

    // 攻撃で使用するHitbox ID一覧
    [SerializeField, Header("使用Hitbox")]
    private List<THitboxId> m_hitboxIds = new();

    /// <summary>
    /// 攻撃IDを取得します。
    /// </summary>
    public AttackIdentifier AttackIdentifier =>
        m_attackIdentifier;

    /// <summary>
    /// 攻撃で使用するHitbox ID一覧を取得します。
    /// </summary>
    public IReadOnlyList<THitboxId> HitboxIds =>
        m_hitboxIds;
}
