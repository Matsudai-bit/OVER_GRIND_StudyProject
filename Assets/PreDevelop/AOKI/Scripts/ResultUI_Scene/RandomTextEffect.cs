using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// 最初はランダムな文字を高速で切り替えまくり、
/// 最後に指定されたタイトルテキストをバシッと確定表示する演出クラスです。
/// </summary>
public class RandomTextEffect : MonoBehaviour
{
    public enum EffectMode
    {
        [Tooltip("最初は全体の文字をランダム切り替えまくり、最後に一気にタイトルを表示")]
        FullRandomThenTitle,

        [Tooltip("進行度に合わせて左から順番に文字が確定していく（デコード風）")]
        SequentialDecode
    }

    [Header("★ アニメーション対象の UI オブジェクト")]
    [Tooltip("演出させたい親オブジェクト（Result_Scene_UI_Stage など）をドラッグ＆ドロップ（未指定時は自身）")]
    [SerializeField] private GameObject[] m_targetObjects;

    [Header("★ 演出モード設定")]
    [SerializeField] private EffectMode m_effectMode = EffectMode.FullRandomThenTitle;

    [Tooltip("タイトル確定時に一瞬『ポンッ』とテキストを拡大・インパクトバウンドさせます")]
    [SerializeField] private bool m_usePunchScaleOnComplete = true;

    [Header("★ 表示テキスト設定")]
    [Tooltip("チェックを入れると、Textコンポーネントに元々書いてある文字列をそのまま最終タイトルとして使用します")]
    [SerializeField] private bool m_useCurrentTextAsTarget = true;

    [Tooltip("UseCurrentTextAsTarget がオフの場合に使用する文字列リスト（複数ある場合はランダム選択）")]
    [SerializeField] private string[] m_textCandidates = new string[] { "Stage00", "Tutorial", "STAGE CLEAR!" };

    [Tooltip("シャッフル演出中に表示しまくるランダム文字のプール")]
    [SerializeField] private string m_randomCharPool = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!?#$@&%☆";

    [Header("★ 演出パラメータ")]
    [Tooltip("チェックを入れると自動で再生されます")]
    [SerializeField] private bool m_playOnStart = true;

    [Tooltip("演出が始まるまでの待ち時間（秒）")]
    [SerializeField] private float m_delay = 0.5f;

    [Tooltip("ランダム文字を表示しまくる演出時間（秒）")]
    [SerializeField] private float m_duration = 0.8f;

    [Tooltip("ランダム文字の切り替えスピード（秒）。0.02?0.04 あたりが超高速でカッコいいです")]
    [SerializeField] private float m_interval = 0.03f;

    private Coroutine m_effectCoroutine;

    private class TextTargetData
    {
        public TextMeshProUGUI tmpText;
        public Text uiText;
        public string originalText; // 初回に読み取った元のテキスト
        public string currentTargetText; // 今回表示するターゲット文字列
        public RectTransform rectTransform;
        public Vector3 originalScale;
    }

    private readonly List<TextTargetData> m_targetDataList = new List<TextTargetData>();
    private bool m_isInitialized = false;

    private void OnEnable()
    {
        if (m_playOnStart)
        {
            PlayEffect();
        }
    }

    private void OnDisable()
    {
        if (m_effectCoroutine != null)
        {
            StopCoroutine(m_effectCoroutine);
            m_effectCoroutine = null;
        }
    }

    /// <summary>
    /// 設定に基づきランダム文字演出を開始します。
    /// </summary>
    public void PlayEffect()
    {
        PlayEffectInternal(null);
    }

    /// <summary>
    /// 外部から特定のタイトル文字列を指定して演出を開始します。
    /// </summary>
    /// <param name="overrideTitle">表示したいタイトル文字列</param>
    public void PlayEffect(string overrideTitle)
    {
        PlayEffectInternal(overrideTitle);
    }

    private void PlayEffectInternal(string overrideTitle)
    {
        CollectTextComponents();

        if (m_targetDataList.Count == 0) return;

        // ターゲット文字列の更新
        foreach (var target in m_targetDataList)
        {
            if (!string.IsNullOrEmpty(overrideTitle))
            {
                target.currentTargetText = overrideTitle;
            }
            else if (m_useCurrentTextAsTarget)
            {
                target.currentTargetText = target.originalText;
            }
            else if (m_textCandidates != null && m_textCandidates.Length > 0)
            {
                target.currentTargetText = m_textCandidates[Random.Range(0, m_textCandidates.Length)];
            }
            else
            {
                target.currentTargetText = target.originalText;
            }
        }

        if (m_effectCoroutine != null)
        {
            StopCoroutine(m_effectCoroutine);
        }

        m_effectCoroutine = StartCoroutine(RandomTextRoutine());
    }

    private void CollectTextComponents()
    {
        // 既に初期化済みでコンポーネント取得済みであれば再利用
        if (m_isInitialized && m_targetDataList.Count > 0) return;

        m_targetDataList.Clear();

        List<GameObject> objectsToSearch = new List<GameObject>();

        if (m_targetObjects != null && m_targetObjects.Length > 0)
        {
            foreach (var obj in m_targetObjects)
            {
                if (obj != null) objectsToSearch.Add(obj);
            }
        }
        else
        {
            objectsToSearch.Add(gameObject);
        }

        foreach (var obj in objectsToSearch)
        {
            var tmps = obj.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var tmp in tmps)
            {
                AddTarget(tmp, null, tmp.text, tmp.rectTransform);
            }

            var texts = obj.GetComponentsInChildren<Text>(true);
            foreach (var txt in texts)
            {
                AddTarget(null, txt, txt.text, txt.rectTransform);
            }
        }

        m_isInitialized = true;
    }

    private void AddTarget(TextMeshProUGUI tmp, Text txt, string initialText, RectTransform rect)
    {
        if (string.IsNullOrEmpty(initialText)) return;

        m_targetDataList.Add(new TextTargetData
        {
            tmpText = tmp,
            uiText = txt,
            originalText = initialText,
            currentTargetText = initialText,
            rectTransform = rect,
            originalScale = rect != null ? rect.localScale : Vector3.one
        });
    }

    private IEnumerator RandomTextRoutine()
    {
        // 開始前は空文字にして消しておく
        foreach (var target in m_targetDataList)
        {
            SetText(target, "");
        }

        if (m_delay > 0f)
        {
            yield return new WaitForSeconds(m_delay);
        }

        float elapsedTime = 0f;

        // 指定した時間（m_duration）の間、ランダムな文字を高速表示しまくる
        while (elapsedTime < m_duration)
        {
            elapsedTime += m_interval;
            float progress = Mathf.Clamp01(elapsedTime / m_duration);

            foreach (var target in m_targetDataList)
            {
                string targetText = target.currentTargetText;
                int targetLength = targetText.Length;

                char[] currentChars = new char[targetLength];

                if (m_effectMode == EffectMode.FullRandomThenTitle)
                {
                    // 全体ランダムモード：すべての文字をランダムシャッフル表示
                    for (int i = 0; i < targetLength; i++)
                    {
                        if (char.IsWhiteSpace(targetText[i]))
                        {
                            currentChars[i] = targetText[i];
                        }
                        else
                        {
                            int randomIndex = Random.Range(0, m_randomCharPool.Length);
                            currentChars[i] = m_randomCharPool[randomIndex];
                        }
                    }
                }
                else
                {
                    // 順次確定モード（左から決まっていく）
                    int confirmedCount = Mathf.FloorToInt(progress * targetLength);

                    for (int i = 0; i < targetLength; i++)
                    {
                        if (char.IsWhiteSpace(targetText[i]) || i < confirmedCount)
                        {
                            currentChars[i] = targetText[i];
                        }
                        else
                        {
                            int randomIndex = Random.Range(0, m_randomCharPool.Length);
                            currentChars[i] = m_randomCharPool[randomIndex];
                        }
                    }
                }

                SetText(target, new string(currentChars));
            }

            yield return new WaitForSeconds(m_interval);
        }

        // 最後にバシッと目標のタイトル（タイトル名）を確定表示！
        foreach (var target in m_targetDataList)
        {
            SetText(target, target.currentTargetText);

            // 確定時に「ポンッ」と小さく弾ける演出
            if (m_usePunchScaleOnComplete && target.rectTransform != null)
            {
                target.rectTransform.DOKill();
                target.rectTransform.localScale = target.originalScale;
                target.rectTransform.DOPunchScale(Vector3.one * 0.25f, 0.25f, 5, 0.5f);
            }
        }

        m_effectCoroutine = null;
    }

    private void SetText(TextTargetData target, string value)
    {
        if (target.tmpText != null) target.tmpText.text = value;
        if (target.uiText != null) target.uiText.text = value;
    }
}