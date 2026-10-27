using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 移動を遮る対象へ接触中の攻撃判定を追跡します。
/// </summary>
public static class AttackMovementBlockRegistry
{
    private static readonly HashSet<AttackHitbox> s_blockedHitboxes = new();

    /// <summary>
    /// 対象へ接触した攻撃判定を登録します。
    /// </summary>
    /// <param name="attackHitbox">登録する攻撃判定。</param>
    public static void Register(AttackHitbox attackHitbox)
    {
        if (attackHitbox != null)
        {
            s_blockedHitboxes.Add(attackHitbox);
        }
    }

    /// <summary>
    /// 対象から離れた攻撃判定を解除します。
    /// </summary>
    /// <param name="attackHitbox">解除する攻撃判定。</param>
    public static void Unregister(AttackHitbox attackHitbox)
    {
        if (attackHitbox != null)
        {
            s_blockedHitboxes.Remove(attackHitbox);
        }
    }

    /// <summary>
    /// 指定した所有者配下の攻撃判定が移動遮断対象へ接触中か確認します。
    /// </summary>
    /// <param name="ownerRoot">攻撃所有者のルート。</param>
    /// <returns>true：接触中。false：接触していない。</returns>
    public static bool IsBlocked(Transform ownerRoot)
    {
        if (ownerRoot == null)
        {
            return false;
        }

        foreach (AttackHitbox attackHitbox in s_blockedHitboxes)
        {
            if (attackHitbox != null &&
                attackHitbox.transform.IsChildOf(ownerRoot))
            {
                return true;
            }
        }

        return false;
    }
}
