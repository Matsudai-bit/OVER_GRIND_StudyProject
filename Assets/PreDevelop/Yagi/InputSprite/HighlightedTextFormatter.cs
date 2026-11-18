using System.Text;
using UnityEngine;

/// <summary>
/// [[文字]] で囲んだ部分を TextMesh Pro の色タグに変換します。
/// </summary>
public static class HighlightedTextFormatter
{
    private const string OPEN_MARKER = "[[";
    private const string CLOSE_MARKER = "]]";

    /// <summary>
    /// 囲まれた部分へ指定色を適用したリッチテキストを作成します。
    /// </summary>
    /// <param name="source">変換前の文章。</param>
    /// <param name="highlightColor">強調部分の色。</param>
    /// <returns>TextMesh Pro に渡す文章。</returns>
    public static string Format(string source, Color highlightColor)
    {
        if (string.IsNullOrEmpty(source))
        {
            return source ?? string.Empty;
        }

        string colorCode = ColorUtility.ToHtmlStringRGBA(highlightColor);
        StringBuilder result = new StringBuilder(source.Length);
        int position = 0;

        while (position < source.Length)
        {
            int start = source.IndexOf(OPEN_MARKER, position, System.StringComparison.Ordinal);
            if (start < 0)
            {
                result.Append(source, position, source.Length - position);
                break;
            }

            int end = source.IndexOf(CLOSE_MARKER, start + OPEN_MARKER.Length,
                System.StringComparison.Ordinal);
            if (end < 0)
            {
                result.Append(source, position, source.Length - position);
                break;
            }

            result.Append(source, position, start - position);
            result.Append("<color=#").Append(colorCode).Append('>');
            result.Append(source, start + OPEN_MARKER.Length,
                end - start - OPEN_MARKER.Length);
            result.Append("</color>");
            position = end + CLOSE_MARKER.Length;
        }

        return result.ToString();
    }
}
