using System;
using UnityEngine;

/// <summary>
/// ステージ固有のゲーム設定を保持します。
/// </summary>
[CreateAssetMenu(
    fileName = "StageSettings",
    menuName = "Game/Stage/Stage Settings")]
public sealed class StageSettingsAsset : ScriptableObject
{
    // リザルトなどで表示するステージ名
    [SerializeField, Header("ステージ情報")]
    private string m_stageName = "Stage";

    // ステージの制限時間
    [SerializeField, Header("制限時間"), Min(0.0f)]
    private float m_timeLimitSeconds = 300.0f;

    // S+評価となるクリア時間の上限
    [SerializeField, Header("クリア評価")]
    [Min(0.0f)]
    private float m_sPlusTimeSeconds = 60.0f;

    // S評価となるクリア時間の上限
    [SerializeField, Min(0.0f)]
    private float m_sTimeSeconds = 90.0f;

    // A評価となるクリア時間の上限
    [SerializeField, Min(0.0f)]
    private float m_aTimeSeconds = 120.0f;

    // B評価となるクリア時間の上限
    [SerializeField, Min(0.0f)]
    private float m_bTimeSeconds = 180.0f;

    /// <summary>
    /// リザルトなどで表示するステージ名を取得します。
    /// </summary>
    public string StageName =>
        m_stageName;

    /// <summary>
    /// ステージの制限時間を取得します。
    /// </summary>
    public double TimeLimitSeconds =>
        m_timeLimitSeconds;

    /// <summary>
    /// S+評価の上限時間を取得します。
    /// </summary>
    public double SPlusTimeSeconds =>
        m_sPlusTimeSeconds;

    /// <summary>
    /// S評価の上限時間を取得します。
    /// </summary>
    public double STimeSeconds =>
        m_sTimeSeconds;

    /// <summary>
    /// A評価の上限時間を取得します。
    /// </summary>
    public double ATimeSeconds =>
        m_aTimeSeconds;

    /// <summary>
    /// B評価の上限時間を取得します。
    /// </summary>
    public double BTimeSeconds =>
        m_bTimeSeconds;

    /// <summary>
    /// 指定したクリア時間から評価ランクを取得します。
    /// </summary>
    /// <param name="clearTimeSeconds">
    /// ステージクリアまでにかかった累計時間。
    /// </param>
    /// <returns>
    /// 制限時間内であれば対応する評価ランク。
    /// 制限時間外または不正な値の場合はNONE。
    /// </returns>
    public StageRank EvaluateRank(
        double clearTimeSeconds)
    {
        if (clearTimeSeconds < 0.0
            || clearTimeSeconds > m_timeLimitSeconds)
        {
            return StageRank.NONE;
        }

        /*
         * 「○秒未満」を基準とするため < で判定します。
         *
         * 例:
         * A = 120秒未満
         * 119.99秒 -> A
         * 120.00秒 -> B
         */
        if (clearTimeSeconds <
            m_sPlusTimeSeconds)
        {
            return StageRank.S_PLUS;
        }

        if (clearTimeSeconds <
            m_sTimeSeconds)
        {
            return StageRank.S;
        }

        if (clearTimeSeconds <
            m_aTimeSeconds)
        {
            return StageRank.A;
        }

        if (clearTimeSeconds <
            m_bTimeSeconds)
        {
            return StageRank.B;
        }

        // B基準を超えても制限時間内にクリアしていればC
        return StageRank.C;
    }

    /// <summary>
    /// 指定時間がステージ制限時間内か確認します。
    /// </summary>
    /// <param name="elapsedSeconds">
    /// 確認する経過時間。
    /// </param>
    /// <returns>
    /// true：制限時間内です。
    /// false：制限時間外です。
    /// </returns>
    public bool IsWithinTimeLimit(
        double elapsedSeconds)
    {
        return elapsedSeconds >= 0.0
            && elapsedSeconds <=
                m_timeLimitSeconds;
    }

#if UNITY_EDITOR

    private void OnValidate()
    {
        m_sPlusTimeSeconds =
            Mathf.Max(
                0.0f,
                m_sPlusTimeSeconds);

        m_sTimeSeconds =
            Mathf.Max(
                m_sPlusTimeSeconds,
                m_sTimeSeconds);

        m_aTimeSeconds =
            Mathf.Max(
                m_sTimeSeconds,
                m_aTimeSeconds);

        m_bTimeSeconds =
            Mathf.Max(
                m_aTimeSeconds,
                m_bTimeSeconds);

        m_timeLimitSeconds =
            Mathf.Max(
                m_bTimeSeconds,
                m_timeLimitSeconds);
    }

#endif
}
