/// <summary>
/// ゲームプレイCue変更時の情報です。
/// </summary>
public readonly struct GameplayCueChangedEventData
{
    /// <summary>
    /// 対象のCueを取得します。
    /// </summary>
    public GameplayCueAsset Cue { get; }

    /// <summary>
    /// Cueの状態変更種別を取得します。
    /// </summary>
    public GameplayCueEventType EventType { get; }

    public GameplayCueChangedEventData(
        GameplayCueAsset cue,
        GameplayCueEventType eventType)
    {
        Cue = cue;
        EventType = eventType;
    }
}