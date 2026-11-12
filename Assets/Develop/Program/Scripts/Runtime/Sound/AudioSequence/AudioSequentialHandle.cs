using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// START・LOOP・ENDをDSP予約再生で接続する再生ハンドルです。
/// </summary>
public sealed class AudioSequentialHandle
{
    private enum PlaybackState
    {
        IDLE,
        STARTING,
        LOOPING,
        ENDING
    }

    // 再生設定と発生地点
    private readonly AudioSequentialAsset m_asset;
    private Transform m_emissionPoint;
    private readonly Vector3 m_fixedPosition;
    private readonly AudioSequentialSpatialMode m_spatialMode;

    // STARTとLOOPは同時に予約されるため、それぞれ独立して保持します。
    private AudioPlayHandle m_startHandle;
    private AudioPlayHandle m_loopHandle;
    private AudioPlayHandle m_endHandle;
    private Coroutine m_endTransitionCoroutine;
    private PlaybackState m_state = PlaybackState.IDLE;

    /// <summary>
    /// 一連の再生が終了した際に通知します。
    /// </summary>
    public event Action<AudioSequentialHandle> Finished;

    /// <summary>いずれかの段階が動作中かを取得します。</summary>
    public bool IsActive => m_state != PlaybackState.IDLE;

    /// <summary>ENDへの切り替えを要求済みかを取得します。</summary>
    public bool IsStopping => m_state == PlaybackState.ENDING;

    /// <summary>
    /// 連続音声の再生設定と発生地点を保存します。
    /// </summary>
    internal AudioSequentialHandle(AudioSequentialAsset asset, Transform emissionPoint)
        : this(asset,
            asset != null ? asset.SpatialMode : AudioSequentialSpatialMode.TWO_DIMENSIONAL,
            emissionPoint, emissionPoint != null ? emissionPoint.position : Vector3.zero)
    {
    }

    /// <summary>
    /// 指定ワールド座標で固定位置3D再生するハンドルを生成します。
    /// </summary>
    internal AudioSequentialHandle(AudioSequentialAsset asset, Vector3 position)
        : this(asset, AudioSequentialSpatialMode.THREE_DIMENSIONAL_AT_POSITION, null, position)
    {
    }

    /// <summary>
    /// 音声設定と再生位置方式を指定してハンドルを生成します。
    /// </summary>
    internal AudioSequentialHandle(AudioSequentialAsset asset, AudioSequentialSpatialMode spatialMode,
        Transform emissionPoint, Vector3 position)
    {
        m_asset = asset;
        m_spatialMode = spatialMode;
        m_emissionPoint = emissionPoint;
        m_fixedPosition = position;
    }

    /// <summary>
    /// STARTとLOOPを先に予約し、無音区間を作らずに接続します。
    /// </summary>
    /// <returns>true：再生開始に成功。false：再生できません。</returns>
    internal bool Begin()
    {
        AudioManager manager = AudioManager.Instance;
        if (m_asset == null || manager == null || m_state != PlaybackState.IDLE)
        {
            return false;
        }

        if (!m_asset.HasStartAudio)
        {
            // 開始音を使わない設定では、ループ音を直ちに開始します。
            m_loopHandle = PlayStageNow(m_asset.LoopAudioID, true);
            if (m_loopHandle == null)
            {
                return false;
            }

            m_state = PlaybackState.LOOPING;
            m_loopHandle.Finished += OnLoopFinished;
            return true;
        }

        if (!manager.TryGetClipDuration(m_asset.StartAudioID, out double startDuration))
        {
            Debug.LogWarning("[AudioSequentialHandle] STARTの音声長が取得できません。AudioClipとPitchを確認してください。");
            return false;
        }

        // 再生終了をUpdateで検出するのではなく、先にLOOPの開始時刻を確定します。
        double startDspTime = AudioSettings.dspTime + m_asset.ScheduleLeadSeconds;
        double loopDspTime = startDspTime + startDuration;
        m_state = PlaybackState.STARTING;

        m_startHandle = PlayStageScheduled(m_asset.StartAudioID, startDspTime, false);
        if (m_startHandle == null)
        {
            FinishSequence();
            return false;
        }

        m_startHandle.Finished += OnStartFinished;
        m_loopHandle = PlayStageScheduled(m_asset.LoopAudioID, loopDspTime, true);
        if (m_loopHandle == null)
        {
            Debug.LogWarning("[AudioSequentialHandle] LOOPの予約に失敗しました。", m_emissionPoint);
            FinishSequence();
            return false;
        }

        m_loopHandle.Finished += OnLoopFinished;
        return true;
    }

    /// <summary>
    /// 継続音からENDへクロスフェードします。
    /// </summary>
    public void Stop()
    {
        if (m_state == PlaybackState.IDLE || m_state == PlaybackState.ENDING)
        {
            return;
        }

        AudioManager manager = AudioManager.Instance;
        if (m_asset == null || !m_asset.HasEndAudio || manager == null)
        {
            StopImmediate();
            return;
        }

        // ENDを先に予約し、START/LOOPをENDの開始まで保持します。
        double endDspTime = AudioSettings.dspTime + m_asset.ScheduleLeadSeconds;
        float crossfade = m_asset.EndCrossfadeSeconds;
        float initialVolume = crossfade > 0.0f ? 0.0f : 1.0f;

        AudioPlayHandle endHandle = PlayStageScheduled(
            m_asset.EndAudioID, endDspTime, false, initialVolume);
        if (endHandle == null)
        {
            Debug.LogWarning("[AudioSequentialHandle] ENDを予約できなかったため、再生を終了します。");
            StopImmediate();
            return;
        }

        m_endHandle = endHandle;
        m_state = PlaybackState.ENDING;
        m_endHandle.Finished += OnEndFinished;

        // STARTとLOOPは、停止要求時に両方生存している場合があります。
        // 特にSTARTの終了直前では、予約済みLOOPを残して無音を防ぎます。
        m_endTransitionCoroutine = manager.StartCoroutine(
            CrossfadeToEnd(endHandle, m_startHandle, m_loopHandle, endDspTime, crossfade));
    }

    /// <summary>
    /// ENDを鳴らさず、予約中も含めてすべての音声を停止します。
    /// </summary>
    public void StopImmediate()
    {
        FinishSequence();
    }

    /// <summary>
    /// 再生中・予約中の各段階を、新しいTransformへ追従させます。
    /// </summary>
    /// <param name="newTarget">新しい追従先。</param>
    /// <returns>true：更新成功。false：追従再生ではない、または参照が無効。</returns>
    public bool ChangeFollowTarget(Transform newTarget)
    {
        if (m_state == PlaybackState.IDLE ||
            m_spatialMode != AudioSequentialSpatialMode.THREE_DIMENSIONAL_FOLLOWING ||
            newTarget == null)
        {
            return false;
        }

        m_emissionPoint = newTarget;
        m_startHandle?.ChangeFollowTarget(newTarget);
        m_loopHandle?.ChangeFollowTarget(newTarget);
        m_endHandle?.ChangeFollowTarget(newTarget);
        return true;
    }

    /// <summary>
    /// 予約時刻から終了音へ音量を移し、旧音声を解放します。
    /// </summary>
    private IEnumerator CrossfadeToEnd(AudioPlayHandle endHandle, AudioPlayHandle startHandle,
        AudioPlayHandle loopHandle, double endDspTime, float duration)
    {
        // ENDの実際の開始時刻までは旧音声を維持します。
        while (m_state == PlaybackState.ENDING && endHandle != null && endHandle.IsActive &&
            AudioSettings.dspTime < endDspTime)
        {
            yield return null;
        }

        if (m_state != PlaybackState.ENDING || endHandle == null || !endHandle.IsActive)
        {
            yield break;
        }

        if (duration > 0.0f)
        {
            while (m_state == PlaybackState.ENDING && endHandle.IsActive)
            {
                float progress = Mathf.Clamp01((float)((AudioSettings.dspTime - endDspTime) / duration));
                endHandle.SetVolumeMultiplier(progress);
                SetOldVolumes(startHandle, loopHandle, 1.0f - progress);

                if (progress >= 1.0f)
                {
                    break;
                }

                yield return null;
            }
        }

        // ENDの音量を元に戻し、旧音声はプールへ返します。
        if (endHandle.IsActive)
        {
            endHandle.SetVolumeMultiplier(1.0f);
        }

        ReleaseOldStages(startHandle, loopHandle);
        m_endTransitionCoroutine = null;
    }

    /// <summary>
    /// STARTとLOOPの音量を同じ倍率で更新します。
    /// </summary>
    private static void SetOldVolumes(AudioPlayHandle startHandle, AudioPlayHandle loopHandle, float multiplier)
    {
        if (startHandle != null && startHandle.IsActive)
        {
            startHandle.SetVolumeMultiplier(multiplier);
        }

        if (loopHandle != null && loopHandle.IsActive)
        {
            loopHandle.SetVolumeMultiplier(multiplier);
        }
    }

    /// <summary>
    /// ENDへ移行した後の旧音声を停止します。
    /// </summary>
    private void ReleaseOldStages(AudioPlayHandle startHandle, AudioPlayHandle loopHandle)
    {
        if (ReferenceEquals(m_startHandle, startHandle))
        {
            StopHandle(ref m_startHandle, OnStartFinished);
        }

        if (ReferenceEquals(m_loopHandle, loopHandle))
        {
            StopHandle(ref m_loopHandle, OnLoopFinished);
        }
    }

    /// <summary>
    /// STARTの再生終了時に状態を切り替えます（LOOPの開始は予約済みです）。
    /// </summary>
    private void OnStartFinished(AudioPlayHandle handle, bool completedNaturally)
    {
        if (!ReferenceEquals(handle, m_startHandle))
        {
            return;
        }

        handle.Finished -= OnStartFinished;
        m_startHandle = null;

        if (m_state == PlaybackState.ENDING)
        {
            return;
        }

        if (!completedNaturally || m_loopHandle == null || !m_loopHandle.IsActive)
        {
            FinishSequence();
            return;
        }

        m_state = PlaybackState.LOOPING;
    }

    /// <summary>
    /// LOOPが外部から停止された場合には連続再生を終了します。
    /// </summary>
    private void OnLoopFinished(AudioPlayHandle handle, bool completedNaturally)
    {
        if (!ReferenceEquals(handle, m_loopHandle))
        {
            return;
        }

        handle.Finished -= OnLoopFinished;
        m_loopHandle = null;

        if (m_state != PlaybackState.ENDING)
        {
            FinishSequence();
        }
    }

    /// <summary>
    /// ENDの自然終了を受け取り、残ったハンドルを解放します。
    /// </summary>
    private void OnEndFinished(AudioPlayHandle handle, bool completedNaturally)
    {
        if (ReferenceEquals(handle, m_endHandle))
        {
            FinishSequence();
        }
    }

    /// <summary>
    /// 現在の発生位置設定に応じて音声を直ちに再生します。
    /// </summary>
    private AudioPlayHandle PlayStageNow(AudioID id, bool loop)
    {
        AudioManager manager = AudioManager.Instance;
        if (manager == null)
        {
            return null;
        }

        switch (m_spatialMode)
        {
            case AudioSequentialSpatialMode.THREE_DIMENSIONAL_AT_POSITION:
                return manager.Play3DAtPosition(id, m_fixedPosition, loop);

            case AudioSequentialSpatialMode.THREE_DIMENSIONAL_FOLLOWING:
                return m_emissionPoint != null
                    ? manager.Play3DFollowing(id, m_emissionPoint, loop)
                    : null;

            default:
                return manager.Play2D(id, loop);
        }
    }

    /// <summary>
    /// 現在の発生位置設定に応じてDSP時刻で音声を予約します。
    /// </summary>
    private AudioPlayHandle PlayStageScheduled(AudioID id, double dspTime, bool loop,
        float volumeMultiplier = 1.0f)
    {
        AudioManager manager = AudioManager.Instance;
        if (manager == null)
        {
            return null;
        }

        switch (m_spatialMode)
        {
            case AudioSequentialSpatialMode.THREE_DIMENSIONAL_AT_POSITION:
                return manager.PlayScheduled3DAtPosition(
                    id, m_fixedPosition, dspTime, loop, volumeMultiplier);

            case AudioSequentialSpatialMode.THREE_DIMENSIONAL_FOLLOWING:
                return m_emissionPoint != null
                    ? manager.PlayScheduled3DFollowing(
                        id, m_emissionPoint, dspTime, loop, volumeMultiplier)
                    : null;

            default:
                return manager.PlayScheduled2D(id, dspTime, loop, volumeMultiplier);
        }
    }

    /// <summary>
    /// 音声を停止して終了通知を解除します。
    /// </summary>
    private static void StopHandle(ref AudioPlayHandle handle, Action<AudioPlayHandle, bool> onFinished)
    {
        AudioPlayHandle oldHandle = handle;
        handle = null;
        if (oldHandle == null)
        {
            return;
        }

        oldHandle.Finished -= onFinished;
        oldHandle.Stop();
    }

    /// <summary>
    /// 再生終了を一度だけ通知します。
    /// </summary>
    private void FinishSequence()
    {
        if (m_state == PlaybackState.IDLE)
        {
            return;
        }

        m_state = PlaybackState.IDLE;

        AudioManager manager = AudioManager.Instance;
        if (m_endTransitionCoroutine != null && manager != null)
        {
            manager.StopCoroutine(m_endTransitionCoroutine);
            m_endTransitionCoroutine = null;
        }

        StopHandle(ref m_startHandle, OnStartFinished);
        StopHandle(ref m_loopHandle, OnLoopFinished);
        StopHandle(ref m_endHandle, OnEndFinished);

        Action<AudioSequentialHandle> finishedAction = Finished;
        Finished = null;
        finishedAction?.Invoke(this);
    }
}
