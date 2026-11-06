using TMPro;
using UnityEngine;

/// <summary>
/// HintDataAssetから選択したHint内容をUIへ適用します。
/// </summary>
[DisallowMultipleComponent]
public sealed class HintDataReceiver : MonoBehaviour
{
    [SerializeField, Header("Hintデータ")]
    private HintDataAsset m_hintData;

    [SerializeField, Min(0), Header("Hint番号")]
    private int m_hintIndex;

    [SerializeField, Header("見出しText")]
    private TMP_Text m_headingText;

    [SerializeField, Header("本文Text")]
    private TMP_Text m_bodyText;

    /// <summary>
    /// 有効化時にHint内容をUIへ適用します。
    /// </summary>
    private void OnEnable()
    {
        ApplyHint();
    }

#if UNITY_EDITOR
    /// <summary>
    /// Inspector変更時にHint内容をプレビューへ反映します。
    /// </summary>
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            ApplyHint();
        }
    }
#endif

    /// <summary>
    /// 選択したHint内容を見出しと本文へ設定します。
    /// </summary>
    public void ApplyHint()
    {
        if (m_hintData == null)
        {
            Debug.LogWarning(
                $"{nameof(HintDataAsset)}が設定されていません。",
                this);
            return;
        }

        if (!m_hintData.TryGetHint(
                m_hintIndex,
                out string heading,
                out string body))
        {
            Debug.LogWarning(
                $"Hint番号が範囲外です。 Index: {m_hintIndex}",
                this);
            return;
        }

        if (m_headingText != null && m_headingText == m_bodyText)
        {
            m_headingText.text = $"{heading}\n{body}";
            return;
        }

        if (m_headingText != null)
        {
            m_headingText.text = heading;
        }

        if (m_bodyText != null)
        {
            m_bodyText.text = m_headingText == null
                ? $"{heading}\n{body}"
                : body;
        }
    }
}
