using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 接触コールバックから参照できるよう、攻撃中の所有者と威力種別を保持します。
/// </summary>
public static class AttackPowerSourceRegistry
{
    private static readonly Dictionary<int, (AttackPowerType powerType, int sessionId)>
        s_attackSessions = new();
    private static int s_nextSessionId;

    /// <summary>攻撃所有者の威力種別を登録します。</summary>
    /// <param name="owner">攻撃判定を子階層に持つ所有者。</param>
    /// <param name="attackPowerType">登録する威力種別。</param>
    public static void Register(
        Transform owner,
        AttackPowerType attackPowerType)
    {
        if (owner == null) return;
        int ownerId = owner.GetInstanceID();
        if (s_attackSessions.TryGetValue(ownerId, out var session))
        {
            s_attackSessions[ownerId] = (attackPowerType, session.sessionId);
            return;
        }

        s_attackSessions[ownerId] = (attackPowerType, ++s_nextSessionId);
    }

    /// <summary>攻撃所有者の登録を解除します。</summary>
    /// <param name="owner">登録を解除する所有者。</param>
    public static void Unregister(Transform owner)
    {
        if (owner == null) return;
        s_attackSessions.Remove(owner.GetInstanceID());
    }

    /// <summary>攻撃判定の親階層から登録済みの威力種別を取得します。</summary>
    /// <param name="attackSource">攻撃判定のTransform。</param>
    /// <param name="attackPowerType">取得した威力種別。</param>
    /// <param name="sessionId">攻撃開始から終了まで共通の識別子。</param>
    /// <returns>true：登録あり。false：登録なし。</returns>
    public static bool TryGet(
        Transform attackSource,
        out AttackPowerType attackPowerType,
        out int sessionId)
    {
        Transform current = attackSource;

        while (current != null)
        {
            if (s_attackSessions.TryGetValue(current.GetInstanceID(), out var session))
            {
                attackPowerType = session.powerType;
                sessionId = session.sessionId;
                return true;
            }

            current = current.parent;
        }

        attackPowerType = AttackPowerType.NORMAL;
        sessionId = 0;
        return false;
    }
}
