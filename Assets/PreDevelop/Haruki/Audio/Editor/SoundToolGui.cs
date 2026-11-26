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

    // パートバッジの高さ
    private const float BADGE_HEIGHT = 16.0f;

    // 3D 距離の下限値
    private const float MIN_DISTANCE_LIMIT = 0.01f;

    // 最大距離を最小距離より大きく保つための余白
    private const float DISTANCE_MARGIN = 0.01f;

    // dB 変換の係数
    private const float DECIBEL_MULTIPLIER = 20.0f;

    // 高音量ゲージの色変化しきい値
    private const float HIGH_VOLUME_THRESHOLD = 0.85f;

    // 無音（0 以下）を表す dB の文字列
    private const string SILENT_DECIBEL_TEXT = "-inf dB";

    // 区切り線の色
    private static readonly Color SEPARATOR_COLOR = new Color(0.5f, 0.5f, 0.5f, 0.5f);

    // 音量ゲージの背景色
    private static readonly Color VOLUME_METER_BG_COLOR = new Color(0.0f, 0.0f, 0.0f, 0.2f);

    // 音量ゲージ（標準）の色
    private static readonly Color VOLUME_METER_NORMAL_COLOR = new Color(0.30f, 0.72f, 0.45f, 0.75f);

    // 音量ゲージ（高音量警告）の色
    private static readonly Color VOLUME_METER_HIGH_COLOR = new Color(0.95f, 0.65f, 0.20f, 0.85f);

    // バッジ色：Intro
    private static readonly Color BADGE_INTRO_COLOR = new Color(0.25f, 0.55f, 0.85f);

    // バッジ色：Loop
    private static readonly Color BADGE_LOOP_COLOR = new Color(0.25f, 0.65f, 0.45f);

    // バッジ色：Outro
    private static readonly Color BADGE_OUTRO_COLOR = new Color(0.75f, 0.45f, 0.25f);

    // バッジ色：未登録または非アクティブ
    private static readonly Color BADGE_INACTIVE_COLOR = new Color(0.35f, 0.35f, 0.35f, 0.45f);

    // バッジ色：再生中ハイライト
    private static readonly Color BADGE_PLAYING_COLOR = new Color(0.95f, 0.75f, 0.20f);

    // カテゴリータグの文字スタイル（初回使用時に生成）
    private static GUIStyle m_tagStyle;

    // パートバッジの文字スタイル（初回使用時に生成）
    private static GUIStyle m_badgeStyle;

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
    /// パート種別（I / L / O）を示すバッジを描画します。
    /// </summary>
    /// <param name="label">表示文字。</param>
    /// <param name="partType">パート種別。</param>
    /// <param name="isAvailable">データが存在するか。</param>
    /// <param name="isPlaying">再生中か。</param>
    /// <param name="width">バッジ幅。</param>
    public static void DrawPartBadge(string label, SoundPartType partType, bool isAvailable, bool isPlaying, float width)
    {
        Rect badgeRect = GUILayoutUtility.GetRect(
            width,
            BADGE_HEIGHT,
            GUILayout.Width(width),
            GUILayout.Height(BADGE_HEIGHT));

        Color backgroundColor;

        if (!isAvailable)
        {
            backgroundColor = BADGE_INACTIVE_COLOR;
        }
        else if (isPlaying)
        {
            backgroundColor = BADGE_PLAYING_COLOR;
        }
        else
        {
            backgroundColor = GetPartColor(partType);
        }

        EditorGUI.DrawRect(badgeRect, backgroundColor);

        GUIStyle style = GetBadgeStyle();
        Color previousTextColor = style.normal.textColor;
        style.normal.textColor = isPlaying ? Color.black : (isAvailable ? Color.white : new Color(0.7f, 0.7f, 0.7f));
        GUI.Label(badgeRect, label, style);
        style.normal.textColor = previousTextColor;
    }

    /// <summary>
    /// 音量の視覚的ゲージを描画します。
    /// </summary>
    /// <param name="volume">音量（0～1）。</param>
    /// <param name="width">ゲージ幅。</param>
    /// <param name="height">ゲージ高さ。</param>
    public static void DrawVolumeMeter(float volume, float width, float height)
    {
        Rect meterRect = GUILayoutUtility.GetRect(
            width,
            height,
            GUILayout.Width(width),
            GUILayout.Height(height));

        EditorGUI.DrawRect(meterRect, VOLUME_METER_BG_COLOR);

        float clampedVolume = Mathf.Clamp01(volume);

        if (clampedVolume > 0.0f)
        {
            Rect fillRect = new Rect(meterRect.x, meterRect.y, meterRect.width * clampedVolume, meterRect.height);
            Color fillColor = clampedVolume >= HIGH_VOLUME_THRESHOLD ? VOLUME_METER_HIGH_COLOR : VOLUME_METER_NORMAL_COLOR;
            EditorGUI.DrawRect(fillRect, fillColor);
        }
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

        return $"{DECIBEL_MULTIPLIER * Mathf.Log10(volume):F1} dB";
    }

    /// <summary>
    /// dB の変化量を線形倍率へ変換します。
    /// </summary>
    /// <param name="decibelDelta">dB の変化量。</param>
    /// <returns>乗算する倍率。</returns>
    public static float ConvertDecibelDeltaToMultiplier(float decibelDelta)
    {
        return Mathf.Pow(10.0f, decibelDelta / DECIBEL_MULTIPLIER);
    }

    /// <summary>
    /// 秒数を mm:ss.f 形式の文字列へ変換します。
    /// </summary>
    /// <param name="seconds">秒数。</param>
    /// <returns>フォーマットされた時間文字列。</returns>
    public static string FormatTime(float seconds)
    {
        float safeSeconds = Mathf.Max(0.0f, seconds);
        int minutes = Mathf.FloorToInt(safeSeconds / 60.0f);
        float remainder = safeSeconds - (minutes * 60.0f);
        return $"{minutes:00}:{remainder:00.0}";
    }

    /// <summary>
    /// 3D 距離減衰の設定欄（モード・最小距離・最大距離・カスタムカーブ）を描画します。
    /// </summary>
    /// <param name="data">編集する設定。</param>
    public static void DrawRolloffFields(SoundDatabase.SoundData data)
    {
        if (data == null)
        {
            return;
        }

        data.m_rolloffMode = (AudioRolloffMode)EditorGUILayout.EnumPopup("距離減衰モード", data.m_rolloffMode);

        EditorGUILayout.BeginHorizontal();
        data.m_minDistance = Mathf.Max(
            MIN_DISTANCE_LIMIT,
            EditorGUILayout.FloatField("最小距離 (Min)", data.m_minDistance));

        data.m_maxDistance = Mathf.Max(
            data.m_minDistance + DISTANCE_MARGIN,
            EditorGUILayout.FloatField("最大距離 (Max)", data.m_maxDistance));
        EditorGUILayout.EndHorizontal();

        if (data.m_rolloffMode == AudioRolloffMode.Custom)
        {
            if (data.m_customRolloffCurve == null)
            {
                data.m_customRolloffCurve = AnimationCurve.Linear(0.0f, 1.0f, 1.0f, 0.0f);
            }

            data.m_customRolloffCurve = EditorGUILayout.CurveField("カスタムカーブ", data.m_customRolloffCurve);
        }
    }

    /// <summary>
    /// パート種別に応じた色を取得します。
    /// </summary>
    /// <param name="partType">パート種別。</param>
    /// <returns>色。</returns>
    private static Color GetPartColor(SoundPartType partType)
    {
        switch (partType)
        {
            case SoundPartType.PART_INTRO:
                return BADGE_INTRO_COLOR;
            case SoundPartType.PART_LOOP:
                return BADGE_LOOP_COLOR;
            case SoundPartType.PART_OUTRO:
                return BADGE_OUTRO_COLOR;
            default:
                return Color.gray;
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

    /// <summary>
    /// パートバッジの文字スタイルを取得します。
    /// </summary>
    /// <returns>文字スタイル。</returns>
    private static GUIStyle GetBadgeStyle()
    {
        if (m_badgeStyle == null)
        {
            m_badgeStyle = new GUIStyle(EditorStyles.miniBoldLabel);
            m_badgeStyle.alignment = TextAnchor.MiddleCenter;
        }

        return m_badgeStyle;
    }
}

#endif