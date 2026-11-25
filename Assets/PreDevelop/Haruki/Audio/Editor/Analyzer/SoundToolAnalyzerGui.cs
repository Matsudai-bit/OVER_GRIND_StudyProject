#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

/// <summary>
/// リアルタイム・スペクトラムアナライザー（EQ風）、オシロスコープ波形、ステレオ L/R メーターを描画します。
/// </summary>
public static class SoundToolAnalyzerGui
{
    // パネルの背景色
    private static readonly Color PANEL_BG_COLOR = new Color(0.09f, 0.10f, 0.12f, 1.0f);

    // 枠線の色
    private static readonly Color PANEL_BORDER_COLOR = new Color(0.24f, 0.26f, 0.30f, 1.0f);

    // グリッド線の色
    private static readonly Color GRID_LINE_COLOR = new Color(1.0f, 1.0f, 1.0f, 0.07f);

    // 波形ラインの色
    private static readonly Color WAVEFORM_COLOR = new Color(0.35f, 0.85f, 1.00f, 0.90f);

    // レベル緑（通常域）
    private static readonly Color METER_GREEN = new Color(0.22f, 0.82f, 0.45f, 1.0f);

    // レベル黄（高音量域）
    private static readonly Color METER_YELLOW = new Color(0.95f, 0.78f, 0.18f, 1.0f);

    // レベル赤（クリッピング警告域）
    private static readonly Color METER_RED = new Color(0.95f, 0.28f, 0.25f, 1.0f);

    // ピークホールドキャップの色
    private static readonly Color PEAK_CAP_COLOR = new Color(1.0f, 0.95f, 0.75f, 0.95f);

    // Ref 基準比較ラインの色
    private static readonly Color REF_LINE_COLOR = new Color(1.0f, 0.75f, 0.15f, 0.95f);

    // 黄色へ切り替わる dB しきい値
    private const float YELLOW_DB_THRESHOLD = -12.0f;

    // 赤色へ切り替わる dB しきい値
    private const float RED_DB_THRESHOLD = -3.0f;

    // 縦型メーター内のセグメント数
    private const int VERTICAL_METER_SEGMENTS = 20;

    // 周波数目盛り表示ラベル
    private static readonly string[] FREQ_LABELS = { "60Hz", "250Hz", "1kHz", "4kHz", "16kHz" };

    // dB 目盛り値
    private static readonly float[] DB_GRID_VALUES = { -6.0f, -12.0f, -24.0f, -48.0f };

    // 極小ラベルスタイル
    private static GUIStyle m_tinyLabelStyle;

    /// <summary>
    /// アナライザーパネル全体（24バンド EQ・波形・ステレオ L/R メーター）を描画します。
    /// </summary>
    /// <param name="height">パネルの高さ。</param>
    public static void DrawAnalyzerPanel(float height)
    {
        Rect panelRect = GUILayoutUtility.GetRect(0.0f, height, GUILayout.ExpandWidth(true), GUILayout.Height(height));

        if (Event.current.type != EventType.Repaint)
        {
            return;
        }

        // 背景と外枠
        EditorGUI.DrawRect(panelRect, PANEL_BG_COLOR);
        DrawOutline(panelRect, PANEL_BORDER_COLOR);

        // レイアウト分割（左：スペクトラム、中央：波形、右：ステレオ L/R メーター＋数値）
        float padding = 8.0f;
        float meterAreaWidth = 220.0f;
        float waveAreaWidth = panelRect.width >= 760.0f ? 170.0f : 0.0f;
        float spectrumWidth = Mathf.Max(140.0f, panelRect.width - meterAreaWidth - waveAreaWidth - (padding * 4.0f));

        Rect spectrumRect = new Rect(
            panelRect.x + padding,
            panelRect.y + padding,
            spectrumWidth,
            panelRect.height - (padding * 2.0f));

        DrawSpectrumAnalyzer(spectrumRect);

        if (waveAreaWidth > 0.0f)
        {
            Rect waveRect = new Rect(
                spectrumRect.xMax + padding,
                panelRect.y + padding,
                waveAreaWidth,
                panelRect.height - (padding * 2.0f));

            DrawWaveformScope(waveRect);
        }

        Rect meterRect = new Rect(
            panelRect.xMax - meterAreaWidth - padding,
            panelRect.y + padding,
            meterAreaWidth,
            panelRect.height - (padding * 2.0f));

        DrawStereoLevelMeter(meterRect);
    }

    /// <summary>
    /// ミキサー卓チャンネル用のスリムな縦型レベルメーターを描画します。
    /// </summary>
    /// <param name="rect">描画領域。</param>
    /// <param name="normalizedLevel">現在レベル（0～1）。</param>
    /// <param name="peakHold">ピークホールド（0～1）。</param>
    public static void DrawVerticalChannelMeter(Rect rect, float normalizedLevel, float peakHold)
    {
        if (Event.current.type != EventType.Repaint)
        {
            return;
        }

        EditorGUI.DrawRect(rect, PANEL_BG_COLOR);
        DrawOutline(rect, PANEL_BORDER_COLOR);

        float innerPadding = 2.0f;
        Rect inner = new Rect(
            rect.x + innerPadding,
            rect.y + innerPadding,
            rect.width - (innerPadding * 2.0f),
            rect.height - (innerPadding * 2.0f));

        float segGap = 1.0f;
        float segHeight = (inner.height - (segGap * (VERTICAL_METER_SEGMENTS - 1))) / VERTICAL_METER_SEGMENTS;
        float yellowNorm = SoundToolAudioAnalyzer.DecibelToNormalized(YELLOW_DB_THRESHOLD);
        float redNorm = SoundToolAudioAnalyzer.DecibelToNormalized(RED_DB_THRESHOLD);

        for (int i = 0; i < VERTICAL_METER_SEGMENTS; i++)
        {
            float ratio = (float)(i + 1) / VERTICAL_METER_SEGMENTS;
            float y = inner.yMax - ((i + 1) * segHeight) - (i * segGap);
            Rect segRect = new Rect(inner.x, y, inner.width, segHeight);

            Color litColor = ratio >= redNorm ? METER_RED : (ratio >= yellowNorm ? METER_YELLOW : METER_GREEN);
            bool isLit = normalizedLevel >= ((float)i / VERTICAL_METER_SEGMENTS) && normalizedLevel > 0.005f;
            EditorGUI.DrawRect(segRect, isLit ? litColor : new Color(litColor.r, litColor.g, litColor.b, 0.14f));
        }

        // ピークホールド線
        if (peakHold > 0.01f)
        {
            float peakY = Mathf.Lerp(inner.yMax, inner.y, Mathf.Clamp01(peakHold));
            EditorGUI.DrawRect(new Rect(inner.x, peakY - 1.0f, inner.width, 2.0f), PEAK_CAP_COLOR);
        }

        // Ref 基準ライン
        float refNorm = SoundToolAudioAnalyzer.ReferencePeakNormalized;
        if (refNorm > 0.0f)
        {
            float refY = Mathf.Lerp(inner.yMax, inner.y, Mathf.Clamp01(refNorm));
            EditorGUI.DrawRect(new Rect(inner.x - 1.0f, refY, inner.width + 2.0f, 1.5f), REF_LINE_COLOR);
        }
    }

    /// <summary>
    /// 24バンド・イコライザー風スペクトラムアナライザーを描画します。
    /// </summary>
    private static void DrawSpectrumAnalyzer(Rect rect)
    {
        float headerHeight = 14.0f;
        float footerHeight = 14.0f;
        Rect graphRect = new Rect(rect.x, rect.y + headerHeight, rect.width, rect.height - headerHeight - footerHeight);

        GUI.Label(new Rect(rect.x, rect.y, rect.width, headerHeight), "SPECTRUM ANALYZER (EQ 24-BAND)", GetTinyLabelStyle());

        // dB 横グリッド線
        for (int i = 0; i < DB_GRID_VALUES.Length; i++)
        {
            float norm = SoundToolAudioAnalyzer.DecibelToNormalized(DB_GRID_VALUES[i]);
            float y = Mathf.Lerp(graphRect.yMax, graphRect.y, norm);
            EditorGUI.DrawRect(new Rect(graphRect.x, y, graphRect.width, 1.0f), GRID_LINE_COLOR);
        }

        float[] bands = SoundToolAudioAnalyzer.GetBandLevels();
        float[] peaks = SoundToolAudioAnalyzer.GetBandPeaks();
        int count = SoundToolAudioAnalyzer.SPECTRUM_BAND_COUNT;

        float gap = 2.0f;
        float barWidth = Mathf.Max(2.0f, (graphRect.width - (gap * (count - 1))) / count);
        float yellowNorm = SoundToolAudioAnalyzer.DecibelToNormalized(YELLOW_DB_THRESHOLD);
        float redNorm = SoundToolAudioAnalyzer.DecibelToNormalized(RED_DB_THRESHOLD);

        for (int i = 0; i < count; i++)
        {
            float x = graphRect.x + (i * (barWidth + gap));
            float val = Mathf.Clamp01(bands[i]);
            float peak = Mathf.Clamp01(peaks[i]);

            // バー背景
            EditorGUI.DrawRect(new Rect(x, graphRect.y, barWidth, graphRect.height), new Color(1.0f, 1.0f, 1.0f, 0.03f));

            // 値に応じた3段階カラーバー（緑 → 黄 → 赤）
            if (val > 0.005f)
            {
                float barHeight = graphRect.height * val;
                float barTop = graphRect.yMax - barHeight;

                Color barColor = val >= redNorm ? METER_RED : (val >= yellowNorm ? METER_YELLOW : METER_GREEN);
                EditorGUI.DrawRect(new Rect(x, barTop, barWidth, barHeight), barColor);
            }

            // ピークホールドキャップ
            if (peak > 0.01f)
            {
                float peakY = Mathf.Lerp(graphRect.yMax - 2.0f, graphRect.y, peak);
                EditorGUI.DrawRect(new Rect(x, peakY, barWidth, 2.0f), PEAK_CAP_COLOR);
            }
        }

        // 周波数軸ラベル
        for (int i = 0; i < FREQ_LABELS.Length; i++)
        {
            float t = (float)i / (FREQ_LABELS.Length - 1);
            float lx = Mathf.Lerp(graphRect.x, graphRect.xMax - 32.0f, t);
            GUI.Label(new Rect(lx, graphRect.yMax + 1.0f, 38.0f, footerHeight), FREQ_LABELS[i], GetTinyLabelStyle());
        }
    }

    /// <summary>
    /// リアルタイム波形モニター（オシロスコープ）を描画します。
    /// </summary>
    private static void DrawWaveformScope(Rect rect)
    {
        float headerHeight = 14.0f;
        Rect scopeRect = new Rect(rect.x, rect.y + headerHeight, rect.width, rect.height - headerHeight);

        GUI.Label(new Rect(rect.x, rect.y, rect.width, headerHeight), "WAVEFORM", GetTinyLabelStyle());
        EditorGUI.DrawRect(scopeRect, new Color(0.0f, 0.0f, 0.0f, 0.25f));
        DrawOutline(scopeRect, GRID_LINE_COLOR);

        float centerY = scopeRect.y + (scopeRect.height * 0.5f);
        EditorGUI.DrawRect(new Rect(scopeRect.x, centerY, scopeRect.width, 1.0f), GRID_LINE_COLOR);

        float[] points = SoundToolAudioAnalyzer.GetWaveformPoints();
        int count = points.Length;
        float halfH = (scopeRect.height * 0.5f) - 2.0f;
        float barW = Mathf.Max(1.0f, scopeRect.width / count);

        for (int i = 0; i < count; i++)
        {
            float sample = points[i];
            float x = scopeRect.x + ((float)i / count * scopeRect.width);
            float h = Mathf.Abs(sample) * halfH;
            float y = sample >= 0.0f ? (centerY - h) : centerY;
            EditorGUI.DrawRect(new Rect(x, y, barW, Mathf.Max(1.0f, h)), WAVEFORM_COLOR);
        }
    }

    /// <summary>
    /// ステレオ L/R レベルメーターと数値・Ref 比較ラインを描画します。
    /// </summary>
    private static void DrawStereoLevelMeter(Rect rect)
    {
        float headerHeight = 14.0f;
        GUI.Label(new Rect(rect.x, rect.y, rect.width, headerHeight), "OUTPUT LEVEL (L / R)", GetTinyLabelStyle());

        float barY = rect.y + headerHeight + 4.0f;
        float barHeight = 12.0f;
        float labelW = 14.0f;
        float barW = rect.width - labelW - 4.0f;

        // L チャンネル
        GUI.Label(new Rect(rect.x, barY - 1.0f, labelW, barHeight + 2.0f), "L", GetTinyLabelStyle());
        DrawHorizontalMeterBar(
            new Rect(rect.x + labelW, barY, barW, barHeight),
            SoundToolAudioAnalyzer.LeftRmsNormalized,
            SoundToolAudioAnalyzer.LeftPeakNormalized,
            SoundToolAudioAnalyzer.LeftPeakHold);

        // R チャンネル
        float barRY = barY + barHeight + 4.0f;
        GUI.Label(new Rect(rect.x, barRY - 1.0f, labelW, barHeight + 2.0f), "R", GetTinyLabelStyle());
        DrawHorizontalMeterBar(
            new Rect(rect.x + labelW, barRY, barW, barHeight),
            SoundToolAudioAnalyzer.RightRmsNormalized,
            SoundToolAudioAnalyzer.RightPeakNormalized,
            SoundToolAudioAnalyzer.RightPeakHold);

        // dB スケール表示
        float scaleY = barRY + barHeight + 2.0f;
        GUI.Label(new Rect(rect.x + labelW, scaleY, 34.0f, 12.0f), "-60", GetTinyLabelStyle());
        GUI.Label(new Rect(rect.x + labelW + (barW * 0.55f), scaleY, 34.0f, 12.0f), "-24", GetTinyLabelStyle());
        GUI.Label(new Rect(rect.x + labelW + (barW * 0.78f), scaleY, 34.0f, 12.0f), "-12", GetTinyLabelStyle());
        GUI.Label(new Rect(rect.xMax - 20.0f, scaleY, 24.0f, 12.0f), "0dB", GetTinyLabelStyle());

        // 数値ステータス
        float statsY = scaleY + 14.0f;
        string peakText = SoundToolAudioAnalyzer.CurrentPeakDb <= SoundToolAudioAnalyzer.MIN_DISPLAY_DB + 0.1f
            ? "-inf dB"
            : $"{SoundToolAudioAnalyzer.CurrentPeakDb:F1} dB";
        string rmsText = SoundToolAudioAnalyzer.CurrentRmsDb <= SoundToolAudioAnalyzer.MIN_DISPLAY_DB + 0.1f
            ? "-inf dB"
            : $"{SoundToolAudioAnalyzer.CurrentRmsDb:F1} dB";

        GUIStyle style = GetTinyLabelStyle();
        Color prevColor = style.normal.textColor;

        if (SoundToolAudioAnalyzer.IsClipping)
        {
            style.normal.textColor = METER_RED;
            GUI.Label(new Rect(rect.x, statsY, rect.width, 14.0f), $"CLIP!  Peak: {peakText}   RMS: {rmsText}", style);
        }
        else
        {
            GUI.Label(new Rect(rect.x, statsY, rect.width, 14.0f), $"Peak: {peakText}   RMS: {rmsText}", style);
        }

        // Ref 基準レベル表示
        if (SoundToolAudioAnalyzer.ReferencePeakNormalized > 0.0f)
        {
            style.normal.textColor = REF_LINE_COLOR;
            GUI.Label(
                new Rect(rect.x, statsY + 13.0f, rect.width, 14.0f),
                $"Ref 基準ライン: {SoundToolAudioAnalyzer.ReferencePeakDb:F1} dB",
                style);
        }

        style.normal.textColor = prevColor;
    }

    /// <summary>
    /// 横型ステレオメーターバー（RMS + Peak + PeakHold + Refライン）を描画します。
    /// </summary>
    private static void DrawHorizontalMeterBar(Rect rect, float rmsNorm, float peakNorm, float peakHold)
    {
        EditorGUI.DrawRect(rect, new Color(0.0f, 0.0f, 0.0f, 0.45f));

        float yellowNorm = SoundToolAudioAnalyzer.DecibelToNormalized(YELLOW_DB_THRESHOLD);
        float redNorm = SoundToolAudioAnalyzer.DecibelToNormalized(RED_DB_THRESHOLD);

        // ピークバー（半透明）
        if (peakNorm > 0.005f)
        {
            Color peakColor = peakNorm >= redNorm ? METER_RED : (peakNorm >= yellowNorm ? METER_YELLOW : METER_GREEN);
            EditorGUI.DrawRect(
                new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(peakNorm), rect.height),
                new Color(peakColor.r, peakColor.g, peakColor.b, 0.45f));
        }

        // RMS バー（メイン輝度）
        if (rmsNorm > 0.005f)
        {
            Color rmsColor = rmsNorm >= redNorm ? METER_RED : (rmsNorm >= yellowNorm ? METER_YELLOW : METER_GREEN);
            EditorGUI.DrawRect(
                new Rect(rect.x, rect.y + 1.0f, rect.width * Mathf.Clamp01(rmsNorm), rect.height - 2.0f),
                rmsColor);
        }

        // ピークホールドライン
        if (peakHold > 0.01f)
        {
            float px = rect.x + (rect.width * Mathf.Clamp01(peakHold));
            EditorGUI.DrawRect(new Rect(px - 1.0f, rect.y, 2.0f, rect.height), PEAK_CAP_COLOR);
        }

        // Ref 基準比較ライン
        float refNorm = SoundToolAudioAnalyzer.ReferencePeakNormalized;
        if (refNorm > 0.0f)
        {
            float rx = rect.x + (rect.width * Mathf.Clamp01(refNorm));
            EditorGUI.DrawRect(new Rect(rx - 1.0f, rect.y - 1.0f, 2.0f, rect.height + 2.0f), REF_LINE_COLOR);
        }
    }

    /// <summary>
    /// 矩形の外枠を描画します。
    /// </summary>
    private static void DrawOutline(Rect rect, Color color)
    {
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1.0f), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1.0f, rect.width, 1.0f), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1.0f, rect.height), color);
        EditorGUI.DrawRect(new Rect(rect.xMax - 1.0f, rect.y, 1.0f, rect.height), color);
    }

    /// <summary>
    /// アナライザー内の極小ラベルスタイルを取得します。
    /// </summary>
    private static GUIStyle GetTinyLabelStyle()
    {
        if (m_tinyLabelStyle == null)
        {
            m_tinyLabelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 9,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0)
            };
            m_tinyLabelStyle.normal.textColor = new Color(0.75f, 0.78f, 0.82f);
        }

        return m_tinyLabelStyle;
    }
}

#endif