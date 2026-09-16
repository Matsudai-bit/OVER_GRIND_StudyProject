using System;
using UnityEngine;

/// <summary>
/// ゲーム進行に関するセーブデータです。
/// </summary>
[Serializable]
public sealed class GameProgressSaveData
{
    // 最後にクリアしたステージ
    [SerializeField, Min(0)]
    private int m_clearedStage = 0;

    // OPムービーを視聴済みか
    [SerializeField]
    private bool m_hasWatchedOpening = false;

    /// <summary>
    /// 最後にクリアしたステージを取得します。
    /// </summary>
    public int ClearedStage => m_clearedStage;

    /// <summary>
    /// OPムービーを視聴済みか取得します。
    /// </summary>
    public bool HasWatchedOpening => m_hasWatchedOpening;

    /// <summary>
    /// ステージをクリア済みにします。
    /// </summary>
    /// <param name="stageNumber">クリアしたステージ番号。</param>
    /// <returns>
    /// true：データを更新しました。
    /// false：更新する必要がありませんでした。
    /// </returns>
    public bool MarkStageCleared(int stageNumber)
    {
        if (stageNumber <= 0)
        {
            return false;
        }

        if (stageNumber <= m_clearedStage)
        {
            return false;
        }

        m_clearedStage = stageNumber;

        return true;
    }

    /// <summary>
    /// OPムービーを視聴済みにします。
    /// </summary>
    /// <returns>
    /// true：データを更新しました。
    /// false：既に視聴済みでした。
    /// </returns>
    public bool MarkOpeningWatched()
    {
        if (m_hasWatchedOpening)
        {
            return false;
        }

        m_hasWatchedOpening = true;

        return true;
    }
}