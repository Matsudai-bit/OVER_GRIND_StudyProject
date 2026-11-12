using System;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 音声の個別再生、追従、およびプールへの返却を管理します。
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AudioSourceNode : MonoBehaviour
{
    private const float DEFAULT_MIN_DISTANCE = 1.0f;
    private const float DEFAULT_MAX_DISTANCE = 30.0f;
    private const float MIN_VALID_DISTANCE = 0.01f;
    private const float DISTANCE_EPSILON = 0.01f;
    private const float SPATIAL_BLEND_3D = 1.0f;
    private const float SPATIAL_BLEND_2D = 0.0f;

    // 再生処理で利用する AudioSource
    private AudioSource m_audioSource;

    // SoundDatabaseで指定した音量の初期値
    private float m_baseVolume = 1.0f;

    // 追従対象と追従中フラグ
    private Transform m_followTarget;
    private bool m_isFollowing;

    // 再生状態とプール再利用時の識別番号
    private bool m_isPlaybackActive;
    private bool m_isPaused;
    private int m_playbackVersion;

    // DSP予約再生の開始・終了予定時間
    private bool m_isScheduledPlayback;
    private double m_scheduledStartDspTime;
    private double m_scheduledEndDspTime;

    // プール返却と再生終了通知
    private Action<AudioSourceNode> m_onReturnPool;
    private Action<bool> m_onPlaybackFinished;

    /// <summary>
    /// 現在の再生が有効かを取得します。
    /// </summary>
    public bool IsPlaybackActive => m_isPlaybackActive;

    /// <summary>
    /// 現在の音声が実際に再生中かを取得します。
    /// </summary>
    public bool IsPlaying => m_isPlaybackActive && !m_isPaused && Source.isPlaying;

    /// <summary>
    /// 現在の音声が一時停止中かを取得します。
    /// </summary>
    public bool IsPaused => m_isPlaybackActive && m_isPaused;

    /// <summary>
    /// 現在追従中のTransformを取得します。
    /// </summary>
    public Transform FollowTarget => m_isPlaybackActive && m_isFollowing ? m_followTarget : null;

    /// <summary>
    /// 現在の音声カテゴリを取得します。
    /// </summary>
    public SoundCategory Category { get; private set; }

    /// <summary>
    /// プール再利用時に更新される識別番号を取得します。
    /// </summary>
    public int PlaybackVersion => m_playbackVersion;

    /// <summary>
    /// AudioSource を取得します。
    /// </summary>
    private AudioSource Source
    {
        get
        {
            if (m_audioSource == null)
            {
                m_audioSource = GetComponent<AudioSource>();
                m_audioSource.playOnAwake = false;
            }

            return m_audioSource;
        }
    }

    /// <summary>
    /// 音声の再生設定を初期化します。
    /// </summary>
    /// <param name="data">登録済み音声データ。</param>
    /// <param name="mixerGroup">出力するMixerGroup。</param>
    /// <param name="returnAction">プールへの返却処理。</param>
    public void Setup(SoundDatabase.SoundData data, AudioMixerGroup mixerGroup, Action<AudioSourceNode> returnAction)
    {
        Source.Stop();
        ResetTarget();

        m_isPlaybackActive = false;
        m_isPaused = false;
        m_isScheduledPlayback = false;
        m_scheduledStartDspTime = 0.0;
        m_scheduledEndDspTime = 0.0;
        m_onReturnPool = returnAction;
        m_onPlaybackFinished = null;
        m_playbackVersion++;
        Category = data.m_category;

        // 前回の再生設定に依存しないように毎回設定します。
        Source.clip = data.m_clip;
        m_baseVolume = data.m_volume;
        Source.volume = m_baseVolume;
        Source.pitch = data.m_pitch;
        Source.loop = data.m_loop;
        Source.outputAudioMixerGroup = mixerGroup;
        // プールから再利用されても、前の音の距離減衰設定を引き継がないよう初期化します。
        // 既存データに未設定の新規フィールドがある場合も従来値で動作させます。
        float minDistance = data.m_minDistance > 0.0f
            ? data.m_minDistance : DEFAULT_MIN_DISTANCE;
        float maxDistance = data.m_maxDistance > 0.0f
            ? data.m_maxDistance : DEFAULT_MAX_DISTANCE;
        Source.minDistance = Mathf.Max(MIN_VALID_DISTANCE, minDistance);
        Source.maxDistance = Mathf.Max(Source.minDistance + DISTANCE_EPSILON, maxDistance);
        Source.rolloffMode = data.m_rolloffMode;
        if (data.m_rolloffMode == AudioRolloffMode.Custom)
        {
            AnimationCurve curve = data.m_customRolloffCurve;
            if (curve == null || curve.length == 0)
            {
                // 未設定のカーブでは意図せず無音になるため、直線減衰を使用します。
                curve = AnimationCurve.Linear(0.0f, 1.0f, 1.0f, 0.0f);
            }

            Source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, curve);
        }

        Source.spatialBlend = data.m_is3D ? SPATIAL_BLEND_3D : SPATIAL_BLEND_2D;
    }

    /// <summary>
    /// 再生終了時の通知先を登録します。
    /// </summary>
    /// <param name="onFinished">trueは自然終了、falseは強制終了。</param>
    public void SetPlaybackFinishedCallback(Action<bool> onFinished)
    {
        m_onPlaybackFinished = onFinished;
    }

    /// <summary>
    /// 今回の再生に限りループ設定を上書きします。
    /// </summary>
    /// <param name="loop">ループの有効・無効。</param>
    public void SetLoop(bool loop)
    {
        Source.loop = loop;
    }

    /// <summary>
    /// 登録された元の音量に対して個別再生用の倍率を適用します。
    /// </summary>
    /// <param name="multiplier">元音量に対する倍率（0〜1）。</param>
    public void SetVolumeMultiplier(float multiplier)
    {
        Source.volume = m_baseVolume * Mathf.Clamp01(multiplier);
    }

    /// <summary>
    /// 2D音声を再生します。
    /// </summary>
    public void Play2D()
    {
        ResetTarget();
        Source.spatialBlend = SPATIAL_BLEND_2D;
        StartPlayback();
    }

    /// <summary>
    /// 指定した地点で3D音声を再生します。
    /// </summary>
    /// <param name="position">再生位置。</param>
    public void Play3DAtPosition(Vector3 position)
    {
        ResetTarget();
        transform.position = position;
        Source.spatialBlend = SPATIAL_BLEND_3D;
        StartPlayback();
    }

    /// <summary>
    /// 指定したTransformに追従して3D音声を再生します。
    /// </summary>
    /// <param name="target">追従先。</param>
    public void Play3DFollowing(Transform target)
    {
        m_followTarget = target;
        m_isFollowing = true;
        transform.position = target.position;
        Source.spatialBlend = SPATIAL_BLEND_3D;
        StartPlayback();
    }

    /// <summary>
    /// DSP時間を指定して2D音声を予約します。
    /// </summary>
    public void Schedule2D(double dspTime)
    {
        ResetTarget();
        Source.spatialBlend = SPATIAL_BLEND_2D;
        SchedulePlayback(dspTime);
    }

    /// <summary>
    /// DSP時間を指定して3D音声を固定位置で予約します。
    /// </summary>
    public void Schedule3DAtPosition(Vector3 position, double dspTime)
    {
        ResetTarget();
        transform.position = position;
        Source.spatialBlend = SPATIAL_BLEND_3D;
        SchedulePlayback(dspTime);
    }

    /// <summary>
    /// DSP時間を指定して3D追従音声を予約します。
    /// </summary>
    public void Schedule3DFollowing(Transform target, double dspTime)
    {
        m_followTarget = target;
        m_isFollowing = true;
        transform.position = target.position;
        Source.spatialBlend = SPATIAL_BLEND_3D;
        SchedulePlayback(dspTime);
    }

    /// <summary>
    /// AudioSettings.dspTimeを基準に再生開始を予約します。
    /// </summary>
    private void SchedulePlayback(double dspTime)
    {
        m_isPlaybackActive = true;
        m_isScheduledPlayback = true;
        m_scheduledStartDspTime = dspTime;

        // 非ループ音源の再生時間はクリップ長とPitchで決まります。
        if (Source.clip != null && Source.clip.frequency > 0 && Mathf.Abs(Source.pitch) > 0.0001f)
        {
            double duration = (double)Source.clip.samples / Source.clip.frequency / Mathf.Abs(Source.pitch);
            m_scheduledEndDspTime = dspTime + duration;
        }
        else
        {
            m_scheduledEndDspTime = dspTime;
        }

        Source.PlayScheduled(dspTime);
    }

    /// <summary>
    /// 再生中の追従先を変更します。
    /// </summary>
    /// <param name="newTarget">新しい追従先。</param>
    public void SetFollowTarget(Transform newTarget)
    {
        if (m_isPlaybackActive && m_isFollowing && newTarget != null)
        {
            m_followTarget = newTarget;
            transform.position = newTarget.position;
        }
    }

    /// <summary>
    /// 音声を一時停止します。
    /// </summary>
    public void Pause()
    {
        if (!m_isPlaybackActive || m_isPaused) return;

        Source.Pause();
        m_isPaused = true;
    }

    /// <summary>
    /// 一時停止中の音声を再開します。
    /// </summary>
    public void Resume()
    {
        if (!m_isPlaybackActive || !m_isPaused) return;

        Source.UnPause();
        m_isPaused = false;
    }

    /// <summary>
    /// 音声を強制終了してプールへ返却します。
    /// </summary>
    public void StopAndReturnToPool()
    {
        FinishPlayback(false);
    }

    /// <summary>
    /// 音声再生を開始します。
    /// </summary>
    private void StartPlayback()
    {
        m_isPlaybackActive = true;
        m_isScheduledPlayback = false;
        Source.Play();
    }

    private void Update()
    {
        if (!m_isPlaybackActive) return;

        // 追従が必要な音声のみTransformを更新します。
        if (m_isFollowing)
        {
            if (m_followTarget == null)
            {
                StopAndReturnToPool();
                return;
            }

            transform.position = m_followTarget.position;
        }

        // Pause中やAudioListenerの一時停止中は自然終了と判定しません。
        if (m_isPaused || (AudioListener.pause && !Source.ignoreListenerPause)) return;

        // 予約開始前のisPlaying=falseを「自然終了」と誤判定しないようにします。
        if (m_isScheduledPlayback && AudioSettings.dspTime < m_scheduledStartDspTime)
        {
            return;
        }

        // 予約済み音声は終了予定のDSP時刻まではプールへ返却しません。
        if (!Source.loop &&
            (!m_isScheduledPlayback || AudioSettings.dspTime >= m_scheduledEndDspTime) &&
            !Source.isPlaying)
        {
            FinishPlayback(true);
        }
    }

    /// <summary>
    /// プールに返却してから終了通知を発行します。
    /// </summary>
    /// <param name="completedNaturally">自然終了かどうか。</param>
    private void FinishPlayback(bool completedNaturally)
    {
        if (!m_isPlaybackActive) return;

        m_isPlaybackActive = false;
        m_isPaused = false;
        m_isScheduledPlayback = false;
        Source.Stop();
        Source.clip = null;
        ResetTarget();

        // コールバックは今回の再生に限ります。
        Action<AudioSourceNode> returnAction = m_onReturnPool;
        Action<bool> finishedAction = m_onPlaybackFinished;
        m_onReturnPool = null;
        m_onPlaybackFinished = null;

        gameObject.SetActive(false);
        returnAction?.Invoke(this);
        finishedAction?.Invoke(completedNaturally);
    }

    /// <summary>
    /// 追従先を初期化します。
    /// </summary>
    private void ResetTarget()
    {
        m_followTarget = null;
        m_isFollowing = false;
    }
}
