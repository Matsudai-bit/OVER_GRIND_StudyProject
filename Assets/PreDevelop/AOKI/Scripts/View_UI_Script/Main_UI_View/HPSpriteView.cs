using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class HPDisplayView : MonoBehaviour
{
    [Header("数字スプライト素材 (0～9)")]
    [SerializeField] private Sprite[] numberSprites = new Sprite[10];

    [Header("HP表示用Image")]
    [SerializeField] private Image digit100Image;
    [SerializeField] private Image digit10Image;
    [SerializeField] private Image digit1Image;

    [Header("Gauge Elements")]
    [SerializeField] private Image greenGauge; // Presenterから移動[cite: 23]
    [SerializeField] private Image redGauge;   // Presenterから移動[cite: 23]

    [Header("Animation Settings")]
    [SerializeField] private float greenDuration = 0.2f;
    [SerializeField] private float redDelay = 0.4f;
    [SerializeField] private float redDuration = 0.6f;

    private Tween redTween;
    private Tween textTween;
    private float displayHP;

    public void Initialize(float maxHP)
    {
        displayHP = maxHP;
        if (greenGauge != null) greenGauge.fillAmount = 1f;
        if (redGauge != null) redGauge.fillAmount = 1f;
        UpdateNumberSprites(Mathf.RoundToInt(maxHP));
    }

    public void UpdateHPDisplay(float currentHP, float maxHP)
    {
        float targetFillAmount = currentHP / maxHP;
        float previousDisplayHP = displayHP;
        displayHP = currentHP;

        if (greenGauge != null)
            greenGauge.DOFillAmount(targetFillAmount, greenDuration).SetEase(Ease.OutQuad);

        if (redGauge != null)
        {
            if (redTween != null && redTween.IsActive()) redTween.Kill();
            redTween = redGauge.DOFillAmount(targetFillAmount, redDuration).SetDelay(redDelay).SetEase(Ease.OutCubic);
        }

        if (textTween != null && textTween.IsActive()) textTween.Kill();
        textTween = DOVirtual.Float(previousDisplayHP, currentHP, redDuration, value =>
        {
            UpdateNumberSprites(Mathf.RoundToInt(value));
        }).SetDelay(redDelay).SetEase(Ease.OutCubic);
    }

    private void UpdateNumberSprites(int value)
    {
        if (numberSprites == null || numberSprites.Length < 10) return;

        int safeValue = Mathf.Clamp(value, 0, 999);
        int digit100 = (safeValue / 100) % 10;
        int digit10 = (safeValue / 10) % 10;
        int digit1 = safeValue % 10;

        if (digit100Image != null) digit100Image.sprite = numberSprites[digit100];
        if (digit10Image != null) digit10Image.sprite = numberSprites[digit10];
        if (digit1Image != null) digit1Image.sprite = numberSprites[digit1];
    }
}