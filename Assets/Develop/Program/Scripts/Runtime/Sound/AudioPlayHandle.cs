using System;
using UnityEngine;

/// <summary>
/// 1回分の音声再生を操作するハンドルです。
/// </summary>
public class AudioPlayHandle
{
    // プールで再利用される音源ノードと再生識別番号
    private AudioSourceNode m_node;
    private readonly int m_playbackVersion;

    /// <summary>
    /// 音声終了時に発行されます。boolは自然終了かどうかを示します。
    /// </summary>
    public event Action<AudioPlayHandle, bool> Finished;

    /// <summary>
    /// このハンドルの音声が有効かを取得します。
    /// </summary>
    public bool IsActive => HasControl();

    /// <summary>
    /// このハンドルの音声が再生中かを取得します。
    /// </summary>
    public bool IsPlaying => HasControl() && m_node.IsPlaying;

    /// <summary>
    /// このハンドルの音声が一時停止中かを取得します。
    /// </summary>
    public bool IsPaused => HasControl() && m_node.IsPaused;

    /// <summary>
    /// 指定した再生ノードのハンドルを生成します。
    /// </summary>
    /// <param name="node">再生ノード。</param>
    public AudioPlayHandle(AudioSourceNode node)
    {
        m_node = node;
        m_playbackVersion = node != null ? node.PlaybackVersion : 0;
    }

    /// <summary>
    /// この音声を停止します。
    /// </summary>
    public void Stop()
    {
        if (HasControl()) m_node.StopAndReturnToPool();
    }

    /// <summary>
    /// この音声を一時停止します。
    /// </summary>
    public void Pause()
    {
        if (HasControl()) m_node.Pause();
    }

    /// <summary>
    /// この音声の一時停止を解除します。
    /// </summary>
    public void Resume()
    {
        if (HasControl()) m_node.Resume();
    }

    /// <summary>
    /// この再生にだけ音量倍率を適用します。
    /// </summary>
    /// <param name="multiplier">元音量に対する倍率（0〜1）。</param>
    public void SetVolumeMultiplier(float multiplier)
    {
        if (HasControl()) m_node.SetVolumeMultiplier(multiplier);
    }

    /// <summary>
    /// この音声の追従先を変更します。
    /// </summary>
    /// <param name="newTarget">新しい追従先。</param>
    public void ChangeFollowTarget(Transform newTarget)
    {
        if (HasControl()) m_node.SetFollowTarget(newTarget);
    }

    /// <summary>
    /// 再生終了を通知し、このハンドルを無効化します。
    /// </summary>
    /// <param name="completedNaturally">自然終了かどうか。</param>
    internal void NotifyFinished(bool completedNaturally)
    {
        m_node = null;

        // 外部からの再入を考慮し、通知前に購読先を退避します。
        Action<AudioPlayHandle, bool> finishedAction = Finished;
        Finished = null;
        finishedAction?.Invoke(this, completedNaturally);
    }

    /// <summary>
    /// 同じプールノードが別の再生に利用された場合は操作を拒否します。
    /// </summary>
    private bool HasControl()
    {
        return m_node != null &&
            m_node.PlaybackVersion == m_playbackVersion &&
            m_node.IsPlaybackActive;
    }
}
