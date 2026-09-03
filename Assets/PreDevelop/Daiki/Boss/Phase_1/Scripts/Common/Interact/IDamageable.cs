

/// <summary>
/// 攻撃ダメージを受け取る機能を定義します。
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// 攻撃ダメージを受け取ります。
    /// </summary>
    /// <param name="damage">受けるダメージ量。</param>
    void TakeDamage(int damage);
}