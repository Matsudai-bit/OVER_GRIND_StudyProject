using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 壁として扱うドラム缶の攻撃接触とダメージを共有します。
/// </summary>
[DisallowMultipleComponent]
public sealed class AttackReactiveInteractObjectGroup : MonoBehaviour
{
    [SerializeField]
    [Tooltip("同じ壁として一斉に反応させるドラム缶です。")]
    private List<AttackReactiveInteractObject> m_members = new();

    private readonly List<AttackReactiveInteractObject> m_registeredMembers = new();
    private readonly Dictionary<int, HashSet<AttackReactiveInteractObject>>
        m_contactMembersBySource = new();
    private readonly HashSet<int> m_damagedContactSourceIds = new();
    private bool m_hasDissolved;

    /// <summary>登録されたドラム缶にこのグループを設定します。</summary>
    private void Awake()
    {
        RegisterMembers();
    }

    /// <summary>有効化時にメンバー登録を復元します。</summary>
    private void OnEnable()
    {
        RegisterMembers();
    }

    /// <summary>無効化時に共有接触を解除し、メンバー登録を外します。</summary>
    private void OnDisable()
    {
        ReleaseMembers();
    }

    /// <summary>メンバーの攻撃接触開始をグループ全体へ共有します。</summary>
    /// <param name="sourceMember">直接接触したメンバー。</param>
    /// <param name="attackSourceId">接触した攻撃判定のInstance ID。</param>
    internal void BeginAttackContact(
        AttackReactiveInteractObject sourceMember,
        int attackSourceId)
    {
        if (!Contains(sourceMember))
        {
            sourceMember?.BeginAttackContactLocally(attackSourceId);
            return;
        }

        if (!m_contactMembersBySource.TryGetValue(
                attackSourceId,
                out HashSet<AttackReactiveInteractObject> contactMembers))
        {
            contactMembers = new HashSet<AttackReactiveInteractObject>();
            m_contactMembersBySource.Add(attackSourceId, contactMembers);
        }

        if (!contactMembers.Add(sourceMember))
        {
            // 前回の終了通知を取り逃がした状態で同じHitboxが再度開始した場合は、
            // 古い接触とダメージ履歴を破棄して新しい攻撃として扱う。
            NotifyContactEnded(attackSourceId);
            contactMembers.Clear();
            m_damagedContactSourceIds.Remove(attackSourceId);
            contactMembers.Add(sourceMember);
        }

        if (contactMembers.Count == 1)
        {
            NotifyContactStarted(attackSourceId);
        }
    }

    /// <summary>メンバーの攻撃接触終了をグループ全体へ共有します。</summary>
    /// <param name="sourceMember">直接接触していたメンバー。</param>
    /// <param name="attackSourceId">離れた攻撃判定のInstance ID。</param>
    internal void EndAttackContact(
        AttackReactiveInteractObject sourceMember,
        int attackSourceId)
    {
        if (!Contains(sourceMember))
        {
            sourceMember?.EndAttackContactLocally(attackSourceId);
            return;
        }

        if (!m_contactMembersBySource.TryGetValue(
                attackSourceId,
                out HashSet<AttackReactiveInteractObject> contactMembers))
        {
            // グループ側の記録がなくても、メンバーに残った表示状態は確実に解除する。
            NotifyContactEnded(attackSourceId);
            return;
        }

        contactMembers.Remove(sourceMember);

        if (contactMembers.Count > 0)
        {
            return;
        }

        m_contactMembersBySource.Remove(attackSourceId);
        m_damagedContactSourceIds.Remove(attackSourceId);
        NotifyContactEnded(attackSourceId);
    }

    /// <summary>受けたダメージを有効なメンバー全員へ適用します。</summary>
    /// <param name="sourceMember">直接ダメージを受けたメンバー。</param>
    /// <param name="damageData">受けるダメージと攻撃情報。</param>
    /// <param name="attackCenter">攻撃中心のワールド座標。</param>
    /// <returns>true：1つ以上のメンバーがダメージを受けた。false：全員が拒否した。</returns>
    internal bool TryApplyDamage(
        AttackReactiveInteractObject sourceMember,
        AttackDamageData damageData,
        Vector3 attackCenter)
    {
        if (!Contains(sourceMember))
        {
            return sourceMember != null &&
                   sourceMember.TryApplyGroupDamage(damageData, attackCenter);
        }

        if (!TryReserveDamageForCurrentContact())
        {
            return false;
        }

        bool hasAcceptedDamage = false;
        bool hasStartedLaunch = false;

        foreach (AttackReactiveInteractObject member in m_registeredMembers)
        {
            if (member != null && member.isActiveAndEnabled)
            {
                bool wasLaunched = member.HasBeenLaunched;
                bool acceptedDamage =
                    member.TryApplyGroupDamage(damageData, attackCenter);

                hasAcceptedDamage |= acceptedDamage;
                hasStartedLaunch |=
                    !wasLaunched && member.HasBeenLaunched;
            }
        }

        if (hasStartedLaunch)
        {
            // 最初の一斉吹き飛び後は各ドラム缶を従来の単体管理へ戻す。
            DissolveGroup();
        }

        return hasAcceptedDamage;
    }

    /// <summary>同じ攻撃判定が複数のドラム缶へ触れても、1回分のダメージに制限します。</summary>
    /// <returns>true：今回のダメージを適用できる。false：同じ接触中に適用済み。</returns>
    private bool TryReserveDamageForCurrentContact()
    {
        if (m_contactMembersBySource.Count == 0)
        {
            return true;
        }

        foreach (int attackSourceId in m_contactMembersBySource.Keys)
        {
            if (m_damagedContactSourceIds.Add(attackSourceId))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>登録メンバー全員へ接触開始を通知します。</summary>
    /// <param name="attackSourceId">攻撃判定のInstance ID。</param>
    private void NotifyContactStarted(int attackSourceId)
    {
        foreach (AttackReactiveInteractObject member in m_registeredMembers)
        {
            if (member != null && member.isActiveAndEnabled)
            {
                member.BeginAttackContactLocally(attackSourceId);
            }
        }
    }

    /// <summary>登録メンバー全員へ接触終了を通知します。</summary>
    /// <param name="attackSourceId">攻撃判定のInstance ID。</param>
    private void NotifyContactEnded(int attackSourceId)
    {
        foreach (AttackReactiveInteractObject member in m_registeredMembers)
        {
            if (member != null)
            {
                member.EndAttackContactLocally(attackSourceId);
            }
        }
    }

    /// <summary>Inspectorの全メンバーへグループを登録します。</summary>
    private void RegisterMembers()
    {
        if (m_hasDissolved)
        {
            return;
        }

        m_registeredMembers.Clear();

        foreach (AttackReactiveInteractObject member in m_members)
        {
            if (member == null || m_registeredMembers.Contains(member))
            {
                continue;
            }

            if (member.TryRegisterGroup(this))
            {
                m_registeredMembers.Add(member);
                continue;
            }

            Debug.LogWarning(
                $"{member.name}は別の{nameof(AttackReactiveInteractObjectGroup)}に登録済みです。",
                member);
        }
    }

    /// <summary>指定したドラム缶がこのグループに含まれるか確認します。</summary>
    /// <param name="member">確認するドラム缶。</param>
    /// <returns>true：グループに含まれる。false：含まれない。</returns>
    private bool Contains(AttackReactiveInteractObject member)
    {
        return member != null && m_registeredMembers.Contains(member);
    }

    /// <summary>
    /// 共有中の攻撃接触を解除し、以後は各ドラム缶を単体管理へ戻します。
    /// </summary>
    private void DissolveGroup()
    {
        if (m_hasDissolved)
        {
            return;
        }

        m_hasDissolved = true;

        ReleaseMembers();
    }

    /// <summary>共有中の接触状態を解除し、登録メンバーを解放します。</summary>
    private void ReleaseMembers()
    {
        foreach (int attackSourceId in m_contactMembersBySource.Keys)
        {
            NotifyContactEnded(attackSourceId);
        }

        m_contactMembersBySource.Clear();
        m_damagedContactSourceIds.Clear();

        foreach (AttackReactiveInteractObject member in m_registeredMembers)
        {
            if (member != null)
            {
                member.UnregisterGroup(this);
            }
        }

        m_registeredMembers.Clear();
    }
}
