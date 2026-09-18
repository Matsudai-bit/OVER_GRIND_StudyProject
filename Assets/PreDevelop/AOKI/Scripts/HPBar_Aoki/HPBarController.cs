using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.InputSystem;

public class HPBarController : MonoBehaviour
{
    [Header("UI要素のアサイン")]
    [SerializeField] private Image greenGauge;       // 緑ゲージ (HP_Green)
    [SerializeField] private Image redGauge;         // 赤ゲージ (HP_Red)
    [SerializeField] private TextMeshProUGUI hpText; // HP数値テキスト (100など)

    [Header("アニメーション設定")]
    [SerializeField] private float maxHP = 100f;
    [SerializeField] private float greenDuration = 0.2f; // 緑ゲージが減る速度
    [SerializeField] private float redDelay = 0.4f;      // 赤ゲージが減り始めるまでの待ち時間
    [SerializeField] private float redDuration = 0.6f;   // 赤ゲージが追従して減る速度

    private float currentHP;
    private Tween redTween;
    private Tween textTween;

    void Start()
    {
        currentHP = maxHP;
        UpdateUIImmediate();
    }

    /// <summary>
    /// ダメージを与える処理（外部から呼び出す）
    /// </summary>
    public void TakeDamage(float damage)
    {
        float previousHP = currentHP;
        currentHP = Mathf.Max(0, currentHP - damage);
        float targetFillAmount = currentHP / maxHP;

        // 1. 緑ゲージを素早く減らす
        if (greenGauge != null)
        {
            greenGauge.DOFillAmount(targetFillAmount, greenDuration)
                .SetEase(Ease.OutQuad);
        }

        // 2. 赤ゲージを少し遅れてアニメーションさせて減らす
        if (redGauge != null)
        {
            if (redTween != null && redTween.IsActive()) redTween.Kill();
            redTween = redGauge.DOFillAmount(targetFillAmount, redDuration)
                .SetDelay(redDelay)
                .SetEase(Ease.OutCubic);
        }

        // 3. HP数値テキストのカウントダウンアニメーション
        if (hpText != null)
        {
            if (textTween != null && textTween.IsActive()) textTween.Kill();
            textTween = DOVirtual.Float(previousHP, currentHP, greenDuration + 0.1f, value =>
            {
                hpText.text = Mathf.RoundToInt(value).ToString();
            });
        }
    }

    /// <summary>
    /// 初期状態に即時更新
    /// </summary>
    public void UpdateUIImmediate()
    {
        float fillAmount = currentHP / maxHP;
        if (greenGauge != null) greenGauge.fillAmount = fillAmount;
        if (redGauge != null) redGauge.fillAmount = fillAmount;
        if (hpText != null) hpText.text = Mathf.RoundToInt(currentHP).ToString();
    }

    // デバッグ用（Spaceキーでダメージテスト）
    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            TakeDamage(15f);
        }
    }
}