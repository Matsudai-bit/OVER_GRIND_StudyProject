/// <summary>
/// ステージシーン開始時の要求情報です。
/// </summary>
public readonly struct StageSceneRequest
{
    /// <summary>
    /// 開始方法を取得します。
    /// </summary>
    public StageEntryType EntryType { get; }

    /// <summary>
    /// チェックポイントスナップショットを取得します。
    /// </summary>
    public StageCheckpointSnapshot CheckpointSnapshot { get; }

    /// <summary>
    /// ステージシーン開始要求を生成します。
    /// </summary>
    /// <param name="entryType">開始方法。</param>
    /// <param name="checkpointSnapshot">
    /// チェックポイントスナップショット。
    /// </param>
    public StageSceneRequest(
        StageEntryType entryType,
        StageCheckpointSnapshot checkpointSnapshot)
    {
        EntryType = entryType;
        CheckpointSnapshot = checkpointSnapshot;
    }

    /// <summary>
    /// 通常開始要求を取得します。
    /// </summary>
    public static StageSceneRequest NewGame =>
        new StageSceneRequest(
            StageEntryType.NEW_GAME,
            null);
}
