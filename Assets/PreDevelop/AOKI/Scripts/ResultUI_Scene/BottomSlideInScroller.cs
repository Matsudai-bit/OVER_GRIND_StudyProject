using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// UI要素（RectTransform）を画面下側から上方向へスライドイン・スライドアウトさせる制御クラスです。
/// Alpha（不透明度）の開始値および目標値を個別に調整可能です。
/// </summary>
public class BottomSlideInScroller : MonoBehaviour
{
    private const float DEFAULT_DELAY = 0.5f;
    private const float DEFAULT_DURATION = 0.5f;
    private const float DEFAULT_OFFSET_DISTANCE = 150f;

    [Header("自動再生設定")]
    /// <summary>有効化時に自動で再生するかどうか。</summary>
    [Tooltip("チェックを入れるとシーン開始時に自動で下からスライドインします")]
    [SerializeField] private bool m_playOnStart = true;

    [Header("アニメーション設定")]
    /// <summary>再生開始までのディレイ時間（秒）。</summary>
    [Tooltip("自動再生（Start）時にスライドアニメーションが始まるまでの待ち時間（秒）")]
    [SerializeField] private float m_delay = DEFAULT_DELAY;

    /// <summary>スライドにかかる時間（秒）。</summary>
    [Tooltip("スライドにかかる時間（秒）")]
    [SerializeField] private float m_slideDuration = DEFAULT_DURATION;

    /// <summary>下側に移動させるオフセット距離（ピクセル）。</summary>
    [Tooltip("下側に移動させるオフセット距離（ピクセル）※正の数で指定")]
    [SerializeField] private float m_slideOffsetDistance = DEFAULT_OFFSET_DISTANCE;

    [Header("不透明度（Alpha）設定")]
    /// <summary>スライドイン開始時の Alpha 値。</summary>
    [Tooltip("スライドイン開始時の Alpha 値（0: 完全透明 ～ 1: 完全不透明）")]
    [Range(0f, 1f)]
    [SerializeField] private float m_startAlpha = 0f;

    /// <summary>スライドイン終了時の Alpha 値。</summary>
    [Tooltip("スライドイン終了時の Alpha 値（0: 完全透明 ～ 1: 完全不透明）")]
    [Range(0f, 1f)]
    [SerializeField] private float m_targetAlpha = 1f;

    [Header("イージング設定（速さ＆不透明度カーブ）")]
    /// <summary>手動補間用の AnimationCurve。</summary>
    [Tooltip("アニメーションの緩急および不透明度カーブ")]
    [SerializeField] private AnimationCurve m_easeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    /// <summary>DOTween の Ease を使用するかどうか。</summary>
    [Tooltip("DOTween の Ease を使用するかどうか")]
    [SerializeField] private bool m_useDotweenEase = true;

    /// <summary>DOTween 使用時のイージングタイプ。</summary>
    [Tooltip("DOTween 使用時のイージングタイプ")]
    [SerializeField] private Ease m_dotweenEase = Ease.OutCubic;

    [Header("デバッグテスト設定")]
    /// <summary>キー入力によるテスト再生を有効にするかどうか。</summary>
    [Tooltip("Spaceキーでイン、Shiftキーでアウト再生テストをするか")]
    [SerializeField] private bool m_enableTestInput = false;

    [Header("対象")]
    /// <summary>アニメーション対象の RectTransform。</summary>
    [Tooltip("アニメーション対象の RectTransform（未指定時は自身）")]
    [SerializeField] private RectTransform m_slideTarget;

    /// <summary>フェード制御用の CanvasGroup。</summary>
    [Tooltip("フェード制御用の CanvasGroup（未指定時は自動取得/追加）")]
    [SerializeField] private CanvasGroup m_canvasGroup;

    /// <summary>初期位置記憶用変数。</summary>
    private Vector2 m_originalAnchoredPosition;

    /// <summary>初期化完了フラグ。</summary>
    private bool m_isInitialized = false;

    /// <summary>アニメーション用コルーチン参照。</summary>
    private Coroutine m_slideCoroutine;

    /// <summary>DOTween シーケンス参照。</summary>
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

    private void OnDisable()
    {
        StopActiveAnimations();
    }

    private void OnDestroy()
    {
        StopActiveAnimations();
    }

    /// <summary>
    /// 各コンポーネントの初期位置および参照の初期化を行います。
    /// </summary>
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

    /// <summary>
    /// 対象オブジェクトを画面外（初期 Alpha 位置）へリセットします。
    /// </summary>
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
            m_canvasGroup.alpha = m_startAlpha;
        }
    }

    /// <summary>
    /// 即座にスライドイン再生を開始します（遅延なし）。
    /// </summary>
    public void PlaySlideIn()
    {
        PlaySlideInWithDelay(0f);
    }

    /// <summary>
    /// 指定された遅延時間（秒）を待ってからスライドイン再生を開始します。
    /// </summary>
    /// <param name="customDelay">開始までの待ち時間（秒）。</param>
    public void PlaySlideInWithDelay(float customDelay)
    {
        InitializePositions();
        StopActiveAnimations();

        float absOffset = Mathf.Abs(m_slideOffsetDistance);
        Vector2 startPos = m_originalAnchoredPosition - new Vector2(0f, absOffset);
        Vector2 targetPos = m_originalAnchoredPosition;

        if (m_useDotweenEase)
        {
            PlayDotweenAnimation(startPos, targetPos, m_startAlpha, m_targetAlpha, customDelay);
        }
        else
        {
            m_slideCoroutine = StartCoroutine(AnimateSlideCoroutine(startPos, targetPos, m_startAlpha, m_targetAlpha, customDelay));
        }
    }

    /// <summary>
    /// 現在位置から下画面外へスライドアウト再生を開始します。
    /// </summary>
    /// <param name="customDelay">開始までの待ち時間（秒）。</param>
    public void PlaySlideOut(float customDelay = 0f)
    {
        InitializePositions();
        StopActiveAnimations();

        float absOffset = Mathf.Abs(m_slideOffsetDistance);
        Vector2 startPos = m_slideTarget.anchoredPosition;
        Vector2 targetPos = m_originalAnchoredPosition - new Vector2(0f, absOffset);

        float currentAlpha = (m_canvasGroup != null) ? m_canvasGroup.alpha : m_targetAlpha;

        if (m_useDotweenEase)
        {
            PlayDotweenAnimation(startPos, targetPos, currentAlpha, m_startAlpha, customDelay);
        }
        else
        {
            m_slideCoroutine = StartCoroutine(AnimateSlideCoroutine(startPos, targetPos, currentAlpha, m_startAlpha, customDelay));
        }
    }

    /// <summary>
    /// 実行中の全アニメーション（DOTween / コルーチン）を停止します。
    /// </summary>
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

    /// <summary>
    /// DOTween を用いてスライドおよびフェードアニメーションを実行します。
    /// </summary>
    /// <param name="startPos">開始位置。</param>
    /// <param name="targetPos">目標位置。</param>
    /// <param name="startAlpha">開始不透明度。</param>
    /// <param name="targetAlpha">目標不透明度。</param>
    /// <param name="delay">遅延時間（秒）。</param>
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

    /// <summary>
    /// コルーチンを用いてスライドおよびフェードアニメーションを実行します。
    /// </summary>
    /// <param name="startPosition">開始位置。</param>
    /// <param name="targetPosition">目標位置。</param>
    /// <param name="startAlpha">開始不透明度。</param>
    /// <param name="targetAlpha">目標不透明度。</param>
    /// <param name="delay">遅延時間（秒）。</param>
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
            float evaluatedRatio = (m_easeCurve != null) ? m_easeCurve.Evaluate(timeRatio) : timeRatio;

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
}