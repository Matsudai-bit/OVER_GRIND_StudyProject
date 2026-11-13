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
    /// Scene再読み込み後も引き継ぐ累計経過時間を取得します。
    /// </summary>
    public double CarriedElapsedSeconds { get; }

    /// <summary>
    /// ステージシーン開始要求を生成します。
    /// </summary>
    /// <param name="entryType">開始方法。</param>
    /// <param name="checkpointSnapshot">
    /// チェックポイントスナップショット。
    /// </param>
    /// <param name="carriedElapsedSeconds">
    /// Sceneを跨いで引き継ぐ累計経過時間。
    /// </param>
    public StageSceneRequest(
        StageEntryType entryType,
        StageCheckpointSnapshot checkpointSnapshot,
        double carriedElapsedSeconds)
    {
        EntryType =
            entryType;

        CheckpointSnapshot =
            checkpointSnapshot;

        CarriedElapsedSeconds =
            carriedElapsedSeconds;
    }

    /// <summary>
    /// 通常開始要求を取得します。
    /// </summary>
    public static StageSceneRequest NewGame =>
        new StageSceneRequest(
            StageEntryType.NEW_GAME,
            null,
            0.0);
}
