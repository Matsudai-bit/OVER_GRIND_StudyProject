/// <summary>
/// ゲームプレイモード変更時の情報です。
/// </summary>
public readonly struct GameplayModeChangedEventData
{
    /// <summary>
    /// 変更前のモードを取得します。
    /// </summary>
    public GameplayModeType PreviousMode { get; }

    /// <summary>
    /// 変更後のモードを取得します。
    /// </summary>
    public GameplayModeType CurrentMode { get; }

    public GameplayModeChangedEventData(
        GameplayModeType previousMode,
        GameplayModeType currentMode)
    {
        PreviousMode = previousMode;
        CurrentMode = currentMode;
    }
}