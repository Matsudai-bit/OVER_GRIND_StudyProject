/// <summary>
/// ポーズ状態変更時の情報です。
/// </summary>
public readonly struct PauseChangedEventData
{
    /// <summary>
    /// 変更前のポーズ状態を取得します。
    /// </summary>
    public bool PreviousIsPaused { get; }

    /// <summary>
    /// 現在のポーズ状態を取得します。
    /// </summary>
    public bool IsPaused { get; }

    public PauseChangedEventData(
        bool previousIsPaused,
        bool isPaused)
    {
        PreviousIsPaused = previousIsPaused;
        IsPaused = isPaused;
    }
}