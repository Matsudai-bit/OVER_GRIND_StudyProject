using UnityEngine;
using TMPro; // TextMeshProを使用

public class ParameterView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI vGaugeText;

    // HPの表示を更新する
    public void RenderHP(int hpValue)
    {
        if (hpText != null)
        {
            // hpTextだけを更新する
            hpText.text = $"HP: {hpValue}";
        }
    }

    // Vゲージの表示を更新する
    public void RenderVGauge(int vGaugeValue)
    {
        if (vGaugeText != null)
        {
            // "Gauge: " にし、受け取った数値(vGaugeValue)を入れる
            vGaugeText.text = $"Gauge: {vGaugeValue}";
        }
    }
}