using System;
using UnityEngine;

/// <summary>
/// リザルト画面におけるリトライ回数データのインターフェースです。
/// </summary>
public interface IResultRetryModel
{
    /// <summary>リトライ回数が変更された際に発生するイベント。</summary>
    event Action<int> OnRetryCountChanged;

    /// <summary>現在のリトライ回数を取得します。</summary>
    int RetryCount { get; }
}

/// <summary>
/// リザルト画面におけるリトライ回数データを保持・管理する Model クラスです。
/// </summary>
public class ResultRetryModel : MonoBehaviour, IResultRetryModel
{
    private const int DEFAULT_RETRY_COUNT = 0;

    /// <summary>
    /// リトライ回数が変更された際に発生するイベントです。
    /// </summary>
    public event Action<int> OnRetryCountChanged;

    [Header("リトライ回数設定（テスト・初期値）")]
    [Tooltip("インスペクターからテスト・確認用に設定可能な初期リトライ回数です")]
    [SerializeField] private int m_initialRetryCount = DEFAULT_RETRY_COUNT;

    private int m_retryCount;

    /// <summary>
    /// 現在のリトライ回数を取得します。
    /// </summary>
    public int RetryCount => m_retryCount;

    /// <summary>
    /// コンポーネントの初期化を行います。
    /// </summary>
    private void Awake()
    {
        SetRetryCount(m_initialRetryCount);
    }

    /// <summary>
    /// リトライ回数を直接設定します。
    /// </summary>
    /// <param name="count">設定するリトライ回数。</param>
    public void SetRetryCount(int count)
    {
        int clampedCount = Mathf.Max(DEFAULT_RETRY_COUNT, count);

        if (m_retryCount == clampedCount)
        {
            return;
        }

        m_retryCount = clampedCount;
        OnRetryCountChanged?.Invoke(m_retryCount);
    }

    /// <summary>
    /// リトライ回数を 1 加算します。
    /// </summary>
    public void IncrementRetryCount()
    {
        SetRetryCount(m_retryCount + 1);
    }
}