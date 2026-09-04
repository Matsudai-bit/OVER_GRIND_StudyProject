using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 1つの攻撃で使用するHitboxごとのダメージ情報を保持します。
/// </summary>
/// <typeparam name="THitboxId">Hitboxを識別する列挙型。</typeparam>
[Serializable]
public class AttackDamageParameter<THitboxId>
    where THitboxId : struct, Enum
{
    // 攻撃を識別するID
    [SerializeField, Header("攻撃ID")]
    private AttackIdentifier m_attackIdentifier;

    // 攻撃中に使用するHitboxごとのダメージ情報
    [SerializeField, Header("Hitbox別ダメージ")]
    private List<HitboxDamageParameter<THitboxId>> m_hitboxDamageParameters = new();

    /// <summary>
    /// 攻撃IDを取得します。
    /// </summary>
    public AttackIdentifier AttackIdentifier => m_attackIdentifier;

    /// <summary>
    /// Hitboxごとのダメージ情報を取得します。
    /// </summary>
    public IReadOnlyList<HitboxDamageParameter<THitboxId>> HitboxDamageParameters =>
        m_hitboxDamageParameters;

    /// <summary>
    /// 指定したHitboxのダメージ量を取得します。
    /// </summary>
    /// <param name="hitboxId">取得するHitbox ID。</param>
    /// <param name="damage">取得したダメージ量。</param>
    /// <returns>
    /// true：ダメージ量を取得しました。
    /// false：指定したHitboxの設定がありません。
    /// </returns>
    public bool TryGetDamage(
        THitboxId hitboxId,
        out int damage)
    {
        damage = 0;

        if (m_hitboxDamageParameters == null)
        {
            return false;
        }

        EqualityComparer<THitboxId> comparer = EqualityComparer<THitboxId>.Default;

        foreach (HitboxDamageParameter<THitboxId> parameter in m_hitboxDamageParameters)
        {
            if (parameter == null ||
                !comparer.Equals(parameter.HitboxId, hitboxId))
            {
                continue;
            }

            damage = parameter.Damage;
            return true;
        }

        return false;
    }
}
