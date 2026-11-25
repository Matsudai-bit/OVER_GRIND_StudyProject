using System;
using UnityEngine;

/// <summary>
/// ステージの残り制限時間をチェックポイントへ保存・復元します。
/// 累計経過時間は保存対象にしません。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(StageTimeLimitController))]
public sealed class StageTimeLimitCheckpointParticipant :
    CheckpointParticipant
{
    /// <summary>
    /// 制限時間のチェックポイント記録データです。
    /// </summary>
    [Serializable]
    private struct TimeLimitSnapshot
    {
        [SerializeField]
        private double m_remainingSeconds;

        /// <summary>
        /// 記録時の残り時間を取得します。
        /// </summary>
        public double RemainingSeconds =>
            m_remainingSeconds;

        /// <summary>
        /// 制限時間スナップショットを生成します。
        /// </summary>
        /// <param name="remainingSeconds">
        /// 記録する残り時間。
        /// </param>
        public TimeLimitSnapshot(
            double remainingSeconds)
        {
            m_remainingSeconds =
                remainingSeconds;
        }
    }

    // 制限時間管理
    [SerializeField, Header("Time Limit")]
    private StageTimeLimitController
        m_timeLimitController;

    private void Awake()
    {
        ResolveTimeLimitController();
    }

    /// <summary>
    /// 現在の残り制限時間をJSONへ変換します。
    /// </summary>
    /// <returns>
    /// 制限時間状態を表すJSON。
    /// 取得できない場合はnull。
    /// </returns>
    protected override string CaptureStateJson()
    {
        if (!ResolveTimeLimitController())
        {
            return null;
        }

        TimeLimitSnapshot snapshot =
            new TimeLimitSnapshot(
                m_timeLimitController
                    .RemainingSeconds);

        return JsonUtility.ToJson(
            snapshot);
    }

    /// <summary>
    /// JSONから残り制限時間を復元します。
    /// </summary>
    /// <param name="json">
    /// 復元する制限時間データ。
    /// </param>
    /// <returns>
    /// true：復元しました。
    /// false：復元できませんでした。
    /// </returns>
    protected override bool RestoreStateJson(
        string json)
    {
        if (!ResolveTimeLimitController()
            || string.IsNullOrEmpty(json))
        {
            return false;
        }

        TimeLimitSnapshot snapshot =
            JsonUtility.FromJson<TimeLimitSnapshot>(
                json);

        m_timeLimitController.SetRemainingTime(
            snapshot.RemainingSeconds);

        return true;
    }

    /// <summary>
    /// StageTimeLimitController参照を取得します。
    /// </summary>
    /// <returns>
    /// true：取得できました。
    /// false：取得できませんでした。
    /// </returns>
    private bool ResolveTimeLimitController()
    {
        if (m_timeLimitController != null)
        {
            return true;
        }

        m_timeLimitController =
            GetComponent<StageTimeLimitController>();

        if (m_timeLimitController != null)
        {
            return true;
        }

        Debug.LogError(
            $"{nameof(StageTimeLimitController)} "
            + "が見つかりません。",
            this);

        return false;
    }
}
