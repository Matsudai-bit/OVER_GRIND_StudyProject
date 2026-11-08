using System;
using UnityEngine;

/// <summary>
/// ポーズ画面に表示するステージ進行情報を管理します。
/// </summary>
[DisallowMultipleComponent]
public sealed class PauseStatusModel : MonoBehaviour
{
    // 現在の残り時間
    private double m_remainingSeconds;

    // 現在のリトライ回数
    private int m_retryCount;

    /// <summary>
    /// 残り時間が変更されたときに通知します。
    /// </summary>
    public event Action<double> RemainingTimeChanged;

    /// <summary>
    /// リトライ回数が変更されたときに通知します。
    /// </summary>
    public event Action<int> RetryCountChanged;

    /// <summary>
    /// 現在の残り時間を取得します。
    /// </summary>
    public double RemainingSeconds =>
        m_remainingSeconds;

    /// <summary>
    /// 現在のリトライ回数を取得します。
    /// </summary>
    public int RetryCount =>
        m_retryCount;

    /// <summary>
    /// ポーズ画面に表示する情報を設定します。
    /// </summary>
    /// <param name="remainingSeconds">
    /// 現在の残り時間。
    /// </param>
    /// <param name="retryCount">
    /// 現在のリトライ回数。
    /// </param>
    public void SetStatus(
        double remainingSeconds,
        int retryCount)
    {
        SetRemainingTime(
            remainingSeconds);

        SetRetryCount(
            retryCount);
    }

    /// <summary>
    /// 残り時間を設定します。
    /// </summary>
    /// <param name="remainingSeconds">
    /// 設定する残り時間。
    /// </param>
    private void SetRemainingTime(
        double remainingSeconds)
    {
        m_remainingSeconds =
            Math.Max(
                0.0,
                remainingSeconds);

        RemainingTimeChanged?.Invoke(
            m_remainingSeconds);
    }

    /// <summary>
    /// リトライ回数を設定します。
    /// </summary>
    /// <param name="retryCount">
    /// 設定するリトライ回数。
    /// </param>
    private void SetRetryCount(
        int retryCount)
    {
        m_retryCount =
            Mathf.Max(
                0,
                retryCount);

        RetryCountChanged?.Invoke(
            m_retryCount);
    }
}