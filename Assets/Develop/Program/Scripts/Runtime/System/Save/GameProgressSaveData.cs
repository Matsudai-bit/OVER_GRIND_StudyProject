using System;
using UnityEngine;

/// <summary>
/// ゲーム進行に関するセーブデータです。
/// </summary>
[Serializable]
public sealed class GameProgressSaveData
{
    /// <summary>
    /// ステージごとの記録データです。
    /// </summary>
    [Serializable]
    public struct StageData
    {
        // ステージランク
        [SerializeField]
        private int m_rank;

        // ベストタイム
        [SerializeField]
        private int m_bestTime;

        /// <summary>
        /// ステージランクを取得します。
        /// </summary>
        public int Rank => m_rank;

        /// <summary>
        /// ベストタイムを取得します。
        /// </summary>
        public int BestTime => m_bestTime;

        /// <summary>
        /// ステージデータを生成します。
        /// </summary>
        /// <param name="rank">初期ランク。</param>
        /// <param name="bestTime">初期ベストタイム。</param>
        public StageData(
            int rank = -1,
            int bestTime = -1)
        {
            m_rank = rank;
            m_bestTime = bestTime;
        }

        /// <summary>
        /// ステージランクを設定します。
        /// </summary>
        /// <param name="rank">設定するランク。</param>
        /// <returns>
        /// true：値を更新しました。
        /// false：値は変更されませんでした。
        /// </returns>
        public bool SetRank(int rank)
        {
            if (m_rank == rank)
            {
                return false;
            }

            m_rank = rank;

            return true;
        }

        /// <summary>
        /// ベストタイムを更新します。
        /// </summary>
        /// <param name="time">今回のクリアタイム。</param>
        /// <returns>
        /// true：ベストタイムを更新しました。
        /// false：更新する必要がありませんでした。
        /// </returns>
        public bool UpdateBestTime(int time)
        {
            if (time < 0)
            {
                return false;
            }

            // 未記録、または今回の方が速い場合のみ更新
            if (m_bestTime >= 0
                && time >= m_bestTime)
            {
                return false;
            }

            m_bestTime = time;

            return true;
        }
    }

    // 最後にクリアしたステージ
    [SerializeField, Min(0)]
    private int m_clearedStage = 0;

    // OPムービーを視聴済みか
    [SerializeField]
    private bool m_hasWatchedOpening = false;

    // ステージごとの記録
    [SerializeField]
    private SerializeDictionary<int, StageData> m_stageMap = new();

    /// <summary>
    /// 最後にクリアしたステージを取得します。
    /// </summary>
    public int ClearedStage => m_clearedStage;

    /// <summary>
    /// OPムービーを視聴済みか取得します。
    /// </summary>
    public bool HasWatchedOpening => m_hasWatchedOpening;

    /// <summary>
    /// ステージデータ一覧を取得します。
    /// </summary>
    public SerializeDictionary<int, StageData> StageDataMap =>
        m_stageMap;

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

        EnsureStageData(stageNumber);

        if (stageNumber <= m_clearedStage)
        {
            return false;
        }

        m_clearedStage = stageNumber;

        return true;
    }

    /// <summary>
    /// ステージランクを設定します。
    /// </summary>
    /// <param name="stageNumber">ステージ番号。</param>
    /// <param name="rank">設定するランク。</param>
    /// <returns>
    /// true：データを更新しました。
    /// false：データは変更されませんでした。
    /// </returns>
    public bool SetStageRank(
        int stageNumber,
        int rank)
    {
        if (stageNumber <= 0)
        {
            return false;
        }

        StageData stageData =
            GetOrCreateStageData(stageNumber);

        if (!stageData.SetRank(rank))
        {
            return false;
        }

        // StageDataは構造体なので書き戻す
        m_stageMap[stageNumber] =
            stageData;

        return true;
    }

    /// <summary>
    /// ステージのベストタイムを更新します。
    /// </summary>
    /// <param name="stageNumber">ステージ番号。</param>
    /// <param name="time">今回のクリアタイム。</param>
    /// <returns>
    /// true：ベストタイムを更新しました。
    /// false：更新する必要がありませんでした。
    /// </returns>
    public bool UpdateStageBestTime(
        int stageNumber,
        int time)
    {
        if (stageNumber <= 0)
        {
            return false;
        }

        StageData stageData =
            GetOrCreateStageData(stageNumber);

        if (!stageData.UpdateBestTime(time))
        {
            return false;
        }

        // StageDataは構造体なので書き戻す
        m_stageMap[stageNumber] =
            stageData;

        return true;
    }

    /// <summary>
    /// 指定されたステージの記録を取得します。
    /// </summary>
    /// <param name="stageNumber">ステージ番号。</param>
    /// <param name="stageData">取得したステージデータ。</param>
    /// <returns>
    /// true：ステージデータが存在します。
    /// false：ステージデータが存在しません。
    /// </returns>
    public bool TryGetStageData(
        int stageNumber,
        out StageData stageData)
    {
        stageData = default;

        if (stageNumber <= 0
            || m_stageMap == null)
        {
            return false;
        }

        return m_stageMap.TryGetValue(
            stageNumber,
            out stageData);
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

    /// <summary>
    /// 指定されたステージデータが存在することを保証します。
    /// </summary>
    /// <param name="stageNumber">ステージ番号。</param>
    private void EnsureStageData(int stageNumber)
    {
        if (m_stageMap == null)
        {
            m_stageMap =
                new SerializeDictionary<int, StageData>();
        }

        if (m_stageMap.ContainsKey(stageNumber))
        {
            return;
        }

        m_stageMap.TryAdd(
            stageNumber,
            new StageData());
    }

    /// <summary>
    /// 指定されたステージデータを取得し、
    /// 存在しない場合は新しく作成します。
    /// </summary>
    /// <param name="stageNumber">ステージ番号。</param>
    /// <returns>ステージデータ。</returns>
    private StageData GetOrCreateStageData(
        int stageNumber)
    {
        EnsureStageData(stageNumber);

        return m_stageMap[stageNumber];
    }
}