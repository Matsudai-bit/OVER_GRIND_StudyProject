#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

/// <summary>
/// Sound ツールの各画面で共通して使う描画処理をまとめます。
/// </summary>
public static class SoundToolGui
{
    // 区切り線の高さ
    private const float SEPARATOR_HEIGHT = 1.0f;

    // 区切り線の上下余白
    private const float SEPARATOR_SPACE = 6.0f;

    // カテゴリータグの高さ
    private const float TAG_HEIGHT = 16.0f;

    // 3D 距離の下限値
    private const float MIN_DISTANCE_LIMIT = 0.01f;

    // 最大距離を最小距離より大きく保つための余白
    private const float DISTANCE_MARGIN = 0.01f;

    // 無音（0 以下）を表す dB の文字列
    private const string SILENT_DECIBEL_TEXT = "-inf dB";

    // 区切り線の色
    private static readonly Color SEPARATOR_COLOR = new Color(0.5f, 0.5f, 0.5f, 0.5f);

    // カテゴリータグの文字スタイル（初回使用時に生成）
    private static GUIStyle m_tagStyle;

    /// <summary>
    /// 区切り線を描画します。
    /// </summary>
    public static void DrawSeparator()
    {
        EditorGUILayout.Space(SEPARATOR_SPACE);

        Rect lineRect = EditorGUILayout.GetControlRect(false, SEPARATOR_HEIGHT);
        EditorGUI.DrawRect(lineRect, SEPARATOR_COLOR);

        EditorGUILayout.Space(SEPARATOR_SPACE);
    }

    /// <summary>
    /// SoundDatabase が見つからない場合の案内と作成ボタンを描画します。
    /// </summary>
    public static void DrawMissingDatabase()
    {
        EditorGUILayout.HelpBox(
            "SoundDatabase が見つかりません。下のボタンで Assets/Resources に作成できます。",
            MessageType.Warning);

        if (GUILayout.Button("SoundDatabase を作成する", GUILayout.Height(30.0f)))
        {
            SoundToolDatabaseProvider.CreateDatabase();
        }
    }

    /// <summary>
    /// 文字色を指定してミニラベルを描画します。
    /// </summary>
    /// <param name="text">表示する文字列。</param>
    /// <param name="color">文字色。</param>
    public static void DrawColoredMiniLabel(string text, Color color)
    {
        Color previousColor = GUI.contentColor;
        GUI.contentColor = color;

        EditorGUILayout.LabelField(text, EditorStyles.miniLabel);

        GUI.contentColor = previousColor;
    }

    /// <summary>
    /// カテゴリーを色付きタグで描画します。
    /// </summary>
    /// <param name="category">描画するカテゴリー。</param>
    /// <param name="width">タグの幅。</param>
    public static void DrawCategoryTag(SoundCategory category, float width)
    {
        Rect tagRect = GUILayoutUtility.GetRect(
            width,
            TAG_HEIGHT,
            GUILayout.Width(width),
            GUILayout.Height(TAG_HEIGHT));

        EditorGUI.DrawRect(tagRect, GetCategoryColor(category));
        GUI.Label(tagRect, category.ToString(), GetTagStyle());
    }

    /// <summary>
    /// カテゴリーに対応する色を取得します。
    /// </summary>
    /// <param name="category">カテゴリー。</param>
    /// <returns>カテゴリーの色。</returns>
    public static Color GetCategoryColor(SoundCategory category)
    {
        switch (category)
        {
            case SoundCategory.BGM:
                return new Color(0.25f, 0.45f, 0.80f);
            case SoundCategory.SE_InGame:
                return new Color(0.80f, 0.50f, 0.20f);
            case SoundCategory.SE_System:
                return new Color(0.30f, 0.60f, 0.40f);
            case SoundCategory.Voice:
                return new Color(0.65f, 0.40f, 0.75f);
            case SoundCategory.SE_InGame_Player:
                return new Color(0.75f, 0.35f, 0.35f);
            default:
                return Color.gray;
        }
    }

    /// <summary>
    /// 音量（0～1）を dB 表記の文字列へ変換します。
    /// </summary>
    /// <param name="volume">音量。</param>
    /// <returns>dB 表記の文字列。</returns>
    public static string GetDecibelText(float volume)
    {
        if (volume <= 0.0f)
        {
            return SILENT_DECIBEL_TEXT;
        }

        return $"{20.0f * Mathf.Log10(volume):F1} dB";
    }

    /// <summary>
    /// 3D 距離減衰の設定欄を描画します。
    /// </summary>
    /// <param name="data">編集する設定。</param>
    public static void DrawRolloffFields(SoundDatabase.SoundData data)
    {
        data.m_rolloffMode = (AudioRolloffMode)EditorGUILayout.EnumPopup("距離減衰", data.m_rolloffMode);

        data.m_minDistance = Mathf.Max(
            MIN_DISTANCE_LIMIT,
            EditorGUILayout.FloatField("最小距離", data.m_minDistance));

        // 対数減衰では最大距離を使わないため、それ以外のときだけ表示する
        if (data.m_rolloffMode != AudioRolloffMode.Logarithmic)
        {
            data.m_maxDistance = Mathf.Max(
                data.m_minDistance + DISTANCE_MARGIN,
                EditorGUILayout.FloatField("最大距離", data.m_maxDistance));
        }

        if (data.m_rolloffMode == AudioRolloffMode.Custom)
        {
            data.m_customRolloffCurve = EditorGUILayout.CurveField("カスタムカーブ", data.m_customRolloffCurve);
        }
    }

    /// <summary>
    /// カテゴリータグの文字スタイルを取得します。
    /// </summary>
    /// <returns>文字スタイル。</returns>
    private static GUIStyle GetTagStyle()
    {
        if (m_tagStyle == null)
        {
            m_tagStyle = new GUIStyle(EditorStyles.miniLabel);
            m_tagStyle.alignment = TextAnchor.MiddleCenter;
            m_tagStyle.normal.textColor = Color.white;
        }

        return m_tagStyle;
    }
}

#endif