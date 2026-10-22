using System;
using TMPro;
using UnityEngine;

/// <summary>
/// リザルト画面におけるリトライ回数の UI 表示（TextMeshPro）を制御する View クラスです。
/// </summary>
public class ResultRetryView : MonoBehaviour
{
    /// <summary>
    /// 表示モードの定義です。
    /// </summary>
    public enum DisplayMode
    {
        DYNAMIC_DIGITS, // 必要な桁数だけ表示（例: 0 -> "0", 12 -> "12"）
        FIXED_DIGITS    // 指定した桁数で固定表示（例: 2桁固定なら 0 -> "00", 1 -> "01"）
    }

    [Header("TextMeshPro UI 参照")]
    [Tooltip("リトライ回数を表示する UI テキスト (TextMeshProUGUI) コンポーネント")]
    [SerializeField] private TextMeshProUGUI m_retryText;

    [Header("表示モード設定")]
    [Tooltip("Dynamic: 必要な桁数のみ表示 / Fixed: 常に指定桁数で表示")]
    [SerializeField] private DisplayMode m_displayMode = DisplayMode.DYNAMIC_DIGITS;

    [Tooltip("固定桁数表示（FIXED_DIGITS）時の表示桁数（例: 2 で \"01\" 表示）")]
    [SerializeField] private int m_fixedDigits = 2;

    private void Awake()
    {
        EnsureReferences();
    }

    /// <summary>
    /// 未設定の場合、自身の GameObject や子要素から TextMeshProUGUI を自動取得します。
    /// </summary>
    private void EnsureReferences()
    {
        if (m_retryText == null)
        {
            m_retryText = GetComponent<TextMeshProUGUI>();
            if (m_retryText == null)
            {
                m_retryText = GetComponentInChildren<TextMeshProUGUI>();
            }
        }
    }

    /// <summary>
    /// Presenter から受け取ったリトライ回数を元に、TextMeshPro のテキスト表示を更新します。
    /// </summary>
    /// <param name="retryCount">表示するリトライ回数。</param>
    public void UpdateRetryCountDisplay(int retryCount)
    {
        EnsureReferences();

        if (m_retryText == null)
        {
            Debug.LogWarning($"{gameObject.name}: TextMeshProUGUI コンポーネント (m_retryText) が設定されていません。", this);
            return;
        }

        int clampedCount = Mathf.Max(0, retryCount);

        if (m_displayMode == DisplayMode.FIXED_DIGITS)
        {
            // 指定桁数で 0 埋めして表示（例: m_fixedDigits = 2 の場合 "01"）
            string format = "D" + Mathf.Max(1, m_fixedDigits);
            m_retryText.text = clampedCount.ToString(format);
        }
        else
        {
            // 可変桁数でそのまま表示（例: "0", "12"）
            m_retryText.text = clampedCount.ToString();
        }
    }
}