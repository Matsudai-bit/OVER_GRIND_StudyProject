/// <summary>
/// ポーズ要求時の情報です。
/// </summary>
public readonly struct PauseRequestEventData
{
    /// <summary>
    /// ポーズ要求種別を取得します。
    /// </summary>
    public PauseRequestType RequestType { get; }

    public PauseRequestEventData(PauseRequestType requestType)
    {
        RequestType = requestType;
    }
}