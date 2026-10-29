using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// UIのフェード演出を制御します。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public sealed class UIFadeController : MonoBehaviour
{
    [SerializeField, Header("フェード時間")]
    [Min(0.0f)]
    private float m_fadeInDuration = 0.5f;

    [SerializeField, Min(0.0f)]
    private float m_fadeOutDuration = 0.5f;

    [SerializeField, Header("TimeScaleの影響を受けない")]
    private bool m_useUnscaledTime = true;

    // 透過率の制御対象
    private CanvasGroup m_canvasGroup;

    // 実行中のフェード処理
    private Coroutine m_fadeCoroutine;

    [SerializeField, Header("ウィンドウ連携")]
    private UIWindow m_window;

    /// <summary>
    /// ウィンドウのフェードインを実行します。
    /// </summary>
    public void ShowWindow()
    {
        SetAlpha(0.0f);

        FadeIn(() =>
        {
            if (m_window != null)
            {
                m_window.NotifyShowCompleted();
            }
        });
    }

    /// <summary>
    /// ウィンドウのフェードアウトを実行します。
    /// </summary>
    public void HideWindow()
    {
        FadeOut(() =>
        {
            if (m_window != null)
            {
                m_window.NotifyHideCompleted();
            }
        });
    }

    private void Awake()
    {
        m_canvasGroup = GetComponent<CanvasGroup>();
    }

    private void OnDisable()
    {
        m_fadeCoroutine = null;
    }

    /// <summary>
    /// フェードインを開始します。
    /// </summary>
    public void FadeIn(Action onCompleted = null)
    {
        FadeTo(1.0f, m_fadeInDuration, onCompleted);
    }

    /// <summary>
    /// フェードアウトを開始します。
    /// </summary>
    public void FadeOut(Action onCompleted = null)
    {
        FadeTo(0.0f, m_fadeOutDuration, onCompleted);
    }

    /// <summary>
    /// 指定した透過率までフェードします。
    /// </summary>
    public void FadeTo(
        float targetAlpha,
        float duration,
        Action onCompleted = null)
    {
        StopFade();

        targetAlpha = Mathf.Clamp01(targetAlpha);

        if (duration <= 0.0f)
        {
            SetAlpha(targetAlpha);
            onCompleted?.Invoke();
            return;
        }

        if (!isActiveAndEnabled)
        {
            Debug.LogWarning(
                "非アクティブな状態ではフェードを開始できません。",
                this);
            return;
        }

        m_fadeCoroutine = StartCoroutine(
            FadeCoroutine(targetAlpha, duration, onCompleted));
    }

    /// <summary>
    /// フェードを停止します。
    /// </summary>
    public void StopFade()
    {
        if (m_fadeCoroutine == null)
        {
            return;
        }

        StopCoroutine(m_fadeCoroutine);
        m_fadeCoroutine = null;
    }

    /// <summary>
    /// 透過率を即座に変更します。
    /// </summary>
    public void SetAlpha(float alpha)
    {
        if (m_canvasGroup == null)
        {
            m_canvasGroup = GetComponent<CanvasGroup>();
        }

        m_canvasGroup.alpha = Mathf.Clamp01(alpha);
    }

    /// <summary>
    /// フェードを実行します。
    /// </summary>
    private IEnumerator FadeCoroutine(
        float targetAlpha,
        float duration,
        Action onCompleted)
    {
        float startAlpha = m_canvasGroup.alpha;
        float elapsedTime = 0.0f;

        while (elapsedTime < duration)
        {
            float deltaTime = m_useUnscaledTime
                ? Time.unscaledDeltaTime
                : Time.deltaTime;

            elapsedTime += deltaTime;

            float t = Mathf.Clamp01(elapsedTime / duration);

            // 滑らかな補間
            t = Mathf.SmoothStep(0.0f, 1.0f, t);

            SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, t));

            yield return null;
        }

        SetAlpha(targetAlpha);

        m_fadeCoroutine = null;

        onCompleted?.Invoke();
    }
}