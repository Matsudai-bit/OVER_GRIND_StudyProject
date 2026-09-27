using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

// HP用プレゼンター
public class HP_UI_Presenter : MonoBehaviour
{
    [Header("モデルとビュー")]
    [SerializeField] private HP_UI_Model hpModel;
    [SerializeField] private HPDisplayView hpNumberView;

    [Header("UI Elements")]
    [SerializeField] private Image greenGauge;
    [SerializeField] private Image redGauge;

    [Header("Animation Settings")]
    [SerializeField] private float greenDuration = 0.2f;
    [SerializeField] private float redDelay = 0.4f;
    [SerializeField] private float redDuration = 0.6f;

    private Tween redTween;
    private Tween textTween;
    private float displayHP; // 見た目上の現在HP

    void OnEnable()
    {
        // Modelのイベントを購読
        if (hpModel != null)
        {
            hpModel.OnHPChanged += HandleHPChanged;
            displayHP = hpModel.MaxHP;
        }
    }

    // ★追加：ゲーム開始直後にUIを初期化する
    void Start()
    {
        if (hpModel != null)
        {
            // アニメーションを待たずに、即座に最大HPの表示にする
            if (greenGauge != null) greenGauge.fillAmount = 1f;
            if (redGauge != null) redGauge.fillAmount = 1f;
            if (hpNumberView != null) hpNumberView.SetValue(Mathf.RoundToInt(hpModel.MaxHP));
        }
    }

    void OnDisable()
    {
        if (hpModel != null) hpModel.OnHPChanged -= HandleHPChanged;
    }

    // Modelの値が変わった時に自動的に呼ばれる
    private void HandleHPChanged(float currentHP, float maxHP)
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
            if (hpNumberView != null) hpNumberView.SetValue(Mathf.RoundToInt(value));
        })
        .SetDelay(redDelay).SetEase(Ease.OutCubic);
    }
}