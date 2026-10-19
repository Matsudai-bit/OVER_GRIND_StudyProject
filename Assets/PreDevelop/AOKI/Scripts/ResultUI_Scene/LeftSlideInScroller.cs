using System.Collections;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 初速の速い減速カーブ（Ease Out）を用いて、位置のスライドと不透明度（Alpha）の
/// フェードインを連動して再生するスクリプトです。
/// </summary>
public class LeftSlideInScroller : MonoBehaviour
{
    private const float DEFAULT_DELAY = 0.5f;
    private const float DEFAULT_DURATION = 0.5f;
    private const float DEFAULT_OFFSET_DISTANCE = 200f;
    private const float ALPHA_MIN = 0f;
    private const float ALPHA_MAX = 1f;

    /// <summary>
    /// スライドの侵入方向です。
    /// </summary>
    public enum SlideDirection
    {
        LEFT_TO_RIGHT, // 左からスライドイン
        RIGHT_TO_LEFT  // 右からスライドイン
    }

    [Header("自動再生設定")]
    [Tooltip("有効化時に自動で再生するかどうか")]
    [SerializeField] private bool m_playOnStart = true;

    [Tooltip("再生開始までのディレイ時間（秒）")]
    [SerializeField] private float m_startDelay = DEFAULT_DELAY;

    [Header("スライド演出設定")]
    [Tooltip("スライド対象の RectTransform（未指定時は自身）")]
    [SerializeField] private RectTransform m_slideTarget;

    [Tooltip("スライド方向")]
    [SerializeField] private SlideDirection m_direction = SlideDirection.RIGHT_TO_LEFT;

    [Tooltip("アニメーション時間（秒）")]
    [SerializeField] private float m_slideDuration = DEFAULT_DURATION;

    [Tooltip("スライドの開始オフセット距離（ピクセル）")]
    [SerializeField] private float m_slideOffsetDistance = DEFAULT_OFFSET_DISTANCE;

    [Header("イージング設定（速さ＆不透明度カーブ）")]
    [Tooltip("インスペクターのグラフでカスタムカーブを指定する場合に使用します")]
    [SerializeField]
    private AnimationCurve m_easeCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 2f, 2f), // 初速を速くする急傾斜
        new Keyframe(1f, 1f, 0f, 0f)  // 着地時に減速する水平傾斜
    );

    [Tooltip("AnimationCurve の代わりに DOTween の Ease タイプを使用するかどうか")]
    [SerializeField] private bool m_useDotweenEase = false;

    [Tooltip("DOTween 使用時の Ease タイプ（Ease.OutCubic や Ease.OutExpo が初速最速でおすすめです）")]
    [SerializeField] private Ease m_dotweenEase = Ease.OutCubic;

    // 内部取得コンポーネント
    private CanvasGroup m_canvasGroup;
    private Vector2 m_targetAnchoredPosition;
    private Coroutine m_slideCoroutine;

    private void Awake()
    {
        InitializeComponents();
    }

    private void OnEnable()
    {
        if (m_playOnStart)
        {
            PlaySlideIn();
        }
    }

    private void OnDisable()
    {
        StopSlideIn();
    }

    /// <summary>
    /// コンポーネントおよび初期位置の初期化を行います。
    /// </summary>
    private void InitializeComponents()
    {
        if (m_slideTarget == null)
        {
            m_slideTarget = GetComponent<RectTransform>();
        }

        if (m_slideTarget != null)
        {
            m_targetAnchoredPosition = m_slideTarget.anchoredPosition;

            m_canvasGroup = m_slideTarget.GetComponent<CanvasGroup>();
            if (m_canvasGroup == null)
            {
                m_canvasGroup = m_slideTarget.gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

    /// <summary>
    /// スライドインおよびフェードイン演出を開始します。
    /// </summary>
    public void PlaySlideIn()
    {
        InitializeComponents();
        if (m_slideTarget == null) return;

        StopSlideIn();
        m_slideCoroutine = StartCoroutine(SlideInRoutine());
    }

    /// <summary>
    /// アニメーションを停止します。
    /// </summary>
    public void StopSlideIn()
    {
        if (m_slideCoroutine != null)
        {
            StopCoroutine(m_slideCoroutine);
            m_slideCoroutine = null;
        }

        if (m_slideTarget != null)
        {
            m_slideTarget.DOKill();
        }
    }

    /// <summary>
    /// スライド＆フェード処理を実行するコルーチンです。
    /// </summary>
    private IEnumerator SlideInRoutine()
    {
        // 1. 初期状態のセット（Alpha 0、オフセット位置へ配置）
        m_canvasGroup.alpha = ALPHA_MIN;

        float directionSign = (m_direction == SlideDirection.RIGHT_TO_LEFT) ? 1f : -1f;
        Vector2 startPosition = m_targetAnchoredPosition + new Vector2(m_slideOffsetDistance * directionSign, 0f);
        m_slideTarget.anchoredPosition = startPosition;

        // 2. ディレイ待機
        if (m_startDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(m_startDelay);
        }

        // 3. アニメーション再生（移動＆不透明度）
        if (m_useDotweenEase)
        {
            // DOTween での連動再生
            m_slideTarget.DOAnchorPos(m_targetAnchoredPosition, m_slideDuration)
                .SetEase(m_dotweenEase)
                .SetUpdate(true);

            m_canvasGroup.DOFade(ALPHA_MAX, m_slideDuration)
                .SetEase(m_dotweenEase)
                .SetUpdate(true);
        }
        else
        {
            // AnimationCurve での連動手動補間
            float elapsedTime = 0f;

            while (elapsedTime < m_slideDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsedTime / m_slideDuration);

                // カーブから評価値を取得（初速が急で後半減速）
                float curveValue = m_easeCurve.Evaluate(t);

                // 位置と不透明度を同じカーブ値で補間
                m_slideTarget.anchoredPosition = Vector2.LerpUnclamped(startPosition, m_targetAnchoredPosition, curveValue);
                m_canvasGroup.alpha = Mathf.LerpUnclamped(ALPHA_MIN, ALPHA_MAX, curveValue);

                yield return null;
            }

            m_slideTarget.anchoredPosition = m_targetAnchoredPosition;
            m_canvasGroup.alpha = ALPHA_MAX;
        }

        m_slideCoroutine = null;
    }
}