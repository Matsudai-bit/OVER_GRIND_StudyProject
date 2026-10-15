using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 速度とゲージのUI表示を管理するクラスです。
/// </summary>
public class SpeedDisplayView : MonoBehaviour
{
    [Header("数字スプライト素材 (0～9)")]
    [SerializeField] private Sprite[] m_numberSprites = new Sprite[10];

    [Header("スピード表示用Image")]
    [SerializeField] private Image m_digit10Image;
    [SerializeField] private Image m_digit1Image;
    [SerializeField] private Image m_digitDecimalImage;

    [Header("ゲージ連番スプライト素材 (150枚・波アニメーション用)")]
    [SerializeField] private Sprite[] m_gaugeSprites = new Sprite[150];

    [Header("ゲージ表示用Image")]
    [SerializeField] private Image m_gaugeFillImage;

    [Header("アニメーション設定")]
    [Tooltip("波アニメーションの再生スピード (FPS)")]
    [SerializeField] private float m_animationFps = 30f;

    [Header("ゲージ上下移動の位置設定 (Y座標)")]
    [Tooltip("ゲージが0%（空）の時のY座標（下に下がって隠れる位置）")]
    [SerializeField] private float m_minYPosition = -120f;

    [Tooltip("ゲージが100%（満タン）の時のY座標（通常の位置）")]
    [SerializeField] private float m_maxYPosition = 0f;

    [Tooltip("ゲージ移動の滑らかさ")]
    [SerializeField] private float m_moveSmoothSpeed = 10f;

    private float m_currentGaugeRatio = 0f;
    private float m_targetGaugeRatio = 0f;
    private RectTransform m_gaugeRectTransform;
    private float m_animTimer = 0f;
    private int m_currentFrameIndex = 0;

    private void Awake()
    {
        if (m_gaugeFillImage != null)
        {
            m_gaugeRectTransform = m_gaugeFillImage.rectTransform;
        }
    }

    private void Update()
    {
        // 1. 150枚の画像をパラパラ再生（液体のゆらゆらアニメーション）
        AnimateGaugeSprite();

        // 2. ゲージ量に応じた上下移動（Y座標の変更による水位調整）
        UpdateGaugePosition();
    }

    /// <summary>
    /// 連番スプライトをループ再生します
    /// </summary>
    private void AnimateGaugeSprite()
    {
        if (m_gaugeFillImage == null || m_gaugeSprites == null || m_gaugeSprites.Length == 0) return;

        m_animTimer += Time.deltaTime;
        if (m_animTimer >= (1f / m_animationFps))
        {
            m_animTimer = 0f;
            m_currentFrameIndex = (m_currentFrameIndex + 1) % m_gaugeSprites.Length;
            m_gaugeFillImage.sprite = m_gaugeSprites[m_currentFrameIndex];
        }
    }

    /// <summary>
    /// ゲージ割合に応じてRectTransformのY座標を上下に動かします
    /// </summary>
    private void UpdateGaugePosition()
    {
        if (m_gaugeRectTransform == null) return;

        // 現在割合を目標割合へ滑らかに近づける
        m_currentGaugeRatio = Mathf.MoveTowards(
            m_currentGaugeRatio,
            m_targetGaugeRatio,
            m_moveSmoothSpeed * Time.deltaTime
        );

        // 割合（0.0～1.0）に応じたY座標を計算
        float targetY = Mathf.Lerp(m_minYPosition, m_maxYPosition, m_currentGaugeRatio);

        // 位置（Y座標）を更新
        Vector2 pos = m_gaugeRectTransform.anchoredPosition;
        pos.y = targetY;
        m_gaugeRectTransform.anchoredPosition = pos;
    }

    /// <summary>
    /// 速度表示を更新します。
    /// </summary>
    public void UpdateSpeed(float speed)
    {
        if (m_numberSprites == null || m_numberSprites.Length < 10) return;

        int speedInt = Mathf.FloorToInt(speed);
        int digit10 = (speedInt / 10) % 10;
        int digit1 = speedInt % 10;
        int digitDecimal = Mathf.FloorToInt((speed - speedInt) * 10f) % 10;

        if (m_digit10Image != null)
        {
            m_digit10Image.enabled = (speedInt >= 10);
            m_digit10Image.sprite = m_numberSprites[digit10];
        }
        if (m_digit1Image != null) m_digit1Image.sprite = m_numberSprites[digit1];
        if (m_digitDecimalImage != null) m_digitDecimalImage.sprite = m_numberSprites[digitDecimal];
    }

    /// <summary>
    /// ゲージの目標表示割合を設定します（0.0～1.0）。
    /// </summary>
    public void UpdateGauge(float currentGauge)
    {
        m_targetGaugeRatio = Mathf.Clamp01(currentGauge);
    }
}