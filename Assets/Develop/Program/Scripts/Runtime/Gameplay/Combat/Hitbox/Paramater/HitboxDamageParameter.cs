using System;
using UnityEngine;

/// <summary>
/// 1つのHitboxに設定するダメージ情報を保持します。
/// </summary>
/// <typeparam name="THitboxId">Hitboxを識別する列挙型。</typeparam>
[Serializable]
public class HitboxDamageParameter<THitboxId>
    where THitboxId : struct, Enum
{
    // 対象HitboxのID
    [SerializeField, Header("Hitbox")]
    private THitboxId m_hitboxId;

    // 対象Hitboxのダメージ量
    [SerializeField, Header("ダメージ")]
    [Min(0)]
    private int m_damage;

    /// <summary>
    /// Hitbox IDを取得します。
    /// </summary>
    public THitboxId HitboxId => m_hitboxId;

    /// <summary>
    /// ダメージ量を取得します。
    /// </summary>
    public int Damage => m_damage;
}
