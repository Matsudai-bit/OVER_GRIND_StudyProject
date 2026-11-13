using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// AudioCueMarkerの通知を受け取り、AudioEmitterへ再生・停止を要求します。
/// このTimelineから開始したシーケンスのみ停止対象として保持します。
/// </summary>
[DisallowMultipleComponent]
public sealed class AudioCueTimelineReceiver : MonoBehaviour, INotificationReceiver
{
    /// <summary>Timelineが開始したシーケンスの記録です。</summary>
    private sealed class SequenceRecord
    {
        public AudioCueId CueId;
        public AudioSequentialHandle Handle;
    }

    [SerializeField, Header("音声演出の発生元")]
    private AudioEmitter m_audioEmitter;

    [SerializeField, Header("対象のPlayableDirector")]
    private PlayableDirector m_director;

    [SerializeField, Header("Timeline停止時にシーケンスをENDへ移行")]
    private bool m_stopSequencesOnDirectorStopped = true;

    // 単発SEは保持せず、シーケンスの再生ハンドルだけを管理します。
    private readonly List<SequenceRecord> m_activeSequences = new List<SequenceRecord>();

    private void Awake()
    {
        if (m_audioEmitter == null)
        {
            m_audioEmitter = GetComponent<AudioEmitter>();
            if (m_audioEmitter == null)
            {
                m_audioEmitter = GetComponentInParent<AudioEmitter>(true);
            }
        }

        if (m_director == null)
        {
            m_director = GetComponent<PlayableDirector>();
            if (m_director == null)
            {
                m_director = GetComponentInParent<PlayableDirector>(true);
            }
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
    /// Timelineから受信したAudioCueMarkerを実行します。
    /// </summary>
    /// <param name="origin">通知元Playable。</param>
    /// <param name="notification">受信したMarker。</param>
    /// <param name="context">通知の付加情報。</param>
    public void OnNotify(Playable origin, INotification notification, object context)
    {
        // Timelineウィンドウ上でのプレビュー操作では音を鳴らしません。
        if (!Application.isPlaying || !(notification is AudioCueMarker marker))
        {
            return;
        }

        AudioCueId cueId = marker.CueId;
        if (cueId == AudioCueId.NONE || !Enum.IsDefined(typeof(AudioCueId), cueId))
        {
            Debug.LogWarning($"[AudioCueTimelineReceiver] 未定義のCueIDです: {cueId}", this);
            return;
        }

        switch (marker.Action)
        {
            case AudioCueTimelineAction.PLAY:
                PlayCue(cueId);
                break;

            case AudioCueTimelineAction.STOP:
                StopCue(cueId, false);
                break;

            case AudioCueTimelineAction.STOP_IMMEDIATE:
                StopCue(cueId, true);
                break;

            default:
                Debug.LogWarning($"[AudioCueTimelineReceiver] 未対応の操作です: {marker.Action}", this);
                break;
        }
    }

    /// <summary>
    /// CueIDに登録された単発SEまたはシーケンスを再生します。
    /// </summary>
    /// <param name="cueId">再生する演出ID。</param>
    private void PlayCue(AudioCueId cueId)
    {
        if (m_audioEmitter == null)
        {
            Debug.LogWarning("[AudioCueTimelineReceiver] AudioEmitterが設定されていません。", this);
            return;
        }

        if (!m_audioEmitter.TryPlayCue(cueId, out AudioSequentialHandle sequenceHandle,
                out _))
        {
            // AudioEmitter側で重複再生防止や未登録IDの判定を行います。
            return;
        }

        // 単発SEはAudioManagerが自然終了まで管理します。
        if (sequenceHandle == null)
        {
            return;
        }

        m_activeSequences.Add(new SequenceRecord { CueId = cueId, Handle = sequenceHandle });
        sequenceHandle.Finished += OnSequenceFinished;
    }

    /// <summary>
    /// このTimelineから開始した指定Cueのシーケンスを停止します。
    /// </summary>
    /// <param name="cueId">停止対象の演出ID。</param>
    /// <param name="immediate">true：即時停止。false：ENDへ移行。</param>
    private void StopCue(AudioCueId cueId, bool immediate)
    {
        // Stop処理中にFinishedイベントでリストが変わる可能性があるためコピーします。
        SequenceRecord[] records = m_activeSequences.ToArray();
        foreach (SequenceRecord record in records)
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

    /// <summary>
    /// このTimelineから開始したシーケンスをENDへ移行させます。
    /// </summary>
    public void StopAllSequences()
    {
        SequenceRecord[] records = m_activeSequences.ToArray();
        foreach (SequenceRecord record in records)
        {
            if (record.Handle != null && record.Handle.IsActive)
            {
                record.Handle.Stop();
            }
        }
    }

    /// <summary>
    /// このTimelineから開始したシーケンスを即時停止します。
    /// </summary>
    public void StopAllSequencesImmediate()
    {
        // イベント解除後に停止し、同じハンドルを二重処理しないようにします。
        SequenceRecord[] records = m_activeSequences.ToArray();
        m_activeSequences.Clear();
        foreach (SequenceRecord record in records)
        {
            if (record.Handle == null)
            {
                continue;
            }

            record.Handle.Finished -= OnSequenceFinished;
            if (record.Handle.IsActive)
            {
                record.Handle.StopImmediate();
            }
        }
    }

    /// <summary>
    /// シーケンスの終了時に追跡対象から除外します。
    /// </summary>
    /// <param name="handle">終了した再生ハンドル。</param>
    private void OnSequenceFinished(AudioSequentialHandle handle)
    {
        if (handle == null)
        {
            return;
        }

        handle.Finished -= OnSequenceFinished;
        for (int i = m_activeSequences.Count - 1; i >= 0; i--)
        {
            if (ReferenceEquals(m_activeSequences[i].Handle, handle))
            {
                m_activeSequences.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// PlayableDirectorが停止したとき、設定に応じてシーケンスを終了させます。
    /// </summary>
    /// <param name="director">停止したPlayableDirector。</param>
    private void OnDirectorStopped(PlayableDirector director)
    {
        if (director == m_director && m_stopSequencesOnDirectorStopped)
        {
            StopAllSequences();
        }
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
