using UnityEngine;

/// <summary>攻撃中心の位置を伴うダメージを受け付けます。</summary>
public interface IDirectionalDamageable : IDamageable
{
    /// <summary>攻撃中心からのダメージを適用し、実際に受け付けたかを返します。</summary>
    bool TryTakeDamage(int damage, Vector3 attackCenter);
}
