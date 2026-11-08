using System;
using UnityEngine;

/// <summary>
/// ランクの種類を定義する列挙型です。
/// </summary>
public enum RankType
{
    S_PLUS,
    S,
    A,
    B,
    C
}

/// <summary>
/// リザルト画面のランクデータを管理します。
/// ランク判定自体はStageSettingsAssetへ委譲します。
/// </summary>
public class ResultRankModel : MonoBehaviour
{
    /// <summary>
    /// ランクが変更されたときに通知します。
    /// </summary>
    public event Action<RankType> OnRankChanged;

    /// <summary>
    /// 決定されたランクを取得します。
    /// </summary>
    public RankType CurrentRank { get; private set; } =
        RankType.B;

    /// <summary>
    /// ResultTimeModelのクリアタイムを使用して、
    /// StageSettingsAssetへランク判定を要求します。
    /// </summary>
    /// <param name="timeModel">
    /// 今回のクリアタイムを保持するResultTimeModel。
    /// </param>
    /// <param name="stageSettings">
    /// ランク評価基準を保持するStageSettingsAsset。
    /// </param>
    /// <returns>
    /// true：ランクを設定しました。
    /// false：判定または変換に失敗しました。
    /// </returns>
    public bool EvaluateRank(
        IResultTimeModel timeModel,
        StageSettingsAsset stageSettings)
    {
        if (timeModel == null)
        {
            Debug.LogWarning(
                $"{nameof(IResultTimeModel)} が設定されていません。",
                this);

            return false;
        }

        return EvaluateRank(
            timeModel.ClearTime,
            stageSettings);
    }

    /// <summary>
    /// クリアタイムを使用して、
    /// StageSettingsAssetへランク判定を要求します。
    /// </summary>
    /// <param name="clearTime">
    /// 今回のクリアタイム（秒）。
    /// </param>
    /// <param name="stageSettings">
    /// ランク評価基準を保持するStageSettingsAsset。
    /// </param>
    /// <returns>
    /// true：ランクを設定しました。
    /// false：判定または変換に失敗しました。
    /// </returns>
    public bool EvaluateRank(
        float clearTime,
        StageSettingsAsset stageSettings)
    {
        if (stageSettings == null)
        {
            Debug.LogWarning(
                $"{nameof(StageSettingsAsset)} が設定されていません。",
                this);

            return false;
        }

        StageRank stageRank =
            stageSettings.EvaluateRank(
                clearTime);

        if (!TryConvertRank(
            stageRank,
            out RankType rank))
        {
            Debug.LogWarning(
                $"ランク判定結果を変換できませんでした。"
                + $" StageRank: {stageRank}",
                this);

            return false;
        }

        SetRankDirect(
            rank);

        return true;
    }

    /// <summary>
    /// 外部から直接ランクを設定します。
    /// </summary>
    /// <param name="rank">設定するランク。</param>
    public void SetRankDirect(
        RankType rank)
    {
        if (CurrentRank == rank)
        {
            return;
        }

        CurrentRank =
            rank;

        OnRankChanged?.Invoke(
            CurrentRank);
    }

    /// <summary>
    /// StageRankをリザルト表示用RankTypeへ変換します。
    /// </summary>
    /// <param name="stageRank">
    /// StageSettingsAssetが返したランク。
    /// </param>
    /// <param name="rank">
    /// 変換後のリザルト用ランク。
    /// </param>
    /// <returns>
    /// true：変換できました。
    /// false：対応するランクがありません。
    /// </returns>
    private static bool TryConvertRank(
        StageRank stageRank,
        out RankType rank)
    {
        switch (stageRank)
        {
            case StageRank.S_PLUS:
                rank =
                    RankType.S_PLUS;

                return true;

            case StageRank.S:
                rank =
                    RankType.S;

                return true;

            case StageRank.A:
                rank =
                    RankType.A;

                return true;

            case StageRank.B:
                rank =
                    RankType.B;

                return true;

            case StageRank.C:
                rank =
                    RankType.C;

                return true;

            default:
                rank =
                    default;

                return false;
        }
    }
}
