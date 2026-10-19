using System;
using UnityEngine;

/// <summary>
/// 経過時間を計測します。
/// </summary>
[DisallowMultipleComponent]
public sealed class ElapsedTimer : MonoBehaviour
{
    // 使用する時間の種類
    [SerializeField, Header("Timer設定")]
    private ElapsedTimerTimeMode m_timeMode =
        ElapsedTimerTimeMode.SCALED;

    // シーン開始時に自動で計測を開始するか
    [SerializeField]
    private bool m_startOnStart = false;

    // 現在の経過時間
    [SerializeField, Header("Runtime")]
    private double m_elapsedSeconds = 0.0;

    // 現在計測中か
    private bool m_isRunning = false;

    /// <summary>
    /// 経過時間が変更されたときに通知します。
    /// </summary>
    public event Action<double> ElapsedTimeChanged;

    /// <summary>
    /// 現在の経過時間を秒単位で取得します。
    /// </summary>
    public double ElapsedSeconds => m_elapsedSeconds;

    /// <summary>
    /// 現在計測中か取得します。
    /// </summary>
    public bool IsRunning => m_isRunning;

    private void Start()
    {
        if (m_startOnStart)
        {
            StartTimer();
            return;
        }

        // 計測していない間はUpdate自体を停止
        enabled = false;
    }

    private void Update()
    {
        double deltaTime = GetDeltaTime();

        if (deltaTime <= 0.0)
        {
            return;
        }

        m_elapsedSeconds += deltaTime;

        RaiseElapsedTimeChanged();
    }

    /// <summary>
    /// 時間計測を開始します。
    /// </summary>
    public void StartTimer()
    {
        if (m_isRunning)
        {
            return;
        }

        m_isRunning = true;

        // Updateを有効化
        enabled = true;
    }

    /// <summary>
    /// 時間計測を停止します。
    /// </summary>
    public void StopTimer()
    {
        if (!m_isRunning)
        {
            return;
        }

        m_isRunning = false;

        // 不要なUpdateを停止
        enabled = false;
    }

    /// <summary>
    /// 経過時間を0秒へ戻します。
    /// </summary>
    public void ResetTimer()
    {
        if (m_elapsedSeconds == 0.0)
        {
            return;
        }

        m_elapsedSeconds = 0.0;

        RaiseElapsedTimeChanged();
    }

    /// <summary>
    /// 経過時間を0秒へ戻して計測を開始します。
    /// </summary>
    public void RestartTimer()
    {
        m_elapsedSeconds = 0.0;

        RaiseElapsedTimeChanged();

        StartTimer();
    }

    /// <summary>
    /// 経過時間を指定した値へ変更します。
    /// </summary>
    /// <param name="elapsedSeconds">設定する経過時間（秒）。</param>
    public void SetElapsedTime(double elapsedSeconds)
    {
        double newElapsedSeconds =
            Math.Max(0.0, elapsedSeconds);

        if (Math.Abs(
            m_elapsedSeconds - newElapsedSeconds) <
            double.Epsilon)
        {
            return;
        }

        m_elapsedSeconds = newElapsedSeconds;

        RaiseElapsedTimeChanged();
    }

    /// <summary>
    /// 現在の設定に対応したDeltaTimeを取得します。
    /// </summary>
    /// <returns>前フレームからの経過時間。</returns>
    private double GetDeltaTime()
    {
        switch (m_timeMode)
        {
            case ElapsedTimerTimeMode.UNSCALED:
                return Time.unscaledDeltaTime;

            case ElapsedTimerTimeMode.SCALED:
            default:
                return Time.deltaTime;
        }
    }

    /// <summary>
    /// 経過時間変更を通知します。
    /// </summary>
    private void RaiseElapsedTimeChanged()
    {
        ElapsedTimeChanged?.Invoke(
            m_elapsedSeconds);
    }
}