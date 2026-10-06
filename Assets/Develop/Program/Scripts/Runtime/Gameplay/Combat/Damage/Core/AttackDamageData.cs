using System;
using UnityEngine;

/// <summary>
/// 攻撃で受け渡すダメージ量と攻撃識別情報を保持します。
/// </summary>
[Serializable]
public struct AttackDamageData
{
    [SerializeField, Min(0)]
    private int m_damage;

    [SerializeField]
    private AttackIdentifier m_attackIdentifier;

    /// <summary>
    /// 攻撃情報を生成します。
    /// </summary>
    /// <param name="damage">与えるダメージ量。</param>
    /// <param name="attackIdentifier">攻撃を識別するアセット。</param>
    public AttackDamageData(
        int damage,
        AttackIdentifier attackIdentifier = null)
    {
        m_damage = Mathf.Max(0, damage);
        m_attackIdentifier = attackIdentifier;
    }

    /// <summary>与えるダメージ量を取得します。</summary>
    public int Damage => Mathf.Max(0, m_damage);

    /// <summary>攻撃を識別するアセットを取得します。</summary>
    public AttackIdentifier AttackIdentifier => m_attackIdentifier;

    /// <summary>
    /// 攻撃IDを維持したままダメージ量を変更した情報を生成します。
    /// </summary>
    /// <param name="damage">変更後のダメージ量。</param>
    /// <returns>ダメージ量を変更した攻撃情報。</returns>
    public AttackDamageData WithDamage(int damage)
    {
        return new AttackDamageData(damage, AttackIdentifier);
    }
}
