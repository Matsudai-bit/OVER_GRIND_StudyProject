using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.InputSystem;

public class HPBarUIController : MonoBehaviour
{
    [Header("Viewへの参照")]
    [SerializeField] private NumberSpriteView numberView; // 追加

    [Header("HP表示用Image (桁ごとのUI)")]
    [SerializeField] private Image digit100Image;
    [SerializeField] private Image digit10Image;
    [SerializeField] private Image digit1Image;

    [Header("UI要素のアサイン")]
    [SerializeField] private Image greenGauge;
    [SerializeField] private Image redGauge;

    [Header("アニメーション設定")]
    [SerializeField] private float maxHP = 100f;
    [SerializeField] private float greenDuration = 0.2f;
    [SerializeField] private float redDelay = 0.4f;
    [SerializeField] private float redDuration = 0.6f;

    private float currentHP;
    private Tween redTween;
    private Tween textTween;

    void Start()
    {
        currentHP = maxHP;
        UpdateUIImmediate();
    }

    public void TakeDamage(float damage)
    {
        float previousHP = currentHP;
        currentHP = Mathf.Max(0, currentHP - damage);
        float targetFillAmount = currentHP / maxHP;

        if (greenGauge != null)
        {
            greenGauge.DOFillAmount(targetFillAmount, greenDuration)
                .SetEase(Ease.OutQuad);
        }

        if (redGauge != null)
        {
            if (redTween != null && redTween.IsActive()) redTween.Kill();
            redTween = redGauge.DOFillAmount(targetFillAmount, redDuration)
                .SetDelay(redDelay)
                .SetEase(Ease.OutCubic);
        }

        if (textTween != null && textTween.IsActive()) textTween.Kill();
        textTween = DOVirtual.Float(previousHP, currentHP, redDuration, value =>
        {
            UpdateHPDisplay(value);
        })
        .SetDelay(redDelay)
        .SetEase(Ease.OutCubic);
    }

    public void UpdateUIImmediate()
    {
        float fillAmount = currentHP / maxHP;
        if (greenGauge != null) greenGauge.fillAmount = fillAmount;
        if (redGauge != null) redGauge.fillAmount = fillAmount;
        UpdateHPDisplay(currentHP);
    }

    private void UpdateHPDisplay(float hp)
    {
        if (numberView == null) return; // Viewがない場合は処理しない

        int hpInt = Mathf.RoundToInt(hp);

        // 共通のViewを利用して各桁の画像を設定
        numberView.SetDigit(digit100Image, (hpInt / 100) % 10);
        numberView.SetDigit(digit10Image, (hpInt / 10) % 10);
        numberView.SetDigit(digit1Image, hpInt % 10);
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            TakeDamage(15f);
        }
    }
}