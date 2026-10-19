using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// リザルト画面におけるリトライ回数の UI 表示（画像スプライト描画）を制御する View クラスです。
/// 不要な桁の GameObject 自体を非表示（SetActive(false)）にして表示崩れを防ぎます。
/// </summary>
public class ResultRetryView : MonoBehaviour
{
    private const int REQUIRED_SPRITE_COUNT = 10;
    private const int DIGIT_BASE = 10;

    /// <summary>
    /// 表示モードの定義です。
    /// </summary>
    public enum DisplayMode
    {
        DYNAMIC_DIGITS, // 必要な桁数だけ表示（例: 0 -> "0", 12 -> "12"）
        FIXED_DIGITS    // 指定した桁数で固定表示（例: 2桁固定なら 0 -> "00", 1 -> "01"）
    }

    [Header("★ 0～9の数字スプライト")]
    [Tooltip("0?9に対応するスプライト画像配列（要素数 10）")]
    [SerializeField] private Sprite[] m_numberSprites = new Sprite[REQUIRED_SPRITE_COUNT];

    [Header("★ 各桁の Image オブジェクト（※ 右端の「1の位」から順に登録）")]
    [Tooltip("数字を表示する各桁の Image コンポーネント（[0]: 1の位(右), [1]: 10の位, [2]: 100の位...）")]
    [SerializeField] private Image[] m_digitImages;

    [Header("★ 表示モード設定")]
    [Tooltip("Dynamic: 必要な桁数のみ表示 / Fixed: 常に指定桁数で表示")]
    [SerializeField] private DisplayMode m_displayMode = DisplayMode.DYNAMIC_DIGITS;

    [Tooltip("固定桁数表示（FIXED_DIGITS）時の表示桁数（例: 2 で \"01\" 表示）")]
    [SerializeField] private int m_fixedDigits = 2;

    /// <summary>
    /// Presenter から受け取ったリトライ回数を元に、各桁のスプライト画像を更新します。
    /// 不要な桁の GameObject 自体を SetActive(false) で完全に消去・詰めます。
    /// </summary>
    /// <param name="retryCount">表示するリトライ回数。</param>
    public void UpdateRetryCountDisplay(int retryCount)
    {
        if (m_digitImages == null || m_digitImages.Length == 0)
        {
            Debug.LogWarning($"{gameObject.name}: 各桁の Image オブジェクト (m_digitImages) が設定されていません。", this);
            return;
        }

        if (m_numberSprites == null || m_numberSprites.Length < REQUIRED_SPRITE_COUNT)
        {
            Debug.LogWarning($"{gameObject.name}: 0?9 の数字スプライト (m_numberSprites) が 10 個設定されていません。", this);
            return;
        }

        int clampedCount = Mathf.Max(0, retryCount);
        int currentNumber = clampedCount;
        int maxSupportedDigits = m_digitImages.Length;

        for (int i = 0; i < maxSupportedDigits; i++)
        {
            if (m_digitImages[i] == null)
            {
                continue;
            }

            int digitValue = currentNumber % DIGIT_BASE;
            bool shouldShow = false;

            if (m_displayMode == DisplayMode.FIXED_DIGITS)
            {
                // 固定桁モード: 指定桁数以内なら常に表示（0 埋め）
                shouldShow = (i < m_fixedDigits);
            }
            else
            {
                // 可変桁モード: 1の位は常に表示、上位桁は数値が存在する場合のみ表示
                shouldShow = (i == 0) || (currentNumber > 0);
            }

            if (shouldShow)
            {
                //オブジェクトごとアクティブ化してスプライトを設定
                m_digitImages[i].gameObject.SetActive(true);
                m_digitImages[i].sprite = m_numberSprites[digitValue];
            }
            else
            {
                //使わない桁は GameObject ごと完全に消す
                m_digitImages[i].gameObject.SetActive(false);
            }

            currentNumber /= DIGIT_BASE;
        }

        // Horizontal Layout Group などを使用している場合の位置を自動更新
        RectTransform rectTransform = transform as RectTransform;
        if (rectTransform != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }
    }
}