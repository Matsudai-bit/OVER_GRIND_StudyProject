using System;
using UnityEngine;

/// <summary>
/// ランクの種類を定義する列挙型です。
/// </summary>
public enum RankType
{
    S,
    A,
    B,
    C
}

/// <summary>
/// リザルト画面のランク判定・データを管理する仮 Model です。
/// </summary>
public class ResultRankModel : MonoBehaviour
{
    public event Action<RankType> OnRankChanged;

    /// <summary>決定されたランク</summary>
    public RankType CurrentRank { get; private set; } = RankType.B;

    /// <summary>
    /// クリアタイムとリトライ数をもとにランクを仮計算・決定します。
    /// </summary>
    /// <param name="clearTime">クリアタイム（秒）</param>
    /// <param name="retryCount">リトライ回数</param>
    public RankType CalculateRank(float clearTime, int retryCount = 0)
    {
        // 仮のランク判定ロジック
        if (clearTime <= 10f && retryCount == 0)
        {
            CurrentRank = RankType.S;
        }
        else if (clearTime <= 20f && retryCount <= 2)
        {
            CurrentRank = RankType.A;
        }
        else if (clearTime <= 40f)
        {
            CurrentRank = RankType.B;
        }
        else
        {
            CurrentRank = RankType.C;
        }

        OnRankChanged?.Invoke(CurrentRank);
        return CurrentRank;
    }

    /// <summary>
    /// 外部から直接ランクを設定したい場合
    /// </summary>
    public void SetRankDirect(RankType rank)
    {
        CurrentRank = rank;
        OnRankChanged?.Invoke(CurrentRank);
    }
}