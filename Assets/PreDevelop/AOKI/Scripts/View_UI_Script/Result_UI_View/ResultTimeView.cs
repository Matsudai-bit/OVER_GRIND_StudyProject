using System;
using TMPro;
using UnityEngine;

/// <summary>
/// タイム（秒数またはフォーマット済み文字列）を受け取り、TextMeshPro を用いてタイムを表示する View クラスです。
/// </summary>
public class ResultTimeView : MonoBehaviour
{
    // タイムの上限・下限値
    private const float MIN_TOTAL_SECONDS = 0f;
    private const float MAX_TOTAL_SECONDS = 5999.99f;

    // タイムのフォーマット（分:秒.ミリ秒 2桁）
    private const string TIME_FORMAT = @"mm\:ss\.ff";

    [Header("TextMeshPro UI 参照")]
    /// <summary>タイム表示用の TextMeshProUGUI コンポーネント。</summary>
    [Tooltip("タイムを表示する UI テキスト (TextMeshProUGUI) コンポーネント")]
    [SerializeField] private TextMeshProUGUI m_timeText;

    private void Awake()
    {
        EnsureReferences();
    }

    /// <summary>
    /// 未設定の場合、自身の GameObject や子要素から TextMeshProUGUI を自動取得します。
    /// </summary>
    private void EnsureReferences()
    {
        if (m_timeText == null)
        {
            m_timeText = GetComponent<TextMeshProUGUI>();
            if (m_timeText == null)
            {
                m_timeText = GetComponentInChildren<TextMeshProUGUI>();
            }
        }
    }

    /// <summary>
    /// Presenter から受け取った秒数を整形（例: "01:23.45"）して TextMeshPro に表示します。
    /// </summary>
    /// <param name="totalSeconds">表示する合計秒数。</param>
    public void DisplayTime(float totalSeconds)
    {
        EnsureReferences();
        if (m_timeText == null) return;

        // 負数・オーバーフローの防止処理を行います
        float clampedSeconds = Mathf.Clamp(totalSeconds, MIN_TOTAL_SECONDS, MAX_TOTAL_SECONDS);

        // TimeSpan を使用して "mm:ss.ff" 形式の文字列へフォーマットします
        TimeSpan timeSpan = TimeSpan.FromSeconds(clampedSeconds);
        m_timeText.text = timeSpan.ToString(TIME_FORMAT);
    }

    /// <summary>
    /// Presenter から受け取ったタイム文字列（例: "01:23.45"）を直接 TextMeshPro に反映します。
    /// </summary>
    /// <param name="timeText">表示するタイム文字列。</param>
    public void DisplayTime(string timeText)
    {
        EnsureReferences();
        if (m_timeText == null) return;

        m_timeText.text = timeText;
    }
}