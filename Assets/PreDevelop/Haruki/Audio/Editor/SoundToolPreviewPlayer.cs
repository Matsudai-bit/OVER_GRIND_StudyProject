#if UNITY_EDITOR

using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// シーケンス試聴の再生状態を表します。
/// </summary>
public enum PreviewSequenceState
{
    PREVIEW_IDLE,
    PREVIEW_SINGLE,
    PREVIEW_INTRO,
    PREVIEW_LOOP,
    PREVIEW_OUTRO,
    PREVIEW_DIRECT_CLIP
}

/// <summary>
/// Editor 上でのサウンド試聴（単体再生・I/L/O 一連シーケンス再生・未登録 Clip 直接試聴）とリアルタイム解析連携を担当します。
/// </summary>
public static class SoundToolPreviewPlayer
{
    // 試聴用オブジェクトの名前
    private const string PREVIEW_OBJECT_NAME = "SoundToolPreviewSource";

    // Unity 内部の AudioUtil 型名
    private const string AUDIO_UTIL_TYPE_NAME = "UnityEditor.AudioUtil";

    // AudioUtil のメソッド名（再生）
    private const string METHOD_PLAY_PREVIEW_CLIP = "PlayPreviewClip";

    // AudioUtil のメソッド名（停止）
    private const string METHOD_STOP_ALL_PREVIEW_CLIPS = "StopAllPreviewClips";

    // AudioUtil のメソッド名（再生中確認）
    private const string METHOD_IS_PREVIEW_CLIP_PLAYING = "IsPreviewClipPlaying";

    // シーク先の終端安全余白（秒）
    private const float SEEK_END_MARGIN = 0.05f;

    // 末尾ジャンプの最小残り時間（秒）
    private const float MIN_JUMP_REMAINING_SECONDS = 0.1f;

    // Outro 末尾接続時の標準残り時間（秒）
    private const float LOOP_END_TO_OUTRO_SECONDS = 2.0f;

    // 試聴用の非表示オブジェクト
    private static GameObject m_previewObject;

    // 試聴用の AudioSource
    private static AudioSource m_previewSource;

    // 再生中のサウンド ID
    private static AudioID m_playingId = AudioID.None;

    // 直接試聴中の AudioClip（追加予定行の試聴用）
    private static AudioClip m_playingDirectClip;

    // 比較基準（Ref）に設定中のサウンド ID
    private static AudioID m_referenceId = AudioID.None;

    // 再生中のグループベース名（単体再生時は空文字）
    private static string m_playingGroupBaseName = "";

    // 現在の試聴状態
    private static PreviewSequenceState m_currentState = PreviewSequenceState.PREVIEW_IDLE;

    // 試聴中の再生に AudioSource を使っているか
    private static bool m_isUsingAudioSource = false;

    // 登録中の単体データ
    private static SoundDatabase.SoundData m_singleData;

    // 登録中の開始データ（_I）
    private static SoundDatabase.SoundData m_introData;

    // 登録中のループデータ（_L）
    private static SoundDatabase.SoundData m_loopData;

    // 登録中の終了データ（_O）
    private static SoundDatabase.SoundData m_outroData;

    // ループを1周したら自動で Outro へ移行するか
    private static bool m_autoOutro = false;

    // ループの最後から Outro へ接続する待機中か
    private static bool m_pendingOutroAfterLoop = false;

    // EditorApplication.update を購読中か
    private static bool m_isUpdating = false;

    // AudioUtil のメソッドを取得済みか
    private static bool m_isMethodCached = false;

    // AudioUtil.PlayPreviewClip
    private static MethodInfo m_playMethod;

    // AudioUtil.StopAllPreviewClips
    private static MethodInfo m_stopMethod;

    // AudioUtil.IsPreviewClipPlaying
    private static MethodInfo m_isPlayingMethod;

    /// <summary>
    /// 試聴状態やアナライザー描画が更新された際に通知されます。
    /// </summary>
    public static event Action OnStateChanged;

    /// <summary>
    /// 試聴に音量・ピッチを反映するかを取得または設定します。
    /// </summary>
    public static bool UseVolume { get; set; } = true;

    /// <summary>
    /// 試聴中のサウンド ID を取得します。
    /// </summary>
    public static AudioID PlayingId => m_playingId;

    /// <summary>
    /// 一連試聴中のグループベース名を取得します。
    /// </summary>
    public static string PlayingGroupBaseName => m_playingGroupBaseName;

    /// <summary>
    /// 現在の試聴状態を取得します。
    /// </summary>
    public static PreviewSequenceState CurrentState => m_currentState;

    /// <summary>
    /// 現在再生中の SoundData を取得します。
    /// </summary>
    public static SoundDatabase.SoundData CurrentData => GetDataForState(m_currentState);

    static SoundToolPreviewPlayer()
    {
        AssemblyReloadEvents.beforeAssemblyReload += Dispose;
    }

    /// <summary>
    /// アナライザーの Ref 比較基準となる AudioID を設定します。
    /// </summary>
    /// <param name="referenceId">基準にする AudioID。</param>
    public static void SetReferenceId(AudioID referenceId)
    {
        m_referenceId = referenceId;
    }

    /// <summary>
    /// ID 未登録の AudioClip を直接試聴します（ID 追加欄の試聴用）。
    /// </summary>
    /// <param name="clip">試聴する AudioClip。</param>
    /// <param name="volume">音量。</param>
    /// <param name="pitch">ピッチ。</param>
    /// <param name="loop">ループするか。</param>
    public static void PlayClipDirect(AudioClip clip, float volume, float pitch, bool loop)
    {
        Stop(false);

        if (clip == null)
        {
            return;
        }

        m_playingDirectClip = clip;
        m_currentState = PreviewSequenceState.PREVIEW_DIRECT_CLIP;
        m_playingId = AudioID.None;
        m_playingGroupBaseName = "";

        if (UseVolume)
        {
            PlayWithAudioSource(clip, volume, pitch, loop);
        }
        else
        {
            PlayWithAudioUtil(clip, loop);
        }

        StartUpdateLoop();
        OnStateChanged?.Invoke();
    }

    /// <summary>
    /// 単体サウンドを試聴します。
    /// </summary>
    /// <param name="data">試聴するサウンドデータ。</param>
    /// <param name="referenceId">Ref 基準の AudioID（省略可）。</param>
    public static void PlaySingle(SoundDatabase.SoundData data, AudioID referenceId = AudioID.None)
    {
        Stop(false);

        if (referenceId != AudioID.None)
        {
            m_referenceId = referenceId;
        }

        if (data == null || data.m_clip == null)
        {
            return;
        }

        m_singleData = data;
        m_playingGroupBaseName = "";
        m_autoOutro = false;
        m_pendingOutroAfterLoop = false;

        StartPart(PreviewSequenceState.PREVIEW_SINGLE, data, data.m_loop);
    }

    /// <summary>
    /// _I / _L / _O を一連のシーケンスとして試聴します。
    /// </summary>
    /// <param name="groupBaseName">グループのベース名。</param>
    /// <param name="introData">開始パート。</param>
    /// <param name="loopData">ループパート。</param>
    /// <param name="outroData">終了パート。</param>
    /// <param name="autoOutro">ループ1周で自動 Outro 移行するか。</param>
    /// <param name="referenceId">Ref 基準の AudioID（省略可）。</param>
    public static void PlaySequence(
        string groupBaseName,
        SoundDatabase.SoundData introData,
        SoundDatabase.SoundData loopData,
        SoundDatabase.SoundData outroData,
        bool autoOutro,
        AudioID referenceId = AudioID.None)
    {
        Stop(false);

        if (referenceId != AudioID.None)
        {
            m_referenceId = referenceId;
        }

        m_playingGroupBaseName = groupBaseName ?? "";
        m_introData = introData;
        m_loopData = loopData;
        m_outroData = outroData;
        m_autoOutro = autoOutro;
        m_pendingOutroAfterLoop = false;

        if (HasPlayableClip(m_introData))
        {
            StartPart(PreviewSequenceState.PREVIEW_INTRO, m_introData, false);
        }
        else if (HasPlayableClip(m_loopData))
        {
            StartPart(PreviewSequenceState.PREVIEW_LOOP, m_loopData, !m_autoOutro);
        }
        else if (HasPlayableClip(m_outroData))
        {
            StartPart(PreviewSequenceState.PREVIEW_OUTRO, m_outroData, false);
        }
    }

    /// <summary>
    /// シーケンス再生中のループから Outro（_O）へ移行します。
    /// </summary>
    /// <param name="fromLoopEnd">true の場合はループ末尾から接続し、false の場合は即座に切り替えます。</param>
    public static void TriggerOutro(bool fromLoopEnd)
    {
        if (!HasActivePreview())
        {
            return;
        }

        if (!HasPlayableClip(m_outroData))
        {
            Stop();
            return;
        }

        if (fromLoopEnd && m_currentState == PreviewSequenceState.PREVIEW_LOOP && m_isUsingAudioSource && m_previewSource != null)
        {
            m_pendingOutroAfterLoop = true;
            m_previewSource.loop = false;
            JumpNearEnd(LOOP_END_TO_OUTRO_SECONDS);
            return;
        }

        StartPart(PreviewSequenceState.PREVIEW_OUTRO, m_outroData, false);
    }

    /// <summary>
    /// 現在の再生位置を指定秒数へシークします（AudioSource 試聴時のみ有効）。
    /// </summary>
    /// <param name="timeSeconds">移動先の秒数。</param>
    public static void Seek(float timeSeconds)
    {
        if (!m_isUsingAudioSource || m_previewSource == null || m_previewSource.clip == null)
        {
            return;
        }

        float maxTime = Mathf.Max(0.0f, m_previewSource.clip.length - SEEK_END_MARGIN);
        m_previewSource.time = Mathf.Clamp(timeSeconds, 0.0f, maxTime);
    }

    /// <summary>
    /// 現在再生中のクリップの末尾付近へジャンプします（繋ぎ目の試聴用）。
    /// </summary>
    /// <param name="remainingSeconds">末尾から何秒前へ移動するか。</param>
    public static void JumpNearEnd(float remainingSeconds)
    {
        if (!m_isUsingAudioSource || m_previewSource == null || m_previewSource.clip == null)
        {
            return;
        }

        float safeRemaining = Mathf.Max(MIN_JUMP_REMAINING_SECONDS, remainingSeconds);
        float targetTime = Mathf.Max(0.0f, m_previewSource.clip.length - safeRemaining);
        Seek(targetTime);
    }

    /// <summary>
    /// 自動 Outro 移行設定を切り替えます。
    /// </summary>
    /// <param name="autoOutro">自動移行するか。</param>
    public static void SetAutoOutro(bool autoOutro)
    {
        m_autoOutro = autoOutro;

        if (m_currentState == PreviewSequenceState.PREVIEW_LOOP && m_isUsingAudioSource && m_previewSource != null)
        {
            m_previewSource.loop = !m_autoOutro && !m_pendingOutroAfterLoop;
        }
    }

    /// <summary>
    /// 試聴を停止します。
    /// </summary>
    public static void Stop()
    {
        Stop(true);
    }

    /// <summary>
    /// 現在の再生経過時間（秒）を取得します。
    /// </summary>
    /// <returns>経過時間（秒）。</returns>
    public static float GetCurrentTime()
    {
        if (m_isUsingAudioSource && m_previewSource != null && m_previewSource.clip != null)
        {
            return m_previewSource.time;
        }

        return 0.0f;
    }

    /// <summary>
    /// 現在のクリップの総再生時間（秒）を取得します。
    /// </summary>
    /// <returns>総再生時間（秒）。</returns>
    public static float GetCurrentLength()
    {
        if (m_isUsingAudioSource && m_previewSource != null && m_previewSource.clip != null)
        {
            return m_previewSource.clip.length;
        }

        SoundDatabase.SoundData data = CurrentData;
        return data != null && data.m_clip != null ? data.m_clip.length : 0.0f;
    }

    /// <summary>
    /// 指定 AudioClip が直接試聴中かを確認します。
    /// </summary>
    /// <param name="clip">確認する AudioClip。</param>
    /// <returns>
    /// true：このクリップを直接試聴中です。
    /// false：試聴していません。
    /// </returns>
    public static bool IsPlayingDirectClip(AudioClip clip)
    {
        if (clip == null || m_currentState != PreviewSequenceState.PREVIEW_DIRECT_CLIP || m_playingDirectClip != clip)
        {
            return false;
        }

        return HasActivePreview();
    }

    /// <summary>
    /// 指定 ID が試聴中かを確認します。
    /// </summary>
    /// <param name="id">確認するサウンド ID。</param>
    /// <returns>
    /// true：試聴中です。
    /// false：試聴していません。
    /// </returns>
    public static bool IsPlaying(AudioID id)
    {
        if (id == AudioID.None || m_playingId != id)
        {
            return false;
        }

        return HasActivePreview();
    }

    /// <summary>
    /// 指定グループが一連試聴中かを確認します。
    /// </summary>
    /// <param name="groupBaseName">グループのベース名。</param>
    /// <returns>
    /// true：このグループを試聴中です。
    /// false：試聴していません。
    /// </returns>
    public static bool IsPlayingGroup(string groupBaseName)
    {
        if (string.IsNullOrEmpty(groupBaseName) || m_playingGroupBaseName != groupBaseName)
        {
            return false;
        }

        return HasActivePreview();
    }

    /// <summary>
    /// 何かが試聴中かを確認します。終了していた場合は状態を初期化します。
    /// </summary>
    /// <returns>
    /// true：試聴中です。
    /// false：試聴していません。
    /// </returns>
    public static bool HasActivePreview()
    {
        if (m_currentState == PreviewSequenceState.PREVIEW_IDLE)
        {
            return false;
        }

        if (m_currentState != PreviewSequenceState.PREVIEW_DIRECT_CLIP && m_playingId == AudioID.None)
        {
            return false;
        }

        if (IsAnyPlaying())
        {
            return true;
        }

        if (m_currentState == PreviewSequenceState.PREVIEW_SINGLE
            || m_currentState == PreviewSequenceState.PREVIEW_OUTRO
            || m_currentState == PreviewSequenceState.PREVIEW_DIRECT_CLIP)
        {
            Stop(true);
            return false;
        }

        return true;
    }

    /// <summary>
    /// 内部停止処理を行います。
    /// </summary>
    private static void Stop(bool allowVisualDecay)
    {
        if (m_previewSource != null)
        {
            m_previewSource.Stop();
        }

        CacheAudioUtilMethods();
        m_stopMethod?.Invoke(null, null);

        bool wasPlaying = m_currentState != PreviewSequenceState.PREVIEW_IDLE;

        m_playingId = AudioID.None;
        m_playingDirectClip = null;
        m_playingGroupBaseName = "";
        m_currentState = PreviewSequenceState.PREVIEW_IDLE;
        m_isUsingAudioSource = false;
        m_singleData = null;
        m_introData = null;
        m_loopData = null;
        m_outroData = null;
        m_pendingOutroAfterLoop = false;

        if (!allowVisualDecay || !SoundToolAudioAnalyzer.HasVisualActivity())
        {
            StopUpdateLoop();
        }

        if (wasPlaying)
        {
            OnStateChanged?.Invoke();
        }
    }

    /// <summary>
    /// 指定されたパートの再生を開始します。
    /// </summary>
    private static void StartPart(PreviewSequenceState state, SoundDatabase.SoundData data, bool loop)
    {
        if (data == null || data.m_clip == null)
        {
            Stop(true);
            return;
        }

        m_currentState = state;
        m_playingId = data.m_id;

        if (UseVolume)
        {
            PlayWithAudioSource(data.m_clip, data.m_volume, data.m_pitch, loop);
        }
        else
        {
            PlayWithAudioUtil(data.m_clip, loop);
        }

        StartUpdateLoop();
        OnStateChanged?.Invoke();
    }

    /// <summary>
    /// 再生中の状態監視・音量同期・リアルタイム音声解析・パート遷移を行います。
    /// </summary>
    private static void UpdatePreviewState()
    {
        if (m_currentState == PreviewSequenceState.PREVIEW_IDLE)
        {
            SoundToolAudioAnalyzer.UpdateAnalysis(null, AudioID.None, m_referenceId);
            OnStateChanged?.Invoke();

            if (!SoundToolAudioAnalyzer.HasVisualActivity())
            {
                StopUpdateLoop();
            }
            return;
        }

        SoundDatabase.SoundData activeData = CurrentData;

        if (m_isUsingAudioSource && m_previewSource != null && activeData != null)
        {
            m_previewSource.volume = activeData.m_volume;
            m_previewSource.pitch = activeData.m_pitch;
        }

        SoundToolAudioAnalyzer.UpdateAnalysis(m_isUsingAudioSource ? m_previewSource : null, m_playingId, m_referenceId);
        OnStateChanged?.Invoke();

        if (IsAnyPlaying())
        {
            return;
        }

        switch (m_currentState)
        {
            case PreviewSequenceState.PREVIEW_INTRO:
                if (HasPlayableClip(m_loopData))
                {
                    StartPart(PreviewSequenceState.PREVIEW_LOOP, m_loopData, !m_autoOutro);
                }
                else if (HasPlayableClip(m_outroData))
                {
                    StartPart(PreviewSequenceState.PREVIEW_OUTRO, m_outroData, false);
                }
                else
                {
                    Stop(true);
                }
                break;

            case PreviewSequenceState.PREVIEW_LOOP:
                if ((m_autoOutro || m_pendingOutroAfterLoop) && HasPlayableClip(m_outroData))
                {
                    m_pendingOutroAfterLoop = false;
                    StartPart(PreviewSequenceState.PREVIEW_OUTRO, m_outroData, false);
                }
                else
                {
                    Stop(true);
                }
                break;

            default:
                Stop(true);
                break;
        }
    }

    /// <summary>
    /// 指定の再生状態に対応する SoundData を取得します。
    /// </summary>
    private static SoundDatabase.SoundData GetDataForState(PreviewSequenceState state)
    {
        switch (state)
        {
            case PreviewSequenceState.PREVIEW_SINGLE:
                return m_singleData;
            case PreviewSequenceState.PREVIEW_INTRO:
                return m_introData;
            case PreviewSequenceState.PREVIEW_LOOP:
                return m_loopData;
            case PreviewSequenceState.PREVIEW_OUTRO:
                return m_outroData;
            default:
                return null;
        }
    }

    /// <summary>
    /// 再生可能な AudioClip が設定されているかを確認します。
    /// </summary>
    private static bool HasPlayableClip(SoundDatabase.SoundData data)
    {
        return data != null && data.m_clip != null;
    }

    /// <summary>
    /// 非表示の AudioSource で試聴します。
    /// </summary>
    private static void PlayWithAudioSource(AudioClip clip, float volume, float pitch, bool loop)
    {
        if (!EnsurePreviewSource())
        {
            return;
        }

        clip.LoadAudioData();

        m_previewSource.Stop();
        m_previewSource.clip = clip;
        m_previewSource.volume = volume;
        m_previewSource.pitch = pitch;
        m_previewSource.loop = loop;
        m_previewSource.spatialBlend = 0.0f;
        m_previewSource.time = 0.0f;
        m_previewSource.Play();

        m_isUsingAudioSource = true;
    }

    /// <summary>
    /// Unity 内部の AudioUtil で試聴します。
    /// </summary>
    private static void PlayWithAudioUtil(AudioClip clip, bool loop)
    {
        CacheAudioUtilMethods();
        m_stopMethod?.Invoke(null, null);
        m_playMethod?.Invoke(null, new object[] { clip, 0, loop });

        m_isUsingAudioSource = false;
    }

    /// <summary>
    /// 現在の再生方式で再生中かを確認します。
    /// </summary>
    private static bool IsAnyPlaying()
    {
        if (m_isUsingAudioSource)
        {
            return m_previewSource != null && m_previewSource.isPlaying;
        }

        CacheAudioUtilMethods();

        if (m_isPlayingMethod == null)
        {
            return false;
        }

        object result = m_isPlayingMethod.Invoke(null, null);
        return result is bool isPlaying && isPlaying;
    }

    /// <summary>
    /// EditorApplication.update 監視を開始します。
    /// </summary>
    private static void StartUpdateLoop()
    {
        if (m_isUpdating)
        {
            return;
        }

        m_isUpdating = true;
        EditorApplication.update += UpdatePreviewState;
    }

    /// <summary>
    /// EditorApplication.update 監視を停止します。
    /// </summary>
    private static void StopUpdateLoop()
    {
        if (!m_isUpdating)
        {
            return;
        }

        m_isUpdating = false;
        EditorApplication.update -= UpdatePreviewState;
    }

    /// <summary>
    /// 試聴用の AudioSource を用意します。
    /// </summary>
    private static bool EnsurePreviewSource()
    {
        if (m_previewSource != null)
        {
            return true;
        }

        m_previewObject = EditorUtility.CreateGameObjectWithHideFlags(
            PREVIEW_OBJECT_NAME,
            HideFlags.HideAndDontSave,
            typeof(AudioSource));

        m_previewSource = m_previewObject.GetComponent<AudioSource>();

        if (m_previewSource == null)
        {
            return false;
        }

        m_previewSource.playOnAwake = false;
        return true;
    }

    /// <summary>
    /// AudioUtil のメソッドをリフレクションで取得します。
    /// </summary>
    private static void CacheAudioUtilMethods()
    {
        if (m_isMethodCached)
        {
            return;
        }

        m_isMethodCached = true;

        Type audioUtilType = typeof(AudioImporter).Assembly.GetType(AUDIO_UTIL_TYPE_NAME);

        if (audioUtilType == null)
        {
            return;
        }

        const BindingFlags flags = BindingFlags.Static | BindingFlags.Public;

        m_playMethod = audioUtilType.GetMethod(
            METHOD_PLAY_PREVIEW_CLIP,
            flags,
            null,
            new[] { typeof(AudioClip), typeof(int), typeof(bool) },
            null);

        m_stopMethod = audioUtilType.GetMethod(
            METHOD_STOP_ALL_PREVIEW_CLIPS,
            flags,
            null,
            Type.EmptyTypes,
            null);

        m_isPlayingMethod = audioUtilType.GetMethod(
            METHOD_IS_PREVIEW_CLIP_PLAYING,
            flags,
            null,
            Type.EmptyTypes,
            null);
    }

    /// <summary>
    /// 試聴を止め、試聴用オブジェクトを破棄します。
    /// </summary>
    private static void Dispose()
    {
        Stop(false);

        if (m_previewObject != null)
        {
            UnityEngine.Object.DestroyImmediate(m_previewObject);
        }

        m_previewObject = null;
        m_previewSource = null;
    }
}

#endif