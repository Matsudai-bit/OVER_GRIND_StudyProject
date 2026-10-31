using System;

/// <summary>同じHitRate更新内で複数の対象に当たった攻撃を識別します。</summary>
public static class AttackHitTickContext
{
    private static int s_nextId;
    private static int s_currentId;

    /// <summary>新しい攻撃ヒットの識別子を設定します。</summary>
    /// <returns>終了時に前の識別子へ戻すスコープ。</returns>
    public static Scope Begin()
    {
        int previousId = s_currentId;
        s_currentId = ++s_nextId;
        return new Scope(previousId);
    }

    /// <summary>現在の攻撃ヒット識別子を取得します。</summary>
    /// <param name="id">攻撃ヒット識別子。</param>
    /// <returns>ヒット処理中ならtrue。</returns>
    public static bool TryGetCurrent(out int id)
    {
        id = s_currentId;
        return id != 0;
    }

    /// <summary>以前の攻撃ヒット識別子へ戻します。</summary>
    public readonly struct Scope : IDisposable
    {
        private readonly int m_previousId;

        /// <summary>復元する識別子を保持します。</summary>
        /// <param name="previousId">復元する識別子。</param>
        public Scope(int previousId)
        {
            m_previousId = previousId;
        }

        /// <summary>スコープ開始前の識別子を復元します。</summary>
        public void Dispose()
        {
            s_currentId = m_previousId;
        }
    }
}
