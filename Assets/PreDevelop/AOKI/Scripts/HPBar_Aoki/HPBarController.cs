using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.InputSystem;

public class HPBarController : MonoBehaviour
{
    [Header("数字スプライト素材 ")]
    [SerializeField] private Sprite[] numberSprites = new Sprite[10];

    [Header("HP表示用Image ")]
    [SerializeField] private Image digit10Image;  // 十の位
    [SerializeField] private Image digit1Image;   // 一の位

    [Header("UI要素のアサイン")]
    [SerializeField] private Image greenGauge;       // 緑ゲージ 
    [SerializeField] private Image redGauge;         // 赤ゲージ 

    [Header("アニメーション設定")]
    [SerializeField] private float maxHP = 99f;          // 最大HP
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
    /// ダメージを与える処理
    /// </summary>
    public void TakeDamage(float damage)
    {
        float previousHP = currentHP;
        currentHP = Mathf.Max(0, currentHP - damage);
        float targetFillAmount = currentHP / maxHP;

        // 緑ゲージを素早く減らす
        if (greenGauge != null)
        {
            greenGauge.DOFillAmount(targetFillAmount, greenDuration)
                .SetEase(Ease.OutQuad);
        }

        // 赤ゲージを少し遅れてアニメーションさせて減らす
        if (redGauge != null)
        {
            if (redTween != null && redTween.IsActive()) redTween.Kill();
            redTween = redGauge.DOFillAmount(targetFillAmount, redDuration)
                .SetDelay(redDelay)
                .SetEase(Ease.OutCubic);
        }

        // HP数字スプライトのカウントダウンアニメーション
        if (textTween != null && textTween.IsActive()) textTween.Kill();
        textTween = DOVirtual.Float(previousHP, currentHP, redDuration, value =>
        {
            UpdateHPDisplay(value);
        })
        .SetDelay(redDelay)         // 赤ゲージと同じだけ待ってからカウントダウン開始
        .SetEase(Ease.OutCubic);   // 赤ゲージと同じ減り方のカーブを適用
    }

    /// <summary>
    /// 初期状態に即時更新
    /// </summary>
    public void UpdateUIImmediate()
    {
        float fillAmount = currentHP / maxHP;
        if (greenGauge != null) greenGauge.fillAmount = fillAmount;
        if (redGauge != null) redGauge.fillAmount = fillAmount;
        UpdateHPDisplay(currentHP);
    }

    /// <summary>
    /// HPの数字スプライト更新処理 
    /// </summary>
    private void UpdateHPDisplay(float hp)
    {
        if (numberSprites == null || numberSprites.Length < 10) return;

        int hpInt = Mathf.RoundToInt(hp);
        int digit10 = (hpInt / 10) % 10;
        int digit1 = hpInt % 10;

        if (digit10Image != null) digit10Image.sprite = numberSprites[digit10];
        if (digit1Image != null) digit1Image.sprite = numberSprites[digit1];
    }

    // デバッグ用
    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            TakeDamage(15f);
        }
    }
}