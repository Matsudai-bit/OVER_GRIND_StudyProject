using UnityEngine;

/// <summary>攻撃中心の位置を伴うダメージを受け付けます。</summary>
public interface IDirectionalDamageable : IDamageable
{
    /// <summary>攻撃中心からのダメージを適用し、実際に受け付けたかを返します。</summary>
    /// <param name="damageData">受けるダメージと攻撃識別情報。</param>
    /// <param name="attackCenter">攻撃中心のワールド座標。</param>
    /// <returns>true：ダメージを受け付けた。false：ダメージを拒否した。</returns>
    bool TryTakeDamage(AttackDamageData damageData, Vector3 attackCenter);
}
