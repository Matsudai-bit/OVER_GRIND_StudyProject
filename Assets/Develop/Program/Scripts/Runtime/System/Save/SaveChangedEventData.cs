/// <summary>
/// セーブデータ状態変更時の情報です。
/// </summary>
public readonly struct SaveChangedEventData
{
    /// <summary>
    /// 状態変更種別を取得します。
    /// </summary>
    public SaveChangedEventType EventType { get; }

    /// <summary>
    /// 未保存の変更が存在するか取得します。
    /// </summary>
    public bool IsDirty { get; }

    public SaveChangedEventData(
        SaveChangedEventType eventType,
        bool isDirty)
    {
        EventType = eventType;
        IsDirty = isDirty;
    }
}