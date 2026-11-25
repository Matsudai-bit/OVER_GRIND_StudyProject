#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 試聴中の音声をリアルタイムに解析し、L/R レベル・波形・24バンド周波数スペクトラム（EQ風）を算出します。
/// Edit Mode でも確実に動作するよう、AudioClip の PCM データと設定音量を直接解析します。
/// </summary>
public static class SoundToolAudioAnalyzer
{
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

    // クリッピング（0dB 以上）が発生しているか
    private static bool m_isClipping = false;

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
    /// クリッピングが発生しているかを取得します。
    /// </summary>
    public static bool IsClipping => m_isClipping;

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
    /// 試聴中の AudioSource からリアルタイム解析を更新します。
    /// </summary>
    /// <param name="source">再生中の AudioSource。</param>
    /// <param name="playingId">再生中の AudioID。</param>
    /// <param name="referenceId">比較基準に設定中の AudioID。</param>
    public static void UpdateAnalysis(AudioSource source, AudioID playingId, AudioID referenceId)
    {
        EnsureInitialized();

        double now = EditorApplication.timeSinceStartup;
        float deltaTime = m_lastUpdateTime > 0.0 ? Mathf.Clamp((float)(now - m_lastUpdateTime), 0.0f, 0.1f) : 0.016f;
        m_lastUpdateTime = now;

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

        // 再生中でなければ滑らかにゼロへ減衰させる
        if (source == null || !source.isPlaying || source.clip == null)
        {
            DecayAllMeters(deltaTime);
            return;
        }

        AudioClip clip = source.clip;
        float volume = Mathf.Clamp01(source.volume);

        if (!TryReadAudioSamples(source, clip, volume, out float leftPeak, out float rightPeak, out float leftRms, out float rightRms))
        {
            DecayAllMeters(deltaTime);
            return;
        }

        // 線形振幅から dB および 0～1 正規化メーター値へ変換する
        float maxPeakLinear = Mathf.Max(leftPeak, rightPeak);
        float avgRmsLinear = (leftRms + rightRms) * 0.5f;

        m_currentPeakDb = LinearToDecibel(maxPeakLinear);
        m_currentRmsDb = LinearToDecibel(avgRmsLinear);
        m_isClipping = maxPeakLinear >= 0.99f;

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

        // リファレンス音源を再生している間、最大ピークを基準ラインとして記録する
        if (referenceId != AudioID.None && playingId == referenceId)
        {
            float currentNorm = Mathf.Max(targetLeftPeak, targetRightPeak);
            if (currentNorm > m_referencePeakNormalized)
            {
                m_referencePeakNormalized = currentNorm;
                m_referencePeakDb = m_currentPeakDb;
            }
        }

        // FFT による 24 バンド・スペクトラム解析を実行する
        ComputeSpectrumBands(clip.frequency, deltaTime);
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
    /// AudioClip（または AudioSource）から現在再生位置のサンプルを読み取り、L/R レベルと波形を抽出します。
    /// </summary>
    private static bool TryReadAudioSamples(
        AudioSource source,
        AudioClip clip,
        float volume,
        out float leftPeak,
        out float rightPeak,
        out float leftRms,
        out float rightRms)
    {
        leftPeak = 0.0f;
        rightPeak = 0.0f;
        leftRms = 0.0f;
        rightRms = 0.0f;

        int channels = Mathf.Max(1, clip.channels);
        int totalSamples = clip.samples;

        if (totalSamples <= 0)
        {
            return false;
        }

        int readCount = Mathf.Min(FFT_SIZE, totalSamples);
        int requiredLength = readCount * channels;

        if (m_pcmBuffer == null || m_pcmBuffer.Length < requiredLength)
        {
            m_pcmBuffer = new float[requiredLength];
        }

        bool isReadSuccess = false;

        // Edit Mode でも確実に生波形を取るため AudioClip.GetData を優先する
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

        // Streaming などで GetData が使えない場合は AudioSource.GetOutputData へフォールバックする
        if (!isReadSuccess)
        {
            Array.Clear(m_monoSamples, 0, FFT_SIZE);
            source.GetOutputData(m_monoSamples, 0);

            float sumSq = 0.0f;
            float peak = 0.0f;

            for (int i = 0; i < FFT_SIZE; i++)
            {
                float sample = m_monoSamples[i];
                float abs = Mathf.Abs(sample);
                if (abs > peak)
                {
                    peak = abs;
                }
                sumSq += sample * sample;
            }

            leftPeak = peak;
            rightPeak = peak;
            leftRms = Mathf.Sqrt(sumSq / FFT_SIZE);
            rightRms = leftRms;

            FillWaveformPoints();
            return true;
        }

        float sumSqL = 0.0f;
        float sumSqR = 0.0f;
        Array.Clear(m_monoSamples, 0, FFT_SIZE);

        for (int i = 0; i < readCount; i++)
        {
            int baseIndex = i * channels;
            float sampleL = m_pcmBuffer[baseIndex] * volume;
            float sampleR = channels > 1 ? m_pcmBuffer[baseIndex + 1] * volume : sampleL;

            float absL = Mathf.Abs(sampleL);
            float absR = Mathf.Abs(sampleR);

            if (absL > leftPeak)
            {
                leftPeak = absL;
            }

            if (absR > rightPeak)
            {
                rightPeak = absR;
            }

            sumSqL += sampleL * sampleL;
            sumSqR += sampleR * sampleR;

            m_monoSamples[i] = (sampleL + sampleR) * 0.5f;
        }

        leftRms = Mathf.Sqrt(sumSqL / readCount);
        rightRms = Mathf.Sqrt(sumSqR / readCount);

        FillWaveformPoints();
        return true;
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
        // ハニング窓を適用して入力をセットする
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

            // 高域のエネルギー減衰を視覚的に見やすく補正して dB 正規化する
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
        // ビット反転による並び替え
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

        // バタフライ演算
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