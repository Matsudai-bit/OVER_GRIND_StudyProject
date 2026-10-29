using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 複数のヒント内容をまとめて管理するScriptableObjectです。
/// </summary>
[CreateAssetMenu(
    fileName = "HintData",
    menuName = "Game/UI/Hint Data")]
public sealed class HintDataAsset : ScriptableObject
{
    [SerializeField, Header("Hint一覧")]
    private List<HintTextData> m_hints = new();

    /// <summary>
    /// 指定した番号のHint内容を取得します。
    /// </summary>
    /// <param name="index">Hint一覧のインデックス。</param>
    /// <param name="heading">取得した見出し。</param>
    /// <param name="body">取得した本文。</param>
    /// <returns>true：取得成功。false：番号が範囲外。</returns>
    public bool TryGetHint(
        int index,
        out string heading,
        out string body)
    {
        heading = string.Empty;
        body = string.Empty;

        if (index < 0 || index >= m_hints.Count)
        {
            return false;
        }

        HintTextData hint = m_hints[index];
        if (hint == null)
        {
            return false;
        }

        heading = hint.Heading;
        body = hint.Body;
        return true;
    }
}
