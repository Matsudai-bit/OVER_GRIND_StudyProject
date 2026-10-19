using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HPのUI表示とアニメーションを管理するクラスです。
/// </summary>
public class HpDisplayView : MonoBehaviour
{
    /// <summary>数字スプライト素材 (0～9)</summary>
    [Header("数字スプライト素材 (0～9)")]
    [SerializeField] private Sprite[] m_numberSprites = new Sprite[10];

    /// <summary>HPの100の位を表示する画像</summary>
    [Header("HP表示用Image")]
    [SerializeField] private Image m_digit100Image;

    /// <summary>HPの10の位を表示する画像</summary>
    [SerializeField] private Image m_digit10Image;

    /// <summary>HPの1の位を表示する画像</summary>
    [SerializeField] private Image m_digit1Image;

    /// <summary>ダメージ時に即座に減る緑色のゲージ</summary>
    [Header("Gauge Elements")]
    [SerializeField] private Image m_greenGauge;

    /// <summary>ダメージ時に遅れて減る赤色のゲージ</summary>
    [SerializeField] private Image m_redGauge;

    /// <summary>緑ゲージのアニメーション時間（秒）</summary>
    [Header("Animation Settings")]
    [SerializeField] private float m_greenDuration = 0.2f;

    /// <summary>赤ゲージが減り始めるまでの待機時間（秒）</summary>
    [SerializeField] private float m_redDelay = 0.4f;

    /// <summary>赤ゲージと数値アニメーションの実行時間（秒）</summary>
    [SerializeField] private float m_redDuration = 0.6f;

    /// <summary>赤ゲージ用のアニメーション状態</summary>
    private Tween m_redTween;

    /// <summary>テキスト数字用のアニメーション状態</summary>
    private Tween m_textTween;

    /// <summary>現在表示中のHP</summary>
    private float m_displayHp;

    /// <summary>
    /// HP表示の初期設定を行います。
    /// </summary>
    /// <param name="maxHp">最大HP。</param>
    public void Initialize(float maxHp)
    {
        m_displayHp = maxHp;

        // ゲージを満タン状態にする
        if (m_greenGauge != null) m_greenGauge.fillAmount = 1f;
        if (m_redGauge != null) m_redGauge.fillAmount = 1f;

        UpdateNumberSprites(Mathf.RoundToInt(maxHp));
    }

    /// <summary>
    /// HPの表示とゲージのアニメーションを更新します。
    /// </summary>
    /// <param name="currentHp">現在のHP。</param>
    /// <param name="maxHp">最大HP。</param>
    public void UpdateHpDisplay(float currentHp, float maxHp)
    {
        float targetFillAmount = currentHp / maxHp;
        float previousDisplayHp = m_displayHp;
        m_displayHp = currentHp;

        // 緑ゲージのアニメーション処理
        if (m_greenGauge != null)
        {
            m_greenGauge.DOFillAmount(targetFillAmount, m_greenDuration).SetEase(Ease.OutQuad);
        }

        // 赤ゲージのアニメーション処理
        if (m_redGauge != null)
        {
            if (m_redTween != null && m_redTween.IsActive())
            {
                m_redTween.Kill();
            }
            m_redTween = m_redGauge.DOFillAmount(targetFillAmount, m_redDuration)
                                   .SetDelay(m_redDelay)
                                   .SetEase(Ease.OutCubic);
        }

        // 数値表示のアニメーション処理
        if (m_textTween != null && m_textTween.IsActive())
        {
            m_textTween.Kill();
        }
        m_textTween = DOVirtual.Float(previousDisplayHp, currentHp, m_redDuration, value =>
        {
            UpdateNumberSprites(Mathf.RoundToInt(value));
        }).SetDelay(m_redDelay).SetEase(Ease.OutCubic);
    }

    /// <summary>
    /// 渡された数値をもとに、UI上の数字スプライトを切り替えます。
    /// </summary>
    /// <param name="value">表示する数値。</param>
    private void UpdateNumberSprites(int value)
    {
        if (m_numberSprites == null || m_numberSprites.Length < 10) return;

        // 0～999の範囲に制限する
        int safeValue = Mathf.Clamp(value, 0, 999);

        // 各桁の数値を計算する
        int digit100 = (safeValue / 100) % 10;
        int digit10 = (safeValue / 10) % 10;
        int digit1 = safeValue % 10;

        // スプライトを適用する
        if (m_digit100Image != null) m_digit100Image.sprite = m_numberSprites[digit100];
        if (m_digit10Image != null) m_digit10Image.sprite = m_numberSprites[digit10];
        if (m_digit1Image != null) m_digit1Image.sprite = m_numberSprites[digit1];
    }
}