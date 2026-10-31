using DG.Tweening;
using System;
using UnityEngine;

/// <summary>
/// UIの表示・非表示をDOTweenで制御します。
/// </summary>
[DisallowMultipleComponent]
public sealed class UiTweenAnimator : MonoBehaviour
{
    public enum AnimationType
    {
        SLIDE,
        FADE,
        HORIZONTAL_EXPAND
    }

    [SerializeField, Header("アニメーション種類")]
    private AnimationType m_animationType = AnimationType.FADE;

    [SerializeField, Min(0f), Header("表示アニメーション時間")]
    private float m_showDuration = 0.24f;

    [SerializeField, Min(0f), Header("非表示アニメーション時間")]
    private float m_hideDuration = 0.16f;

    [SerializeField, Header("表示時のEase")]
    private Ease m_showEase = Ease.OutCubic;

    [SerializeField, Header("非表示時のEase")]
    private Ease m_hideEase = Ease.InCubic;

    [SerializeField, Min(0f), Header("スライド距離")]
    private float m_slideDistance = 500f;

    [SerializeField, Range(0.01f, 1f), Header("展開開始時の横幅倍率")]
    private float m_collapsedWidthRatio = 0.05f;

    private CanvasGroup m_canvasGroup;
    private RectTransform m_rectTransform;
    private Vector2 m_visiblePosition;
    private Vector3 m_visibleScale;
    private Tween m_activeTween;
    private Action m_hideCompleted;
    private bool m_isHiding;
    private bool m_isInitialized;

    /// <summary>
    /// 初期表示状態を保存します。
    /// </summary>
    private void Awake()
    {
        Initialize();
    }

    /// <summary>
    /// UIアニメーションに必要な参照と初期状態を取得します。
    /// 非アクティブなUIに対するController操作でも呼び出されます。
    /// </summary>
    private void Initialize()
    {
        if (m_isInitialized)
        {
            return;
        }

        m_rectTransform = GetComponent<RectTransform>();
        m_canvasGroup = GetComponent<CanvasGroup>();

        if (m_canvasGroup == null)
        {
            m_canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        m_visiblePosition = m_rectTransform != null
            ? m_rectTransform.anchoredPosition
            : Vector2.zero;
        m_visibleScale = transform.localScale;
        m_isInitialized = true;
    }

    /// <summary>
    /// アニメーション種類を設定します。
    /// </summary>
    /// <param name="animationType">適用するアニメーション。</param>
    public void SetAnimationType(AnimationType animationType)
    {
        m_animationType = animationType;
    }

    /// <summary>
    /// UIを表示状態へアニメーションします。
    /// </summary>
    public void PlayShow()
    {
        Initialize();
        StopActiveTween(false);
        m_isHiding = false;
        m_hideCompleted = null;
        gameObject.SetActive(true);

        Sequence sequence = DOTween.Sequence();
        if (m_animationType == AnimationType.SLIDE && m_rectTransform != null)
        {
            m_rectTransform.anchoredPosition = m_visiblePosition + Vector2.left * m_slideDistance;
            sequence.Append(m_rectTransform.DOAnchorPos(m_visiblePosition, m_showDuration).SetEase(m_showEase));
        }
        else if (m_animationType == AnimationType.HORIZONTAL_EXPAND)
        {
            Vector3 collapsedScale = new Vector3(
                m_visibleScale.x * m_collapsedWidthRatio,
                m_visibleScale.y,
                m_visibleScale.z);
            transform.localScale = collapsedScale;
            sequence.Append(transform.DOScale(m_visibleScale, m_showDuration).SetEase(m_showEase));
        }
        else
        {
            m_canvasGroup.alpha = 0f;
            sequence.Append(m_canvasGroup.DOFade(1f, m_showDuration).SetEase(m_showEase));
        }

        m_activeTween = sequence.SetTarget(this);
    }

    /// <summary>
    /// UIを非表示状態へアニメーションし、完了後に非アクティブ化します。
    /// </summary>
    public void PlayHide(System.Action onComplete = null)
    {
        Initialize();

        if (!gameObject.activeSelf)
        {
            onComplete?.Invoke();
            return;
        }

        if (m_isHiding && m_activeTween != null && m_activeTween.IsActive())
        {
            m_hideCompleted += onComplete;
            return;
        }

        StopActiveTween(false);
        m_isHiding = true;
        m_hideCompleted = onComplete;

        Sequence sequence = DOTween.Sequence();
        if (m_animationType == AnimationType.SLIDE && m_rectTransform != null)
        {
            Vector2 hiddenPosition = m_visiblePosition + Vector2.left * m_slideDistance;
            sequence.Append(m_rectTransform.DOAnchorPos(hiddenPosition, m_hideDuration).SetEase(m_hideEase));
        }
        else if (m_animationType == AnimationType.HORIZONTAL_EXPAND)
        {
            Vector3 collapsedScale = new Vector3(
                m_visibleScale.x * m_collapsedWidthRatio,
                m_visibleScale.y,
                m_visibleScale.z);
            sequence.Append(transform.DOScale(collapsedScale, m_hideDuration).SetEase(m_hideEase));
        }
        else
        {
            sequence.Append(m_canvasGroup.DOFade(0f, m_hideDuration).SetEase(m_hideEase));
        }
        sequence.OnComplete(() =>
        {
            m_activeTween = null;
            m_isHiding = false;
            gameObject.SetActive(false);
            Action completed = m_hideCompleted;
            m_hideCompleted = null;
            completed?.Invoke();
        });

        m_activeTween = sequence.SetTarget(this);
    }

    /// <summary>
    /// アニメーションを停止し、UIを即時非表示にします。
    /// </summary>
    public void HideImmediately()
    {
        Initialize();
        StopActiveTween(false);
        m_isHiding = false;
        m_hideCompleted = null;
        if (m_rectTransform != null)
        {
            m_rectTransform.anchoredPosition = m_visiblePosition;
        }
        transform.localScale = m_visibleScale;
        m_canvasGroup.alpha = 1f;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// このUIに紐づくDOTweenアニメーションを停止します。
    /// </summary>
    private void StopActiveTween(bool complete)
    {
        Tween activeTween = m_activeTween;
        m_activeTween = null;
        if (activeTween == null || !activeTween.IsActive())
        {
            return;
        }

        if (complete)
        {
            activeTween.Complete();
        }
        else
        {
            activeTween.Kill();
        }
    }
}
