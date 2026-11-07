using System;
using UnityEngine;

/// <summary>
/// ステージシーンをどの状態から開始するかを一時的に保持します。
/// </summary>
[CreateAssetMenu(
    fileName = "StageSceneContext",
    menuName = "Game/Context/Stage Scene Context")]
public sealed class StageSceneContext : ScriptableObject
{
    // 次回ステージシーンの開始方法
    [NonSerialized]
    private StageEntryType m_entryType =
        StageEntryType.NEW_GAME;

    // 次回復帰時に使用するチェックポイント
    [NonSerialized]
    private StageCheckpointSnapshot m_checkpointSnapshot;

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
    /// 通常開始を設定します。
    /// </summary>
    public void SetNewGame()
    {
        m_entryType =
            StageEntryType.NEW_GAME;

        m_checkpointSnapshot =
            null;
    }

    /// <summary>
    /// ステージ最初からのリスタートを設定します。
    /// </summary>
    public void SetRestart()
    {
        m_entryType =
            StageEntryType.RESTART;

        m_checkpointSnapshot =
            null;
    }

    /// <summary>
    /// チェックポイントからの復帰を設定します。
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
        if (snapshot == null)
        {
            return false;
        }

        m_entryType =
            StageEntryType.CHECKPOINT;

        // Scene側のSnapshotと共有しないよう複製して保持
        m_checkpointSnapshot =
            snapshot.CreateCopy();

        return true;
    }

    /// <summary>
    /// 現在の開始要求を取得し、Contextを初期状態へ戻します。
    /// </summary>
    /// <returns>今回のステージ開始要求。</returns>
    public StageSceneRequest ConsumeRequest()
    {
        StageCheckpointSnapshot snapshot =
            m_checkpointSnapshot?.CreateCopy();

        StageSceneRequest request =
            new StageSceneRequest(
                m_entryType,
                snapshot);

        SetNewGame();

        return request;
    }

    /// <summary>
    /// Contextを初期状態へ戻します。
    /// </summary>
    public void Clear()
    {
        SetNewGame();
    }
}
