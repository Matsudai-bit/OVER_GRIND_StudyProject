/// <summary>
/// ゲームプレイCue要求時の情報です。
/// </summary>
public readonly struct GameplayCueRequestEventData
{
    /// <summary>
    /// 対象のCueを取得します。
    /// </summary>
    public GameplayCueAsset Cue { get; }

    /// <summary>
    /// 要求種別を取得します。
    /// </summary>
    public GameplayCueRequestType RequestType { get; }

    public GameplayCueRequestEventData(
        GameplayCueAsset cue,
        GameplayCueRequestType requestType)
    {
        Cue = cue;
        RequestType = requestType;
    }
}