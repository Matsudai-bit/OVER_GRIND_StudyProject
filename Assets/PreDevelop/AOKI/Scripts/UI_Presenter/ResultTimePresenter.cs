using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// クリアタイム（秒数）を受け取り、デジタル時計形式（00:00.00）で表示・演出制御する Presenter クラスです。
/// </summary>
public class ResultTimePresenter : MonoBehaviour
{
    [Header("★ UI テキストコンポーネント")]
    [SerializeField] private TextMeshProUGUI m_tmpText;
    [SerializeField] private Text m_uiText;

    [Header("★ 演出設定")]
    [Tooltip("チェックを入れると開始時に自動でカウントアップ演出を再生します")]
    [SerializeField] private bool m_playOnStart = false;

    [Tooltip("自動再生時のテスト用クリアタイム（秒）")]
    [SerializeField] private float m_debugClearTime = 91.46f; // 例: 01:31.46

    [Tooltip("カウントアップ演出にかかる時間（秒）")]
    [SerializeField] private float m_countUpDuration = 1.0f;

    [Tooltip("表示が始まるまでの待ち時間（秒）")]
    [SerializeField] private float m_delay = 0.5f;

    private Tween m_countUpTween;

    private void Awake()
    {
        InitializeComponents();
    }

    private void Start()
    {
        if (m_playOnStart)
        {
            SetTimeWithAnimation(m_debugClearTime);
        }
    }

    private void OnDisable()
    {
        m_countUpTween?.Kill();
    }

    private void InitializeComponents()
    {
        if (m_tmpText == null) TryGetComponent<TextMeshProUGUI>(out m_tmpText);
        if (m_uiText == null) TryGetComponent<Text>(out m_uiText);
    }

    /// <summary>
    /// タイム（秒数）を直接セットして即座に「00:00.00」形式で表示します。
    /// 例: 91.46f -> "01:31.46"
    /// </summary>
    /// <param name="clearTimeInSeconds">クリアタイム（秒）</param>
    public void SetTime(float clearTimeInSeconds)
    {
        InitializeComponents();
        m_countUpTween?.Kill();

        string formattedTime = FormatTime(clearTimeInSeconds);
        ApplyText(formattedTime);
    }

    /// <summary>
    /// 0秒から指定タイムまでパラパラと高速カウントアップしながら表示する演出を行います。
    /// </summary>
    /// <param name="clearTimeInSeconds">最終目標のクリアタイム（秒）</param>
    /// <param name="delay">開始までの待ち時間（秒）</param>
    public void SetTimeWithAnimation(float clearTimeInSeconds, float delay = -1f)
    {
        InitializeComponents();
        m_countUpTween?.Kill();

        float startDelay = (delay >= 0f) ? delay : m_delay;

        // 開始前は 00:00.00 表示
        ApplyText(FormatTime(0f));

        Sequence seq = DOTween.Sequence();

        if (startDelay > 0f)
        {
            seq.AppendInterval(startDelay);
        }

        float currentTime = 0f;

        seq.Append(
            DOVirtual.Float(0f, clearTimeInSeconds, m_countUpDuration, value =>
            {
                currentTime = value;
                ApplyText(FormatTime(currentTime));
            })
            .SetEase(Ease.OutCubic)
        );

        // 最後にピタッと正確なタイムをセット
        seq.OnComplete(() =>
        {
            ApplyText(FormatTime(clearTimeInSeconds));
        });

        m_countUpTween = seq;
    }

    /// <summary>
    /// 秒数を 「00:00.00」 形式の文字列にフォーマット変換します。
    /// </summary>
    private string FormatTime(float timeInSeconds)
    {
        if (timeInSeconds < 0f) timeInSeconds = 0f;

        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        int fraction = Mathf.FloorToInt((timeInSeconds * 100f) % 100f);

        // 99分59秒99 を上限とするガード
        if (minutes > 99)
        {
            minutes = 99;
            seconds = 59;
            fraction = 99;
        }

        return string.Format("{0:D2}:{1:D2}.{2:D2}", minutes, seconds, fraction);
    }

    private void ApplyText(string textValue)
    {
        if (m_tmpText != null) m_tmpText.text = textValue;
        if (m_uiText != null) m_uiText.text = textValue;
    }
}