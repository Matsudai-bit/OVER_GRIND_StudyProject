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

    /// <summary>
    /// Hintの見出しを取得します。
    /// </summary>
    public string Heading => m_heading;

    /// <summary>
    /// Hintの本文を取得します。
    /// </summary>
    public string Body => m_body;
}
