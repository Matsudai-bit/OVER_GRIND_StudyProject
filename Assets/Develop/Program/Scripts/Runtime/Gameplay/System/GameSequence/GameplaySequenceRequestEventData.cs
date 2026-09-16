/// <summary>
/// ゲームプレイシーケンス開始要求の情報です。
/// </summary>
public readonly struct GameplaySequenceRequestEventData
{
    /// <summary>
    /// 開始要求されたシーケンスを取得します。
    /// </summary>
    public GameplaySequenceAsset Sequence { get; }

    public GameplaySequenceRequestEventData(
        GameplaySequenceAsset sequence)
    {
        Sequence = sequence;
    }
}