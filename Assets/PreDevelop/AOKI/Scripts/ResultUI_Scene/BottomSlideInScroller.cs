using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// UI要素（RectTransform）を画面下側から上方向へスライドイン・スライドアウトさせる制御クラスです。
/// 指定した遅延時間（秒）を待ってからアニメーションを開始する機能に対応しています。
/// </summary>
public class BottomSlideInScroller : MonoBehaviour
{
    [Header("★ 自動再生設定")]
    [Tooltip("チェックを入れるとシーン開始時に自動で下からスライドインします")]
    [SerializeField] private bool m_playOnStart = true;

    [Header("★ アニメーション設定")]
    [Tooltip("スライドアニメーションが始まるまでの待ち時間（秒）")]
    [SerializeField] private float m_delay = 0.5f;

    [Tooltip("スライドにかかる時間（秒）")]
    [SerializeField] private float m_slideDuration = 0.5f;

    [Tooltip("下側に移動させるオフセット距離（ピクセル）")]
    [SerializeField] private float m_slideOffsetDistance = 500f;

    [Tooltip("アニメーションの緩急カーブ")]
    [SerializeField] private AnimationCurve m_easeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("デバッグテスト設定")]
    [Tooltip("Spaceキーでイン、Shiftキーでアウト再生テストをするか")]
    [SerializeField] private bool m_enableTestInput = false;

    [Header("対象")]
    /// <summary>アニメーション対象の RectTransform（未指定時は自身）</summary>
    [SerializeField] private RectTransform m_slideTarget;

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
            // 即座に下画面外に配置し、設定されたDelay後にスライドイン開始
            ResetToOffscreen();
            PlaySlideIn();
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
    /// 対象を即座に下画面外の位置へ移動させます。
    /// </summary>
    public void ResetToOffscreen()
    {
        if (m_slideTarget == null) return;

        if (m_slideCoroutine != null)
        {
            StopCoroutine(m_slideCoroutine);
        }

        Vector2 offscreenPos = m_originalAnchoredPosition + new Vector2(0f, -m_slideOffsetDistance);
        m_slideTarget.anchoredPosition = offscreenPos;
    }

    /// <summary>
    /// 設定された Delay（待ち時間）を待ってから下画面外よりスライドイン再生します。
    /// </summary>
    public void PlaySlideIn()
    {
        PlaySlideIn(m_delay);
    }

    /// <summary>
    /// 指定された遅延時間（秒）を待ってから下画面外よりスライドイン再生します。
    /// </summary>
    /// <param name="customDelay">待ち時間（秒）</param>
    public void PlaySlideIn(float customDelay)
    {
        if (m_slideTarget == null) return;

        Vector2 startPos = m_originalAnchoredPosition + new Vector2(0f, -m_slideOffsetDistance);
        Vector2 targetPos = m_originalAnchoredPosition;

        StartSlideAnimation(startPos, targetPos, customDelay);
    }

    /// <summary>
    /// 本来の位置から下画面外へスライドアウト再生します。
    /// </summary>
    /// <param name="customDelay">待ち時間（秒）</param>
    public void PlaySlideOut(float customDelay = 0f)
    {
        if (m_slideTarget == null) return;

        Vector2 startPos = m_slideTarget.anchoredPosition;
        Vector2 targetPos = m_originalAnchoredPosition + new Vector2(0f, -m_slideOffsetDistance);

        StartSlideAnimation(startPos, targetPos, customDelay);
    }

    /// <summary>
    /// スライド移動処理の開始・更新制御を行います。
    /// </summary>
    private void StartSlideAnimation(Vector2 startPosition, Vector2 targetPosition, float delay)
    {
        if (m_slideCoroutine != null)
        {
            StopCoroutine(m_slideCoroutine);
        }

        m_slideCoroutine = StartCoroutine(AnimateSlide(startPosition, targetPosition, delay));
    }

    /// <summary>
    /// 位置の補間計算を行うコルーチンです。
    /// </summary>
    private IEnumerator AnimateSlide(Vector2 startPosition, Vector2 targetPosition, float delay)
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
    }
}