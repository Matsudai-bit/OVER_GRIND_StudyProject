using System;
using UnityEngine;

/// <summary>
/// ステージシーンをどの状態から開始するかと、
/// ステージ実行中に引き継ぐ一時情報を保持します。
/// </summary>
[CreateAssetMenu(
    fileName = "StageSceneContext",
    menuName = "Game/Context/Stage Scene Context")]
public sealed class StageSceneContext : ScriptableObject
{
    private const int MIN_RETRY_COUNT = 0;

    // 次回ステージシーンの開始方法
    [NonSerialized]
    private StageEntryType m_entryType =
        StageEntryType.NEW_GAME;

    // 次回復帰時に使用するチェックポイント
    [NonSerialized]
    private StageCheckpointSnapshot
        m_checkpointSnapshot;

    // Scene再読み込み後も引き継ぐ累計経過時間
    [NonSerialized]
    private double m_carriedElapsedSeconds;

    // 現在プレイしているステージ名
    [NonSerialized]
    private string m_stageName =
        string.Empty;

    // 現在のステージで行ったリトライ回数
    [NonSerialized]
    private int m_retryCount =
        MIN_RETRY_COUNT;

    /// <summary>
    /// 現在設定されている開始方法を取得します。
    /// </summary>
    public StageEntryType EntryType =>
        m_entryType;

    /// <summary>
    /// チェックポイントスナップショットを取得します。
    /// </summary>
    public StageCheckpointSnapshot CheckpointSnapshot =>
        m_checkpointSnapshot;

    /// <summary>
    /// Sceneを跨いで引き継ぐ累計経過時間を取得します。
    /// </summary>
    public double CarriedElapsedSeconds =>
        m_carriedElapsedSeconds;

    /// <summary>
    /// 現在プレイしているステージ名を取得します。
    /// </summary>
    public string StageName =>
        m_stageName;

    /// <summary>
    /// 現在のステージで行ったリトライ回数を取得します。
    /// </summary>
    public int RetryCount =>
        m_retryCount;

    /// <summary>
    /// 現在プレイしているステージ名を設定します。
    /// </summary>
    /// <param name="stageName">設定するステージ名。</param>
    public void SetStageName(
        string stageName)
    {
        m_stageName =
            stageName ?? string.Empty;
    }

    /// <summary>
    /// 通常開始を設定します。
    /// 新しい挑戦としてリトライ回数も初期化します。
    /// </summary>
    public void SetNewGame()
    {
        ResetEntryRequest();

        m_retryCount =
            MIN_RETRY_COUNT;
    }

    /// <summary>
    /// ステージ最初からのリスタートを設定します。
    /// 累計経過時間は0秒からやり直し、リトライ回数を1加算します。
    /// </summary>
    public void SetRestart()
    {
        m_entryType =
            StageEntryType.RESTART;

        m_checkpointSnapshot =
            null;

        m_carriedElapsedSeconds =
            0.0;

        IncrementRetryCount();
    }

    /// <summary>
    /// チェックポイントからの復帰を設定します。
    /// 累計経過時間は死亡時点までの値を引き継ぎ、
    /// リトライ回数を1加算します。
    /// </summary>
    /// <param name="snapshot">
    /// 復帰に使用するスナップショット。
    /// </param>
    /// <param name="elapsedSeconds">
    /// 死亡・再開時点までの累計経過時間。
    /// </param>
    /// <returns>
    /// true：設定しました。
    /// false：スナップショットが無効です。
    /// </returns>
    public bool SetCheckpoint(
        StageCheckpointSnapshot snapshot,
        double elapsedSeconds)
    {
        if (snapshot == null)
        {
            return false;
        }

        m_entryType =
            StageEntryType.CHECKPOINT;

        // Scene側のSnapshotと共有しないよう複製して保持
        m_checkpointSnapshot =
            snapshot.CreateCopy();

        m_carriedElapsedSeconds =
            Math.Max(
                0.0,
                elapsedSeconds);

        IncrementRetryCount();

        return true;
    }

    /// <summary>
    /// チェックポイントからの復帰を設定します。
    /// 累計経過時間を引き継がない互換用オーバーロードです。
    /// </summary>
    /// <param name="snapshot">
    /// 復帰に使用するスナップショット。
    /// </param>
    /// <returns>
    /// true：設定しました。
    /// false：スナップショットが無効です。
    /// </returns>
    public bool SetCheckpoint(
        StageCheckpointSnapshot snapshot)
    {
        return SetCheckpoint(
            snapshot,
            0.0);
    }

    /// <summary>
    /// 現在の開始要求を取得します。
    /// 開始要求のみ消費し、ステージ名とリトライ回数は保持します。
    /// </summary>
    /// <returns>今回のステージ開始要求。</returns>
    public StageSceneRequest ConsumeRequest()
    {
        StageCheckpointSnapshot snapshot =
            m_checkpointSnapshot?.CreateCopy();

        StageSceneRequest request =
            new StageSceneRequest(
                m_entryType,
                snapshot,
                m_carriedElapsedSeconds);

        /*
         * StageName / RetryCountは結果表示まで必要なので、
         * SetNewGame()ではなく開始要求だけを初期化する。
         */
        ResetEntryRequest();

        return request;
    }

    /// <summary>
    /// Contextを完全な初期状態へ戻します。
    /// </summary>
    public void Clear()
    {
        ResetEntryRequest();

        m_stageName =
            string.Empty;

        m_retryCount =
            MIN_RETRY_COUNT;
    }

    /// <summary>
    /// リトライ回数を1加算します。
    /// </summary>
    private void IncrementRetryCount()
    {
        m_retryCount =
            Math.Max(
                MIN_RETRY_COUNT,
                m_retryCount + 1);
    }

    /// <summary>
    /// 次回Scene開始要求だけを初期化します。
    /// ステージ実行情報は変更しません。
    /// </summary>
    private void ResetEntryRequest()
    {
        m_entryType =
            StageEntryType.NEW_GAME;

        m_checkpointSnapshot =
            null;

        m_carriedElapsedSeconds =
            0.0;
    }
}
