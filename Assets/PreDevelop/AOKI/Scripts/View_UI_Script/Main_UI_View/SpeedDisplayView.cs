using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 速度とゲージのUI表示を管理するクラスです。
/// </summary>
public class SpeedDisplayView : MonoBehaviour
{
    /// <summary>数字スプライト素材 (0～9)</summary>
    [Header("数字スプライト素材 (0～9)")]
    [SerializeField] private Sprite[] m_numberSprites = new Sprite[10];

    /// <summary>速度の10の位を表示する画像</summary>
    [Header("スピード表示用Image")]
    [SerializeField] private Image m_digit10Image;

    /// <summary>速度の1の位を表示する画像</summary>
    [SerializeField] private Image m_digit1Image;

    /// <summary>速度の小数第一位を表示する画像</summary>
    [SerializeField] private Image m_digitDecimalImage;

    /// <summary>ゲージの増減を表示する画像</summary>
    [Header("ゲージ表示用Image")]
    [SerializeField] private Image m_gaugeFillImage;

    /// <summary>
    /// 速度表示を更新します。
    /// </summary>
    /// <param name="speed">現在の速度。</param>
    public void UpdateSpeed(float speed)
    {
        if (m_numberSprites == null || m_numberSprites.Length < 10) return;

        // 速度の整数部分と各桁の数値を計算する
        int speedInt = Mathf.FloorToInt(speed);
        int digit10 = (speedInt / 10) % 10;
        int digit1 = speedInt % 10;
        int digitDecimal = Mathf.FloorToInt((speed - speedInt) * 10f) % 10;

        // スプライトを適用する
        if (m_digit10Image != null) m_digit10Image.sprite = m_numberSprites[digit10];
        if (m_digit1Image != null) m_digit1Image.sprite = m_numberSprites[digit1];
        if (m_digitDecimalImage != null) m_digitDecimalImage.sprite = m_numberSprites[digitDecimal];
    }

    /// <summary>
    /// ゲージの表示を更新します。
    /// </summary>
    /// <param name="currentGauge">現在のゲージ割合（0.0～1.0）。</param>
    public void UpdateGauge(float currentGauge)
    {
        if (m_gaugeFillImage != null)
        {
            m_gaugeFillImage.fillAmount = currentGauge;
        }
    }
}