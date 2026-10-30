/// <summary>プール再利用後の誤停止を防止する再生ハンドルです。</summary>
public readonly struct EffectHandle
{
    private readonly EffectInstance m_instance;
    private readonly int m_generation;

    /// <summary>再生世代を保持します。</summary>
    public EffectHandle(EffectInstance instance, int generation)
    {
        m_instance = instance;
        m_generation = generation;
    }

    /// <summary>現在の再生に対して操作可能か。</summary>
    public bool IsValid => m_instance != null && m_instance.IsGenerationActive(m_generation);

    /// <summary>現在の再生を停止します。</summary>
    /// <param name="stopMode">即時消去または自然消滅。</param>
    /// <returns>true：要求を受理しました。false：既に終了しています。</returns>
    public bool Stop(EffectStopMode stopMode = EffectStopMode.GRACEFUL)
    {
        return m_instance != null && m_instance.StopIfGenerationMatches(m_generation, stopMode);
    }
}
