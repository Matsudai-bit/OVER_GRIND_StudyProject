using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// Unity TimelineのSignalやPlayableDirectorの終了に連続音声を連動させます。
/// </summary>
[DisallowMultipleComponent]
public sealed class TimelineAudioSequentialController : MonoBehaviour
{
    [SerializeField, Header("再生する音声セット")]
    private AudioSequentialAsset m_audioAsset;

    [SerializeField, Header("音声再生コンポーネント")]
    private SequentialAudioPlayer m_audioPlayer;

    [SerializeField, Header("発生位置（未指定時はPlayer既定値）")]
    private Transform m_emissionPoint;

    [SerializeField, Header("終了検知対象のPlayableDirector")]
    private PlayableDirector m_director;

    // このTimelineから開始した再生だけを保持します。
    private AudioSequentialHandle m_currentHandle;

    private void Awake()
    {
        if (m_audioPlayer == null)
        {
            m_audioPlayer = GetComponent<SequentialAudioPlayer>();
        }

        if (m_director == null)
        {
            m_director = GetComponent<PlayableDirector>();
        }
    }

    private void OnEnable()
    {
        if (m_director != null)
        {
            m_director.stopped += OnDirectorStopped;
        }
    }

    /// <summary>
    /// Timeline Signalから呼び出す再生メソッドです。
    /// </summary>
    public void Play()
    {
        if (m_audioAsset == null || m_audioPlayer == null)
        {
            Debug.LogWarning("[TimelineAudioSequentialController] AssetまたはSequentialAudioPlayerがありません。", this);
            return;
        }

        if (m_currentHandle != null && m_currentHandle.IsActive)
        {
            return;
        }

        m_currentHandle = m_audioPlayer.Play(m_audioAsset, m_emissionPoint);
    }

    /// <summary>
    /// Timeline Signalから呼び出す終了メソッドです。
    /// </summary>
    public void Stop()
    {
        m_currentHandle?.Stop();
    }

    /// <summary>
    /// 終了音なしで即座に停止します。
    /// </summary>
    public void StopImmediate()
    {
        m_currentHandle?.StopImmediate();
        m_currentHandle = null;
    }

    /// <summary>
    /// PlayableDirectorが終了した場合、設定に従って停止します。
    /// </summary>
    private void OnDirectorStopped(PlayableDirector director)
    {
        if (director == m_director && m_audioAsset != null && m_audioAsset.StopOnAnimationEnd)
        {
            Stop();
        }
    }

    private void OnDisable()
    {
        if (m_director != null)
        {
            m_director.stopped -= OnDirectorStopped;
        }

        StopImmediate();
    }
}
