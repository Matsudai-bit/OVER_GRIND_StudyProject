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

    [SerializeField]
    private AttackPowerType m_attackPowerType;

    private Vector3 m_sourceFacingDirection;
    private bool m_hasSourceFacingDirection;

    /// <summary>
    /// 攻撃情報を生成します。
    /// </summary>
    /// <param name="damage">与えるダメージ量。</param>
    /// <param name="attackIdentifier">攻撃を識別するアセット。</param>
    /// <param name="attackPowerType">攻撃の威力種別。</param>
    public AttackDamageData(
        int damage,
        AttackIdentifier attackIdentifier = null,
        AttackPowerType attackPowerType = AttackPowerType.NORMAL)
    {
        m_damage = Mathf.Max(0, damage);
        m_attackIdentifier = attackIdentifier;
        m_attackPowerType = attackPowerType;
        m_sourceFacingDirection = Vector3.zero;
        m_hasSourceFacingDirection = false;
    }

    /// <summary>与えるダメージ量を取得します。</summary>
    public int Damage => Mathf.Max(0, m_damage);

    /// <summary>攻撃を識別するアセットを取得します。</summary>
    public AttackIdentifier AttackIdentifier => m_attackIdentifier;

    /// <summary>攻撃の威力種別を取得します。</summary>
    public AttackPowerType PowerType => m_attackPowerType;

    /// <summary>
    /// 攻撃IDを維持したままダメージ量を変更した情報を生成します。
    /// </summary>
    /// <param name="damage">変更後のダメージ量。</param>
    /// <returns>ダメージ量を変更した攻撃情報。</returns>
    public AttackDamageData WithDamage(int damage)
    {
        AttackDamageData result = new AttackDamageData(
            damage,
            AttackIdentifier,
            PowerType);

        result.m_sourceFacingDirection = m_sourceFacingDirection;
        result.m_hasSourceFacingDirection = m_hasSourceFacingDirection;
        return result;
    }

    /// <summary>
    /// ダメージ量と攻撃IDを維持したまま威力種別を変更した情報を生成します。
    /// </summary>
    /// <param name="attackPowerType">変更後の威力種別。</param>
    /// <returns>威力種別を変更した攻撃情報。</returns>
    public AttackDamageData WithAttackPowerType(
        AttackPowerType attackPowerType)
    {
        AttackDamageData result = new AttackDamageData(
            Damage,
            AttackIdentifier,
            attackPowerType);

        result.m_sourceFacingDirection = m_sourceFacingDirection;
        result.m_hasSourceFacingDirection = m_hasSourceFacingDirection;
        return result;
    }

    /// <summary>攻撃元キャラクターの正面方向を追加した攻撃情報を生成します。</summary>
    /// <param name="sourceFacingDirection">攻撃元のワールド正面方向。</param>
    /// <returns>有効な水平正面方向を保持した攻撃情報。</returns>
    public AttackDamageData WithSourceFacingDirection(
        Vector3 sourceFacingDirection)
    {
        AttackDamageData result = this;
        sourceFacingDirection.y = 0.0f;

        if (sourceFacingDirection.sqrMagnitude <= 0.0001f)
        {
            result.m_sourceFacingDirection = Vector3.zero;
            result.m_hasSourceFacingDirection = false;
            return result;
        }

        result.m_sourceFacingDirection = sourceFacingDirection.normalized;
        result.m_hasSourceFacingDirection = true;
        return result;
    }

    /// <summary>攻撃元キャラクターの水平正面方向を取得します。</summary>
    /// <param name="sourceFacingDirection">正規化済みのワールド正面方向。</param>
    /// <returns>true：方向情報がある。false：方向情報がない。</returns>
    public bool TryGetSourceFacingDirection(
        out Vector3 sourceFacingDirection)
    {
        sourceFacingDirection = m_sourceFacingDirection;
        return m_hasSourceFacingDirection;
    }
}
