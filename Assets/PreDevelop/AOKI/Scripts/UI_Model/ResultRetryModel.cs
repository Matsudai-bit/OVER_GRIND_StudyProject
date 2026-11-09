using System;
using UnityEngine;

/// <summary>
/// リザルト画面におけるリトライ回数データのインターフェースです。
/// </summary>
public interface IResultRetryModel
{
    /// <summary>
    /// リトライ回数が変更された際に発生するイベント。
    /// </summary>
    event Action<int> OnRetryCountChanged;

    /// <summary>
    /// 現在のリトライ回数を取得します。
    /// </summary>
    int RetryCount { get; }
}

/// <summary>
/// リザルト画面におけるリトライ回数データを保持・管理します。
/// </summary>
public class ResultRetryModel :
    MonoBehaviour,
    IResultRetryModel
{
    private const int DEFAULT_RETRY_COUNT = 0;

    /// <summary>
    /// リトライ回数が変更された際に発生するイベントです。
    /// </summary>
    public event Action<int> OnRetryCountChanged;

    [Header("リトライ回数設定（テスト・初期値）")]
    [Tooltip(
        "インスペクターからテスト・確認用に設定可能な初期リトライ回数です")]
    [SerializeField]
    private int m_initialRetryCount =
        DEFAULT_RETRY_COUNT;

    // 現在のリトライ回数
    private int m_retryCount;

    /// <summary>
    /// 現在のリトライ回数を取得します。
    /// </summary>
    public int RetryCount =>
        m_retryCount;

    private void Awake()
    {
        SetRetryCount(
            m_initialRetryCount);
    }

    /// <summary>
    /// StageSceneContextからリトライ回数を取得して設定します。
    /// </summary>
    /// <param name="stageSceneContext">
    /// リトライ回数を保持しているContext。
    /// </param>
    /// <returns>
    /// true：設定しました。
    /// false：Contextが設定されていません。
    /// </returns>
    public bool SetRetryCount(
        StageSceneContext stageSceneContext)
    {
        if (stageSceneContext == null)
        {
            return false;
        }

        SetRetryCount(
            stageSceneContext.RetryCount);

        return true;
    }

    /// <summary>
    /// リトライ回数を直接設定します。
    /// </summary>
    /// <param name="count">設定するリトライ回数。</param>
    public void SetRetryCount(
        int count)
    {
        int clampedCount =
            Mathf.Max(
                DEFAULT_RETRY_COUNT,
                count);

        if (m_retryCount == clampedCount)
        {
            /*
             * リザルト開始時の初期値が同じ場合でも、
             * Viewへ現在値を通知できるようイベントを発行する。
             */
            OnRetryCountChanged?.Invoke(
                m_retryCount);

            return;
        }

        m_retryCount =
            clampedCount;

        OnRetryCountChanged?.Invoke(
            m_retryCount);
    }

    /// <summary>
    /// リトライ回数を1加算します。
    /// </summary>
    public void IncrementRetryCount()
    {
        SetRetryCount(
            m_retryCount + 1);
    }
}
