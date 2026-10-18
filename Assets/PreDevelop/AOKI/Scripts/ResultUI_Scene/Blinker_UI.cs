using System.Collections;
using UnityEngine;

/// <summary>
/// UIグループ（CanvasGroup）を右画面外からスライドインさせ、
/// 「赤丸 ＋ NEW RECORD」などの要素をまとめて明滅（点滅）させる制御クラスです。
/// </summary>
public class Blinker_UI : MonoBehaviour
{
    public enum BlinkType
    {
        [Tooltip("サイン波による滑らかなフェード点滅（NEW RECORD演出向け）")]
        Smooth,

        [Tooltip("一定間隔で表示・非表示がパチパチ切り替わる点滅")]
        Hard
    }

    [Header("★ スライド設定")]
    [Tooltip("チェックを入れると自動で右からスライドインします（手動呼び出し時は自動オフ推奨）")]
    [SerializeField] private bool m_slideOnStart = false;

    [Tooltip("スライドが始まるまでの待ち時間（秒）")]
    [SerializeField] private float m_slideDelay = 0f;

    [Tooltip("スライド移動にかかる時間（秒）")]
    [SerializeField] private float m_slideDuration = 0.5f;

    [Tooltip("右側に移動させるオフセット距離（ピクセル）")]
    [SerializeField] private float m_slideOffsetDistance = 1000f;

    [Tooltip("アニメーションの緩急カーブ")]
    [SerializeField] private AnimationCurve m_easeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("スライド移動させる RectTransform（未指定時は自身の RectTransform）")]
    [SerializeField] private RectTransform m_slideTarget;

    [Header("★ 点滅設定")]
    [Tooltip("チェックを入れると自動で点滅を開始します")]
    [SerializeField] private bool m_blinkOnStart = false;

    [Tooltip("スライド完了後に点滅を開始する場合はチェック（オフで移動中も点滅）")]
    [SerializeField] private bool m_startBlinkAfterSlide = true;

    [Tooltip("点滅が始まるまでの待ち時間（秒）")]
    [SerializeField] private float m_blinkDelay = 0f;

    [Tooltip("点滅のタイプ（Smooth: ふわっと明滅 / Hard: パチパチ点滅）")]
    [SerializeField] private BlinkType m_blinkType = BlinkType.Smooth;

    [Tooltip("点滅するスピード（数値が大きいほど速く点滅）")]
    [SerializeField] private float m_speed = 3f;

    [Range(0f, 1f)]
    [Tooltip("一番暗くなった（薄くなった）時のアルファ値")]
    [SerializeField] private float m_minAlpha = 0.2f;

    [Range(0f, 1f)]
    [Tooltip("一番明るくなった時のアルファ値")]
    [SerializeField] private float m_maxAlpha = 1f;

    [Tooltip("点滅制御用 CanvasGroup（未指定時は自動生成・取得）")]
    [SerializeField] private CanvasGroup m_canvasGroup;

    private Vector2 m_originalAnchoredPosition;
    private bool m_isInitialized = false;
    private Coroutine m_slideCoroutine;
    private Coroutine m_blinkCoroutine;

    private void Awake()
    {
        InitializeComponents();
    }

    private void OnEnable()
    {
        InitializeComponents();

        if (m_slideOnStart)
        {
            ResetToOffscreen();
            PlaySlideIn();
        }
        else if (m_blinkOnStart && !m_startBlinkAfterSlide)
        {
            StartBlink();
        }
    }

    private void OnDisable()
    {
        StopSlide();
        StopBlink();
    }

    private void InitializeComponents()
    {
        if (m_slideTarget == null)
        {
            m_slideTarget = GetComponent<RectTransform>();
        }

        if (m_canvasGroup == null)
        {
            if (!TryGetComponent<CanvasGroup>(out m_canvasGroup))
            {
                m_canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (!m_isInitialized && m_slideTarget != null)
        {
            m_originalAnchoredPosition = m_slideTarget.anchoredPosition;
            m_isInitialized = true;
        }
    }

    /// <summary>
    /// ★ 新記録かどうかを設定して表示・アニメーションを制御します。
    /// </summary>
    /// <param name="isNewRecord">新記録の場合は true、更新しなかった場合は false</param>
    /// <param name="delay">演出開始までの遅延時間（秒）</param>
    public void SetNewRecord(bool isNewRecord, float delay = 0f)
    {
        InitializeComponents();

        if (isNewRecord)
        {
            // 新記録：表示を有効化してスライドイン＋点滅演出を開始
            gameObject.SetActive(true);
            if (m_canvasGroup != null) m_canvasGroup.alpha = 1f;

            ResetToOffscreen();
            PlaySlideIn(delay);
        }
        else
        {
            // 記録更新なし：完全非表示にして動作を停止
            StopSlide();
            StopBlink();

            if (m_canvasGroup != null) m_canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 対象を即座に右画面外の位置へ移動させます。
    /// </summary>
    public void ResetToOffscreen()
    {
        InitializeComponents();
        if (m_slideTarget == null) return;

        StopSlide();
        Vector2 offscreenPos = m_originalAnchoredPosition + new Vector2(m_slideOffsetDistance, 0f);
        m_slideTarget.anchoredPosition = offscreenPos;
    }

    /// <summary>
    /// 右画面外から本来の位置へスライドイン再生します。
    /// </summary>
    public void PlaySlideIn()
    {
        PlaySlideIn(m_slideDelay);
    }

    /// <summary>
    /// 指定された遅延時間（秒）を待ってから右画面外よりスライドイン再生します。
    /// </summary>
    public void PlaySlideIn(float delay)
    {
        InitializeComponents();
        if (m_slideTarget == null) return;

        StopSlide();
        Vector2 startPos = m_originalAnchoredPosition + new Vector2(m_slideOffsetDistance, 0f);
        Vector2 targetPos = m_originalAnchoredPosition;

        m_slideCoroutine = StartCoroutine(SlideRoutine(startPos, targetPos, delay));
    }

    /// <summary>
    /// 本来の位置から右画面外へスライドアウト再生します。
    /// </summary>
    public void PlaySlideOut(float delay = 0f)
    {
        InitializeComponents();
        if (m_slideTarget == null) return;

        StopSlide();
        Vector2 startPos = m_slideTarget.anchoredPosition;
        Vector2 targetPos = m_originalAnchoredPosition + new Vector2(m_slideOffsetDistance, 0f);

        m_slideCoroutine = StartCoroutine(SlideRoutine(startPos, targetPos, delay));
    }

    /// <summary>
    /// スライド移動処理を停止します。
    /// </summary>
    public void StopSlide()
    {
        if (m_slideCoroutine != null)
        {
            StopCoroutine(m_slideCoroutine);
            m_slideCoroutine = null;
        }
    }

    private IEnumerator SlideRoutine(Vector2 startPosition, Vector2 targetPosition, float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        float elapsedTime = 0f;

        while (elapsedTime < m_slideDuration)
        {
            elapsedTime += Time.deltaTime;
            float timeRatio = Mathf.Clamp01(elapsedTime / m_slideDuration);
            float evaluatedRatio = m_easeCurve.Evaluate(timeRatio);

            m_slideTarget.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, evaluatedRatio);
            yield return null;
        }

        m_slideTarget.anchoredPosition = targetPosition;
        m_slideCoroutine = null;

        // スライド完了後に点滅開始
        StartBlink();
    }

    /// <summary>
    /// 点滅アニメーションを開始します。
    /// </summary>
    public void StartBlink()
    {
        StopBlink();
        m_blinkCoroutine = StartCoroutine(BlinkRoutine());
    }

    /// <summary>
    /// 点滅アニメーションを停止し、不透明状態に戻します。
    /// </summary>
    public void StopBlink()
    {
        if (m_blinkCoroutine != null)
        {
            StopCoroutine(m_blinkCoroutine);
            m_blinkCoroutine = null;
        }

        if (m_canvasGroup != null)
        {
            m_canvasGroup.alpha = m_maxAlpha;
        }
    }

    private IEnumerator BlinkRoutine()
    {
        if (m_blinkDelay > 0f)
        {
            yield return new WaitForSeconds(m_blinkDelay);
        }

        float time = 0f;

        while (true)
        {
            time += Time.deltaTime * m_speed;

            float currentAlpha;
            if (m_blinkType == BlinkType.Smooth)
            {
                // サイン波で滑らかにフェード明滅
                float wave = (Mathf.Sin(time) + 1f) * 0.5f;
                currentAlpha = Mathf.Lerp(m_minAlpha, m_maxAlpha, wave);
            }
            else
            {
                float wave = Mathf.Sin(time);
                currentAlpha = wave >= 0f ? m_maxAlpha : m_minAlpha;
            }

            if (m_canvasGroup != null)
            {
                m_canvasGroup.alpha = currentAlpha;
            }

            yield return null;
        }
    }
}