using System;

/// <summary>
/// 同期的な攻撃適用中に使用する威力種別を一時的に保持します。
/// </summary>
public static class AttackPowerContext
{
    private static AttackPowerType s_current = AttackPowerType.NORMAL;
    private static bool s_hasCurrent;

    /// <summary>
    /// 指定した威力種別を一時的に適用します。
    /// </summary>
    /// <param name="attackPowerType">適用する威力種別。</param>
    /// <returns>スコープ終了時に以前の威力種別へ戻すハンドル。</returns>
    public static Scope Begin(AttackPowerType attackPowerType)
    {
        AttackPowerType previous = s_current;
        bool previousHasCurrent = s_hasCurrent;
        s_current = attackPowerType;
        s_hasCurrent = true;
        return new Scope(previous, previousHasCurrent);
    }

    /// <summary>
    /// 現在適用中の威力種別を取得します。
    /// </summary>
    /// <param name="attackPowerType">現在適用中の威力種別。</param>
    /// <returns>true：一時的な威力種別が設定中。false：設定されていない。</returns>
    public static bool TryGetCurrent(
        out AttackPowerType attackPowerType)
    {
        attackPowerType = s_current;
        return s_hasCurrent;
    }

    /// <summary>
    /// 攻撃威力種別を以前の状態へ戻すスコープです。
    /// </summary>
    public readonly struct Scope : IDisposable
    {
        private readonly AttackPowerType m_previous;
        private readonly bool m_previousHasCurrent;

        /// <summary>復元する威力種別を保持します。</summary>
        /// <param name="previous">復元する威力種別。</param>
        /// <param name="previousHasCurrent">復元後に一時設定が有効か。</param>
        public Scope(
            AttackPowerType previous,
            bool previousHasCurrent)
        {
            m_previous = previous;
            m_previousHasCurrent = previousHasCurrent;
        }

        /// <summary>攻撃威力種別をスコープ開始前の状態へ戻します。</summary>
        public void Dispose()
        {
            s_current = m_previous;
            s_hasCurrent = m_previousHasCurrent;
        }
    }
}
