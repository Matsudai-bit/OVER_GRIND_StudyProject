using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 速度とゲージのUI表示および、速度に応じた3D傾き・描画位置移動演出を管理するクラスです。
/// </summary>
public class SpeedDisplayView : MonoBehaviour
{
    /// <summary>数字表示用の定数</summary>
    private const int NUMBER_SPRITE_COUNT = 10;

    /// <summary>デフォルトの最高速度 (km/h)</summary>
    private const float DEFAULT_MAX_SPEED = 40f;

    /// <summary>数字スプライト素材 (0～9)</summary>
    [Header("数字スプライト素材 (0～9)")]
    [SerializeField] private Sprite[] m_numberSprites = new Sprite[NUMBER_SPRITE_COUNT];

    /// <summary>速度の10の位表示用Image</summary>
    [Header("スピード表示用Image")]
    [SerializeField] private Image m_digit10Image;

    /// <summary>速度の1の位表示用Image</summary>
    [SerializeField] private Image m_digit1Image;

    /// <summary>速度の小数第1位表示用Image</summary>
    [SerializeField] private Image m_digitDecimalImage;

    /// <summary>ゲージ連番スプライト素材 (150枚・波アニメーション用)</summary>
    [Header("ゲージ連番スプライト素材 (150枚・波アニメーション用)")]
    [SerializeField] private Sprite[] m_gaugeSprites = new Sprite[150];

    /// <summary>ゲージ表示用Image</summary>
    [Header("ゲージ表示用Image")]
    [SerializeField] private Image m_gaugeFillImage;

    /// <summary>波アニメーションの再生スピード (FPS)</summary>
    [Header("アニメーション設定")]
    [Tooltip("波アニメーションの再生スピード (FPS)")]
    [SerializeField] private float m_animationFps = 30f;

    /// <summary>ゲージが0%（空）の時のY座標</summary>
    [Header("ゲージ上下移動の位置設定 (Y座標)")]
    [Tooltip("ゲージが0%（空）の時のY座標（下に下がって隠れる位置）")]
    [SerializeField] private float m_minYPosition = -120f;

    /// <summary>ゲージが100%（満タン）の時のY座標</summary>
    [Tooltip("ゲージが100%（満タン）の時のY座標（通常の位置）")]
    [SerializeField] private float m_maxYPosition = 0f;

    /// <summary>ゲージ移動の滑らかさ</summary>
    [Tooltip("ゲージ移動の滑らかさ")]
    [SerializeField] private float m_moveSmoothSpeed = 10f;

    /// <summary>傾ける対象のRectTransform群</summary>
    [Header("3D Tilt Settings (エアライド風傾き)")]
    [Tooltip("傾ける対象のRectTransform群（ゲージや数字など、複数指定可能）")]
    [SerializeField] private RectTransform[] m_tiltRoots;

    /// <summary>速度0（停止時）の回転角度</summary>
    [Tooltip("速度0（停止時）の回転角度")]
    [SerializeField] private Vector3 m_zeroSpeedRotation = Vector3.zero;

    /// <summary>最高速度時の3D回転角度</summary>
    [Tooltip("最高速度時の3D回転角度 (X:上下傾き, Y:左右奥行き, Z:斜め回転)")]
    [SerializeField] private Vector3 m_maxSpeedRotation = new Vector3(10f, -25f, -12f);

    /// <summary>左右入力によるZ軸の傾き角度</summary>
    [Tooltip("左右入力（G）によるZ軸の傾き角度")]
    [SerializeField] private float m_maxTurnAngleZ = 15f;

    /// <summary>回転変化のイージングカーブ</summary>
    [Tooltip("回転変化のイージングカーブ (0:停止時 ～ 1:最高速時)")]
    [SerializeField] private AnimationCurve m_tiltCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    /// <summary>傾きの追従スピード</summary>
    [Tooltip("傾きの追従スピード")]
    [SerializeField] private float m_tiltSmoothSpeed = 8f;

    /// <summary>速度0（停止時）の位置オフセット</summary>
    [Header("Position Offset Settings (描画位置の変更)")]
    [Tooltip("速度0（停止時）の位置オフセット (X, Y, Z)")]
    [SerializeField] private Vector3 m_zeroSpeedPositionOffset = Vector3.zero;

    /// <summary>最高速度時の位置オフセット</summary>
    [Tooltip("最高速度時の位置オフセット (X:左右ずれ, Y:上下ずれ, Z:手前・奥)")]
    [SerializeField] private Vector3 m_maxSpeedPositionOffset = new Vector3(-20f, -10f, -100f);

    /// <summary>位置変化のイージングカーブ</summary>
    [Tooltip("位置変化のイージングカーブ (0:停止時 ～ 1:最高速時)")]
    [SerializeField] private AnimationCurve m_positionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    /// <summary>位置変化の追従スピード</summary>
    [Tooltip("位置変化の追従スピード")]
    [SerializeField] private float m_positionSmoothSpeed = 8f;

    /// <summary>現在のゲージ割合 (0.0～1.0)</summary>
    private float m_currentGaugeRatio = 0f;

    /// <summary>目標のゲージ割合 (0.0～1.0)</summary>
    private float m_targetGaugeRatio = 0f;

    /// <summary>ゲージのRectTransform参照</summary>
    private RectTransform m_gaugeRectTransform;

    /// <summary>アニメーションタイマー</summary>
    private float m_animTimer = 0f;

    /// <summary>現在のフレームインデックス</summary>
    private int m_currentFrameIndex = 0;

    /// <summary>現在の回転角度</summary>
    private Vector3 m_currentRotation;

    /// <summary>目標の回転角度</summary>
    private Vector3 m_targetRotation;

    /// <summary>現在の位置オフセット</summary>
    private Vector3 m_currentPosOffset;

    /// <summary>目標の位置オフセット</summary>
    private Vector3 m_targetPosOffset;

    /// <summary>各要素の初期位置配列</summary>
    private Vector3[] m_basePositions;

    /// <summary>
    /// 初期化処理を行います。
    /// </summary>
    private void Awake()
    {
        if (m_gaugeFillImage != null)
        {
            m_gaugeRectTransform = m_gaugeFillImage.rectTransform;
        }

        if (m_tiltRoots == null || m_tiltRoots.Length == 0)
        {
            m_tiltRoots = new RectTransform[] { GetComponent<RectTransform>() };
        }

        // 初期描画位置の保存
        m_basePositions = new Vector3[m_tiltRoots.Length];
        for (int i = 0; i < m_tiltRoots.Length; i++)
        {
            if (m_tiltRoots[i] != null)
            {
                m_basePositions[i] = m_tiltRoots[i].anchoredPosition3D;
            }
        }

        m_currentRotation = m_zeroSpeedRotation;
        m_targetRotation = m_zeroSpeedRotation;

        m_currentPosOffset = m_zeroSpeedPositionOffset;
        m_targetPosOffset = m_zeroSpeedPositionOffset;
    }

    /// <summary>
    /// 毎フレームのUI更新処理を行います。
    /// </summary>
    private void Update()
    {
        //ゲージ画像の連番アニメーション更新
        AnimateGaugeSprite();

        //ゲージのY軸位置（水位）更新
        UpdateGaugePosition();

        //3D回転および描画位置オフセットの補間更新
        UpdateTransformEffects();
    }

    /// <summary>
    /// 連番スプライトをループ再生します。
    /// </summary>
    private void AnimateGaugeSprite()
    {
        if (m_gaugeFillImage == null || m_gaugeSprites == null || m_gaugeSprites.Length == 0)
        {
            return;
        }

        m_animTimer += Time.deltaTime;
        if (m_animTimer >= (1f / m_animationFps))
        {
            m_animTimer = 0f;
            m_currentFrameIndex = (m_currentFrameIndex + 1) % m_gaugeSprites.Length;
            m_gaugeFillImage.sprite = m_gaugeSprites[m_currentFrameIndex];
        }
    }

    /// <summary>
    /// ゲージ割合に応じてRectTransformのY座標を上下に動かします。
    /// </summary>
    private void UpdateGaugePosition()
    {
        if (m_gaugeRectTransform == null)
        {
            return;
        }

        m_currentGaugeRatio = Mathf.MoveTowards(
            m_currentGaugeRatio,
            m_targetGaugeRatio,
            m_moveSmoothSpeed * Time.deltaTime
        );

        float targetY = Mathf.Lerp(m_minYPosition, m_maxYPosition, m_currentGaugeRatio);

        Vector2 pos = m_gaugeRectTransform.anchoredPosition;
        pos.y = targetY;
        m_gaugeRectTransform.anchoredPosition = pos;
    }

    /// <summary>
    /// 登録された全RectTransformの回転・描画位置を一括更新します。
    /// </summary>
    private void UpdateTransformEffects()
    {
        if (m_tiltRoots == null)
        {
            return;
        }

        // 回転補間
        m_currentRotation = Vector3.Lerp(
            m_currentRotation,
            m_targetRotation,
            Time.deltaTime * m_tiltSmoothSpeed
        );

        // 描画位置オフセット補間
        m_currentPosOffset = Vector3.Lerp(
            m_currentPosOffset,
            m_targetPosOffset,
            Time.deltaTime * m_positionSmoothSpeed
        );

        Quaternion targetQuaternion = Quaternion.Euler(m_currentRotation);

        for (int i = 0; i < m_tiltRoots.Length; i++)
        {
            RectTransform root = m_tiltRoots[i];
            if (root != null)
            {
                // 回転の反映
                root.localRotation = targetQuaternion;

                // 描画位置の更新 
                root.anchoredPosition3D = m_basePositions[i] + m_currentPosOffset;
            }
        }
    }

    /// <summary>
    /// 速度表示と3D傾き・描画位置演出を更新します。
    /// </summary>
    /// <param name="speed">現在の速度 (km/h)。</param>
    /// <param name="maxSpeed">最高速度 (km/h)。</param>
    /// <param name="turnInput">左右ステアリング入力 (-1.0 ～ 1.0)。</param>
    public void UpdateSpeed(float speed, float maxSpeed = DEFAULT_MAX_SPEED, float turnInput = 0f)
    {
        // 数字スプライトの更新
        if (m_numberSprites != null && m_numberSprites.Length >= NUMBER_SPRITE_COUNT)
        {
            int speedInt = Mathf.FloorToInt(speed);
            int digit10 = (speedInt / 10) % 10;
            int digit1 = speedInt % 10;
            int digitDecimal = Mathf.FloorToInt((speed - speedInt) * 10f) % 10;

            if (m_digit10Image != null)
            {
                m_digit10Image.enabled = true;
                m_digit10Image.sprite = m_numberSprites[digit10];
            }
            if (m_digit1Image != null)
            {
                m_digit1Image.sprite = m_numberSprites[digit1];
            }
            if (m_digitDecimalImage != null)
            {
                m_digitDecimalImage.sprite = m_numberSprites[digitDecimal];
            }
        }

        // 速度比率の計算
        float rawRatio = Mathf.Clamp01(speed / maxSpeed);

        // イージングカーブ適用
        float tiltRatio = (m_tiltCurve != null) ? m_tiltCurve.Evaluate(rawRatio) : rawRatio;
        float posRatio = (m_positionCurve != null) ? m_positionCurve.Evaluate(rawRatio) : rawRatio;

        // 目標回転の計算
        Vector3 rot = Vector3.Lerp(m_zeroSpeedRotation, m_maxSpeedRotation, tiltRatio);
        rot.z += Mathf.Clamp(turnInput, -1f, 1f) * m_maxTurnAngleZ;
        m_targetRotation = rot;

        // 目標描画位置オフセットの計算 (X, Y, Z)
        m_targetPosOffset = Vector3.Lerp(m_zeroSpeedPositionOffset, m_maxSpeedPositionOffset, posRatio);
    }

    /// <summary>
    /// ゲージの目標表示割合を設定します。
    /// </summary>
    /// <param name="currentGauge">現在のゲージ割合 (0.0～1.0)。</param>
    public void UpdateGauge(float currentGauge)
    {
        m_targetGaugeRatio = Mathf.Clamp01(currentGauge);
    }
}