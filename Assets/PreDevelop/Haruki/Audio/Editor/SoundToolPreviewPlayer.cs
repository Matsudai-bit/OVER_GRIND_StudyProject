#if UNITY_EDITOR

using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor 上でのサウンド試聴を担当します。
/// 「音量を反映」ON：非表示の AudioSource で再生し、音量・ピッチをリアルタイムに反映します。
/// 「音量を反映」OFF：Unity 内部の AudioUtil で再生します（音量は反映されません）。
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

    // 試聴用の非表示オブジェクト
    private static GameObject m_previewObject;

    // 試聴用の AudioSource
    private static AudioSource m_previewSource;

    // 試聴中のサウンド ID
    private static AudioID m_playingId = AudioID.None;

    // 試聴中の再生に AudioSource を使っているか
    private static bool m_isUsingAudioSource = false;

    // AudioUtil のメソッドを取得済みか
    private static bool m_isMethodCached = false;

    // AudioUtil.PlayPreviewClip
    private static MethodInfo m_playMethod;

    // AudioUtil.StopAllPreviewClips
    private static MethodInfo m_stopMethod;

    // AudioUtil.IsPreviewClipPlaying
    private static MethodInfo m_isPlayingMethod;

    /// <summary>
    /// 試聴に音量・ピッチを反映するかを取得または設定します。
    /// </summary>
    public static bool UseVolume { get; set; } = true;

    /// <summary>
    /// 試聴中のサウンド ID を取得します。
    /// </summary>
    public static AudioID PlayingId => m_playingId;

    static SoundToolPreviewPlayer()
    {
        // スクリプトのリロード前に試聴用オブジェクトを片付ける
        AssemblyReloadEvents.beforeAssemblyReload += Dispose;
    }

    /// <summary>
    /// サウンドを試聴します。再生中のものは止めてから再生します。
    /// </summary>
    /// <param name="id">試聴するサウンド ID。</param>
    /// <param name="clip">試聴する AudioClip。</param>
    /// <param name="volume">音量。</param>
    /// <param name="pitch">ピッチ。</param>
    /// <param name="loop">ループ再生するか。</param>
    public static void Play(AudioID id, AudioClip clip, float volume, float pitch, bool loop)
    {
        Stop();

        if (clip == null)
        {
            return;
        }

        m_playingId = id;

        if (UseVolume)
        {
            PlayWithAudioSource(clip, volume, pitch, loop);
        }
        else
        {
            PlayWithAudioUtil(clip, loop);
        }
    }

    /// <summary>
    /// 試聴を停止します。
    /// </summary>
    public static void Stop()
    {
        // AudioSource 側を止める
        if (m_previewSource != null)
        {
            m_previewSource.Stop();
        }

        // AudioUtil 側を止める
        CacheAudioUtilMethods();
        m_stopMethod?.Invoke(null, null);

        m_playingId = AudioID.None;
        m_isUsingAudioSource = false;
    }

    /// <summary>
    /// 試聴中の音量をリアルタイムに更新します（AudioSource 試聴時のみ有効）。
    /// </summary>
    /// <param name="volume">新しい音量。</param>
    public static void SetVolume(float volume)
    {
        if (m_isUsingAudioSource && m_previewSource != null)
        {
            m_previewSource.volume = volume;
        }
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
        // 試聴対象でなければ負荷のかかる確認を行わない
        if (id == AudioID.None || m_playingId != id)
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
        if (m_playingId == AudioID.None)
        {
            return false;
        }

        if (IsAnyPlaying())
        {
            return true;
        }

        // 再生が終わっているので状態を戻す
        m_playingId = AudioID.None;
        return false;
    }

    /// <summary>
    /// 非表示の AudioSource で試聴します。
    /// </summary>
    /// <param name="clip">試聴する AudioClip。</param>
    /// <param name="volume">音量。</param>
    /// <param name="pitch">ピッチ。</param>
    /// <param name="loop">ループ再生するか。</param>
    private static void PlayWithAudioSource(AudioClip clip, float volume, float pitch, bool loop)
    {
        if (!EnsurePreviewSource())
        {
            return;
        }

        m_previewSource.clip = clip;
        m_previewSource.volume = volume;
        m_previewSource.pitch = pitch;
        m_previewSource.loop = loop;
        m_previewSource.spatialBlend = 0.0f;
        m_previewSource.Play();

        m_isUsingAudioSource = true;
    }

    /// <summary>
    /// Unity 内部の AudioUtil で試聴します。
    /// </summary>
    /// <param name="clip">試聴する AudioClip。</param>
    /// <param name="loop">ループ再生するか。</param>
    private static void PlayWithAudioUtil(AudioClip clip, bool loop)
    {
        CacheAudioUtilMethods();
        m_playMethod?.Invoke(null, new object[] { clip, 0, loop });

        m_isUsingAudioSource = false;
    }

    /// <summary>
    /// 現在の再生方式で再生中かを確認します。
    /// </summary>
    /// <returns>
    /// true：再生中です。
    /// false：再生していません。
    /// </returns>
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
    /// 試聴用の AudioSource を用意します。
    /// </summary>
    /// <returns>
    /// true：利用できます。
    /// false：作成に失敗しました。
    /// </returns>
    private static bool EnsurePreviewSource()
    {
        if (m_previewSource != null)
        {
            return true;
        }

        // 保存もヒエラルキー表示もされない試聴用オブジェクトを作る
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
        Stop();

        if (m_previewObject != null)
        {
            UnityEngine.Object.DestroyImmediate(m_previewObject);
        }

        m_previewObject = null;
        m_previewSource = null;
    }
}

#endif