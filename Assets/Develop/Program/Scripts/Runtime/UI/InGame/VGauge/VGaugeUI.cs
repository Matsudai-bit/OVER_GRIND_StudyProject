using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 渡されたVゲージ割合とチャージ演出を表示します。ゲージ値は保持しません。
/// </summary>
public class VGaugeUI : MonoBehaviour
{
    [SerializeField, Header("Gauge")]
    private Image gaugeImage;

    [SerializeField, Header("Blade")]
    private VBladeRotator bladeRotator;

    /// <summary>
    /// モデルから取得したゲージ割合を表示します。モデルの値は変更しません。
    /// </summary>
    /// <param name="rate">0から1までのゲージ割合。</param>
    public void SetGaugeRate(float rate)
    {
        if (gaugeImage != null)
        {
            gaugeImage.fillAmount = Mathf.Clamp01(rate);
        }
    }

    /// <summary>チャージ状態を刃の演出へ反映します。</summary>
    /// <param name="isCharging">true：高速回転、false：通常回転。</param>
    public void SetCharging(bool isCharging)
    {
        if (bladeRotator != null)
        {
            bladeRotator.SetGaugeUsing(isCharging);
        }
    }
}