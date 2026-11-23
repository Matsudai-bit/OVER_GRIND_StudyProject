using UnityEngine;

/// <summary>
/// 1件分のHint見出しと本文を保持するScriptableObjectです。
/// </summary>
[CreateAssetMenu(
    fileName = "HintTextData",
    menuName = "Game/UI/Hint Text Data")]
public sealed class HintTextData : ScriptableObject
{
    [SerializeField, Header("見出し")]
    [TextArea(1, 2)]
    private string m_heading;

    [SerializeField, Header("本文")]
    [TextArea(2, 6)]
    private string m_body;

    [SerializeField, Header("強調色（[[文字]]）")]
    private Color m_highlightColor = Color.yellow;

    /// <summary>
    /// Hintの見出しを取得します。
    /// </summary>
    public string Heading => m_heading;

    /// <summary>
    /// Hintの本文を取得します。
    /// </summary>
    public string Body => m_body;

    /// <summary>強調部分に適用する色。</summary>
    public Color HighlightColor => m_highlightColor;
}
