using UnityEngine;

/// <summary>
/// UIの透過率を繰り返し変化させます。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public sealed class UIAlphaLoop : MonoBehaviour
{
    [SerializeField, Header("透過率")]
    [Range(0.0f, 1.0f)]
    private float m_minAlpha = 0.2f;

    [SerializeField, Range(0.0f, 1.0f)]
    private float m_maxAlpha = 1.0f;

    [SerializeField, Header("ループ時間")]
    [Min(0.01f)]
    private float m_duration = 1.5f;

    [SerializeField, Header("起動時に自動再生")]
    private bool m_playOnEnable = true;

    [SerializeField, Header("TimeScaleの影響を受けない")]
    private bool m_useUnscaledTime = true;

    // 透過率の制御対象
    private CanvasGroup m_canvasGroup;

    // 経過時間
    private float m_elapsedTime = 0.0f;

    // 再生中かどうか
    private bool m_isPlaying = false;

    private void Awake()
    {
        m_canvasGroup = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        if (m_playOnEnable)
        {
            Play();
        }
    }

    private void OnDisable()
    {
        m_isPlaying = false;
    }

    private void Update()
    {
        if (!m_isPlaying)
        {
            return;
        }

        float deltaTime = m_useUnscaledTime
            ? Time.unscaledDeltaTime
            : Time.deltaTime;

        m_elapsedTime += deltaTime;

        // PingPongで0～1を往復させる
        float t = Mathf.PingPong(
            m_elapsedTime / m_duration,
            1.0f);

        t = Mathf.SmoothStep(0.0f, 1.0f, t);

        m_canvasGroup.alpha = Mathf.Lerp(
            m_minAlpha,
            m_maxAlpha,
            t);
    }

    /// <summary>
    /// 透過率ループを開始します。
    /// </summary>
    public void Play()
    {
        if (m_canvasGroup == null)
        {
            m_canvasGroup = GetComponent<CanvasGroup>();
        }

        m_elapsedTime = 0.0f;
        m_isPlaying = true;

        m_canvasGroup.alpha = m_minAlpha;
    }

    /// <summary>
    /// 透過率ループを停止します。
    /// </summary>
    public void Stop()
    {
        m_isPlaying = false;
    }

    /// <summary>
    /// 透過率ループを停止して透明度を元に戻します。
    /// </summary>
    public void ResetAlpha()
    {
        Stop();

        m_canvasGroup.alpha = m_maxAlpha;
    }
}