using System;
using UnityEngine;

/// <summary>
/// 1組の「開始→ループ→終了」の再生を制御します。
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

    // 再生セットと発生地点
    private readonly AudioSequentialAsset m_asset;
    private readonly Transform m_emissionPoint;
    private readonly Vector3 m_fixedPosition;

    // 現在の再生段階
    private AudioPlayHandle m_currentAudioHandle;
    private PlaybackState m_state = PlaybackState.IDLE;

    /// <summary>
    /// 一連の再生が完全に終了した際に通知します。
    /// </summary>
    public event Action<AudioSequentialHandle> Finished;

    /// <summary>開始音・ループ音・終了音のいずれかが動作中かを取得します。</summary>
    public bool IsActive => m_state != PlaybackState.IDLE;

    /// <summary>終了音への遷移後かを取得します。</summary>
    public bool IsStopping => m_state == PlaybackState.ENDING;

    /// <summary>
    /// 再生セットと音源位置を指定します。
    /// </summary>
    internal AudioSequentialHandle(AudioSequentialAsset asset, Transform emissionPoint)
    {
        m_asset = asset;
        m_emissionPoint = emissionPoint;
        m_fixedPosition = emissionPoint != null ? emissionPoint.position : Vector3.zero;
    }

    /// <summary>
    /// 開始音から再生を開始します。開始音を使用しない場合はループ音から始めます。
    /// </summary>
    /// <returns>true：再生を開始しました。false：失敗しました。</returns>
    internal bool Begin()
    {
        if (m_asset == null || AudioManager.Instance == null)
        {
            Debug.LogWarning("[AudioSequentialHandle] AssetまたはAudioManagerがありません。");
            return false;
        }

        if (m_asset.HasStartAudio)
        {
            return StartStage(m_asset.StartAudioID, false, PlaybackState.STARTING);
        }

        return StartLoop();
    }

    /// <summary>
    /// 現在の音声を中断し、終了音へ移行します。
    /// </summary>
    public void Stop()
    {
        if (m_state == PlaybackState.IDLE || m_state == PlaybackState.ENDING)
        {
            return;
        }

        StopCurrentStage();
        if (m_asset != null && m_asset.HasEndAudio)
        {
            StartStage(m_asset.EndAudioID, false, PlaybackState.ENDING);
        }
        else
        {
            FinishSequence();
        }
    }

    /// <summary>
    /// 終了音を再生せず、直ちに停止します。
    /// </summary>
    public void StopImmediate()
    {
        if (m_state == PlaybackState.IDLE)
        {
            return;
        }

        StopCurrentStage();
        FinishSequence();
    }

    /// <summary>
    /// ループ音を開始します。
    /// </summary>
    private bool StartLoop()
    {
        return StartStage(m_asset.LoopAudioID, true, PlaybackState.LOOPING);
    }

    /// <summary>
    /// 指定した段階の音声を再生し、自然終了通知を受け取ります。
    /// </summary>
    private bool StartStage(AudioID id, bool loop, PlaybackState state)
    {
        AudioManager manager = AudioManager.Instance;
        if (manager == null)
        {
            FinishSequence();
            return false;
        }

        AudioPlayHandle handle;
        switch (m_asset.SpatialMode)
        {
            case AudioSequentialSpatialMode.THREE_DIMENSIONAL_AT_POSITION:
                handle = manager.Play3DAtPosition(id, m_fixedPosition, loop);
                break;

            case AudioSequentialSpatialMode.THREE_DIMENSIONAL_FOLLOWING:
                if (m_emissionPoint == null)
                {
                    Debug.LogWarning("[AudioSequentialHandle] 追従先が破棄されています。");
                    FinishSequence();
                    return false;
                }

                handle = manager.Play3DFollowing(id, m_emissionPoint, loop);
                break;

            default:
                handle = manager.Play2D(id, loop);
                break;
        }

        if (handle == null || !handle.IsActive)
        {
            Debug.LogWarning($"[AudioSequentialHandle] {state}の音声再生に失敗しました: {id}");
            FinishSequence();
            return false;
        }

        m_state = state;
        m_currentAudioHandle = handle;
        m_currentAudioHandle.Finished += OnStageFinished;
        return true;
    }

    /// <summary>
    /// 現在の音声が自然終了した場合のみ次の段階へ進みます。
    /// </summary>
    private void OnStageFinished(AudioPlayHandle handle, bool completedNaturally)
    {
        if (!ReferenceEquals(handle, m_currentAudioHandle))
        {
            return;
        }

        m_currentAudioHandle.Finished -= OnStageFinished;
        m_currentAudioHandle = null;

        if (!completedNaturally)
        {
            FinishSequence();
            return;
        }

        switch (m_state)
        {
            case PlaybackState.STARTING:
                StartLoop();
                break;

            case PlaybackState.ENDING:
            case PlaybackState.LOOPING:
            default:
                FinishSequence();
                break;
        }
    }

    /// <summary>
    /// 現在の音声の終了通知を解除してから停止します。
    /// </summary>
    private void StopCurrentStage()
    {
        if (m_currentAudioHandle == null)
        {
            return;
        }

        AudioPlayHandle handle = m_currentAudioHandle;
        m_currentAudioHandle = null;
        handle.Finished -= OnStageFinished;
        handle.Stop();
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
        Action<AudioSequentialHandle> finishedAction = Finished;
        Finished = null;
        finishedAction?.Invoke(this);
    }
}
