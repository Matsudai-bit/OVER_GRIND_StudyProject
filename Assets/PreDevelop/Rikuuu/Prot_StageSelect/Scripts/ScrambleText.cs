using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class ScrambleText : MonoBehaviour
{
    [Header("ランダム文字のプール")]
    [SerializeField] string jaPool = "鉄鋼炉鍛錬溶鋳製所房扉距釦鋼鈑鋲鎚鑪鐵";
    [SerializeField] string enPool = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    [Header("表示文字数")]
    [Tooltip("0なら目標テキストの文字数。1以上ならランダム表示中の文字数をこの数に固定する")]
    [SerializeField] int fixedLength = 0;

    [Header("タイミング")]
    [SerializeField] float flickerInterval = 0.05f;
    [SerializeField] float startDelayPerChar = 0.06f;
    [SerializeField] float scrambleDuration = 0.45f;
    [SerializeField] bool appearFromRight = false;

    TMP_Text tmp;
    Coroutine routine;

    void Awake() => tmp = GetComponent<TMP_Text>();

    public void Play(string target, float duration = 0.5f)
    {
        if (tmp == null) tmp = GetComponent<TMP_Text>();
        if (routine != null) { StopCoroutine(routine); routine = null; }

        if (!isActiveAndEnabled) { tmp.text = target; return; }
        routine = StartCoroutine(Run(target));
    }

    public void SetImmediate(string text)
    {
        if (routine != null) { StopCoroutine(routine); routine = null; }
        if (tmp == null) tmp = GetComponent<TMP_Text>();
        tmp.text = text;
    }

    IEnumerator Run(string target)
    {
        string pool = PickPool(target);

        // 表示スロット数：固定指定があればそれ、なければ目標の文字数
        int slots = fixedLength > 0 ? fixedLength : target.Length;

        var current = new char[slots];
        var sb = new StringBuilder();
        float elapsed = 0f;
        float nextFlicker = 0f;
        float total = (slots - 1) * startDelayPerChar + scrambleDuration;

        while (elapsed < total)
        {
            bool flicker = elapsed >= nextFlicker;
            if (flicker) nextFlicker = elapsed + flickerInterval;

            sb.Clear();
            for (int i = 0; i < slots; i++)
            {
                // 目標より長いスロットは、確定時に「何も表示しない」
                bool hasTarget = i < target.Length;
                char c = hasTarget ? target[i] : '\0';

                if (hasTarget && char.IsWhiteSpace(c)) { sb.Append(c); continue; }

                int order = appearFromRight ? (slots - 1 - i) : i;
                float start = order * startDelayPerChar;
                float end = start + scrambleDuration;

                if (elapsed < start)
                {
                    // 出現前：レイアウト維持のため透明で確保
                    char placeholder = hasTarget ? c : pool[0];
                    sb.Append("<alpha=#00>").Append(placeholder).Append("<alpha=#FF>");
                }
                else if (elapsed < end)
                {
                    if (flicker || current[i] == '\0')
                        current[i] = pool[Random.Range(0, pool.Length)];
                    sb.Append(current[i]);
                }
                else if (hasTarget)
                {
                    sb.Append(c); // 確定
                }
                // hasTarget == false かつ終了後：何も追加しない
            }
            tmp.text = sb.ToString();

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        tmp.text = target;
        routine = null;
    }

    // 目標テキストが半角(ASCII)中心なら英語用、それ以外は日本語用
    private string PickPool(string target)
    {
        foreach (char c in target)
        {
            if (c > 0x7F) return jaPool;   // 全角文字が1つでもあれば日本語用
        }
        return enPool;
    }
}