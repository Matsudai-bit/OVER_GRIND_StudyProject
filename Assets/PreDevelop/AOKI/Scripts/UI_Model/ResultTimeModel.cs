using System;
using UnityEngine;

public interface IResultTimeModel
{
    /// <summary>今回のクリアタイム（新しい数字）</summary>
    float ClearTime { get; }

    /// <summary>前の記録・自己ベストタイム（前の数字）</summary>
    float PersonalBestTime { get; }

    /// <summary>今回の方がタイムが良かった（速かった）かどうか</summary>
    bool IsNewRecord { get; }

    event Action<float> OnTimeChanged;
}

/// <summary>
/// 今回のタイム（新しい数字）と前の記録（前の数字）を比較できる Model スクリプトです。
/// </summary>
public class ResultTimeModel : MonoBehaviour, IResultTimeModel
{
    [Header("★ 今回のクリアタイム（新しい数字）")]
    [Tooltip("分 (0～99分)")]
    [Range(0, 99)]
    [SerializeField] private int m_debugMinutes = 1;

    [Tooltip("秒 (0～59秒)")]
    [Range(0, 59)]
    [SerializeField] private int m_debugSeconds = 31;

    [Tooltip("ミリ秒 (1/100秒単位: 0～99)")]
    [Range(0, 99)]
    [SerializeField] private int m_debugMilliseconds = 46;

    [Header("★ 前の記録 / 自己ベスト（前の数字）")]
    [Tooltip("分 (0～99分)")]
    [Range(0, 99)]
    [SerializeField] private int m_bestMinutes = 1;

    [Tooltip("秒 (0～59秒)")]
    [Range(0, 59)]
    [SerializeField] private int m_bestSeconds = 57;

    [Tooltip("ミリ秒 (1/100秒単位: 0～99)")]
    [Range(0, 99)]
    [SerializeField] private int m_bestMilliseconds = 00;

    /// <summary>値が変更された時に Presenter へ通知するイベント</summary>
    public event Action<float> OnTimeChanged;

    /// <summary>今回のクリアタイム（合計秒数）</summary>
    public float ClearTime
    {
        get
        {
            return (m_debugMinutes * 60f) + m_debugSeconds + (m_debugMilliseconds / 100f);
        }
    }

    /// <summary>前の記録・自己ベストタイム（合計秒数）</summary>
    public float PersonalBestTime
    {
        get
        {
            return (m_bestMinutes * 60f) + m_bestSeconds + (m_bestMilliseconds / 100f);
        }
    }

    /// <summary>
    /// 今回のタイムが前のタイムより速い（良かった）かどうか
    /// </summary>
    public bool IsNewRecord
    {
        get
        {
            if (ClearTime <= 0f) return false;

            // 自己ベストが 0 の場合は初回クリア扱い（新記録）
            if (PersonalBestTime <= 0f) return true;

            // 今回のタイム < 前のタイム なら新記録
            return ClearTime < PersonalBestTime;
        }
    }

    /// <summary>外部から今回のタイムを直接指定する場合</summary>
    public void SetClearTime(int minutes, int seconds, int milliseconds)
    {
        m_debugMinutes = Mathf.Clamp(minutes, 0, 99);
        m_debugSeconds = Mathf.Clamp(seconds, 0, 59);
        m_debugMilliseconds = Mathf.Clamp(milliseconds, 0, 99);

        OnTimeChanged?.Invoke(ClearTime);
    }

    /// <summary>外部から前の記録を直接指定する場合</summary>
    public void SetPersonalBestTime(int minutes, int seconds, int milliseconds)
    {
        m_bestMinutes = Mathf.Clamp(minutes, 0, 99);
        m_bestSeconds = Mathf.Clamp(seconds, 0, 59);
        m_bestMilliseconds = Mathf.Clamp(milliseconds, 0, 99);

        OnTimeChanged?.Invoke(ClearTime);
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            OnTimeChanged?.Invoke(ClearTime);
        }
    }
}