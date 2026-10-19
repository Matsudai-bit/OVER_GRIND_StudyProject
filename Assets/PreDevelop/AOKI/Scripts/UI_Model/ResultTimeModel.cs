using System;
using UnityEngine;

/// <summary>
/// リザルト画面でタイムデータを管理するためのインターフェースです。
/// </summary>
public interface IResultTimeModel
{
    /// <summary>
    /// 今回のクリアタイム（合計秒数）を取得します。
    /// </summary>
    float ClearTime { get; }

    /// <summary>
    /// 前回の自己ベストタイム（合計秒数）を取得します。
    /// </summary>
    float PersonalBestTime { get; }

    /// <summary>
    /// 今回のタイムが新記録かどうかを取得します。
    /// </summary>
    /// <returns>
    /// true：新記録です。
    /// false：新記録ではありません。
    /// </returns>
    bool IsNewRecord { get; }

    /// <summary>
    /// タイム値が変更された時に発生するイベントです。
    /// </summary>
    event Action<float> OnTimeChanged;
}

/// <summary>
/// 今回のクリアタイムと前回の自己ベストタイムを管理・比較するモデルクラスです。
/// </summary>
public class ResultTimeModel : MonoBehaviour, IResultTimeModel
{
    // 時間範囲の制限値
    private const int MIN_MINUTES = 0;
    private const int MAX_MINUTES = 99;
    private const int MIN_SECONDS = 0;
    private const int MAX_SECONDS = 59;
    private const int MIN_MILLISECONDS = 0;
    private const int MAX_MILLISECONDS = 99;

    // 単位変換定数
    private const float SECONDS_PER_MINUTE = 60f;
    private const float MILLISECONDS_PER_SECOND = 100f;
    private const float DEFAULT_CLEAR_TIME = 0f;

    [Header("今回のクリアタイム（新しい数字）")]
    [Tooltip("分 (0～99分)")]
    [Range(MIN_MINUTES, MAX_MINUTES)]
    [SerializeField] private int m_debugMinutes = 1; // 今回のクリアタイム（分）

    [Tooltip("秒 (0～59秒)")]
    [Range(MIN_SECONDS, MAX_SECONDS)]
    [SerializeField] private int m_debugSeconds = 31; // 今回のクリアタイム（秒）

    [Tooltip("ミリ秒 (1/100秒単位: 0～99)")]
    [Range(MIN_MILLISECONDS, MAX_MILLISECONDS)]
    [SerializeField] private int m_debugMilliseconds = 46; // 今回のクリアタイム（ミリ秒）

    [Header("前の記録 / 自己ベスト（前の数字）")]
    [Tooltip("分 (0～99分)")]
    [Range(MIN_MINUTES, MAX_MINUTES)]
    [SerializeField] private int m_bestMinutes = 1; // 前回の自己ベストタイム（分）

    [Tooltip("秒 (0～59秒)")]
    [Range(MIN_SECONDS, MAX_SECONDS)]
    [SerializeField] private int m_bestSeconds = 57; // 前回の自己ベストタイム（秒）

    [Tooltip("ミリ秒 (1/100秒単位: 0～99)")]
    [Range(MIN_MILLISECONDS, MAX_MILLISECONDS)]
    [SerializeField] private int m_bestMilliseconds = 0; // 前回の自己ベストタイム（ミリ秒）

    /// <summary>
    /// タイム値が変更された時に発生するイベントです。
    /// </summary>
    public event Action<float> OnTimeChanged;

    /// <summary>
    /// 今回のクリアタイム（合計秒数）を取得します。
    /// </summary>
    public float ClearTime
    {
        get
        {
            // 分、秒、ミリ秒を合計秒数に変換して返します
            return (m_debugMinutes * SECONDS_PER_MINUTE) + m_debugSeconds + (m_debugMilliseconds / MILLISECONDS_PER_SECOND);
        }
    }

    /// <summary>
    /// 前回の自己ベストタイム（合計秒数）を取得します。
    /// </summary>
    public float PersonalBestTime
    {
        get
        {
            // 分、秒、ミリ秒を合計秒数に変換して返します
            return (m_bestMinutes * SECONDS_PER_MINUTE) + m_bestSeconds + (m_bestMilliseconds / MILLISECONDS_PER_SECOND);
        }
    }

    /// <summary>
    /// 今回のタイムが前回の記録より速いかどうかを取得します。
    /// </summary>
    /// <returns>
    /// true：新記録（今回のタイムの方が速い、または初回記録）です。
    /// false：新記録ではありません。
    /// </returns>
    public bool IsNewRecord
    {
        get
        {
            // クリアタイムが無効（0以下）の場合は新記録判定を行いません
            if (ClearTime <= DEFAULT_CLEAR_TIME)
            {
                return false;
            }

            // 自己ベストが未記録（0以下）の場合は初回クリアとして新記録扱いとします
            if (PersonalBestTime <= DEFAULT_CLEAR_TIME)
            {
                return true;
            }

            // 今回のタイムが前回のタイムより小さい（速い）場合に新記録判定とします
            return ClearTime < PersonalBestTime;
        }
    }

    /// <summary>
    /// 外部から今回のクリアタイムを直接設定します。
    /// </summary>
    /// <param name="minutes">設定する分。</param>
    /// <param name="seconds">設定する秒。</param>
    /// <param name="milliseconds">設定するミリ秒。</param>
    public void SetClearTime(int minutes, int seconds, int milliseconds)
    {
        // 入力値を規定範囲内にクランプします
        m_debugMinutes = Mathf.Clamp(minutes, MIN_MINUTES, MAX_MINUTES);
        m_debugSeconds = Mathf.Clamp(seconds, MIN_SECONDS, MAX_SECONDS);
        m_debugMilliseconds = Mathf.Clamp(milliseconds, MIN_MILLISECONDS, MAX_MILLISECONDS);

        // タイム変更を通知します
        OnTimeChanged?.Invoke(ClearTime);
    }

    /// <summary>
    /// 外部から前回の自己ベストタイムを直接設定します。
    /// </summary>
    /// <param name="minutes">設定する分。</param>
    /// <param name="seconds">設定する秒。</param>
    /// <param name="milliseconds">設定するミリ秒。</param>
    public void SetPersonalBestTime(int minutes, int seconds, int milliseconds)
    {
        // 入力値を規定範囲内にクランプします
        m_bestMinutes = Mathf.Clamp(minutes, MIN_MINUTES, MAX_MINUTES);
        m_bestSeconds = Mathf.Clamp(seconds, MIN_SECONDS, MAX_SECONDS);
        m_bestMilliseconds = Mathf.Clamp(milliseconds, MIN_MILLISECONDS, MAX_MILLISECONDS);

        // タイム変更を通知します
        OnTimeChanged?.Invoke(ClearTime);
    }

    /// <summary>
    /// インスペクターで値が変更された際に実行されます。
    /// </summary>
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            OnTimeChanged?.Invoke(ClearTime);
        }
    }
}