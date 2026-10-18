using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HPゲージのUI表示とアニメーション、速度に応じた3D傾き・描画位置移動演出（イージング機能付き）を管理するクラスです。
/// </summary>
public class HpDisplayView : MonoBehaviour
{
    /// <summary>ダメージ時に即座に減る緑色のゲージ</summary>
    [Header("Gauge Elements")]
    [SerializeField] private Image m_greenGauge;

    /// <summary>ダメージ時に遅れて減る赤色のゲージ</summary>
    [SerializeField] private Image m_redGauge;

    /// <summary>緑ゲージのアニメーション時間（秒）</summary>
    [Header("Animation Settings")]
    [SerializeField] private float m_greenDuration = 0.2f;

    /// <summary>赤ゲージが減り始めるまでの待機時間（秒）</summary>
    [SerializeField] private float m_redDelay = 0.4f;

    /// <summary>赤ゲージのアニメーション実行時間（秒）</summary>
    [SerializeField] private float m_redDuration = 0.6f;

    [Header("3D Tilt Settings (エアライド風傾き)")]
    [Tooltip("傾ける対象のRectTransform（未指定の場合はこのオブジェクト自身）")]
    [SerializeField] private RectTransform m_tiltRoot;

    [Tooltip("速度0（停止時）の回転角度")]
    [SerializeField] private Vector3 m_zeroSpeedRotation = Vector3.zero;

    [Tooltip("最高速度時の3D回転角度 (X:上下傾き, Y:左右奥行き, Z:斜め回転)")]
    [SerializeField] private Vector3 m_maxSpeedRotation = new Vector3(10f, 25f, 12f);

    [Tooltip("左右入力（G）によるZ軸の傾き角度")]
    [SerializeField] private float m_maxTurnAngleZ = 12f;

    [Tooltip("回転変化のイージングカーブ (0:停止時 ～ 1:最高速時)")]
    [SerializeField] private AnimationCurve m_tiltCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("傾きの追従スピード")]
    [SerializeField] private float m_tiltSmoothSpeed = 12f;

    [Header("Position Offset Settings (描画位置の変更)")]
    [Tooltip("速度0（停止時）の位置オフセット (X, Y, Z)")]
    [SerializeField] private Vector3 m_zeroSpeedPositionOffset = Vector3.zero;

    [Tooltip("最高速度時の位置オフセット (X:左右ずれ, Y:上下ずれ, Z:手前・奥)")]
    [SerializeField] private Vector3 m_maxSpeedPositionOffset = new Vector3(20f, -10f, -100f);

    [Tooltip("位置変化のイージングカーブ (0:停止時 ～ 1:最高速時)")]
    [SerializeField] private AnimationCurve m_positionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("位置変化の追従スピード")]
    [SerializeField] private float m_positionSmoothSpeed = 12f;

    /// <summary>赤ゲージ用のアニメーション状態</summary>
    private Tween m_redTween;

    private Vector3 m_currentRotation;
    private Vector3 m_targetRotation;

    private Vector3 m_currentPosOffset;
    private Vector3 m_targetPosOffset;

    // 初期描画位置の保存（配置ズレ防止用）
    private Vector3 m_basePosition;

    private void Awake()
    {
        if (m_tiltRoot == null)
        {
            m_tiltRoot = GetComponent<RectTransform>();
        }

        if (m_tiltRoot != null)
        {
            m_basePosition = m_tiltRoot.anchoredPosition3D;
        }

        m_currentRotation = m_zeroSpeedRotation;
        m_targetRotation = m_zeroSpeedRotation;

        m_currentPosOffset = m_zeroSpeedPositionOffset;
        m_targetPosOffset = m_zeroSpeedPositionOffset;
    }

    private void Update()
    {
        UpdateTransformEffects();
    }

    /// <summary>
    /// 回転および描画位置オフセットの滑らかな更新処理
    /// </summary>
    private void UpdateTransformEffects()
    {
        if (m_tiltRoot == null) return;

        //3D回転補間
        m_currentRotation = Vector3.Lerp(
            m_currentRotation,
            m_targetRotation,
            Time.deltaTime * m_tiltSmoothSpeed
        );

        //描画位置オフセット補間
        m_currentPosOffset = Vector3.Lerp(
            m_currentPosOffset,
            m_targetPosOffset,
            Time.deltaTime * m_positionSmoothSpeed
        );

        // トランスフォームへの反映
        m_tiltRoot.localRotation = Quaternion.Euler(m_currentRotation);
        m_tiltRoot.anchoredPosition3D = m_basePosition + m_currentPosOffset;
    }

    /// <summary>
    /// HP表示の初期設定を行います。
    /// </summary>
    public void Initialize(float maxHp)
    {
        if (m_greenGauge != null) m_greenGauge.fillAmount = 1f;
        if (m_redGauge != null) m_redGauge.fillAmount = 1f;
    }

    /// <summary>
    /// HPゲージのアニメーションを更新します。
    /// </summary>
    public void UpdateHpDisplay(float currentHp, float maxHp)
    {
        float targetFillAmount = Mathf.Clamp01(currentHp / maxHp);

        if (m_greenGauge != null)
        {
            m_greenGauge.DOFillAmount(targetFillAmount, m_greenDuration).SetEase(Ease.OutQuad);
        }

        if (m_redGauge != null)
        {
            if (m_redTween != null && m_redTween.IsActive())
            {
                m_redTween.Kill();
            }
            m_redTween = m_redGauge.DOFillAmount(targetFillAmount, m_redDuration)
                                   .SetDelay(m_redDelay)
                                   .SetEase(Ease.OutCubic);
        }
    }

    /// <summary>
    /// 速度に応じてHPゲージ全体の3D傾き（奥行き）および描画位置を更新します。
    /// </summary>
    public void UpdateSpeed(float speed, float maxSpeed = 40f, float turnInput = 0f)
    {
        float rawRatio = Mathf.Clamp01(speed / maxSpeed);

        // --- イージングカーブ適用 ---
        float tiltRatio = (m_tiltCurve != null) ? m_tiltCurve.Evaluate(rawRatio) : rawRatio;
        float posRatio = (m_positionCurve != null) ? m_positionCurve.Evaluate(rawRatio) : rawRatio;

        // 3D回転の目標値計算
        Vector3 rot = Vector3.Lerp(m_zeroSpeedRotation, m_maxSpeedRotation, tiltRatio);
        rot.z += Mathf.Clamp(turnInput, -1f, 1f) * m_maxTurnAngleZ;
        m_targetRotation = rot;

        // 描画位置オフセットの目標値計算 (X, Y, Z)
        m_targetPosOffset = Vector3.Lerp(m_zeroSpeedPositionOffset, m_maxSpeedPositionOffset, posRatio);
    }
}