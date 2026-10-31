using UnityEngine;

/// <summary>
/// 1件分のManualで使用するコマンド名と本文を保持します。
/// </summary>
[CreateAssetMenu(
    fileName = "ManualTextData",
    menuName = "Game/UI/Manual Text Data")]
public sealed class ManualTextData : ScriptableObject
{
    [SerializeField, Header("コマンドDataアセット名")]
    private string m_commandAssetName;

    [SerializeField, Header("Manual本文")]
    [TextArea(2, 8)]
    private string m_body;

    [SerializeField, Header("2行目のコマンドDataアセット名")]
    private string m_secondCommandAssetName;

    [SerializeField, Header("2行目のManual本文")]
    [TextArea(2, 8)]
    private string m_secondBody;

    /// <summary>
    /// 表示するコマンドDataアセット名を取得します。
    /// </summary>
    public string CommandAssetName => m_commandAssetName;

    /// <summary>
    /// 表示するManual本文を取得します。
    /// </summary>
    public string Body => m_body;

    /// <summary>
    /// 2行目に表示するコマンドDataアセット名を取得します。
    /// </summary>
    public string SecondCommandAssetName => m_secondCommandAssetName;

    /// <summary>
    /// 2行目に表示するManual本文を取得します。
    /// </summary>
    public string SecondBody => m_secondBody;
}
