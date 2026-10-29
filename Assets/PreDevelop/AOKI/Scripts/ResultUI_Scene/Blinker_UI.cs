using System.Collections;
using UnityEngine;

/// <summary>
/// UI要素（CanvasGroup）を明滅（点滅）させる制御クラスです。
/// 「NEW RECORD」や演出ロゴなどの点滅表現に使用します。
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

    [Header("点滅設定")]
    [Tooltip("チェックを入れるとオブジェクト有効化時に自動で点滅を開始します")]
    [SerializeField] private bool m_blinkOnStart = true;

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

    [Header("対象")]
    [Tooltip("点滅制御用 CanvasGroup（未指定時は自動生成・取得）")]
    [SerializeField] private CanvasGroup m_canvasGroup;

    private Coroutine m_blinkCoroutine;

    private void Awake()
    {
        InitializeComponents();
    }

    private void OnEnable()
    {
        InitializeComponents();

        if (m_blinkOnStart)
        {
            StartBlink();
        }
    }

    private void OnDisable()
    {
        StopBlink();
    }

    private void InitializeComponents()
    {
        if (m_canvasGroup == null)
        {
            if (!TryGetComponent<CanvasGroup>(out m_canvasGroup))
            {
                m_canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

    /// <summary>
    /// 新記録かどうかを設定して表示・点滅演出を制御します。
    /// </summary>
    /// <param name="isNewRecord">新記録の場合は true、更新しなかった場合は false</param>
    /// <param name="delay">点滅開始までの遅延時間（秒）</param>
    public void SetNewRecord(bool isNewRecord, float delay = 0f)
    {
        InitializeComponents();

        if (isNewRecord)
        {
            // 新記録：表示を有効化して点滅開始
            gameObject.SetActive(true);
            StartBlink(delay);
        }
        else
        {
            // 記録更新なし：完全非表示にして停止
            StopBlink();
            if (m_canvasGroup != null) m_canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 点滅アニメーションを開始します。
    /// </summary>
    public void StartBlink()
    {
        StartBlink(m_blinkDelay);
    }

    /// <summary>
    /// 指定した遅延時間を待ってから点滅アニメーションを開始します。
    /// </summary>
    /// <param name="delay">開始までの待ち時間（秒）</param>
    public void StartBlink(float delay)
    {
        StopBlink();
        m_blinkCoroutine = StartCoroutine(BlinkRoutine(delay));
    }

    /// <summary>
    /// 点滅アニメーションを停止し、最大アルファ状態に戻します。
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

    /// <summary>
    /// 点滅処理を実行するコルーチンです（Time.timeScale = 0 でも動作します）。
    /// </summary>
    private IEnumerator BlinkRoutine(float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSecondsRealtime(delay);
        }

        float time = 0f;

        while (true)
        {
            time += Time.unscaledDeltaTime * m_speed;

            float currentAlpha;
            if (m_blinkType == BlinkType.Smooth)
            {
                // サイン波で滑らかにフェード明滅
                float wave = (Mathf.Sin(time) + 1f) * 0.5f;
                currentAlpha = Mathf.Lerp(m_minAlpha, m_maxAlpha, wave);
            }
            else
            {
                // パチパチ切り替わる点滅
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