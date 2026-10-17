using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using DG.Tweening;

/// <summary>
/// リザルト画面における選択 UI の入力とアニメーションを制御するクラスです。
/// 複数のボタンパネルに対するシーン登場時フェード＆スライドイン演出に対応しています。
/// </summary>
public class ResultSelectionInput : MonoBehaviour
{
    [Header("★ 登場（ふわっと出現）演出設定")]
    [Tooltip("チェックを入れると開始時に下からフェード＆スライドインして出現します")]
    [SerializeField] private bool m_playEntranceOnStart = true;

    [Tooltip("登場アニメーションが始まるまでの待ち時間（秒）")]
    [SerializeField] private float m_entranceDelay = 1.2f;

    [Tooltip("登場アニメーションにかかる時間（秒）")]
    [SerializeField] private float m_entranceDuration = 0.5f;

    [Tooltip("登場時にどれくらい下から上がってくるかの Y オフセット距離（ピクセル）")]
    [SerializeField] private float m_entranceOffsetY = -80f;

    [Tooltip("アニメーションさせる対象のボタンパネル群（2枚以上ある場合は配列にすべて登録）")]
    [SerializeField] private RectTransform[] m_targetPanels;

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

    /// <summary>非選択時に外側へ消えていく際の拡大サイズ割合 (X, Y)。</summary>
    [Tooltip("非選択時に外側へ消えていく際の拡大サイズ割合 (X, Y)")]
    [SerializeField] private Vector2 m_fadeOutScale = new Vector2(1.2f, 1.2f);

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

    private CanvasGroup[] m_targetCanvasGroups;
    private Vector2[] m_targetOriginalAnchoredPoses;
    private bool m_isEntrancePlaying = false;

    private void Awake()
    {
        InitializeComponents();
    }

    private void Start()
    {
        if (m_playEntranceOnStart)
        {
            PlayEntranceAnimation();
        }
        else
        {
            UpdateVisuals(true);
        }
    }

    private void InitializeComponents()
    {
        if (m_targetPanels != null && m_targetPanels.Length > 0)
        {
            m_targetCanvasGroups = new CanvasGroup[m_targetPanels.Length];
            m_targetOriginalAnchoredPoses = new Vector2[m_targetPanels.Length];

            for (int i = 0; i < m_targetPanels.Length; i++)
            {
                if (m_targetPanels[i] == null) continue;

                if (!m_targetPanels[i].TryGetComponent<CanvasGroup>(out m_targetCanvasGroups[i]))
                {
                    m_targetCanvasGroups[i] = m_targetPanels[i].gameObject.AddComponent<CanvasGroup>();
                }

                m_targetOriginalAnchoredPoses[i] = m_targetPanels[i].anchoredPosition;
            }
        }

        if (m_nextStageVisual != null) m_nsOriginalPosX = m_nextStageVisual.anchoredPosition.x;
        if (m_stageSelectVisual != null) m_ssOriginalScale = m_stageSelectVisual.localScale;
    }

    /// <summary>
    /// 設定された複数枚のパネルを同時に下からフェードインしながら「ふわっと」登場させます。
    /// </summary>
    public void PlayEntranceAnimation()
    {
        InitializeComponents();

        if (m_targetPanels == null || m_targetPanels.Length == 0)
        {
            Debug.LogWarning("[ResultSelectionInput] アニメーション対象の UI パネル（Target Panels）が設定されていません。");
            UpdateVisuals(true);
            return;
        }

        m_isEntrancePlaying = true;

        // ★ 1. 最初に対象パネルをすべて完全透明＆下側に配置
        for (int i = 0; i < m_targetPanels.Length; i++)
        {
            if (m_targetPanels[i] == null || m_targetCanvasGroups[i] == null) continue;

            m_targetCanvasGroups[i].alpha = 0f;
            m_targetPanels[i].anchoredPosition = m_targetOriginalAnchoredPoses[i] + new Vector2(0f, m_entranceOffsetY);
        }

        UpdateVisuals(true);

        Sequence entranceSeq = DOTween.Sequence();

        // ★ 2. 指定時間（Entrance Delay）待機
        if (m_entranceDelay > 0f)
        {
            entranceSeq.AppendInterval(m_entranceDelay);
        }

        // ★ 3. 待機完了後にフェード＆移動アニメーションを開始（最初の要素は Append にする）
        bool isFirstTween = true;

        for (int i = 0; i < m_targetPanels.Length; i++)
        {
            if (m_targetPanels[i] == null || m_targetCanvasGroups[i] == null) continue;

            Tween fadeTween = m_targetCanvasGroups[i].DOFade(1f, m_entranceDuration).SetEase(Ease.OutCubic);
            Tween moveTween = m_targetPanels[i].DOAnchorPos(m_targetOriginalAnchoredPoses[i], m_entranceDuration).SetEase(Ease.OutCubic);

            if (isFirstTween)
            {
                entranceSeq.Append(fadeTween);
                entranceSeq.Join(moveTween);
                isFirstTween = false;
            }
            else
            {
                entranceSeq.Join(fadeTween);
                entranceSeq.Join(moveTween);
            }
        }

        // 登場アニメーション完了後に通常操作ループを開始
        entranceSeq.OnComplete(() =>
        {
            m_isEntrancePlaying = false;
            UpdateVisuals(false);
        });
    }

    /// <summary>
    /// Input System からの方向入力（Navigate）を受け取ります。
    /// </summary>
    public void OnNavigate(InputValue value)
    {
        if (m_isEntrancePlaying) return;

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
    public void OnSubmit(InputValue value)
    {
        if (m_isEntrancePlaying) return;

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
                        if (isInstant || m_isEntrancePlaying) return;
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
                        if (isInstant || m_isEntrancePlaying) return;

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
                Vector3 fadeOutTargetScale = new Vector3(
                    m_ssOriginalScale.x * m_fadeOutScale.x,
                    m_ssOriginalScale.y * m_fadeOutScale.y,
                    m_ssOriginalScale.z
                );

                m_stageSelectVisual.DOScale(fadeOutTargetScale, t).SetEase(Ease.OutCubic);
            }
        }
    }
}