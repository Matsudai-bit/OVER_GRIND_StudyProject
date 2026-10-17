

/// <summary>
/// 攻撃ダメージを受け取る機能を定義します。
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// 攻撃ダメージを受け取ります。
    /// </summary>
    /// <param name="damageData">受けるダメージと攻撃識別情報。</param>
    void TakeDamage(AttackDamageData damageData);
}
