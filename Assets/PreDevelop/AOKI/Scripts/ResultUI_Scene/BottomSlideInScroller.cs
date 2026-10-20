using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// UI要素（RectTransform）を画面下側から上方向へスライドイン・スライドアウトさせる制御クラスです。
/// LeftSlideInScroller と開始タイミングが一致するように遅延制御を統一しています。
/// </summary>
public class BottomSlideInScroller : MonoBehaviour
{
    private const float ALPHA_TRANSPARENT = 0f;
    private const float ALPHA_OPAQUE = 1f;

    [Header("自動再生設定")]
    [Tooltip("チェックを入れるとシーン開始時に自動で下からスライドインします")]
    [SerializeField] private bool m_playOnStart = true;

    [Header("アニメーション設定")]
    [Tooltip("自動再生（Start）時にスライドアニメーションが始まるまでの待ち時間（秒）")]
    [SerializeField] private float m_delay = 0.5f;

    [Tooltip("スライドにかかる時間（秒）")]
    [SerializeField] private float m_slideDuration = 0.5f;

    [Tooltip("下側に移動させるオフセット距離（ピクセル）※150 などの正の数で指定")]
    [SerializeField] private float m_slideOffsetDistance = 150f;

    [Header("イージング設定（速さ＆不透明度カーブ）")]
    [Tooltip("アニメーションの緩急および不透明度カーブ")]
    [SerializeField] private AnimationCurve m_easeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("DOTween の Ease を使用するかどうか")]
    [SerializeField] private bool m_useDotweenEase = true;

    [Tooltip("DOTween 使用時のイージングタイプ")]
    [SerializeField] private Ease m_dotweenEase = Ease.OutCubic;

    [Header("デバッグテスト設定")]
    [Tooltip("Spaceキーでイン、Shiftキーでアウト再生テストをするか")]
    [SerializeField] private bool m_enableTestInput = false;

    [Header("対象")]
    [Tooltip("アニメーション対象の RectTransform（未指定時は自身）")]
    [SerializeField] private RectTransform m_slideTarget;

    [Tooltip("フェード制御用の CanvasGroup（未指定時は自動取得/追加）")]
    [SerializeField] private CanvasGroup m_canvasGroup;

    private Vector2 m_originalAnchoredPosition;
    private bool m_isInitialized = false;
    private Coroutine m_slideCoroutine;
    private Sequence m_dotweenSequence;

    private void Awake()
    {
        InitializePositions();
    }

    private void Start()
    {
        if (m_playOnStart)
        {
            ResetToOffscreen();
            // 自動再生時のみインスペクターの m_delay を待機して再生
            PlaySlideInWithDelay(m_delay);
        }
    }

    private void Update()
    {
        if (!m_enableTestInput) return;

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            PlaySlideIn();
        }

        if (Keyboard.current != null && Keyboard.current.leftShiftKey.wasPressedThisFrame)
        {
            PlaySlideOut();
        }
    }

    private void InitializePositions()
    {
        if (m_isInitialized) return;

        if (m_slideTarget == null)
        {
            m_slideTarget = GetComponent<RectTransform>();
        }

        if (m_slideTarget != null)
        {
            m_originalAnchoredPosition = m_slideTarget.anchoredPosition;

            if (m_canvasGroup == null)
            {
                m_canvasGroup = m_slideTarget.GetComponent<CanvasGroup>();
                if (m_canvasGroup == null)
                {
                    m_canvasGroup = m_slideTarget.gameObject.AddComponent<CanvasGroup>();
                }
            }

            m_isInitialized = true;
        }
    }

    public void ResetToOffscreen()
    {
        InitializePositions();
        StopActiveAnimations();

        if (m_slideTarget != null)
        {
            float absOffset = Mathf.Abs(m_slideOffsetDistance);
            Vector2 offscreenPos = m_originalAnchoredPosition - new Vector2(0f, absOffset);
            m_slideTarget.anchoredPosition = offscreenPos;
        }

        if (m_canvasGroup != null)
        {
            m_canvasGroup.alpha = ALPHA_TRANSPARENT;
        }
    }

    /// <summary>
    /// 【外部スクリプトからの呼び出し用】
    /// 呼び出された瞬間（遅延なし）で即座にスライドインします。
    /// </summary>
    public void PlaySlideIn()
    {
        PlaySlideInWithDelay(0f);
    }

    /// <summary>
    /// 指定された遅延時間（秒）を待ってからスライドインします。
    /// </summary>
    /// <param name="customDelay">待ち時間（秒）</param>
    public void PlaySlideInWithDelay(float customDelay)
    {
        InitializePositions();
        StopActiveAnimations();

        float absOffset = Mathf.Abs(m_slideOffsetDistance);
        Vector2 startPos = m_originalAnchoredPosition - new Vector2(0f, absOffset);
        Vector2 targetPos = m_originalAnchoredPosition;

        if (m_useDotweenEase)
        {
            PlayDotweenAnimation(startPos, targetPos, ALPHA_TRANSPARENT, ALPHA_OPAQUE, customDelay);
        }
        else
        {
            m_slideCoroutine = StartCoroutine(AnimateSlideCoroutine(startPos, targetPos, ALPHA_TRANSPARENT, ALPHA_OPAQUE, customDelay));
        }
    }

    /// <summary>
    /// 本来の位置から下画面外へスライドアウトします。
    /// </summary>
    public void PlaySlideOut(float customDelay = 0f)
    {
        InitializePositions();
        StopActiveAnimations();

        float absOffset = Mathf.Abs(m_slideOffsetDistance);
        Vector2 startPos = m_slideTarget.anchoredPosition;
        Vector2 targetPos = m_originalAnchoredPosition - new Vector2(0f, absOffset);

        float currentAlpha = m_canvasGroup != null ? m_canvasGroup.alpha : ALPHA_OPAQUE;

        if (m_useDotweenEase)
        {
            PlayDotweenAnimation(startPos, targetPos, currentAlpha, ALPHA_TRANSPARENT, customDelay);
        }
        else
        {
            m_slideCoroutine = StartCoroutine(AnimateSlideCoroutine(startPos, targetPos, currentAlpha, ALPHA_TRANSPARENT, customDelay));
        }
    }

    private void StopActiveAnimations()
    {
        if (m_slideCoroutine != null)
        {
            StopCoroutine(m_slideCoroutine);
            m_slideCoroutine = null;
        }

        if (m_dotweenSequence != null && m_dotweenSequence.IsActive())
        {
            m_dotweenSequence.Kill();
            m_dotweenSequence = null;
        }
    }

    private void PlayDotweenAnimation(Vector2 startPos, Vector2 targetPos, float startAlpha, float targetAlpha, float delay)
    {
        m_slideTarget.anchoredPosition = startPos;
        if (m_canvasGroup != null)
        {
            m_canvasGroup.alpha = startAlpha;
        }

        m_dotweenSequence = DOTween.Sequence();
        m_dotweenSequence.SetUpdate(true);

        if (delay > 0f)
        {
            m_dotweenSequence.AppendInterval(delay);
        }

        m_dotweenSequence.Append(m_slideTarget.DOAnchorPos(targetPos, m_slideDuration).SetEase(m_dotweenEase));

        if (m_canvasGroup != null)
        {
            m_dotweenSequence.Join(m_canvasGroup.DOFade(targetAlpha, m_slideDuration).SetEase(m_dotweenEase));
        }
    }

    private IEnumerator AnimateSlideCoroutine(Vector2 startPosition, Vector2 targetPosition, float startAlpha, float targetAlpha, float delay)
    {
        m_slideTarget.anchoredPosition = startPosition;
        if (m_canvasGroup != null)
        {
            m_canvasGroup.alpha = startAlpha;
        }

        if (delay > 0f)
        {
            yield return new WaitForSecondsRealtime(delay);
        }

        float elapsedTime = 0f;

        while (elapsedTime < m_slideDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float timeRatio = Mathf.Clamp01(elapsedTime / m_slideDuration);
            float evaluatedRatio = m_easeCurve != null ? m_easeCurve.Evaluate(timeRatio) : timeRatio;

            m_slideTarget.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, evaluatedRatio);

            if (m_canvasGroup != null)
            {
                m_canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, evaluatedRatio);
            }

            yield return null;
        }

        m_slideTarget.anchoredPosition = targetPosition;
        if (m_canvasGroup != null)
        {
            m_canvasGroup.alpha = targetAlpha;
        }

        m_slideCoroutine = null;
    }

    private void OnDestroy()
    {
        StopActiveAnimations();
    }
}