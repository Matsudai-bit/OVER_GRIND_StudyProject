using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 接触コールバックから参照できるよう、攻撃中の所有者と威力種別を保持します。
/// </summary>
public static class AttackPowerSourceRegistry
{
    private static readonly Dictionary<int, AttackPowerType> s_powerTypes = new();

    /// <summary>攻撃所有者の威力種別を登録します。</summary>
    /// <param name="owner">攻撃判定を子階層に持つ所有者。</param>
    /// <param name="attackPowerType">登録する威力種別。</param>
    public static void Register(
        Transform owner,
        AttackPowerType attackPowerType)
    {
        if (owner == null) return;
        s_powerTypes[owner.GetInstanceID()] = attackPowerType;
    }

    /// <summary>攻撃所有者の登録を解除します。</summary>
    /// <param name="owner">登録を解除する所有者。</param>
    public static void Unregister(Transform owner)
    {
        if (owner == null) return;
        s_powerTypes.Remove(owner.GetInstanceID());
    }

    /// <summary>攻撃判定の親階層から登録済みの威力種別を取得します。</summary>
    /// <param name="attackSource">攻撃判定のTransform。</param>
    /// <param name="attackPowerType">取得した威力種別。</param>
    /// <returns>true：登録あり。false：登録なし。</returns>
    public static bool TryGet(
        Transform attackSource,
        out AttackPowerType attackPowerType)
    {
        Transform current = attackSource;

        while (current != null)
        {
            if (s_powerTypes.TryGetValue(
                    current.GetInstanceID(),
                    out attackPowerType))
            {
                return true;
            }

            current = current.parent;
        }

        attackPowerType = AttackPowerType.NORMAL;
        return false;
    }
}
