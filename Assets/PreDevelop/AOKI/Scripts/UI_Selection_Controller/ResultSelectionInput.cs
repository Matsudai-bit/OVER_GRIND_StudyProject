using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using DG.Tweening;

/// <summary>
/// リザルト画面における選択 UI の入力とアニメーションを制御するクラスです。
/// </summary>
public class ResultSelectionInput : MonoBehaviour
{
    [Header("Next Stage (画像切り替え & スライド移動)")]

    /// <summary>「Next Stage」のアニメーションを適用する RectTransform。</summary>
    [SerializeField] private RectTransform m_nextStageVisual;

    /// <summary>「Next Stage」の画像を切り替える Image コンポーネント。</summary>
    [SerializeField] private Image m_nextStageImage;

    /// <summary>「Next Stage」が非選択のときに表示するスプライト。</summary>
    [SerializeField] private Sprite m_nextStageUnselectedSprite;

    /// <summary>「Next Stage」が選択中のときに表示するスプライト。</summary>
    [SerializeField] private Sprite m_nextStageSelectedSprite;

    /// <summary>非選択時にどれくらい横へずらすかのオフセット値。</summary>
    [Tooltip("非選択時にどれくらい横へずらしておくか")]
    [SerializeField] private float m_slideOffset = -50f;

    [Header("Stage Select (枠フェード & 拡縮アニメーション)")]

    /// <summary>「Stage Select」のアニメーションを適用する RectTransform。</summary>
    [SerializeField] private RectTransform m_stageSelectVisual;

    /// <summary>「Stage Select」選択時に表示する枠の Image コンポーネント。</summary>
    [SerializeField] private Image m_stageSelectFrameImage;

    /// <summary>非選択時の縮小サイズ割合 (X, Y)。</summary>
    [Tooltip("非選択時の縮小サイズ割合 (X, Y)")]
    [SerializeField] private Vector2 m_unselectedScale = new Vector2(0.8f, 0.8f);

    [Header("アニメーション設定")]

    /// <summary>選択切り替え時のアニメーションにかかる時間（秒）。</summary>
    [SerializeField] private float m_duration = 0.25f;

    [Header("ループ設定")]

    /// <summary>選択中のループアニメーションの1サイクルの時間（秒）。</summary>
    [SerializeField] private float m_loopDuration = 0.8f;

    /// <summary>「Next Stage」選択中の横スライドのループ幅。</summary>
    [Tooltip("オレンジが選択中に横にスライドし続ける幅")]
    [SerializeField] private float m_loopSlideAmount = -10f;

    /// <summary>「Stage Select」選択中に拡大し続ける割合 (X, Y)。</summary>
    [Tooltip("白枠が選択中に拡大し続ける割合 (X, Y)")]
    [SerializeField] private Vector2 m_loopScaleAmount = new Vector2(1.05f, 1.2f);

    /// <summary>現在「Next Stage」が選択されているかどうか。</summary>
    private bool m_isNextStageSelected = true;

    /// <summary>「Next Stage」の初期X座標。</summary>
    private float m_nsOriginalPosX;

    /// <summary>「Stage Select」の初期スケール。</summary>
    private Vector3 m_ssOriginalScale;

    /// <summary>
    /// 初期座標・スケールを保存し、初期状態の UI を構築します。
    /// </summary>
    private void Start()
    {
        if (m_nextStageVisual != null) m_nsOriginalPosX = m_nextStageVisual.anchoredPosition.x;
        if (m_stageSelectVisual != null) m_ssOriginalScale = m_stageSelectVisual.localScale;

        UpdateVisuals(true);
    }

    /// <summary>
    /// Input System からの方向入力（Navigate）を受け取ります。
    /// </summary>
    /// <param name="value">入力値情報。</param>
    public void OnNavigate(InputValue value)
    {
        Vector2 input = value.Get<Vector2>();

        if (input.y > 0.5f && !m_isNextStageSelected)
        {
            SelectNextStage();
        }
        else if (input.y < -0.5f && m_isNextStageSelected)
        {
            SelectStageSelect();
        }
    }

    /// <summary>
    /// Input System からの決定入力（Submit）を受け取ります。
    /// </summary>
    /// <param name="value">入力値情報。</param>
    public void OnSubmit(InputValue value)
    {
        if (value.isPressed)
        {
            if (m_isNextStageSelected)
            {
                Debug.Log("Next Stage 決定！");
            }
            else
            {
                Debug.Log("Stage Select 決定！");
            }
        }
    }

    /// <summary>
    /// 「Next Stage」を選択状態にします。
    /// </summary>
    public void SelectNextStage()
    {
        if (m_isNextStageSelected) return;
        m_isNextStageSelected = true;
        UpdateVisuals();
    }

    /// <summary>
    /// 「Stage Select」を選択状態にします。
    /// </summary>
    public void SelectStageSelect()
    {
        if (!m_isNextStageSelected) return;
        m_isNextStageSelected = false;
        UpdateVisuals();
    }

    /// <summary>
    /// 選択状態に応じて UI のアニメーションと画像を更新します。
    /// </summary>
    /// <param name="isInstant">即座に表示を反映させる場合は true。</param>
    private void UpdateVisuals(bool isInstant = false)
    {
        float t = isInstant ? 0f : m_duration;

        // Next Stage (選択中：2本線 / 非選択：1本線)
        if (m_nextStageImage != null)
        {
            m_nextStageImage.sprite = m_isNextStageSelected ? m_nextStageSelectedSprite : m_nextStageUnselectedSprite;
        }

        if (m_nextStageVisual != null)
        {
            m_nextStageVisual.DOKill();

            if (m_isNextStageSelected)
            {
                m_nextStageVisual.DOAnchorPosX(m_nsOriginalPosX, t).SetEase(Ease.OutCubic)
                    .OnComplete(() =>
                    {
                        if (isInstant) return;
                        m_nextStageVisual.DOAnchorPosX(m_nsOriginalPosX + m_loopSlideAmount, m_loopDuration)
                            .SetEase(Ease.InOutSine)
                            .SetLoops(-1, LoopType.Yoyo);
                    });
            }
            else
            {
                m_nextStageVisual.DOAnchorPosX(m_nsOriginalPosX + m_slideOffset, t).SetEase(Ease.OutCubic);
            }
        }

        // Stage Select (選択中：枠表示 / 非選択：枠消去)
        if (m_stageSelectFrameImage != null)
        {
            m_stageSelectFrameImage.DOKill();
            m_stageSelectFrameImage.DOFade(m_isNextStageSelected ? 0f : 1f, t);
        }

        if (m_stageSelectVisual != null)
        {
            m_stageSelectVisual.DOKill();

            if (!m_isNextStageSelected)
            {
                m_stageSelectVisual.DOScale(m_ssOriginalScale, t).SetEase(Ease.OutBack)
                    .OnComplete(() =>
                    {
                        if (isInstant) return;

                        // X（横）と Y（高さ）を個別に計算したループ倍率を適用
                        Vector3 loopTargetScale = new Vector3(
                            m_ssOriginalScale.x * m_loopScaleAmount.x,
                            m_ssOriginalScale.y * m_loopScaleAmount.y,
                            m_ssOriginalScale.z
                        );

                        m_stageSelectVisual.DOScale(loopTargetScale, m_loopDuration)
                            .SetEase(Ease.InOutSine)
                            .SetLoops(-1, LoopType.Yoyo);
                    });
            }
            else
            {
                // X（横）と Y（高さ）を個別に計算した非選択倍率を適用
                Vector3 unselectedTargetScale = new Vector3(
                    m_ssOriginalScale.x * m_unselectedScale.x,
                    m_ssOriginalScale.y * m_unselectedScale.y,
                    m_ssOriginalScale.z
                );

                m_stageSelectVisual.DOScale(unselectedTargetScale, t).SetEase(Ease.OutCubic);
            }
        }
    }
}