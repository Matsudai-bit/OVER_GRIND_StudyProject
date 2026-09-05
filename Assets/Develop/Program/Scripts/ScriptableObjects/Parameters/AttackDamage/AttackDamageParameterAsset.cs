using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 1つの攻撃グループで使用するダメージパラメータを保持します。
/// </summary>
/// <typeparam name="THitboxId">Hitboxを識別する列挙型。</typeparam>
public abstract class AttackDamageParameterAsset<THitboxId> : AttackDamageParameterAssetBase
    where THitboxId : struct, Enum
{
    // 使用する攻撃ごとのダメージ情報
    [SerializeField, Header("攻撃ダメージ")]
    private List<AttackDamageParameter<THitboxId>> m_attackDamageParameters = new();

    /// <summary>
    /// 指定した攻撃のダメージ情報を取得します。
    /// </summary>
    /// <param name="attackIdentifier">取得する攻撃ID。</param>
    /// <param name="parameter">取得した攻撃ダメージ情報。</param>
    /// <returns>
    /// true：攻撃ダメージ情報を取得しました。
    /// false：指定した攻撃の設定がありません。
    /// </returns>
    public bool TryGetAttackParameter(
        AttackIdentifier attackIdentifier,
        out AttackDamageParameter<THitboxId> parameter)
    {
        parameter = null;

        if (attackIdentifier == null ||
            m_attackDamageParameters == null)
        {
            return false;
        }

        foreach (AttackDamageParameter<THitboxId> attackParameter in m_attackDamageParameters)
        {
            if (attackParameter == null ||
                attackParameter.AttackIdentifier != attackIdentifier)
            {
                continue;
            }

            parameter = attackParameter;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 指定した攻撃とHitboxのダメージ量を取得します。
    /// </summary>
    /// <param name="attackIdentifier">取得する攻撃ID。</param>
    /// <param name="hitboxId">取得するHitbox ID。</param>
    /// <param name="damage">取得したダメージ量。</param>
    /// <returns>
    /// true：ダメージ量を取得しました。
    /// false：対応する設定がありません。
    /// </returns>
    public bool TryGetDamage(
        AttackIdentifier attackIdentifier,
        THitboxId hitboxId,
        out int damage)
    {
        damage = 0;

        if (!TryGetAttackParameter(
                attackIdentifier,
                out AttackDamageParameter<THitboxId> attackParameter))
        {
            return false;
        }

        return attackParameter.TryGetDamage(
            hitboxId,
            out damage);
    }
}
