using System.Collections;
using UnityEngine;

/// <summary>
/// シーンごとのBGMを再生・停止・一時停止し、音量フェードを制御します。
/// </summary>
[DisallowMultipleComponent]
public sealed class BGMPlayer : MonoBehaviour
{
    private const float MIN_FADE_DURATION = 0.0f;
    private const float SILENT_VOLUME = 0.0f;
    private const float FULL_VOLUME = 1.0f;

    [SerializeField, Header("再生するBGMのID")]
    private AudioID m_bgmId;

    [SerializeField, Header("シーン開始時に自動再生するか")]
    private bool m_playOnStart = true;

    [SerializeField, Header("フェードイン時間（秒）")]
    [Min(MIN_FADE_DURATION)]
    private float m_fadeInDuration = 1.0f;

    [SerializeField, Header("フェードアウト時間（秒）")]
    [Min(MIN_FADE_DURATION)]
    private float m_fadeOutDuration = 1.0f;

    // 再生中のBGMを制御するハンドル
    private AudioPlayHandle m_playHandle;

    // 実行中のフェード処理
    private Coroutine m_fadeCoroutine;

    // 再生中BGMの元音量に掛ける倍率
    private float m_currentVolumeMultiplier = FULL_VOLUME;

    // 停止を目的としたフェードアウト中か
    private bool m_isFadingOut;

    /// <summary>
    /// BGMが再生中か取得します。
    /// </summary>
    public bool IsPlaying => m_playHandle != null && m_playHandle.IsPlaying;

    /// <summary>
    /// BGMが一時停止中か取得します。
    /// </summary>
    public bool IsPaused => m_playHandle != null && m_playHandle.IsPaused;

    /// <summary>
    /// シーン開始時の自動再生を行います。
    /// </summary>
    private void Start()
    {
        if (m_playOnStart)
        {
            Play();
        }
    }

    /// <summary>
    /// 設定した時間でBGMをフェードイン再生します。
    /// </summary>
    public void Play()
    {
        FadeIn(m_fadeInDuration);
    }

    /// <summary>
    /// 指定した秒数でBGMをフェードインします。
    /// </summary>
    /// <param name="duration">フェードにかける秒数。</param>
    public void FadeIn(float duration)
    {
        // 再生済みであれば、同じBGMを再生成せずに復帰・音量復元します。
        if (m_playHandle != null && m_playHandle.IsActive)
        {
            if (m_playHandle.IsPaused)
            {
                m_playHandle.Resume();
            }

            // フェードイン中の再度のPlay()は無視し、フェードアウト中のみ反転します。
            if (m_isFadingOut)
            {
                StartFade(FULL_VOLUME, duration, false);
            }

            return;
        }

        // 使用できなくなったハンドルの参照を破棄します。
        ReleaseHandleReference();

        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("[BGMPlayer] AudioManagerが見つかりません。", this);
            return;
        }

        float initialVolume = duration > MIN_FADE_DURATION
            ? SILENT_VOLUME
            : FULL_VOLUME;

        // 初回サンプルから無音で開始するため、再生前に音量倍率を設定します。
        AudioPlayHandle handle = AudioManager.Instance.Play2D(
            m_bgmId,
            true,
            initialVolume);

        if (handle == null || !handle.IsActive)
        {
            Debug.LogWarning($"[BGMPlayer] BGMを再生できません: {m_bgmId}", this);
            return;
        }

        m_playHandle = handle;
        m_playHandle.Finished += OnPlaybackFinished;
        m_currentVolumeMultiplier = initialVolume;

        if (duration > MIN_FADE_DURATION)
        {
            StartFade(FULL_VOLUME, duration, false);
        }
    }

    /// <summary>
    /// 設定した時間でBGMをフェードアウトして停止します。
    /// </summary>
    public void Stop()
    {
        FadeOut(m_fadeOutDuration);
    }

    /// <summary>
    /// 指定した秒数でフェードアウトし、完了後に停止します。
    /// </summary>
    /// <param name="duration">フェードにかける秒数。</param>
    public void FadeOut(float duration)
    {
        if (m_playHandle == null || !m_playHandle.IsActive)
        {
            CancelFade();
            ReleaseHandleReference();
            m_currentVolumeMultiplier = FULL_VOLUME;
            return;
        }

        // Pause中は音を鳴らしながらのフェードができないため即時停止します。
        if (m_playHandle.IsPaused || duration <= MIN_FADE_DURATION)
        {
            StopImmediate();
            return;
        }

        if (m_isFadingOut)
        {
            return;
        }

        StartFade(SILENT_VOLUME, duration, true);
    }

    /// <summary>
    /// フェードせず即座にBGMを停止します。
    /// </summary>
    public void StopImmediate()
    {
        CancelFade();

        AudioPlayHandle handle = m_playHandle;
        ReleaseHandleReference();

        if (handle != null && handle.IsActive)
        {
            handle.Stop();
        }

        m_currentVolumeMultiplier = FULL_VOLUME;
    }

    /// <summary>
    /// 現在のBGMを一時停止します。
    /// </summary>
    public void Pause()
    {
        if (m_playHandle != null && m_playHandle.IsPlaying)
        {
            m_playHandle.Pause();
        }
    }

    /// <summary>
    /// 現在のBGMの一時停止を解除します。
    /// </summary>
    public void Resume()
    {
        if (m_playHandle != null && m_playHandle.IsPaused)
        {
            m_playHandle.Resume();
        }
    }

    /// <summary>
    /// フェードの始点から指定音量へ移行します。
    /// </summary>
    /// <param name="targetVolume">目標音量倍率。</param>
    /// <param name="duration">遷移時間。</param>
    /// <param name="stopOnComplete">完了時にBGMを停止するか。</param>
    private void StartFade(float targetVolume, float duration, bool stopOnComplete)
    {
        CancelFade();

        if (m_playHandle == null || !m_playHandle.IsActive)
        {
            return;
        }

        float clampedTarget = Mathf.Clamp01(targetVolume);
        float clampedDuration = Mathf.Max(MIN_FADE_DURATION, duration);

        if (clampedDuration <= MIN_FADE_DURATION ||
            Mathf.Approximately(m_currentVolumeMultiplier, clampedTarget))
        {
            SetVolumeMultiplier(clampedTarget);
            if (stopOnComplete)
            {
                StopImmediate();
            }
            return;
        }

        m_isFadingOut = stopOnComplete;
        m_fadeCoroutine = StartCoroutine(FadeRoutine(clampedTarget, clampedDuration, stopOnComplete));
    }

    /// <summary>
    /// 一時停止中は進めず、再生中のみ経過時間に応じて音量を補間します。
    /// </summary>
    private IEnumerator FadeRoutine(float targetVolume, float duration, bool stopOnComplete)
    {
        float startVolume = m_currentVolumeMultiplier;
        float elapsedTime = 0.0f;

        while (elapsedTime < duration)
        {
            if (m_playHandle == null || !m_playHandle.IsActive)
            {
                m_fadeCoroutine = null;
                m_isFadingOut = false;
                yield break;
            }

            if (m_playHandle.IsPaused)
            {
                yield return null;
                continue;
            }

            elapsedTime += Time.unscaledDeltaTime;
            float rate = Mathf.Clamp01(elapsedTime / duration);
            SetVolumeMultiplier(Mathf.Lerp(startVolume, targetVolume, rate));

            yield return null;
        }

        SetVolumeMultiplier(targetVolume);
        m_fadeCoroutine = null;
        m_isFadingOut = false;

        if (stopOnComplete)
        {
            StopImmediate();
        }
    }

    /// <summary>
    /// SoundDatabaseの音量にかける倍率を更新します。
    /// </summary>
    private void SetVolumeMultiplier(float volume)
    {
        m_currentVolumeMultiplier = Mathf.Clamp01(volume);
        if (m_playHandle != null && m_playHandle.IsActive)
        {
            m_playHandle.SetVolumeMultiplier(m_currentVolumeMultiplier);
        }
    }

    /// <summary>
    /// フェード処理があれば中断します。
    /// </summary>
    private void CancelFade()
    {
        if (m_fadeCoroutine != null)
        {
            StopCoroutine(m_fadeCoroutine);
            m_fadeCoroutine = null;
        }

        m_isFadingOut = false;
    }

    /// <summary>
    /// 再生ハンドルの購読と参照を解除します。
    /// </summary>
    private void ReleaseHandleReference()
    {
        if (m_playHandle == null)
        {
            return;
        }

        m_playHandle.Finished -= OnPlaybackFinished;
        m_playHandle = null;
    }

    /// <summary>
    /// 外部から再生を終了された場合の後片付けを行います。
    /// </summary>
    private void OnPlaybackFinished(AudioPlayHandle handle, bool completedNaturally)
    {
        if (!ReferenceEquals(handle, m_playHandle))
        {
            return;
        }

        CancelFade();
        ReleaseHandleReference();
        m_currentVolumeMultiplier = FULL_VOLUME;
    }

    /// <summary>
    /// シーン終了時はフェードを行わず確実に停止します。
    /// </summary>
    private void OnDisable()
    {
        StopImmediate();
    }
}
