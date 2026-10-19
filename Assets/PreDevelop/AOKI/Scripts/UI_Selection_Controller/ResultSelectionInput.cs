using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// リザルト画面の選択 UI におけるフェードイン登場演出および各ボタンのアニメーション演出を制御するクラスです。
/// </summary>
public class ResultSelectionInput : MonoBehaviour
{
    // アニメーション用の定数
    private const float FADE_OUT_ALPHA = 0f;
    private const float FADE_IN_ALPHA = 1f;
    private const int INFINITE_LOOP = -1;

    [Header(" 登場フェードイン設定")]
    [Tooltip("チェックを入れると開始時に自動でフェードイン登場します")]
    [SerializeField] private bool m_playEntranceOnStart = true; // 開始時自動実行フラグ

    [Tooltip("登場アニメーションが始まるまでの待ち時間（秒）")]
    [SerializeField] private float m_entranceDelay = 1.2f; // 登場ディレイ時間

    [Tooltip("登場アニメーション（フェードイン）にかかる時間（秒）")]
    [SerializeField] private float m_entranceDuration = 0.5f; // フェードイン時間

    [Tooltip("アニメーションさせる対象のボタンパネル群")]
    [SerializeField] private RectTransform[] m_targetPanels; // アニメーション対象パネル配列

    [Header(" Next Stage ボタン演出設定")]
    [Tooltip("「Next Stage」のアニメーションを適用する RectTransform")]
    [SerializeField] private RectTransform m_nextStageVisual; // Next Stage 演出用 RectTransform

    [Tooltip("「Next Stage」の画像を切り替える Image コンポーネント")]
    [SerializeField] private Image m_nextStageImage; // Next Stage 画像 Image

    [Tooltip("「Next Stage」が非選択のときに表示するスプライト")]
    [SerializeField] private Sprite m_nextStageUnselectedSprite; // 非選択時スプライト

    [Tooltip("「Next Stage」が選択中のときに表示するスプライト")]
    [SerializeField] private Sprite m_nextStageSelectedSprite; // 選択時スプライト

    [Tooltip("非選択時に横へずらすオフセット値")]
    [SerializeField] private float m_slideOffset = -50f; // 非選択時スライド量

    [Header(" Stage Select ボタン演出設定")]
    [Tooltip("「Stage Select」のアニメーションを適用する RectTransform")]
    [SerializeField] private RectTransform m_stageSelectVisual; // Stage Select 演出用 RectTransform

    [Tooltip("「Stage Select」選択時に表示する枠の Image コンポーネント")]
    [SerializeField] private Image m_stageSelectFrameImage; // 選択枠 Image

    [Tooltip("非選択時に外側へ消えていく際の拡大サイズ割合 (X, Y)")]
    [SerializeField] private Vector2 m_fadeOutScale = new Vector2(1.2f, 1.2f); // 非選択時スケール

    [Header(" アニメーション共通設定")]
    [Tooltip("選択切り替え時のアニメーション時間（秒）")]
    [SerializeField] private float m_duration = 0.25f; // 切り替えアニメーション時間

    [Tooltip("選択中ループアニメーションの1サイクルの時間（秒）")]
    [SerializeField] private float m_loopDuration = 0.8f; // ループアニメーション時間

    [Tooltip("「Next Stage」選択中の横スライドのループ幅")]
    [SerializeField] private float m_loopSlideAmount = -10f; // スライドループ幅

    [Tooltip("「Stage Select」選択中に拡大し続ける割合 (X, Y)")]
    [SerializeField] private Vector2 m_loopScaleAmount = new Vector2(1.05f, 1.2f); // スケールループ幅

    private CanvasGroup[] m_targetCanvasGroups; // パネルの CanvasGroup 配列
    private Vector2[] m_targetOriginalAnchoredPoses; // パネルの初期位置配列
    private float m_nsOriginalPosX; // Next Stage 初期 X 座標
    private Vector3 m_ssOriginalScale; // Stage Select 初期スケール
    private bool m_isEntrancePlaying; // 登場アニメーション再生中フラグ

    /// <summary>
    /// コンポーネントの初期化を行います。
    /// </summary>
    private void Awake()
    {
        InitializeComponents();
    }

    /// <summary>
    /// 開始処理を行います。
    /// </summary>
    private void Start()
    {
        if (m_playEntranceOnStart)
        {
            PlayEntranceAnimation();
        }
    }

    /// <summary>
    /// 各コンポーネントおよび初期座標の保持を行います。
    /// </summary>
    private void InitializeComponents()
    {
        if (m_targetPanels != null && m_targetPanels.Length > 0)
        {
            m_targetCanvasGroups = new CanvasGroup[m_targetPanels.Length];
            m_targetOriginalAnchoredPoses = new Vector2[m_targetPanels.Length];

            for (int i = 0; i < m_targetPanels.Length; i++)
            {
                if (m_targetPanels[i] == null)
                {
                    continue;
                }

                if (!m_targetPanels[i].TryGetComponent<CanvasGroup>(out m_targetCanvasGroups[i]))
                {
                    m_targetCanvasGroups[i] = m_targetPanels[i].gameObject.AddComponent<CanvasGroup>();
                }

                m_targetOriginalAnchoredPoses[i] = m_targetPanels[i].anchoredPosition;
            }
        }

        if (m_nextStageVisual != null)
        {
            m_nsOriginalPosX = m_nextStageVisual.anchoredPosition.x;
        }

        if (m_stageSelectVisual != null)
        {
            m_ssOriginalScale = m_stageSelectVisual.localScale;
        }
    }

    /// <summary>
    /// パネル群のフェードイン登場アニメーションを再生します。
    /// </summary>
    public void PlayEntranceAnimation()
    {
        InitializeComponents();

        if (m_targetPanels == null || m_targetPanels.Length == 0)
        {
            return;
        }

        m_isEntrancePlaying = true;

        // 初期状態のアルファ値を 0 に設定
        for (int i = 0; i < m_targetPanels.Length; i++)
        {
            if (m_targetPanels[i] == null || m_targetCanvasGroups[i] == null)
            {
                continue;
            }

            m_targetCanvasGroups[i].alpha = FADE_OUT_ALPHA;
            m_targetPanels[i].anchoredPosition = m_targetOriginalAnchoredPoses[i];
        }

        Sequence entranceSequence = DOTween.Sequence();

        if (m_entranceDelay > 0f)
        {
            entranceSequence.AppendInterval(m_entranceDelay);
        }

        bool isFirstTween = true;

        for (int i = 0; i < m_targetPanels.Length; i++)
        {
            if (m_targetPanels[i] == null || m_targetCanvasGroups[i] == null)
            {
                continue;
            }

            Tween fadeTween = m_targetCanvasGroups[i].DOFade(FADE_IN_ALPHA, m_entranceDuration).SetEase(Ease.OutCubic);

            if (isFirstTween)
            {
                entranceSequence.Append(fadeTween);
                isFirstTween = false;
            }
            else
            {
                entranceSequence.Join(fadeTween);
            }
        }

        entranceSequence.OnComplete(() =>
        {
            m_isEntrancePlaying = false;
        });
    }

    /// <summary>
    /// 「Next Stage」ボタンが選択された際の演出を実行します。
    /// </summary>
    public void OnSelectNextStage()
    {
        if (m_isEntrancePlaying)
        {
            return;
        }

        // 画像の切り替え
        if (m_nextStageImage != null)
        {
            m_nextStageImage.sprite = m_nextStageSelectedSprite;
        }

        // スライドおよびループ演出
        if (m_nextStageVisual != null)
        {
            m_nextStageVisual.DOKill();
            m_nextStageVisual.DOAnchorPosX(m_nsOriginalPosX, m_duration).SetEase(Ease.OutCubic)
                .OnComplete(() =>
                {
                    m_nextStageVisual.DOAnchorPosX(m_nsOriginalPosX + m_loopSlideAmount, m_loopDuration)
                        .SetEase(Ease.InOutSine)
                        .SetLoops(INFINITE_LOOP, LoopType.Yoyo);
                });
        }
    }

    /// <summary>
    /// 「Next Stage」ボタンからフォーカスが外れた際の演出を実行します。
    /// </summary>
    public void OnDeselectNextStage()
    {
        if (m_nextStageImage != null)
        {
            m_nextStageImage.sprite = m_nextStageUnselectedSprite;
        }

        if (m_nextStageVisual != null)
        {
            m_nextStageVisual.DOKill();
            m_nextStageVisual.DOAnchorPosX(m_nsOriginalPosX + m_slideOffset, m_duration).SetEase(Ease.OutCubic);
        }
    }

    /// <summary>
    /// 「Stage Select」ボタンが選択された際の演出を実行します。
    /// </summary>
    public void OnSelectStageSelect()
    {
        if (m_isEntrancePlaying)
        {
            return;
        }

        if (m_stageSelectFrameImage != null)
        {
            m_stageSelectFrameImage.DOKill();
            m_stageSelectFrameImage.DOFade(FADE_IN_ALPHA, m_duration);
        }

        if (m_stageSelectVisual != null)
        {
            m_stageSelectVisual.DOKill();
            m_stageSelectVisual.DOScale(m_ssOriginalScale, m_duration).SetEase(Ease.OutBack)
                .OnComplete(() =>
                {
                    Vector3 loopTargetScale = new Vector3(
                        m_ssOriginalScale.x * m_loopScaleAmount.x,
                        m_ssOriginalScale.y * m_loopScaleAmount.y,
                        m_ssOriginalScale.z
                    );

                    m_stageSelectVisual.DOScale(loopTargetScale, m_loopDuration)
                        .SetEase(Ease.InOutSine)
                        .SetLoops(INFINITE_LOOP, LoopType.Yoyo);
                });
        }
    }

    /// <summary>
    /// 「Stage Select」ボタンからフォーカスが外れた際の演出を実行します。
    /// </summary>
    public void OnDeselectStageSelect()
    {
        if (m_stageSelectFrameImage != null)
        {
            m_stageSelectFrameImage.DOKill();
            m_stageSelectFrameImage.DOFade(FADE_OUT_ALPHA, m_duration);
        }

        if (m_stageSelectVisual != null)
        {
            m_stageSelectVisual.DOKill();

            Vector3 fadeOutTargetScale = new Vector3(
                m_ssOriginalScale.x * m_fadeOutScale.x,
                m_ssOriginalScale.y * m_fadeOutScale.y,
                m_ssOriginalScale.z
            );

            m_stageSelectVisual.DOScale(fadeOutTargetScale, m_duration).SetEase(Ease.OutCubic);
        }
    }
}