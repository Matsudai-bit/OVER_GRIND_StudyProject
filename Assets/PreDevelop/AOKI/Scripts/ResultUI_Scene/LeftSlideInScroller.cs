using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// UI要素（RectTransform）を画面右側からスクロールイン・スクロールアウトさせる制御クラスです。
/// </summary>
public class RightSlideInScroller : MonoBehaviour
{
    [Header("★ 自動再生設定")]
    [Tooltip("チェックを入れるとシーン開始時に自動で右からスライドインします")]
    [SerializeField] private bool m_playOnStart = true;

    [Tooltip("シーン開始からスライドインが始まるまでの待ち時間（秒）")]
    [SerializeField] private float m_startDelay = 0f;

    [Header("デバッグテスト設定")]
    [Tooltip("Spaceキーでイン、Shiftキーでアウト再生テストをするか")]
    [SerializeField] private bool m_enableTestInput = false;

    [Header("スライド設定")]
    /// <summary>アニメーション対象の RectTransform（未指定時は自身）</summary>
    [SerializeField] private RectTransform m_slideTarget;

    /// <summary>スライドにかかる時間（秒）</summary>
    [SerializeField] private float m_slideDuration = 0.5f;

    /// <summary>右側に移動させるオフセット距離（ピクセル）</summary>
    [SerializeField] private float m_slideOffsetDistance = 500f;

    /// <summary>アニメーションの緩急カーブ</summary>
    [SerializeField] private AnimationCurve m_easeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    /// <summary>本来の配置場所（目標アンカー座標）</summary>
    private Vector2 m_originalAnchoredPosition;

    /// <summary>実行中のアニメーションコルーチン</summary>
    private Coroutine m_slideCoroutine;

    private void Awake()
    {
        InitializePositions();
    }

    private void Start()
    {
        if (m_playOnStart)
        {
            StartCoroutine(AutoPlayCoroutine());
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

    /// <summary>
    /// シーン開始時に右画面外へ配置し、指定時間待ってからスライドインを開始します。
    /// </summary>
    private IEnumerator AutoPlayCoroutine()
    {
        ResetToOffscreen();

        if (m_startDelay > 0f)
        {
            yield return new WaitForSeconds(m_startDelay);
        }

        PlaySlideIn();
    }

    /// <summary>
    /// 初期位置の取得・記憶を行います。
    /// </summary>
    private void InitializePositions()
    {
        if (m_slideTarget == null)
        {
            m_slideTarget = GetComponent<RectTransform>();
        }

        if (m_slideTarget != null)
        {
            m_originalAnchoredPosition = m_slideTarget.anchoredPosition;
        }
    }

    /// <summary>
    /// 対象を即座に右画面外の位置へ移動させます。
    /// </summary>
    public void ResetToOffscreen()
    {
        if (m_slideTarget == null) return;

        if (m_slideCoroutine != null)
        {
            StopCoroutine(m_slideCoroutine);
        }

        Vector2 offscreenPos = m_originalAnchoredPosition + new Vector2(m_slideOffsetDistance, 0f);
        m_slideTarget.anchoredPosition = offscreenPos;
    }

    /// <summary>
    /// 右画面外から本来の位置へスライドイン再生します。
    /// </summary>
    public void PlaySlideIn()
    {
        if (m_slideTarget == null) return;

        Vector2 startPos = m_originalAnchoredPosition + new Vector2(m_slideOffsetDistance, 0f);
        Vector2 targetPos = m_originalAnchoredPosition;

        StartSlideAnimation(startPos, targetPos);
    }

    /// <summary>
    /// 本来の位置から右画面外へスライドアウト再生します。
    /// </summary>
    public void PlaySlideOut()
    {
        if (m_slideTarget == null) return;

        Vector2 startPos = m_slideTarget.anchoredPosition;
        Vector2 targetPos = m_originalAnchoredPosition + new Vector2(m_slideOffsetDistance, 0f);

        StartSlideAnimation(startPos, targetPos);
    }

    /// <summary>
    /// スライド移動処理の開始・更新制御を行います。
    /// </summary>
    private void StartSlideAnimation(Vector2 startPosition, Vector2 targetPosition)
    {
        if (m_slideCoroutine != null)
        {
            StopCoroutine(m_slideCoroutine);
        }

        m_slideCoroutine = StartCoroutine(AnimateSlide(startPosition, targetPosition));
    }

    /// <summary>
    /// 位置の補間計算を行うコルーチンです。
    /// </summary>
    private IEnumerator AnimateSlide(Vector2 startPosition, Vector2 targetPosition)
    {
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
    }
}