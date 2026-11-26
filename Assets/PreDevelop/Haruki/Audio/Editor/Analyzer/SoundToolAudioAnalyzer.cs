#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 単体および複数トラック同時再生時の音声をリアルタイムに解析し、
/// 合計出力の L/R レベル・クリッピング・波形・24バンド周波数スペクトラム（EQ風）を算出します。
/// </summary>
public static class SoundToolAudioAnalyzer
{
    /// <summary>
    /// マルチトラック解析へ渡す1トラック分の入力情報です。
    /// </summary>
    public struct TrackAnalysisInput
    {
        // 再生中の AudioSource
        public AudioSource m_source;

        // 実効出力音量（設定音量 × 3D減衰 × ミュート/ソロ適用後）
        public float m_effectiveVolume;

        // ステレオパン（-1:左 ～ 0:中央 ～ +1:右）
        public float m_panStereo;

        // 解析対象として有効か
        public bool m_isActive;
    }

    // FFT 解析に使用するサンプル数（2のべき乗）
    private const int FFT_SIZE = 512;

    // イコライザー表示のバンド数
    public const int SPECTRUM_BAND_COUNT = 24;

    // オシロスコープ表示用の波形ポイント数
    public const int WAVEFORM_POINT_COUNT = 128;

    // 解析する最低周波数（Hz）
    private const float MIN_FREQUENCY = 60.0f;

    // 解析する最高周波数（Hz）
    private const float MAX_FREQUENCY = 16000.0f;

    // 表示する最小 dB（これ以下は 0.0 扱い）
    public const float MIN_DISPLAY_DB = -60.0f;

    // メーター減衰速度（1秒あたり）
    private const float LEVEL_FALL_SPEED = 2.4f;

    // スペクトラム減衰速度（1秒あたり）
    private const float BAND_FALL_SPEED = 2.8f;

    // ピークホールドの停止時間（秒）
    private const float PEAK_HOLD_DURATION = 0.65f;

    // ピークホールドの落下速度（1秒あたり）
    private const float PEAK_FALL_SPEED = 0.9f;

    // アクティブ判定の微小しきい値
    private const float ACTIVE_THRESHOLD = 0.002f;

    // クリッピング判定の線形振幅しきい値
    private const float CLIP_THRESHOLD = 0.99f;

    // dB 変換係数
    private const float DECIBEL_MULTIPLIER = 20.0f;

    // FFT 実数部ワーク配列
    private static readonly float[] m_fftReal = new float[FFT_SIZE];

    // FFT 虚数部ワーク配列
    private static readonly float[] m_fftImag = new float[FFT_SIZE];

    // ハニング窓の事前計算テーブル
    private static readonly float[] m_hannWindow = new float[FFT_SIZE];

    // モノラル合成後の生サンプル
    private static readonly float[] m_monoSamples = new float[FFT_SIZE];

    // マルチトラック加算合成用の左チャンネルバッファ
    private static readonly float[] m_mixLeftBuffer = new float[FFT_SIZE];

    // マルチトラック加算合成用の右チャンネルバッファ
    private static readonly float[] m_mixRightBuffer = new float[FFT_SIZE];

    // フォールバック読み取り用バッファ
    private static readonly float[] m_fallbackBuffer = new float[FFT_SIZE];

    // 正規化された各バンドの値（0～1）
    private static readonly float[] m_bandLevels = new float[SPECTRUM_BAND_COUNT];

    // 各バンドのピークホールド値（0～1）
    private static readonly float[] m_bandPeaks = new float[SPECTRUM_BAND_COUNT];

    // 各バンドのピークホールド残り時間（秒）
    private static readonly float[] m_bandPeakTimers = new float[SPECTRUM_BAND_COUNT];

    // オシロスコープ表示用の波形サンプル（-1～1）
    private static readonly float[] m_waveformPoints = new float[WAVEFORM_POINT_COUNT];

    // PCM 読み取り用の一時バッファ
    private static float[] m_pcmBuffer = new float[FFT_SIZE * 2];

    // 窓テーブルを初期化済みか
    private static bool m_isInitialized = false;

    // 左チャンネルの現在 RMS（正規化 0～1）
    private static float m_leftRmsNormalized = 0.0f;

    // 右チャンネルの現在 RMS（正規化 0～1）
    private static float m_rightRmsNormalized = 0.0f;

    // 左チャンネルの現在ピーク（正規化 0～1）
    private static float m_leftPeakNormalized = 0.0f;

    // 右チャンネルの現在ピーク（正規化 0～1）
    private static float m_rightPeakNormalized = 0.0f;

    // 左チャンネルのピークホールド値（正規化 0～1）
    private static float m_leftPeakHold = 0.0f;

    // 右チャンネルのピークホールド値（正規化 0～1）
    private static float m_rightPeakHold = 0.0f;

    // 左ピークホールドの残り時間
    private static float m_leftPeakTimer = 0.0f;

    // 右ピークホールドの残り時間
    private static float m_rightPeakTimer = 0.0f;

    // 現在の最大ピーク（dB）
    private static float m_currentPeakDb = MIN_DISPLAY_DB;

    // 現在の平均 RMS（dB）
    private static float m_currentRmsDb = MIN_DISPLAY_DB;

    // セッション中の最大ピーク保持値（dB）
    private static float m_sessionMaxPeakDb = MIN_DISPLAY_DB;

    // クリッピング（0dB 以上）が発生しているか
    private static bool m_isClipping = false;

    // セッション中に一度でもクリッピングが発生したか
    private static bool m_hasClippedInSession = false;

    // 前回の更新時刻
    private static double m_lastUpdateTime = 0.0;

    // リファレンス音源の記録済みピークレベル（正規化 0～1）
    private static float m_referencePeakNormalized = -1.0f;

    // リファレンス音源の記録済み dB
    private static float m_referencePeakDb = MIN_DISPLAY_DB;

    // リファレンス記録対象の AudioID
    private static AudioID m_referenceRecordedId = AudioID.None;

    /// <summary>
    /// 左チャンネルの RMS メーター値（0～1）を取得します。
    /// </summary>
    public static float LeftRmsNormalized => m_leftRmsNormalized;

    /// <summary>
    /// 右チャンネルの RMS メーター値（0～1）を取得します。
    /// </summary>
    public static float RightRmsNormalized => m_rightRmsNormalized;

    /// <summary>
    /// 左チャンネルのピークメーター値（0～1）を取得します。
    /// </summary>
    public static float LeftPeakNormalized => m_leftPeakNormalized;

    /// <summary>
    /// 右チャンネルのピークメーター値（0～1）を取得します。
    /// </summary>
    public static float RightPeakNormalized => m_rightPeakNormalized;

    /// <summary>
    /// 左チャンネルのピークホールド値（0～1）を取得します。
    /// </summary>
    public static float LeftPeakHold => m_leftPeakHold;

    /// <summary>
    /// 右チャンネルのピークホールド値（0～1）を取得します。
    /// </summary>
    public static float RightPeakHold => m_rightPeakHold;

    /// <summary>
    /// 現在の最大ピーク値（dB）を取得します。
    /// </summary>
    public static float CurrentPeakDb => m_currentPeakDb;

    /// <summary>
    /// 現在の平均 RMS 値（dB）を取得します。
    /// </summary>
    public static float CurrentRmsDb => m_currentRmsDb;

    /// <summary>
    /// リセット以降の最大ピーク値（dB）を取得します。
    /// </summary>
    public static float SessionMaxPeakDb => m_sessionMaxPeakDb;

    /// <summary>
    /// クリッピングが発生しているかを取得します。
    /// </summary>
    public static bool IsClipping => m_isClipping;

    /// <summary>
    /// リセット以降にクリッピングが発生したかを取得します。
    /// </summary>
    public static bool HasClippedInSession => m_hasClippedInSession;

    /// <summary>
    /// リファレンス音源の記録済みピーク値（0～1、未記録時は負値）を取得します。
    /// </summary>
    public static float ReferencePeakNormalized => m_referencePeakNormalized;

    /// <summary>
    /// リファレンス音源の記録済みピーク値（dB）を取得します。
    /// </summary>
    public static float ReferencePeakDb => m_referencePeakDb;

    /// <summary>
    /// 24バンドの現在スペクトラム配列を取得します。
    /// </summary>
    /// <returns>各バンドの正規化値配列。</returns>
    public static float[] GetBandLevels()
    {
        return m_bandLevels;
    }

    /// <summary>
    /// 24バンドのピークホールド配列を取得します。
    /// </summary>
    /// <returns>各バンドのピークホールド値配列。</returns>
    public static float[] GetBandPeaks()
    {
        return m_bandPeaks;
    }

    /// <summary>
    /// オシロスコープ表示用の波形サンプル配列を取得します。
    /// </summary>
    /// <returns>波形サンプル配列。</returns>
    public static float[] GetWaveformPoints()
    {
        return m_waveformPoints;
    }

    /// <summary>
    /// リファレンス音源の記録レベルをリセットします。
    /// </summary>
    public static void ClearReferenceLevel()
    {
        m_referencePeakNormalized = -1.0f;
        m_referencePeakDb = MIN_DISPLAY_DB;
        m_referenceRecordedId = AudioID.None;
    }

    /// <summary>
    /// セッション中の最大ピーク値とクリッピング警告フラグをリセットします。
    /// </summary>
    public static void ResetSessionPeakAndClip()
    {
        m_sessionMaxPeakDb = MIN_DISPLAY_DB;
        m_hasClippedInSession = false;
    }

    /// <summary>
    /// 単体試聴中の AudioSource からリアルタイム解析を更新します。
    /// </summary>
    /// <param name="source">再生中の AudioSource。</param>
    /// <param name="playingId">再生中の AudioID。</param>
    /// <param name="referenceId">比較基準に設定中の AudioID。</param>
    public static void UpdateAnalysis(AudioSource source, AudioID playingId, AudioID referenceId)
    {
        EnsureInitialized();
        float deltaTime = ComputeDeltaTime();

        if (referenceId == AudioID.None)
        {
            ClearReferenceLevel();
        }
        else if (referenceId != m_referenceRecordedId)
        {
            m_referencePeakNormalized = -1.0f;
            m_referencePeakDb = MIN_DISPLAY_DB;
            m_referenceRecordedId = referenceId;
        }

        if (source == null || !source.isPlaying || source.clip == null)
        {
            DecayAllMeters(deltaTime);
            return;
        }

        Array.Clear(m_mixLeftBuffer, 0, FFT_SIZE);
        Array.Clear(m_mixRightBuffer, 0, FFT_SIZE);

        TrackAnalysisInput singleInput = new TrackAnalysisInput
        {
            m_source = source,
            m_effectiveVolume = Mathf.Clamp01(source.volume),
            m_panStereo = source.panStereo,
            m_isActive = true
        };

        if (!AccumulateTrackSamples(singleInput, out float _, out int sampleRate))
        {
            DecayAllMeters(deltaTime);
            return;
        }

        FinalizeMixedAnalysis(sampleRate, deltaTime, referenceId != AudioID.None && playingId == referenceId);
    }

    /// <summary>
    /// 複数トラックの音声を加算合成してリアルタイム解析を更新します。
    /// 各トラックの個別メーター値も同時に出力します。
    /// </summary>
    /// <param name="inputs">解析対象のトラック入力一覧。</param>
    /// <param name="outTrackNormalizedPeaks">各トラックの正規化ピーク出力リスト。</param>
    public static void UpdateMultiTrackAnalysis(
        IReadOnlyList<TrackAnalysisInput> inputs,
        List<float> outTrackNormalizedPeaks)
    {
        EnsureInitialized();
        float deltaTime = ComputeDeltaTime();

        Array.Clear(m_mixLeftBuffer, 0, FFT_SIZE);
        Array.Clear(m_mixRightBuffer, 0, FFT_SIZE);

        if (outTrackNormalizedPeaks != null)
        {
            outTrackNormalizedPeaks.Clear();
        }

        if (inputs == null || inputs.Count == 0)
        {
            DecayAllMeters(deltaTime);
            return;
        }

        bool hasAnyActiveSample = false;
        int primarySampleRate = 44100;

        for (int i = 0; i < inputs.Count; i++)
        {
            TrackAnalysisInput input = inputs[i];
            float trackPeakLinear = 0.0f;

            if (input.m_isActive
                && input.m_effectiveVolume > 0.0001f
                && AccumulateTrackSamples(input, out trackPeakLinear, out int trackRate))
            {
                hasAnyActiveSample = true;
                if (trackRate > 0)
                {
                    primarySampleRate = trackRate;
                }
            }

            if (outTrackNormalizedPeaks != null)
            {
                float normPeak = DecibelToNormalized(LinearToDecibel(trackPeakLinear));
                outTrackNormalizedPeaks.Add(normPeak);
            }
        }

        if (!hasAnyActiveSample)
        {
            DecayAllMeters(deltaTime);
            return;
        }

        FinalizeMixedAnalysis(primarySampleRate, deltaTime, false);
    }

    /// <summary>
    /// メーターやスペクトラムがまだ画面上で動いているかを確認します。
    /// </summary>
    /// <returns>
    /// true：描画更新が必要です。
    /// false：完全に静止しています。
    /// </returns>
    public static bool HasVisualActivity()
    {
        if (m_leftPeakHold > ACTIVE_THRESHOLD || m_rightPeakHold > ACTIVE_THRESHOLD)
        {
            return true;
        }

        for (int i = 0; i < SPECTRUM_BAND_COUNT; i++)
        {
            if (m_bandPeaks[i] > ACTIVE_THRESHOLD || m_bandLevels[i] > ACTIVE_THRESHOLD)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 指定した dB 値をメーター表示用の 0～1 へ変換します。
    /// </summary>
    /// <param name="decibel">変換する dB 値。</param>
    /// <returns>0.0～1.0 の正規化値。</returns>
    public static float DecibelToNormalized(float decibel)
    {
        return Mathf.Clamp01((decibel - MIN_DISPLAY_DB) / (0.0f - MIN_DISPLAY_DB));
    }

    /// <summary>
    /// 線形振幅値を dB へ変換します。
    /// </summary>
    /// <param name="linear">線形振幅（0以上）。</param>
    /// <returns>dB 値。</returns>
    public static float LinearToDecibel(float linear)
    {
        if (linear <= 0.0001f)
        {
            return MIN_DISPLAY_DB;
        }

        return Mathf.Max(MIN_DISPLAY_DB, DECIBEL_MULTIPLIER * Mathf.Log10(linear));
    }

    /// <summary>
    /// ハニング窓テーブルを初回のみ計算します。
    /// </summary>
    private static void EnsureInitialized()
    {
        if (m_isInitialized)
        {
            return;
        }

        m_isInitialized = true;
        float denom = FFT_SIZE - 1;

        for (int i = 0; i < FFT_SIZE; i++)
        {
            m_hannWindow[i] = 0.5f * (1.0f - Mathf.Cos(2.0f * Mathf.PI * i / denom));
        }
    }

    /// <summary>
    /// 前回解析からの経過秒数を算出します。
    /// </summary>
    private static float ComputeDeltaTime()
    {
        double now = EditorApplication.timeSinceStartup;
        float deltaTime = m_lastUpdateTime > 0.0 ? Mathf.Clamp((float)(now - m_lastUpdateTime), 0.0f, 0.1f) : 0.016f;
        m_lastUpdateTime = now;
        return deltaTime;
    }

    /// <summary>
    /// 1トラック分の PCM サンプルを読み取り、ステレオパンと実効音量を適用して合成バッファへ加算します。
    /// </summary>
    private static bool AccumulateTrackSamples(
        TrackAnalysisInput input,
        out float trackPeakLinear,
        out int sampleRate)
    {
        trackPeakLinear = 0.0f;
        sampleRate = 44100;

        AudioSource source = input.m_source;
        if (source == null || !source.isPlaying || source.clip == null)
        {
            return false;
        }

        AudioClip clip = source.clip;
        sampleRate = clip.frequency > 0 ? clip.frequency : 44100;

        int channels = Mathf.Max(1, clip.channels);
        int totalSamples = clip.samples;
        if (totalSamples <= 0)
        {
            return false;
        }

        float pan = Mathf.Clamp(input.m_panStereo, -1.0f, 1.0f);
        float leftGain = input.m_effectiveVolume * Mathf.Clamp01(1.0f - pan);
        float rightGain = input.m_effectiveVolume * Mathf.Clamp01(1.0f + pan);

        int readCount = Mathf.Min(FFT_SIZE, totalSamples);
        int requiredLength = readCount * channels;

        if (m_pcmBuffer == null || m_pcmBuffer.Length < requiredLength)
        {
            m_pcmBuffer = new float[requiredLength];
        }

        bool isReadSuccess = false;

        if (clip.loadState == AudioDataLoadState.Loaded && clip.loadType != AudioClipLoadType.Streaming)
        {
            try
            {
                int offset = Mathf.Clamp(source.timeSamples, 0, Mathf.Max(0, totalSamples - readCount));
                float[] tempSlice = m_pcmBuffer.Length == requiredLength ? m_pcmBuffer : new float[requiredLength];
                isReadSuccess = clip.GetData(tempSlice, offset);
                if (isReadSuccess && tempSlice != m_pcmBuffer)
                {
                    Array.Copy(tempSlice, m_pcmBuffer, requiredLength);
                }
            }
            catch
            {
                isReadSuccess = false;
            }
        }

        if (!isReadSuccess)
        {
            Array.Clear(m_fallbackBuffer, 0, FFT_SIZE);
            source.GetOutputData(m_fallbackBuffer, 0);

            for (int i = 0; i < FFT_SIZE; i++)
            {
                float sL = m_fallbackBuffer[i] * Mathf.Clamp01(1.0f - pan);
                float sR = m_fallbackBuffer[i] * Mathf.Clamp01(1.0f + pan);
                m_mixLeftBuffer[i] += sL;
                m_mixRightBuffer[i] += sR;

                float absMax = Mathf.Max(Mathf.Abs(sL), Mathf.Abs(sR));
                if (absMax > trackPeakLinear)
                {
                    trackPeakLinear = absMax;
                }
            }

            return true;
        }

        for (int i = 0; i < readCount; i++)
        {
            int baseIndex = i * channels;
            float rawL = m_pcmBuffer[baseIndex];
            float rawR = channels > 1 ? m_pcmBuffer[baseIndex + 1] : rawL;

            float sL = rawL * leftGain;
            float sR = rawR * rightGain;

            m_mixLeftBuffer[i] += sL;
            m_mixRightBuffer[i] += sR;

            float absMax = Mathf.Max(Mathf.Abs(sL), Mathf.Abs(sR));
            if (absMax > trackPeakLinear)
            {
                trackPeakLinear = absMax;
            }
        }

        return true;
    }

    /// <summary>
    /// 加算合成された L/R バッファからマスターのレベル・波形・24バンド FFT を算出します。
    /// </summary>
    private static void FinalizeMixedAnalysis(int sampleRate, float deltaTime, bool isRecordingReference)
    {
        float leftPeak = 0.0f;
        float rightPeak = 0.0f;
        float sumSqL = 0.0f;
        float sumSqR = 0.0f;

        for (int i = 0; i < FFT_SIZE; i++)
        {
            float sL = m_mixLeftBuffer[i];
            float sR = m_mixRightBuffer[i];

            float absL = Mathf.Abs(sL);
            float absR = Mathf.Abs(sR);

            if (absL > leftPeak)
            {
                leftPeak = absL;
            }

            if (absR > rightPeak)
            {
                rightPeak = absR;
            }

            sumSqL += sL * sL;
            sumSqR += sR * sR;
            m_monoSamples[i] = (sL + sR) * 0.5f;
        }

        float leftRms = Mathf.Sqrt(sumSqL / FFT_SIZE);
        float rightRms = Mathf.Sqrt(sumSqR / FFT_SIZE);
        float maxPeakLinear = Mathf.Max(leftPeak, rightPeak);
        float avgRmsLinear = (leftRms + rightRms) * 0.5f;

        m_currentPeakDb = LinearToDecibel(maxPeakLinear);
        m_currentRmsDb = LinearToDecibel(avgRmsLinear);
        m_isClipping = maxPeakLinear >= CLIP_THRESHOLD;

        if (m_currentPeakDb > m_sessionMaxPeakDb)
        {
            m_sessionMaxPeakDb = m_currentPeakDb;
        }

        if (m_isClipping)
        {
            m_hasClippedInSession = true;
        }

        float targetLeftPeak = DecibelToNormalized(LinearToDecibel(leftPeak));
        float targetRightPeak = DecibelToNormalized(LinearToDecibel(rightPeak));
        float targetLeftRms = DecibelToNormalized(LinearToDecibel(leftRms));
        float targetRightRms = DecibelToNormalized(LinearToDecibel(rightRms));

        m_leftPeakNormalized = SmoothLevel(m_leftPeakNormalized, targetLeftPeak, LEVEL_FALL_SPEED * deltaTime);
        m_rightPeakNormalized = SmoothLevel(m_rightPeakNormalized, targetRightPeak, LEVEL_FALL_SPEED * deltaTime);
        m_leftRmsNormalized = SmoothLevel(m_leftRmsNormalized, targetLeftRms, LEVEL_FALL_SPEED * deltaTime);
        m_rightRmsNormalized = SmoothLevel(m_rightRmsNormalized, targetRightRms, LEVEL_FALL_SPEED * deltaTime);

        UpdatePeakHold(m_leftPeakNormalized, ref m_leftPeakHold, ref m_leftPeakTimer, deltaTime);
        UpdatePeakHold(m_rightPeakNormalized, ref m_rightPeakHold, ref m_rightPeakTimer, deltaTime);

        if (isRecordingReference)
        {
            float currentNorm = Mathf.Max(targetLeftPeak, targetRightPeak);
            if (currentNorm > m_referencePeakNormalized)
            {
                m_referencePeakNormalized = currentNorm;
                m_referencePeakDb = m_currentPeakDb;
            }
        }

        FillWaveformPoints();
        ComputeSpectrumBands(sampleRate, deltaTime);
    }

    /// <summary>
    /// オシロスコープ表示用に波形サンプルを間引いて格納します。
    /// </summary>
    private static void FillWaveformPoints()
    {
        float step = (float)FFT_SIZE / WAVEFORM_POINT_COUNT;

        for (int i = 0; i < WAVEFORM_POINT_COUNT; i++)
        {
            int sampleIndex = Mathf.Clamp(Mathf.FloorToInt(i * step), 0, FFT_SIZE - 1);
            m_waveformPoints[i] = Mathf.Clamp(m_monoSamples[sampleIndex], -1.0f, 1.0f);
        }
    }

    /// <summary>
    /// Radix-2 FFT を実行し、24 バンドの対数周波数スペクトラムを更新します。
    /// </summary>
    private static void ComputeSpectrumBands(int sampleRate, float deltaTime)
    {
        for (int i = 0; i < FFT_SIZE; i++)
        {
            m_fftReal[i] = m_monoSamples[i] * m_hannWindow[i];
            m_fftImag[i] = 0.0f;
        }

        ExecuteRadix2Fft(m_fftReal, m_fftImag, FFT_SIZE);

        int halfSize = FFT_SIZE / 2;
        float safeSampleRate = sampleRate > 0 ? sampleRate : 44100.0f;
        float hzPerBin = safeSampleRate / FFT_SIZE;
        float freqRatio = MAX_FREQUENCY / MIN_FREQUENCY;
        float normScale = 2.0f / FFT_SIZE;

        for (int band = 0; band < SPECTRUM_BAND_COUNT; band++)
        {
            float t0 = (float)band / SPECTRUM_BAND_COUNT;
            float t1 = (float)(band + 1) / SPECTRUM_BAND_COUNT;

            float lowFreq = MIN_FREQUENCY * Mathf.Pow(freqRatio, t0);
            float highFreq = MIN_FREQUENCY * Mathf.Pow(freqRatio, t1);

            int startBin = Mathf.Clamp(Mathf.FloorToInt(lowFreq / hzPerBin), 1, halfSize - 1);
            int endBin = Mathf.Clamp(Mathf.CeilToInt(highFreq / hzPerBin), startBin + 1, halfSize);

            float maxMagnitude = 0.0f;

            for (int bin = startBin; bin < endBin; bin++)
            {
                float re = m_fftReal[bin];
                float im = m_fftImag[bin];
                float mag = Mathf.Sqrt((re * re) + (im * im)) * normScale;

                if (mag > maxMagnitude)
                {
                    maxMagnitude = mag;
                }
            }

            float visualBoost = 1.0f + (t0 * 1.8f);
            float bandDb = LinearToDecibel(maxMagnitude * visualBoost * 2.2f);
            float targetValue = DecibelToNormalized(bandDb);

            m_bandLevels[band] = SmoothLevel(m_bandLevels[band], targetValue, BAND_FALL_SPEED * deltaTime);
            UpdatePeakHold(m_bandLevels[band], ref m_bandPeaks[band], ref m_bandPeakTimers[band], deltaTime);
        }
    }

    /// <summary>
    /// インプレース Radix-2 高速フーリエ変換（FFT）を実行します。
    /// </summary>
    private static void ExecuteRadix2Fft(float[] real, float[] imag, int n)
    {
        int j = 0;
        for (int i = 0; i < n; i++)
        {
            if (i < j)
            {
                float tempRe = real[i];
                real[i] = real[j];
                real[j] = tempRe;

                float tempIm = imag[i];
                imag[i] = imag[j];
                imag[j] = tempIm;
            }

            int bit = n >> 1;
            while (bit > 0 && (j & bit) != 0)
            {
                j ^= bit;
                bit >>= 1;
            }
            j |= bit;
        }

        for (int len = 2; len <= n; len <<= 1)
        {
            float angle = -2.0f * Mathf.PI / len;
            float wLenRe = Mathf.Cos(angle);
            float wLenIm = Mathf.Sin(angle);
            int halfLen = len >> 1;

            for (int i = 0; i < n; i += len)
            {
                float wRe = 1.0f;
                float wIm = 0.0f;

                for (int k = 0; k < halfLen; k++)
                {
                    int evenIdx = i + k;
                    int oddIdx = evenIdx + halfLen;

                    float oddRe = (real[oddIdx] * wRe) - (imag[oddIdx] * wIm);
                    float oddIm = (real[oddIdx] * wIm) + (imag[oddIdx] * wRe);

                    real[oddIdx] = real[evenIdx] - oddRe;
                    imag[oddIdx] = imag[evenIdx] - oddIm;
                    real[evenIdx] += oddRe;
                    imag[evenIdx] += oddIm;

                    float nextWRe = (wRe * wLenRe) - (wIm * wLenIm);
                    wIm = (wRe * wLenIm) + (wIm * wLenRe);
                    wRe = nextWRe;
                }
            }
        }
    }

    /// <summary>
    /// 停止時にすべてのメーター・スペクトラム・波形を滑らかにゼロへ戻します。
    /// </summary>
    private static void DecayAllMeters(float deltaTime)
    {
        float levelDrop = LEVEL_FALL_SPEED * deltaTime;
        float bandDrop = BAND_FALL_SPEED * deltaTime;

        m_leftPeakNormalized = Mathf.Max(0.0f, m_leftPeakNormalized - levelDrop);
        m_rightPeakNormalized = Mathf.Max(0.0f, m_rightPeakNormalized - levelDrop);
        m_leftRmsNormalized = Mathf.Max(0.0f, m_leftRmsNormalized - levelDrop);
        m_rightRmsNormalized = Mathf.Max(0.0f, m_rightRmsNormalized - levelDrop);

        UpdatePeakHold(m_leftPeakNormalized, ref m_leftPeakHold, ref m_leftPeakTimer, deltaTime);
        UpdatePeakHold(m_rightPeakNormalized, ref m_rightPeakHold, ref m_rightPeakTimer, deltaTime);

        for (int i = 0; i < SPECTRUM_BAND_COUNT; i++)
        {
            m_bandLevels[i] = Mathf.Max(0.0f, m_bandLevels[i] - bandDrop);
            UpdatePeakHold(m_bandLevels[i], ref m_bandPeaks[i], ref m_bandPeakTimers[i], deltaTime);
        }

        for (int i = 0; i < WAVEFORM_POINT_COUNT; i++)
        {
            m_waveformPoints[i] = Mathf.MoveTowards(m_waveformPoints[i], 0.0f, levelDrop);
        }

        if (m_leftPeakNormalized <= ACTIVE_THRESHOLD && m_rightPeakNormalized <= ACTIVE_THRESHOLD)
        {
            m_currentPeakDb = MIN_DISPLAY_DB;
            m_currentRmsDb = MIN_DISPLAY_DB;
            m_isClipping = false;
        }
    }

    /// <summary>
    /// 上昇時は即追従、下降時は滑らかに減衰させます。
    /// </summary>
    private static float SmoothLevel(float current, float target, float maxDrop)
    {
        return target >= current ? target : Mathf.Max(target, current - maxDrop);
    }

    /// <summary>
    /// ピークホールド値と保持タイマーを更新します。
    /// </summary>
    private static void UpdatePeakHold(float current, ref float holdValue, ref float holdTimer, float deltaTime)
    {
        if (current >= holdValue)
        {
            holdValue = current;
            holdTimer = PEAK_HOLD_DURATION;
            return;
        }

        if (holdTimer > 0.0f)
        {
            holdTimer -= deltaTime;
            return;
        }

        holdValue = Mathf.Max(current, holdValue - (PEAK_FALL_SPEED * deltaTime));
    }
}

#endif