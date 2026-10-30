/// <summary>
/// 個別再生または一括再生を安全に停止するハンドルです。
/// </summary>
public readonly struct EffectHandle
{
    // 単体エフェクトの再生対象
    private readonly EffectInstance m_instance;
    // プールの再利用による誤停止を防ぐ世代番号
    private readonly int m_generation;
    // 同一IDで一括再生したハンドル群（単体再生時はnull）
    private readonly EffectHandle[] m_children;

    /// <summary>
    /// 単一の再生インスタンスを保持します。
    /// </summary>
    public EffectHandle(EffectInstance instance, int generation)
    {
        m_instance = instance;
        m_generation = generation;
        m_children = null;
    }

    /// <summary>
    /// 複数の再生をまとめて保持します。
    /// </summary>
    private EffectHandle(EffectHandle[] children)
    {
        m_instance = null;
        m_generation = 0;
        m_children = children;
    }

    /// <summary>
    /// 再生中、または自然停止待機中のインスタンスが1つ以上あるかを返します。
    /// </summary>
    public bool IsValid => ActiveCount > 0;

    /// <summary>
    /// 現在有効な再生インスタンス数を取得します。
    /// </summary>
    public int ActiveCount
    {
        get
        {
            if (m_children == null)
            {
                return m_instance != null && m_instance.IsGenerationActive(m_generation) ? 1 : 0;
            }

            int count = 0;
            foreach (EffectHandle child in m_children)
            {
                count += child.ActiveCount;
            }
            return count;
        }
    }

    /// <summary>
    /// 再生中のすべてのインスタンスに停止要求を送ります。
    /// </summary>
    /// <param name="stopMode">停止方法。</param>
    /// <returns>true：1つ以上停止できました。false：すべて終了済みです。</returns>
    public bool Stop(EffectStopMode stopMode = EffectStopMode.GRACEFUL)
    {
        if (m_children == null)
        {
            return m_instance != null && m_instance.StopIfGenerationMatches(m_generation, stopMode);
        }

        bool hasStopped = false;
        foreach (EffectHandle child in m_children)
        {
            // 短絡評価せず、必ず全インスタンスを停止します。
            if (child.Stop(stopMode))
            {
                hasStopped = true;
            }
        }
        return hasStopped;
    }

    /// <summary>
    /// 複数のハンドルを1つにまとめます。
    /// </summary>
    internal static EffectHandle Combine(System.Collections.Generic.IReadOnlyList<EffectHandle> handles)
    {
        if (handles == null || handles.Count == 0)
        {
            return default;
        }
        if (handles.Count == 1)
        {
            return handles[0];
        }

        // 呼び出し元のリストが変更されても影響しないようにコピーします。
        EffectHandle[] children = new EffectHandle[handles.Count];
        for (int i = 0; i < handles.Count; i++)
        {
            children[i] = handles[i];
        }
        return new EffectHandle(children);
    }
}
