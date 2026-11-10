using System;
using UnityEngine;

/// <summary>
/// ElapsedTimerの経過量を利用してステージの残り制限時間を管理します。
/// ステージ設定Assetには依存せず、外部から設定された制限時間で動作します。
/// </summary>
[DisallowMultipleComponent]
public sealed class StageTimeLimitController : MonoBehaviour
{
    // 経過時間Timer
    [SerializeField, Header("参照")]
    private ElapsedTimer m_elapsedTimer;

    // 現在設定されている制限時間
    [SerializeField, Header("Runtime")]
    private double m_timeLimitSeconds;

    // 現在の残り時間
    [SerializeField]
    private double m_remainingSeconds;

    // 最後に観測した累計経過時間
    private double m_lastObservedElapsedSeconds;

    // 制限時間が設定済みか
    private bool m_isConfigured;

    // 制限時間切れか
    private bool m_isExpired;

    /// <summary>
    /// 残り時間が変更されたときに通知します。
    /// </summary>
    public event Action<double> RemainingTimeChanged;

    /// <summary>
    /// 制限時間が0になったときに通知します。
    /// </summary>
    public event Action TimeExpired;

    /// <summary>
    /// 現在設定されている制限時間を取得します。
    /// </summary>
    public double TimeLimitSeconds =>
        m_timeLimitSeconds;

    /// <summary>
    /// 現在の残り時間を取得します。
    /// </summary>
    public double RemainingSeconds =>
        m_remainingSeconds;

    /// <summary>
    /// 制限時間が設定済みか取得します。
    /// </summary>
    public bool IsConfigured =>
        m_isConfigured;

    /// <summary>
    /// 制限時間切れか取得します。
    /// </summary>
    public bool IsExpired =>
        m_isExpired;

    /// <summary>
    /// 現在カウントダウン中か取得します。
    /// </summary>
    public bool IsCountingDown =>
        m_isConfigured
        && m_elapsedTimer != null
        && m_elapsedTimer.IsRunning
        && !m_isExpired;

    private void Awake()
    {
        SyncElapsedTimeReference();
    }

    private void OnEnable()
    {
        if (m_elapsedTimer == null)
        {
            return;
        }

        m_elapsedTimer.ElapsedTimeChanged +=
            OnElapsedTimeChanged;
    }

    private void Start()
    {
        if (m_elapsedTimer != null)
        {
            return;
        }

        Debug.LogError(
            $"{nameof(ElapsedTimer)} が設定されていません。",
            this);

        enabled = false;
    }

    private void OnDisable()
    {
        if (m_elapsedTimer == null)
        {
            return;
        }

        m_elapsedTimer.ElapsedTimeChanged -=
            OnElapsedTimeChanged;
    }

    /// <summary>
    /// 制限時間を設定し、残り時間を初期化します。
    /// </summary>
    /// <param name="timeLimitSeconds">
    /// 設定する制限時間。
    /// </param>
    /// <returns>
    /// true：設定しました。
    /// false：制限時間が不正です。
    /// </returns>
    public bool Configure(
        double timeLimitSeconds)
    {
        if (timeLimitSeconds <= 0.0)
        {
            Debug.LogWarning(
                "制限時間は0秒より大きい値を設定してください。",
                this);

            return false;
        }

        m_timeLimitSeconds =
            timeLimitSeconds;

        m_remainingSeconds =
            timeLimitSeconds;

        m_isConfigured =
            true;

        m_isExpired =
            false;

        SyncElapsedTimeReference();

        RaiseRemainingTimeChanged();

        return true;
    }

    /// <summary>
    /// 残り時間を設定済みの制限時間へ戻します。
    /// 累計経過時間は変更しません。
    /// </summary>
    public void ResetTimeLimit()
    {
        if (!m_isConfigured)
        {
            return;
        }

        m_remainingSeconds =
            m_timeLimitSeconds;

        m_isExpired =
            false;

        SyncElapsedTimeReference();

        RaiseRemainingTimeChanged();
    }

    /// <summary>
    /// 残り時間を指定した値へ変更します。
    /// チェックポイント復元など状態復元用途を想定しています。
    /// </summary>
    /// <param name="remainingSeconds">
    /// 設定する残り時間。
    /// </param>
    /// <returns>
    /// true：設定しました。
    /// false：制限時間が未設定です。
    /// </returns>
    public bool SetRemainingTime(
        double remainingSeconds)
    {
        if (!m_isConfigured)
        {
            Debug.LogWarning(
                "制限時間が設定されていません。",
                this);

            return false;
        }

        m_remainingSeconds =
            Math.Clamp(
                remainingSeconds,
                0.0,
                m_timeLimitSeconds);

        m_isExpired =
            m_remainingSeconds <= 0.0;

        /*
         * Checkpoint復元後は現在のElapsedTimer値を
         * 新しい差分計算の基準点にします。
         */
        SyncElapsedTimeReference();

        RaiseRemainingTimeChanged();

        return true;
    }

    /// <summary>
    /// ElapsedTimerの変更を受け取り、
    /// 実際に進んだ時間だけ残り時間を減らします。
    /// </summary>
    /// <param name="elapsedSeconds">
    /// 現在の累計経過時間。
    /// </param>
    private void OnElapsedTimeChanged(
        double elapsedSeconds)
    {
        double deltaSeconds =
            elapsedSeconds
            - m_lastObservedElapsedSeconds;

        m_lastObservedElapsedSeconds =
            elapsedSeconds;

        /*
         * Scene復帰時のSetElapsedTimeなど、
         * Timer停止中の累計時間復元では
         * 制限時間を消費しません。
         */
        if (!m_isConfigured
            || !m_elapsedTimer.IsRunning
            || m_isExpired
            || deltaSeconds <= 0.0)
        {
            return;
        }

        ApplyCountdown(
            deltaSeconds);
    }

    /// <summary>
    /// 指定時間だけ残り時間を減らします。
    /// </summary>
    /// <param name="deltaSeconds">
    /// 消費する時間。
    /// </param>
    private void ApplyCountdown(
        double deltaSeconds)
    {
        bool wasExpired =
            m_isExpired;

        m_remainingSeconds =
            Math.Max(
                0.0,
                m_remainingSeconds
                - deltaSeconds);

        m_isExpired =
            m_remainingSeconds <= 0.0;

        RaiseRemainingTimeChanged();

        if (!wasExpired
            && m_isExpired)
        {
            TimeExpired?.Invoke();
        }
    }

    /// <summary>
    /// 現在のElapsedTimer値を差分計算の基準点へ設定します。
    /// </summary>
    private void SyncElapsedTimeReference()
    {
        m_lastObservedElapsedSeconds =
            m_elapsedTimer != null
                ? m_elapsedTimer.ElapsedSeconds
                : 0.0;
    }

    /// <summary>
    /// 残り時間変更を通知します。
    /// </summary>
    private void RaiseRemainingTimeChanged()
    {
        RemainingTimeChanged?.Invoke(
            m_remainingSeconds);
    }
}
