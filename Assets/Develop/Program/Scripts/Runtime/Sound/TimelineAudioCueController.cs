using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// Timeline SignalからAudioCueIdを再生し、このTimelineで開始したシーケンスを管理します。
/// 単発SEはTimeline終了後も自然終了まで再生します。
/// </summary>
[DisallowMultipleComponent]
public sealed class TimelineAudioCueController : MonoBehaviour
{
    /// <summary>このTimelineで開始した連続音声の記録です。</summary>
    private sealed class PlaybackRecord
    {
        public AudioCueId CueId;
        public AudioSequentialHandle Handle;
    }

    [SerializeField, Header("音声演出の発生元")]
    private AudioEmitter m_audioEmitter;

    [SerializeField, Header("監視するPlayableDirector")]
    private PlayableDirector m_director;

    [SerializeField, Header("Timeline終了時に開始済みシーケンスをENDへ移行する")]
    private bool m_stopSequencesOnDirectorStopped = true;

    // Timelineから開始したシーケンスだけを保持します。
    private readonly List<PlaybackRecord> m_activeSequences = new List<PlaybackRecord>();

    private void Awake()
    {
        if (m_audioEmitter == null)
        {
            m_audioEmitter = GetComponentInParent<AudioEmitter>(true);
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
    /// Timeline SignalからAudioCueIdの数値を指定して再生します。
    /// </summary>
    /// <param name="cueId">AudioCueIdの数値。</param>
    public void PlayCue(int cueId)
    {
        if (!TryGetCueId(cueId, out AudioCueId id) || !TryGetEmitter(out AudioEmitter emitter))
        {
            return;
        }

        if (!emitter.TryPlayCue(id, out AudioSequentialHandle sequenceHandle, out _))
        {
            return;
        }

        // 単発SEはAudioManagerで自然終了するためTimeline側では保持しません。
        if (sequenceHandle != null)
        {
            m_activeSequences.Add(new PlaybackRecord { CueId = id, Handle = sequenceHandle });
            sequenceHandle.Finished += OnSequenceFinished;
        }
    }

    /// <summary>
    /// Timelineで開始した指定Cueのシーケンスを終了音へ遷移します。
    /// </summary>
    /// <param name="cueId">AudioCueIdの数値。</param>
    public void StopCue(int cueId)
    {
        if (!TryGetCueId(cueId, out AudioCueId id)) return;
        StopMatchingSequences(id, false);
    }

    /// <summary>
    /// Timelineで開始した指定Cueのシーケンスを即停止します。
    /// </summary>
    /// <param name="cueId">AudioCueIdの数値。</param>
    public void StopCueImmediate(int cueId)
    {
        if (!TryGetCueId(cueId, out AudioCueId id)) return;
        StopMatchingSequences(id, true);
    }

    /// <summary>
    /// このTimelineで開始したすべてのシーケンスをENDへ移行します。
    /// </summary>
    public void StopAllSequences()
    {
        PlaybackRecord[] records = m_activeSequences.ToArray();
        foreach (PlaybackRecord record in records)
        {
            if (record.Handle != null && record.Handle.IsActive)
            {
                record.Handle.Stop();
            }
        }
    }

    /// <summary>
    /// このTimelineで開始したすべてのシーケンスを即停止します。
    /// </summary>
    public void StopAllSequencesImmediate()
    {
        PlaybackRecord[] records = m_activeSequences.ToArray();
        foreach (PlaybackRecord record in records)
        {
            record.Handle?.StopImmediate();
        }
    }

    /// <summary>指定Cueのシーケンスだけを停止します。</summary>
    private void StopMatchingSequences(AudioCueId cueId, bool immediate)
    {
        PlaybackRecord[] records = m_activeSequences.ToArray();
        foreach (PlaybackRecord record in records)
        {
            if (record.CueId != cueId || record.Handle == null || !record.Handle.IsActive)
            {
                continue;
            }

            if (immediate)
            {
                record.Handle.StopImmediate();
            }
            else
            {
                record.Handle.Stop();
            }
        }
    }

    /// <summary>音声終了時にこのTimelineの追跡一覧から削除します。</summary>
    private void OnSequenceFinished(AudioSequentialHandle handle)
    {
        handle.Finished -= OnSequenceFinished;
        for (int i = m_activeSequences.Count - 1; i >= 0; i--)
        {
            if (ReferenceEquals(m_activeSequences[i].Handle, handle))
            {
                m_activeSequences.RemoveAt(i);
            }
        }
    }

    /// <summary>PlayableDirectorの終了時に設定に応じてシーケンスを停止します。</summary>
    private void OnDirectorStopped(PlayableDirector director)
    {
        if (director == m_director && m_stopSequencesOnDirectorStopped)
        {
            StopAllSequences();
        }
    }

    /// <summary>AudioCueIdの数値を安全に変換します。</summary>
    private bool TryGetCueId(int value, out AudioCueId cueId)
    {
        cueId = AudioCueId.NONE;
        if (value == (int)AudioCueId.NONE || !Enum.IsDefined(typeof(AudioCueId), value))
        {
            Debug.LogWarning($"[TimelineAudioCueController] 未定義のAudioCueIdです: {value}", this);
            return false;
        }

        cueId = (AudioCueId)value;
        return true;
    }

    /// <summary>再生先AudioEmitterを取得します。</summary>
    private bool TryGetEmitter(out AudioEmitter emitter)
    {
        if (m_audioEmitter == null)
        {
            m_audioEmitter = GetComponentInParent<AudioEmitter>(true);
        }

        emitter = m_audioEmitter;
        if (emitter != null) return true;

        Debug.LogWarning("[TimelineAudioCueController] AudioEmitterが見つかりません。", this);
        return false;
    }

    private void OnDisable()
    {
        if (m_director != null)
        {
            m_director.stopped -= OnDirectorStopped;
        }

        StopAllSequencesImmediate();
    }
}
