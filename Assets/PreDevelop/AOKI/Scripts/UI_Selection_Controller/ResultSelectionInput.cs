using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// リザルト選択 UI の登場演出と選択時の表示演出を管理します。
/// </summary>
[DisallowMultipleComponent]
public class ResultSelectionInput : MonoBehaviour, IUIInputControllable
{
    private const float FADE_OUT_ALPHA = 0.0f;
    private const float FADE_IN_ALPHA = 1.0f;
    private const int INFINITE_LOOP = -1;

    [Header("登場フェードイン設定")]
    [Tooltip("開始時に自動で登場演出を再生する")]
    [SerializeField]
    private bool m_playEntranceOnStart = true;

    [Tooltip("登場演出の開始を遅らせる時間（秒）")]
    [SerializeField]
    private float m_entranceDelay = 1.2f;

    [Tooltip("登場フェードインにかける時間（秒）")]
    [SerializeField]
    private float m_entranceDuration = 0.5f;

    [Tooltip("登場演出と入力禁止を適用するパネル")]
    [SerializeField]
    private RectTransform[] m_targetPanels;

    [SerializeField]
    ButtonSelector m_buttonSelector;

    [Header("Next Stage ボタン演出設定")]
    [SerializeField]
    private Selectable m_nextStageButton;
    [SerializeField]
    private RectTransform m_nextStageVisual;
    [SerializeField]
    private Image m_nextStageImage;
    [SerializeField]
    private Sprite m_nextStageUnselectedSprite;
    [SerializeField]
    private Sprite m_nextStageSelectedSprite;
    [SerializeField]
    private float m_slideOffset = -50.0f;

    [Header("Stage Select ボタン演出設定")]
    [SerializeField]
    private RectTransform m_stageSelectVisual;
    [SerializeField]
    private Image m_stageSelectFrameImage;
    [SerializeField]
    private Vector2 m_fadeOutScale = new Vector2(1.2f, 1.2f);

    [Header("アニメーション共通設定")]
    [SerializeField]
    private float m_duration = 0.25f;
    [SerializeField]
    private float m_loopDuration = 0.8f;
    [SerializeField]
    private float m_loopSlideAmount = -10.0f;
    [SerializeField]
    private Vector2 m_loopScaleAmount = new Vector2(1.05f, 1.2f);

    [Header("時間設定")]
    [Tooltip("Time.timeScale が 0 の間も演出を進める")]
    [SerializeField]
    private bool m_useUnscaledTime = true;

    // 各パネルの入力制御と初期情報
    private CanvasGroup[] m_targetCanvasGroups;
    private Vector2[] m_targetOriginalAnchoredPositions;
    private bool[] m_originalInteractables;
    private bool[] m_originalBlocksRaycasts;

    // ボタン演出の初期状態
    private float m_nsOriginalPosX;
    private Vector3 m_ssOriginalScale;

    // 登場演出と外部入力許可の管理
    private Sequence m_entranceSequence;
    private bool m_isInitialized;
    private bool m_hasStarted;
    private bool m_isEntrancePlaying;
    private bool m_isEntranceCompleted;
    private bool m_isExternalInputEnabled = true;

    /// <summary>
    /// この UI が入力を受け付けられるかを取得します。
    /// </summary>
    public bool CanProcessInput => isActiveAndEnabled &&
                                   m_isExternalInputEnabled &&
                                   m_isEntranceCompleted &&
                                   !m_isEntrancePlaying;

    private void Awake()
    {
        InitializeComponents();

        // Start より先に入力が来ても反応しないようにする
        m_isEntranceCompleted = false;
        UpdatePanelInputState();
    }

    private void OnEnable()
    {
        // 一度 Start を実行した後の再表示にも対応する
        if (m_hasStarted)
        {
            ShowInitialState();
        }
    }

    private void Start()
    {
        m_hasStarted = true;
        Time.timeScale = 0.0f;

        ShowInitialState();
    }

    private void OnDisable()
    {
        // 非表示中に Tween を動かし続けない
        m_entranceSequence?.Kill();
        m_entranceSequence = null;

        KillSelectionTweens();

        m_isEntrancePlaying = false;
        m_isEntranceCompleted = false;
        UpdatePanelInputState();
        Time.timeScale = 1.0f;

    }

    /// <summary>
    /// 背面 UI の入力を許可、または禁止します。
    /// </summary>
    /// <param name="isEnabled">入力を許可するか。</param>
    public void SetInputEnabled(bool isEnabled)
    {
        m_isExternalInputEnabled = isEnabled;
        UpdatePanelInputState();
    }

    /// <summary>
    /// 各パネルと演出対象の初期状態を記録します。
    /// </summary>
    private void InitializeComponents()
    {
        if (m_isInitialized)
        {
            return;
        }

        int panelCount = m_targetPanels != null ? m_targetPanels.Length : 0;
        m_targetCanvasGroups = new CanvasGroup[panelCount];
        m_targetOriginalAnchoredPositions = new Vector2[panelCount];
        m_originalInteractables = new bool[panelCount];
        m_originalBlocksRaycasts = new bool[panelCount];

        for (int i = 0; i < panelCount; i++)
        {
            RectTransform panel = m_targetPanels[i];
            if (panel == null)
            {
                continue;
            }

            if (!panel.TryGetComponent(out CanvasGroup canvasGroup))
            {
                canvasGroup = panel.gameObject.AddComponent<CanvasGroup>();
            }

            m_targetCanvasGroups[i] = canvasGroup;
            m_targetOriginalAnchoredPositions[i] = panel.anchoredPosition;
            m_originalInteractables[i] = canvasGroup.interactable;
            m_originalBlocksRaycasts[i] = canvasGroup.blocksRaycasts;
        }

        if (m_nextStageVisual != null)
        {
            m_nsOriginalPosX = m_nextStageVisual.anchoredPosition.x;

            if (m_nextStageButton == null)
            {
                m_nextStageButton = m_nextStageVisual.GetComponentInParent<Selectable>();
            }
        }

        if (m_stageSelectVisual != null)
        {
            m_ssOriginalScale = m_stageSelectVisual.localScale;
        }

        m_isInitialized = true;
    }

    /// <summary>
    /// 設定に応じて登場演出、または即時表示を開始します。
    /// </summary>
    private void ShowInitialState()
    {
        if (m_playEntranceOnStart)
        {
            PlayEntranceAnimation();
            return;
        }

        m_isEntrancePlaying = false;
        m_isEntranceCompleted = true;
        SetPanelsAlpha(FADE_IN_ALPHA);
        ResetSelectionVisuals();
        SetInputEnabled(true);
        PlayStageSelectDeselectedVisual();
        PlayNextStageSelectedVisual();
        SelectInitialButton();
    }

    /// <summary>
    /// パネル群の登場フェードイン演出を再生します。
    /// </summary>
    public void PlayEntranceAnimation()
    {
        InitializeComponents();

        // 再生し直す場合は前回の Tween を終了させる
        m_entranceSequence?.Kill();
        m_entranceSequence = null;
        KillSelectionTweens();

        m_isEntrancePlaying = true;
        m_isEntranceCompleted = false;
        SetInputEnabled(false);

        SetPanelsAlpha(FADE_OUT_ALPHA);
        ResetSelectionVisuals();

        int panelCount = m_targetPanels != null ? m_targetPanels.Length : 0;
        if (panelCount == 0)
        {
            CompleteEntranceAnimation();
            return;
        }

        Sequence entranceSequence = DOTween.Sequence();
        m_entranceSequence = entranceSequence;

        if (m_entranceDelay > 0.0f)
        {
            entranceSequence.AppendInterval(m_entranceDelay);
        }

        bool hasTween = false;

        for (int i = 0; i < panelCount; i++)
        {
            CanvasGroup canvasGroup = m_targetCanvasGroups[i];
            if (canvasGroup == null)
            {
                continue;
            }

            Tween fadeTween = canvasGroup.DOFade(FADE_IN_ALPHA, m_entranceDuration)
                .SetEase(Ease.OutCubic);

            if (!hasTween)
            {
                entranceSequence.Append(fadeTween);
                hasTween = true;
            }
            else
            {
                entranceSequence.Join(fadeTween);
            }
        }

        // 全パネルが未設定の場合も入力を永久にロックしない
        if (!hasTween && m_entranceDelay <= 0.0f)
        {
            entranceSequence.Kill();
            m_entranceSequence = null;
            CompleteEntranceAnimation();
            return;
        }

        entranceSequence.SetUpdate(m_useUnscaledTime);
        entranceSequence.OnComplete(CompleteEntranceAnimation);
    }

    /// <summary>
    /// 登場演出完了後に入力を許可して初期選択演出を開始します。
    /// </summary>
    private void CompleteEntranceAnimation()
    {
        m_entranceSequence = null;
        m_isEntrancePlaying = false;
        m_isEntranceCompleted = true;

        SetInputEnabled(true);
        PlayStageSelectDeselectedVisual();
        PlayNextStageSelectedVisual();
        SelectInitialButton();
    }

    /// <summary>
    /// 初期選択ボタン（Next Stage）にフォーカスを合わせます。
    /// </summary>
    private void SelectInitialButton()
    {
        if (m_nextStageButton != null)
        {
            m_nextStageButton.Select();
        }
    }

    /// <summary>
    /// 現在の表示状態に応じてパネルへの入力・カーソル・選択状態を切り替えます。
    /// </summary>
    private void UpdatePanelInputState()
    {
        bool canInteract = CanProcessInput;

        if (m_targetCanvasGroups != null)
        {
            for (int i = 0; i < m_targetCanvasGroups.Length; i++)
            {
                CanvasGroup canvasGroup = m_targetCanvasGroups[i];
                if (canvasGroup == null)
                {
                    continue;
                }

                // 元々無効にしていたパネルは入力解放後も無効のままにする
                canvasGroup.interactable = canInteract && m_originalInteractables[i];
                canvasGroup.blocksRaycasts = canInteract && m_originalBlocksRaycasts[i];
            }
        }

        if (m_buttonSelector != null)
        {
            if (canInteract)
            {
                m_buttonSelector.UnLockCursor();
            }
            else
            {
                m_buttonSelector.LockCursor();
            }
        }

        // 入力禁止中は EventSystem のフォーカスも外し、Enter などの Submit 反応を防ぐ
        if (!canInteract && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    /// <summary>
    /// パネルの透明度と初期位置を設定します。
    /// </summary>
    private void SetPanelsAlpha(float alpha)
    {
        for (int i = 0; i < m_targetCanvasGroups.Length; i++)
        {
            CanvasGroup canvasGroup = m_targetCanvasGroups[i];
            if (canvasGroup == null)
            {
                continue;
            }

            canvasGroup.DOKill();
            canvasGroup.alpha = alpha;
            m_targetPanels[i].anchoredPosition = m_targetOriginalAnchoredPositions[i];
        }
    }

    /// <summary>
    /// ボタン選択の見た目を初期化します。
    /// </summary>
    private void ResetSelectionVisuals()
    {
        KillSelectionTweens();

        if (m_nextStageImage != null)
        {
            m_nextStageImage.sprite = m_nextStageSelectedSprite;
        }

        if (m_nextStageVisual != null)
        {
            m_nextStageVisual.anchoredPosition = new Vector2(
                m_nsOriginalPosX,
                m_nextStageVisual.anchoredPosition.y);
        }

        if (m_stageSelectFrameImage != null)
        {
            Color color = m_stageSelectFrameImage.color;
            color.a = FADE_OUT_ALPHA;
            m_stageSelectFrameImage.color = color;
        }

        if (m_stageSelectVisual != null)
        {
            m_stageSelectVisual.localScale = new Vector3(
                m_ssOriginalScale.x * m_fadeOutScale.x,
                m_ssOriginalScale.y * m_fadeOutScale.y,
                m_ssOriginalScale.z);
        }
    }

    /// <summary>
    /// 再生中の選択演出を停止します。
    /// </summary>
    private void KillSelectionTweens()
    {
        if (m_nextStageVisual != null)
        {
            m_nextStageVisual.DOKill();
        }

        if (m_stageSelectVisual != null)
        {
            m_stageSelectVisual.DOKill();
        }

        if (m_stageSelectFrameImage != null)
        {
            m_stageSelectFrameImage.DOKill();
        }
    }

    /// <summary>
    /// Next Stage が選択されたときの入力通知を受けます。
    /// </summary>
    public void OnSelectNextStage()
    {
        if (!CanProcessInput)
        {
            return;
        }

        PlayStageSelectDeselectedVisual();
        PlayNextStageSelectedVisual();
    }

    /// <summary>
    /// Next Stage の選択解除通知を受けます。
    /// </summary>
    public void OnDeselectNextStage()
    {
        if (!CanProcessInput)
        {
            return;
        }

        PlayNextStageDeselectedVisual();
    }

    /// <summary>
    /// Stage Select が選択されたときの入力通知を受けます。
    /// </summary>
    public void OnSelectStageSelect()
    {
        if (!CanProcessInput)
        {
            return;
        }

        PlayNextStageDeselectedVisual();
        PlayStageSelectSelectedVisual();
    }

    /// <summary>
    /// Stage Select の選択解除通知を受けます。
    /// </summary>
    public void OnDeselectStageSelect()
    {
        if (!CanProcessInput)
        {
            return;
        }

        PlayStageSelectDeselectedVisual();
    }

    /// <summary>
    /// Next Stage を選択状態で描画します。
    /// </summary>
    private void PlayNextStageSelectedVisual()
    {
        if (m_nextStageImage != null)
        {
            m_nextStageImage.sprite = m_nextStageSelectedSprite;
        }

        if (m_nextStageVisual == null)
        {
            return;
        }

        m_nextStageVisual.DOKill();
        m_nextStageVisual.DOAnchorPosX(m_nsOriginalPosX, m_duration)
            .SetEase(Ease.OutCubic)
            .SetUpdate(m_useUnscaledTime)
            .OnComplete(() =>
            {
                if (!CanProcessInput || m_nextStageVisual == null)
                {
                    return;
                }

                m_nextStageVisual.DOAnchorPosX(m_nsOriginalPosX + m_loopSlideAmount, m_loopDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(INFINITE_LOOP, LoopType.Yoyo)
                    .SetUpdate(m_useUnscaledTime);
            });
    }

    /// <summary>
    /// Next Stage を非選択状態で描画します。
    /// </summary>
    private void PlayNextStageDeselectedVisual()
    {
        if (m_nextStageImage != null)
        {
            m_nextStageImage.sprite = m_nextStageUnselectedSprite;
        }

        if (m_nextStageVisual != null)
        {
            m_nextStageVisual.DOKill();
            m_nextStageVisual.DOAnchorPosX(m_nsOriginalPosX + m_slideOffset, m_duration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(m_useUnscaledTime);
        }
    }

    /// <summary>
    /// Stage Select を選択状態で描画します。
    /// </summary>
    private void PlayStageSelectSelectedVisual()
    {
        if (m_stageSelectFrameImage != null)
        {
            m_stageSelectFrameImage.DOKill();
            m_stageSelectFrameImage.DOFade(FADE_IN_ALPHA, m_duration)
                .SetUpdate(m_useUnscaledTime);
        }

        if (m_stageSelectVisual == null)
        {
            return;
        }

        m_stageSelectVisual.DOKill();
        m_stageSelectVisual.DOScale(m_ssOriginalScale, m_duration)
            .SetEase(Ease.OutBack)
            .SetUpdate(m_useUnscaledTime)
            .OnComplete(() =>
            {
                if (!CanProcessInput || m_stageSelectVisual == null)
                {
                    return;
                }

                Vector3 loopTargetScale = new Vector3(
                    m_ssOriginalScale.x * m_loopScaleAmount.x,
                    m_ssOriginalScale.y * m_loopScaleAmount.y,
                    m_ssOriginalScale.z);

                m_stageSelectVisual.DOScale(loopTargetScale, m_loopDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(INFINITE_LOOP, LoopType.Yoyo)
                    .SetUpdate(m_useUnscaledTime);
            });
    }

    /// <summary>
    /// Stage Select を非選択状態で描画します。
    /// </summary>
    private void PlayStageSelectDeselectedVisual()
    {
        if (m_stageSelectFrameImage != null)
        {
            m_stageSelectFrameImage.DOKill();
            m_stageSelectFrameImage.DOFade(FADE_OUT_ALPHA, m_duration)
                .SetUpdate(m_useUnscaledTime);
        }

        if (m_stageSelectVisual != null)
        {
            m_stageSelectVisual.DOKill();

            Vector3 targetScale = new Vector3(
                m_ssOriginalScale.x * m_fadeOutScale.x,
                m_ssOriginalScale.y * m_fadeOutScale.y,
                m_ssOriginalScale.z);

            m_stageSelectVisual.DOScale(targetScale, m_duration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(m_useUnscaledTime);
        }
    }
}