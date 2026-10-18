using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// テキスト（TMP_Text / UI.Text）および画像（UI.Image）のどちらにも対応した解読・グリッチ演出スクリプトです。
/// Stage_Image や Result_Scene_UI_Tutorial などの GameObject をそのまま Inspector にセットできます。
/// </summary>
public class RandomTextEffect : MonoBehaviour
{
    public enum DecodeMode
    {
        [Tooltip("左の文字から1文字ずつ順番に確定（動画風）")]
        SequentialDecode,

        [Tooltip("全体がランダムシャッフルされた後、一斉に確定")]
        FullRandomThenTitle
    }

    [Header("★ 対象オブジェクト（Stage_Image等をそのままドロップ可能）")]
    [Tooltip("未指定時はこのスクリプトが付いている自身を対象にします")]
    [SerializeField] private GameObject m_targetObject;

    [Header("★ アニメーション設定")]
    [SerializeField] private DecodeMode m_decodeMode = DecodeMode.SequentialDecode;
    [SerializeField] private float m_delay = 0.1f;
    [SerializeField] private float m_totalDuration = 0.8f;
    [SerializeField] private float m_shuffleInterval = 0.03f;
    [SerializeField] private bool m_usePunchScale = true;

    [Header("★ テキスト用シャッフル文字")]
    [SerializeField] private string m_randomChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789#$%&@*";

    [Header("★ 自動再生")]
    [SerializeField] private bool m_playOnStart = true;

    // 内部取得用
    private TMP_Text m_tmpText;
    private Text m_uiText;
    private CanvasGroup m_canvasGroup;
    private RectTransform m_rectTransform;

    private string m_defaultText;
    private string m_currentFinalText;
    private Coroutine m_decodeCoroutine;
    private Vector3 m_originalScale;
    private Vector2 m_originalAnchoredPosition;

    private void Awake()
    {
        InitializeTarget();
    }

    private void InitializeTarget()
    {
        GameObject target = m_targetObject != null ? m_targetObject : gameObject;

        // 子要素も含めてテキストコンポーネントを自動検索
        m_tmpText = target.GetComponentInChildren<TMP_Text>();
        m_uiText = target.GetComponentInChildren<Text>();
        m_rectTransform = target.GetComponent<RectTransform>();

        if (m_rectTransform != null)
        {
            m_originalScale = m_rectTransform.localScale;
            m_originalAnchoredPosition = m_rectTransform.anchoredPosition;
        }
        else
        {
            m_originalScale = target.transform.localScale;
        }

        // 画像などのテキスト以外の場合、フェード/グリッチ用に CanvasGroup を準備
        if (m_tmpText == null && m_uiText == null)
        {
            m_canvasGroup = target.GetComponent<CanvasGroup>();
            if (m_canvasGroup == null)
            {
                m_canvasGroup = target.AddComponent<CanvasGroup>();
            }
        }

        m_defaultText = GetTextValue();
        m_currentFinalText = m_defaultText;
    }

    private void OnEnable()
    {
        if (m_playOnStart)
        {
            PlayAnimation();
        }
    }

    private void OnDisable()
    {
        StopAnimation();
    }

    /// <summary>
    /// 解読アニメーションを再生します
    /// </summary>
    public void PlayAnimation(string overrideText = null)
    {
        InitializeTarget();

        m_currentFinalText = !string.IsNullOrEmpty(overrideText) ? overrideText : m_defaultText;

        StopAnimation();
        m_decodeCoroutine = StartCoroutine(DecodeRoutine());
    }

    public void StopAnimation()
    {
        if (m_decodeCoroutine != null)
        {
            StopCoroutine(m_decodeCoroutine);
            m_decodeCoroutine = null;
        }
    }

    private IEnumerator DecodeRoutine()
    {
        if (m_delay > 0f)
        {
            yield return new WaitForSecondsRealtime(m_delay);
        }

        // --- パターンA: テキスト（TMP_Text / UI.Text）の場合 ---
        if (m_tmpText != null || m_uiText != null)
        {
            int textLength = m_currentFinalText.Length;
            if (textLength > 0)
            {
                char[] displayChars = new char[textLength];
                float elapsedTime = 0f;
                float charDecodeTime = m_totalDuration / textLength;

                while (elapsedTime < m_totalDuration)
                {
                    elapsedTime += m_shuffleInterval;

                    if (m_decodeMode == DecodeMode.SequentialDecode)
                    {
                        int confirmedCount = Mathf.FloorToInt(elapsedTime / charDecodeTime);
                        confirmedCount = Mathf.Clamp(confirmedCount, 0, textLength);

                        for (int i = 0; i < textLength; i++)
                        {
                            displayChars[i] = (i < confirmedCount) ? m_currentFinalText[i] : GetRandomChar();
                        }
                    }
                    else
                    {
                        for (int i = 0; i < textLength; i++)
                        {
                            displayChars[i] = GetRandomChar();
                        }
                    }

                    SetTextValue(new string(displayChars));
                    yield return new WaitForSecondsRealtime(m_shuffleInterval);
                }

                SetTextValue(m_currentFinalText);
            }
        }
        // --- パターンB: 画像（Image 等）の場合 ---
        else
        {
            float elapsedTime = 0f;

            while (elapsedTime < m_totalDuration)
            {
                elapsedTime += m_shuffleInterval;

                // ランダム明滅（グリッチ）
                if (m_canvasGroup != null)
                {
                    m_canvasGroup.alpha = Random.Range(0.2f, 1.0f);
                }

                // わずかな位置のズレ（解読中のノイズ振動）
                if (m_rectTransform != null)
                {
                    Vector2 jitter = new Vector2(Random.Range(-4f, 4f), Random.Range(-2f, 2f));
                    m_rectTransform.anchoredPosition = m_originalAnchoredPosition + jitter;
                }

                yield return new WaitForSecondsRealtime(m_shuffleInterval);
            }

            // 元の位置・不透明度へ確定復元
            if (m_canvasGroup != null) m_canvasGroup.alpha = 1f;
            if (m_rectTransform != null) m_rectTransform.anchoredPosition = m_originalAnchoredPosition;
        }

        // 確定時のパンチスケール（ぽんと跳ねる演出）
        if (m_usePunchScale)
        {
            Transform t = m_targetObject != null ? m_targetObject.transform : transform;
            t.DOKill();
            t.localScale = m_originalScale;
            t.DOPunchScale(new Vector3(0.15f, 0.15f, 0f), 0.2f, 5, 0.5f).SetUpdate(true);
        }

        m_decodeCoroutine = null;
    }

    private char GetRandomChar()
    {
        if (string.IsNullOrEmpty(m_randomChars)) return '?';
        return m_randomChars[Random.Range(0, m_randomChars.Length)];
    }

    private string GetTextValue()
    {
        if (m_tmpText != null) return m_tmpText.text;
        if (m_uiText != null) return m_uiText.text;
        return string.Empty;
    }

    private void SetTextValue(string value)
    {
        if (m_tmpText != null) m_tmpText.text = value;
        if (m_uiText != null) m_uiText.text = value;
    }
}