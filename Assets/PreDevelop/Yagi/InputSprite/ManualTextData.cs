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

    [SerializeField, Header("1行目の追加コマンド（左側・任意）")]
    private string m_additionalCommandAssetName;

    [SerializeField, Header("2行目の追加コマンド（左側・任意）")]
    private string m_secondAdditionalCommandAssetName;

    /// <summary>1行目の左側に追加するコマンド名。</summary>
    public string AdditionalCommandAssetName => m_additionalCommandAssetName;

    /// <summary>2行目の左側に追加するコマンド名。</summary>
    public string SecondAdditionalCommandAssetName => m_secondAdditionalCommandAssetName;

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
