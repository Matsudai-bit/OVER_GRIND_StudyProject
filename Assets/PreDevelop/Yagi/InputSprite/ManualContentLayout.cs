using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Manualの文字とコマンド画像を背景の中央に配置します。</summary>
internal static class ManualContentLayout
{
    private const float CONTENT_GAP = 10f;
    private const float TEXT_WIDTH_PADDING = 4f;

    /// <summary>表示中の2行をまとめて左右中央に配置します。</summary>
    public static void Align(
        RectTransform background,
        TMP_Text leftText,
        Image firstCommand,
        TMP_Text firstBody,
        Image secondCommand,
        TMP_Text secondBody)
    {
        if (background == null)
        {
            return;
        }

        Row firstRow = CreateRow(leftText, firstCommand, firstBody);
        Row secondRow = CreateRow(null, secondCommand, secondBody);

        float centerX = background.anchoredPosition.x;
        Row referenceRow = firstRow.LeftExtent + firstRow.RightExtent
            >= secondRow.LeftExtent + secondRow.RightExtent
            ? firstRow
            : secondRow;
        float commandX = centerX
            + (referenceRow.LeftExtent - referenceRow.RightExtent) * 0.5f;

        PlaceRow(firstRow, commandX, centerX);
        PlaceRow(secondRow, commandX, centerX);
    }

    /// <summary>文字の表示幅と画像の幅から1行分の範囲を取得します。</summary>
    private static Row CreateRow(TMP_Text leftText, Image command, TMP_Text body)
    {
        float leftWidth = GetTextWidth(leftText);
        float bodyWidth = GetTextWidth(body);
        float commandWidth = GetCommandWidth(command);
        float halfCommand = commandWidth * 0.5f;

        return new Row(
            leftText,
            command,
            body,
            leftWidth,
            bodyWidth,
            commandWidth,
            commandWidth > 0f
                ? halfCommand + (leftWidth > 0f ? CONTENT_GAP + leftWidth : 0f)
                : 0f,
            commandWidth > 0f
                ? halfCommand + (bodyWidth > 0f ? CONTENT_GAP + bodyWidth : 0f)
                : 0f);
    }

    /// <summary>1行の文字と画像を指定した中心位置へ配置します。</summary>
    private static void PlaceRow(Row row, float commandX, float centerX)
    {
        if (row.CommandWidth <= 0f)
        {
            PlaceText(row.Body, centerX);
            return;
        }

        PlaceImage(row.Command, commandX);

        if (row.LeftWidth > 0f)
        {
            PlaceText(
                row.LeftText,
                commandX - row.CommandWidth * 0.5f
                    - CONTENT_GAP - row.LeftWidth * 0.5f);
        }

        if (row.BodyWidth > 0f)
        {
            PlaceText(
                row.Body,
                commandX + row.CommandWidth * 0.5f
                    + CONTENT_GAP + row.BodyWidth * 0.5f);
        }
    }

    /// <summary>折り返し前の文字幅に合わせてTextの幅を設定します。</summary>
    private static float GetTextWidth(TMP_Text text)
    {
        if (text == null || string.IsNullOrEmpty(text.text))
        {
            return 0f;
        }

        RectTransform rect = text.rectTransform;
        float preferredWidth = text.GetPreferredValues(
            text.text, Mathf.Infinity, Mathf.Infinity).x;
        rect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal, preferredWidth + TEXT_WIDTH_PADDING);
        return rect.rect.width * Mathf.Abs(rect.localScale.x);
    }

    /// <summary>表示中のコマンド画像の幅を取得します。</summary>
    private static float GetCommandWidth(Image image)
    {
        if (image == null || !image.enabled || image.sprite == null
            || !image.gameObject.activeSelf)
        {
            return 0f;
        }

        RectTransform rect = image.rectTransform;
        float width = rect.rect.width;
        if (image.preserveAspect && image.sprite.rect.height > 0f)
        {
            float spriteWidth = rect.rect.height
                * image.sprite.rect.width / image.sprite.rect.height;
            width = Mathf.Min(width, spriteWidth);
        }

        return width * Mathf.Abs(rect.localScale.x);
    }

    /// <summary>Imageの縦位置を保って横位置を更新します。</summary>
    private static void PlaceImage(Image image, float x)
    {
        RectTransform rect = image.rectTransform;
        Vector2 position = rect.anchoredPosition;
        position.x = x;
        rect.anchoredPosition = position;
    }

    /// <summary>Textの縦位置を保って横位置を更新します。</summary>
    private static void PlaceText(TMP_Text text, float x)
    {
        if (text == null)
        {
            return;
        }

        RectTransform rect = text.rectTransform;
        Vector2 position = rect.anchoredPosition;
        position.x = x;
        rect.anchoredPosition = position;
    }

    /// <summary>1行に表示する要素と背景中心からの左右の幅を保持します。</summary>
    private readonly struct Row
    {
        public TMP_Text LeftText { get; }
        public Image Command { get; }
        public TMP_Text Body { get; }
        public float LeftWidth { get; }
        public float BodyWidth { get; }
        public float CommandWidth { get; }
        public float LeftExtent { get; }
        public float RightExtent { get; }

        /// <summary>1行の表示要素と左右の幅を設定します。</summary>
        public Row(
            TMP_Text leftText,
            Image command,
            TMP_Text body,
            float leftWidth,
            float bodyWidth,
            float commandWidth,
            float leftExtent,
            float rightExtent)
        {
            LeftText = leftText;
            Command = command;
            Body = body;
            LeftWidth = leftWidth;
            BodyWidth = bodyWidth;
            CommandWidth = commandWidth;
            LeftExtent = leftExtent;
            RightExtent = rightExtent;
        }
    }
}
