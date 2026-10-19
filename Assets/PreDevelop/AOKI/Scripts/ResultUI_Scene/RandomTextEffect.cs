using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// テキストの文字シャッフル解読演出および画像のランダムスプライト切り替え・グリッチ演出を行うスクリプトです。
/// </summary>
public class RandomTextEffect : MonoBehaviour
{
    /// <summary>
    /// 解読アニメーションの動作モードです。
    /// </summary>
    public enum DecodeMode
    {
        SEQUENTIAL_DECODE, // 順番に確定
        FULL_RANDOM_THEN_TITLE // 一斉に確定
    }

    // デフォルト・演出用定数
    private const float DEFAULT_DELAY = 0.1f;
    private const float DEFAULT_TOTAL_DURATION = 0.8f;
    private const float DEFAULT_SHUFFLE_INTERVAL = 0.03f;
    private const float MIN_ALPHA_RANGE = 0.2f;
    private const float MAX_ALPHA_RANGE = 1.0f;
    private const float JITTER_POS_X = 4f;
    private const float JITTER_POS_Y = 2f;
    private const float PUNCH_SCALE_AMOUNT = 0.15f;
    private const float PUNCH_SCALE_DURATION = 0.2f;
    private const int PUNCH_SCALE_VIBRATO = 5;
    private const float PUNCH_SCALE_ELASTICITY = 0.5f;

    [Header("★ 対象オブジェクト")]
    [Tooltip("未指定時はこのスクリプトが付いている自身を対象にします")]
    [SerializeField] private GameObject m_targetObject; // 演出対象オブジェクト

    [Header("★ アニメーション設定")]
    [Tooltip("解読モード（Sequential: 順番に確定, FullRandom: 一斉に確定）")]
    [SerializeField] private DecodeMode m_decodeMode = DecodeMode.SEQUENTIAL_DECODE; // 解読モード

    [Tooltip("演出開始までのディレイ時間（秒）")]
    [SerializeField] private float m_delay = DEFAULT_DELAY; // 開始ディレイ

    [Tooltip("演出にかかる合計時間（秒）")]
    [SerializeField] private float m_totalDuration = DEFAULT_TOTAL_DURATION; // 全体演出時間

    [Tooltip("文字・スプライトを切り替える間隔（秒）")]
    [SerializeField] private float m_shuffleInterval = DEFAULT_SHUFFLE_INTERVAL; // 切り替え間隔

    [Tooltip("確定時にぽんと跳ねるパンチスケール演出を行うかどうか")]
    [SerializeField] private bool m_usePunchScale = true; // パンチスケール使用フラグ

    [Header("★ テキスト用シャッフル設定")]
    [Tooltip("テキスト解読時に使用するランダム文字群")]
    [SerializeField] private string m_randomChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789#$%&@*"; // シャッフル用文字群

    [Header("★ 画像用シャッフル設定（画像の場合にパラパラ切り替え）")]
    [Tooltip("画像解読時にランダムで切り替えるダミー画像群（未設定の場合は揺れ・明滅のみ）")]
    [SerializeField] private Sprite[] m_randomSprites; // シャッフル用スプライト配列

    [Header("★ 自動再生設定")]
    [Tooltip("チェックを入れると有効化時に自動で再生します")]
    [SerializeField] private bool m_playOnStart = true; // 自動再生フラグ

    // 内部取得コンポーネント
    private TMP_Text m_tmpText; // TextMeshPro コンポーネント
    private Text m_uiText; // Legacy UI Text コンポーネント
    private Image m_targetImage; // UI Image コンポーネント
    private CanvasGroup m_canvasGroup; // CanvasGroup コンポーネント
    private RectTransform m_rectTransform; // RectTransform コンポーネント

    // 内部保持変数
    private string m_defaultText; // 初期テキスト
    private string m_currentFinalText; // 最終表示テキスト
    private Sprite m_defaultSprite; // 初期スプライト
    private Coroutine m_decodeCoroutine; // 解読コルーチン参照
    private Vector3 m_originalScale; // 初期スケール
    private Vector2 m_originalAnchoredPosition; // 初期位置

    /// <summary>
    /// コンポーネントの初期化を行います。
    /// </summary>
    private void Awake()
    {
        InitializeTarget();
    }

    /// <summary>
    /// オブジェクトが有効化された際に自動再生を行います。
    /// </summary>
    private void OnEnable()
    {
        if (m_playOnStart)
        {
            PlayAnimation();
        }
    }

    /// <summary>
    /// オブジェクトが無効化された際にアニメーションを停止します。
    /// </summary>
    private void OnDisable()
    {
        StopAnimation();
    }

    /// <summary>
    /// 参照コンポーネントおよび初期状態のセットアップを行います。
    /// </summary>
    private void InitializeTarget()
    {
        GameObject target = m_targetObject != null ? m_targetObject : gameObject;

        // 子要素も含めてテキストコンポーネントおよび Image コンポーネントを検索
        m_tmpText = target.GetComponentInChildren<TMP_Text>();
        m_uiText = target.GetComponentInChildren<Text>();
        m_targetImage = target.GetComponentInChildren<Image>();
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

        // テキスト以外の画像オブジェクトの場合
        if (m_tmpText == null && m_uiText == null)
        {
            m_canvasGroup = target.GetComponent<CanvasGroup>();
            if (m_canvasGroup == null)
            {
                m_canvasGroup = target.AddComponent<CanvasGroup>();
            }

            if (m_targetImage != null)
            {
                m_defaultSprite = m_targetImage.sprite;
            }
        }

        m_defaultText = GetTextValue();
        m_currentFinalText = m_defaultText;
    }

    /// <summary>
    /// 解読アニメーションを開始します。
    /// </summary>
    /// <param name="overrideText">上書き表示するテキスト（任意）。</param>
    public void PlayAnimation(string overrideText = null)
    {
        InitializeTarget();

        m_currentFinalText = !string.IsNullOrEmpty(overrideText) ? overrideText : m_defaultText;

        StopAnimation();
        m_decodeCoroutine = StartCoroutine(DecodeRoutine());
    }

    /// <summary>
    /// アニメーションを強制停止します。
    /// </summary>
    public void StopAnimation()
    {
        if (m_decodeCoroutine != null)
        {
            StopCoroutine(m_decodeCoroutine);
            m_decodeCoroutine = null;
        }
    }

    /// <summary>
    /// 解読アニメーションを実行するコルーチンです。
    /// </summary>
    private IEnumerator DecodeRoutine()
    {
        if (m_delay > 0f)
        {
            yield return new WaitForSecondsRealtime(m_delay);
        }

        // --- パターン A: テキスト（TMP_Text / UI.Text）の場合 ---
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

                    if (m_decodeMode == DecodeMode.SEQUENTIAL_DECODE)
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
        // --- パターン B: 画像（Image）の場合 ---
        else
        {
            float elapsedTime = 0f;

            while (elapsedTime < m_totalDuration)
            {
                elapsedTime += m_shuffleInterval;

                // 設定されたランダムスプライトを高速で切り替え（画像シャッフル演出）
                if (m_targetImage != null && m_randomSprites != null && m_randomSprites.Length > 0)
                {
                    int randomIndex = Random.Range(0, m_randomSprites.Length);
                    m_targetImage.sprite = m_randomSprites[randomIndex];
                }

                // ランダム明滅（アルファグリッチ）
                if (m_canvasGroup != null)
                {
                    m_canvasGroup.alpha = Random.Range(MIN_ALPHA_RANGE, MAX_ALPHA_RANGE);
                }

                // わずかな位置のズレ（振動ノイズ）
                if (m_rectTransform != null)
                {
                    Vector2 jitter = new Vector2(
                        Random.Range(-JITTER_POS_X, JITTER_POS_X),
                        Random.Range(-JITTER_POS_Y, JITTER_POS_Y)
                    );
                    m_rectTransform.anchoredPosition = m_originalAnchoredPosition + jitter;
                }

                yield return new WaitForSecondsRealtime(m_shuffleInterval);
            }

            // 本来のスプライト・位置・不透明度へ確定復元
            if (m_targetImage != null && m_defaultSprite != null)
            {
                m_targetImage.sprite = m_defaultSprite;
            }

            if (m_canvasGroup != null)
            {
                m_canvasGroup.alpha = MAX_ALPHA_RANGE;
            }

            if (m_rectTransform != null)
            {
                m_rectTransform.anchoredPosition = m_originalAnchoredPosition;
            }
        }

        // 確定時のパンチスケール（ポンと跳ねる演出）
        if (m_usePunchScale)
        {
            Transform targetTransform = m_targetObject != null ? m_targetObject.transform : transform;
            targetTransform.DOKill();
            targetTransform.localScale = m_originalScale;
            targetTransform.DOPunchScale(
                new Vector3(PUNCH_SCALE_AMOUNT, PUNCH_SCALE_AMOUNT, 0f),
                PUNCH_SCALE_DURATION,
                PUNCH_SCALE_VIBRATO,
                PUNCH_SCALE_ELASTICITY
            ).SetUpdate(true);
        }

        m_decodeCoroutine = null;
    }

    /// <summary>
    /// ランダムな文字を1文字取得します。
    /// </summary>
    /// <returns>ランダム抽出された文字。</returns>
    private char GetRandomChar()
    {
        if (string.IsNullOrEmpty(m_randomChars))
        {
            return '?';
        }

        return m_randomChars[Random.Range(0, m_randomChars.Length)];
    }

    /// <summary>
    /// 対象コンポーネントから現在の文字列を取得します。
    /// </summary>
    /// <returns>取得した文字列。</returns>
    private string GetTextValue()
    {
        if (m_tmpText != null)
        {
            return m_tmpText.text;
        }

        if (m_uiText != null)
        {
            return m_uiText.text;
        }

        return string.Empty;
    }

    /// <summary>
    /// 対象コンポーネントに文字列を設定します。
    /// </summary>
    /// <param name="value">設定する文字列。</param>
    private void SetTextValue(string value)
    {
        if (m_tmpText != null)
        {
            m_tmpText.text = value;
        }

        if (m_uiText != null)
        {
            m_uiText.text = value;
        }
    }
}