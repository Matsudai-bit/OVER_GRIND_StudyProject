using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 複数のManual内容をまとめて管理するScriptableObjectです。
/// </summary>
[CreateAssetMenu(
    fileName = "ManualData",
    menuName = "Game/UI/Manual Data")]
public sealed class ManualDataAsset : ScriptableObject
{
    [SerializeField, Header("Manual一覧")]
    private List<ManualTextData> m_manuals = new();

    /// <summary>
    /// 指定した番号のManual内容を取得します。
    /// </summary>
    /// <param name="index">Manual一覧のインデックス。</param>
    /// <param name="commandAssetName">表示するコマンドDataアセット名。</param>
    /// <param name="body">表示するManual本文。</param>
    /// <param name="secondCommandAssetName">2行目に表示するコマンドDataアセット名。</param>
    /// <param name="secondBody">2行目に表示するManual本文。</param>
    /// <returns>true：取得成功。false：番号が範囲外またはData未設定。</returns>
    public bool TryGetManual(
        int index,
        out string commandAssetName,
        out string body,
        out string secondCommandAssetName,
        out string secondBody)
    {
        commandAssetName = string.Empty;
        body = string.Empty;
        secondCommandAssetName = string.Empty;
        secondBody = string.Empty;

        if (index < 0 || index >= m_manuals.Count)
        {
            return false;
        }

        ManualTextData manual = m_manuals[index];
        if (manual == null)
        {
            return false;
        }

        commandAssetName = manual.CommandAssetName;
        body = manual.Body;
        secondCommandAssetName = manual.SecondCommandAssetName;
        secondBody = manual.SecondBody;
        return true;
    }
}
