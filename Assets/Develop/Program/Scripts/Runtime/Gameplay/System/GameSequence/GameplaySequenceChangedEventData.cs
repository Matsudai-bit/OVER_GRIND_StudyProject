/// <summary>
/// ゲームプレイシーケンス変更時の情報です。
/// </summary>
public readonly struct GameplaySequenceChangedEventData
{
    /// <summary>
    /// 対象のシーケンスを取得します。
    /// </summary>
    public GameplaySequenceAsset Sequence { get; }

    /// <summary>
    /// シーケンスイベント種別を取得します。
    /// </summary>
    public GameplaySequenceEventType EventType { get; }

    public GameplaySequenceChangedEventData(
        GameplaySequenceAsset sequence,
        GameplaySequenceEventType eventType)
    {
        Sequence = sequence;
        EventType = eventType;
    }
}