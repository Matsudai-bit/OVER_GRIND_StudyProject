using UnityEngine;
using UnityEngine.UI;

public class SpeedDisplayView : MonoBehaviour
{
    [Header("数字スプライト素材 (0～9)")]
    [SerializeField] private Sprite[] numberSprites = new Sprite[10];

    [Header("スピード表示用Image")]
    [SerializeField] private Image digit10Image;
    [SerializeField] private Image digit1Image;
    [SerializeField] private Image digitDecimalImage;

    [Header("ゲージ表示用Image")]
    [SerializeField] private Image gaugeFillImage; // Presenterから移動[cite: 25]

    public void UpdateSpeed(float speed)
    {
        if (numberSprites == null || numberSprites.Length < 10) return;

        int speedInt = Mathf.FloorToInt(speed);
        int digit10 = (speedInt / 10) % 10;
        int digit1 = speedInt % 10;
        int digitDec = Mathf.FloorToInt((speed - speedInt) * 10f) % 10;

        if (digit10Image != null) digit10Image.sprite = numberSprites[digit10];
        if (digit1Image != null) digit1Image.sprite = numberSprites[digit1];
        if (digitDecimalImage != null) digitDecimalImage.sprite = numberSprites[digitDec];
    }

    public void UpdateGauge(float currentGauge)
    {
        if (gaugeFillImage != null) gaugeFillImage.fillAmount = currentGauge;
    }
}