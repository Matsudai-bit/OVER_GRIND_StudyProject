using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Animation Eventから音声を再生し、再生元アニメーション単位で停止を管理します。
/// </summary>
[DisallowMultipleComponent]
public sealed class AnimationAudioCueEventReceiver : MonoBehaviour
{
    /// <summary>
    /// Animation Eventから開始した1回の再生を記録します。
    /// </summary>
    private sealed class PlaybackRecord
    {
        // 再生時のCueIDと、別の再生との区別に用いるチャンネル。
        public AudioCueId CueId;
        public int Channel;
        public int StateFullPathHash;

        // 再生時の停止設定。Assetを後から編集しても再生中の設定は変えません。
        public AnimationAudioCueStopTiming StopTiming;
        public AnimationAudioCueStopMode StopMode;

        // 単発音声と連続音声のうち、再生した方だけ保持します。
        public AudioSequentialHandle SequenceHandle;
        public AudioPlayHandle OneShotHandle;
    }

    // 音源の発生元。Animatorが子のModelにある場合も親から取得します。
    [SerializeField, Header("音声演出の発生元（省略時は親から検索）")]
    private AudioEmitter m_audioEmitter;

    // 既存のInt ParameterでCueIDを直接指定するAnimation Eventの移行用。
    [SerializeField, Header("旧Int Parameter方式を許可（移行用）")]
    private bool m_allowLegacyIntParameter = true;

    // このReceiverが開始した再生だけを追跡します。
    private readonly List<PlaybackRecord> m_activeRecords = new List<PlaybackRecord>();
    private bool m_hasLoggedLegacyWarning;

    /// <summary>
    /// Animation EventのObject Parameterに指定したCueを再生します。
    /// </summary>
    /// <param name="animationEvent">ObjectにCue Reference、Intにチャンネルを設定します。</param>
    public void OnAnimationAudioCuePlay(AnimationEvent animationEvent)
    {
        if (!TryGetCue(animationEvent, out AudioCueId cueId,
                out AudioCueReferenceAsset reference, out int channel) ||
            !TryGetEmitter(out AudioEmitter emitter))
        {
            return;
        }

        // 同じIDを別のアニメーションから再生しても独立したハンドルを保持します。
        if (!emitter.TryPlayCue(cueId, out AudioSequentialHandle sequenceHandle,
                out AudioPlayHandle oneShotHandle))
        {
            return;
        }

        PlaybackRecord record = new PlaybackRecord
        {
            CueId = cueId,
            Channel = channel,
            StateFullPathHash = animationEvent.isFiredByAnimator
                ? animationEvent.animatorStateInfo.fullPathHash : 0,
            StopTiming = reference != null
                ? reference.StopTiming : AnimationAudioCueStopTiming.MANUAL,
            StopMode = reference != null
                ? reference.StopMode : AnimationAudioCueStopMode.GRACEFUL,
            SequenceHandle = sequenceHandle,
            OneShotHandle = oneShotHandle
        };

        // 自然終了時に追跡対象から外します。OneShotは自然終了を待つこともできます。
        m_activeRecords.Add(record);
        if (sequenceHandle != null)
        {
            sequenceHandle.Finished += OnSequenceFinished;
        }
        if (oneShotHandle != null)
        {
            oneShotHandle.Finished += OnOneShotFinished;
        }
    }

    /// <summary>
    /// 同じCueID・チャンネルでこのReceiverが開始した音声を停止します。
    /// </summary>
    /// <param name="animationEvent">ObjectにCue Reference、Intにチャンネルを設定します。</param>
    public void OnAnimationAudioCueStop(AnimationEvent animationEvent)
    {
        if (TryGetCue(animationEvent, out AudioCueId cueId, out _, out int channel))
        {
            StopMatchingCue(cueId, channel, false);
        }
    }

    /// <summary>
    /// 同じCueID・チャンネルでこのReceiverが開始した音声を即時停止します。
    /// </summary>
    /// <param name="animationEvent">ObjectにCue Reference、Intにチャンネルを設定します。</param>
    public void OnAnimationAudioCueStopImmediate(AnimationEvent animationEvent)
    {
        if (TryGetCue(animationEvent, out AudioCueId cueId, out _, out int channel))
        {
            StopMatchingCue(cueId, channel, true);
        }
    }

    /// <summary>
    /// アニメーション末尾に配置したEventで、State終了指定の音声を停止します。
    /// StateMachineBehaviourを使用しない構成向けの代替です。
    /// </summary>
    /// <param name="animationEvent">Int Parameterに再生時と同じチャンネルを設定します。</param>
    public void OnAnimationAudioCueEnd(AnimationEvent animationEvent)
    {
        if (animationEvent != null)
        {
            NotifyAnimationEnded(animationEvent.intParameter);
        }
    }

    /// <summary>
    /// Animator Stateの終了時に、同じState・チャンネルの自動停止音を停止します。
    /// </summary>
    /// <param name="channel">再生時のInt Parameterで指定したチャンネル。</param>
    /// <param name="stateInfo">終了したAnimator State。</param>
    public void NotifyAnimationStateExited(int channel, AnimatorStateInfo stateInfo)
    {
        int exitedStateHash = stateInfo.fullPathHash;

        // 別Stateのイベントと混同しないよう、取得済みのStateハッシュも比較します。
        for (int i = m_activeRecords.Count - 1; i >= 0; i--)
        {
            PlaybackRecord record = m_activeRecords[i];
            if (record.StopTiming != AnimationAudioCueStopTiming.ON_STATE_EXIT ||
                record.Channel != channel ||
                (record.StateFullPathHash != 0 && record.StateFullPathHash != exitedStateHash))
            {
                continue;
            }

            RemoveRecordAt(i);
            StopRecord(record, record.StopMode == AnimationAudioCueStopMode.IMMEDIATE);
        }
    }

    /// <summary>
    /// アニメーション終端通知から、指定チャンネルの自動停止音を停止します。
    /// </summary>
    /// <param name="channel">Animation EventのInt Parameterで指定したチャンネル。</param>
    public void NotifyAnimationEnded(int channel)
    {
        for (int i = m_activeRecords.Count - 1; i >= 0; i--)
        {
            PlaybackRecord record = m_activeRecords[i];
            if (record.StopTiming != AnimationAudioCueStopTiming.ON_STATE_EXIT ||
                record.Channel != channel)
            {
                continue;
            }

            RemoveRecordAt(i);
            StopRecord(record, record.StopMode == AnimationAudioCueStopMode.IMMEDIATE);
        }
    }

    /// <summary>
    /// 指定Cue・チャンネルに該当する、このReceiverの再生だけを停止します。
    /// </summary>
    private void StopMatchingCue(AudioCueId cueId, int channel, bool immediate)
    {
        for (int i = m_activeRecords.Count - 1; i >= 0; i--)
        {
            PlaybackRecord record = m_activeRecords[i];
            if (record.CueId != cueId || record.Channel != channel)
            {
                continue;
            }

            RemoveRecordAt(i);
            StopRecord(record, immediate);
        }
    }

    /// <summary>
    /// 再生種別に応じて、指定した再生ハンドルだけを停止します。
    /// </summary>
    private static void StopRecord(PlaybackRecord record, bool immediate)
    {
        if (record.SequenceHandle != null && record.SequenceHandle.IsActive)
        {
            if (immediate)
            {
                record.SequenceHandle.StopImmediate();
            }
            else
            {
                // シーケンスのSTOPは、既存仕様に従ってEND音声へ移行します。
                record.SequenceHandle.Stop();
            }
        }

        // OneShotはEND段階を持たないので、いずれの停止方法でも即停止します。
        if (record.OneShotHandle != null && record.OneShotHandle.IsActive)
        {
            record.OneShotHandle.Stop();
        }
    }

    /// <summary>
    /// 完了通知を解除し、追跡リストから1件削除します。
    /// </summary>
    private void RemoveRecordAt(int index)
    {
        PlaybackRecord record = m_activeRecords[index];
        m_activeRecords.RemoveAt(index);

        if (record.SequenceHandle != null)
        {
            record.SequenceHandle.Finished -= OnSequenceFinished;
        }
        if (record.OneShotHandle != null)
        {
            record.OneShotHandle.Finished -= OnOneShotFinished;
        }
    }

    /// <summary>自然終了したシーケンスの記録を解除します。</summary>
    private void OnSequenceFinished(AudioSequentialHandle handle)
    {
        for (int i = m_activeRecords.Count - 1; i >= 0; i--)
        {
            if (ReferenceEquals(m_activeRecords[i].SequenceHandle, handle))
            {
                RemoveRecordAt(i);
            }
        }
    }

    /// <summary>自然終了した単発音声の記録を解除します。</summary>
    private void OnOneShotFinished(AudioPlayHandle handle, bool completedNaturally)
    {
        for (int i = m_activeRecords.Count - 1; i >= 0; i--)
        {
            if (ReferenceEquals(m_activeRecords[i].OneShotHandle, handle))
            {
                RemoveRecordAt(i);
            }
        }
    }

    /// <summary>
    /// Animation Eventに設定された参照Asset、または旧整数CueIDを取得します。
    /// </summary>
    /// <param name="animationEvent">再生または停止用のAnimation Event。</param>
    /// <param name="cueId">取得した音声演出ID。</param>
    /// <param name="reference">参照Asset。旧方式の場合はnull。</param>
    /// <param name="channel">同じアニメーション内の音声を区別するチャンネル。</param>
    /// <returns>true：CueIDが有効。false：設定不正。</returns>
    private bool TryGetCue(AnimationEvent animationEvent, out AudioCueId cueId,
        out AudioCueReferenceAsset reference, out int channel)
    {
        cueId = AudioCueId.NONE;
        reference = null;
        channel = 0;

        if (animationEvent == null)
        {
            Debug.LogWarning("[AnimationAudioCueEventReceiver] Animation Eventがnullです。", this);
            return false;
        }

        UnityEngine.Object parameter = animationEvent.objectReferenceParameter;
        if (parameter is AudioCueReferenceAsset cueReference)
        {
            // 新方式：Object ParameterにCueID、Int Parameterにチャンネルを指定します。
            reference = cueReference;
            cueId = cueReference.CueId;
            channel = animationEvent.intParameter;
        }
        else if (parameter != null)
        {
            Debug.LogWarning("[AnimationAudioCueEventReceiver] Object ParameterにはAudioCueReferenceAssetを指定してください。", this);
            return false;
        }
        else if (m_allowLegacyIntParameter)
        {
            // 旧方式：Int ParameterをCueIDとして扱います。チャンネルは0固定です。
            int value = animationEvent.intParameter;
            if (value == (int)AudioCueId.NONE || !Enum.IsDefined(typeof(AudioCueId), value))
            {
                Debug.LogWarning($"[AnimationAudioCueEventReceiver] 無効な旧CueIDです: {value}", this);
                return false;
            }

            cueId = (AudioCueId)value;
            if (!m_hasLoggedLegacyWarning)
            {
                m_hasLoggedLegacyWarning = true;
                Debug.LogWarning("[AnimationAudioCueEventReceiver] 旧Int Parameter方式です。Cue Referenceへの移行を推奨します。", this);
            }
        }
        else
        {
            Debug.LogWarning("[AnimationAudioCueEventReceiver] Object ParameterにAudioCueReferenceAssetを設定してください。", this);
            return false;
        }

        if (cueId == AudioCueId.NONE || !Enum.IsDefined(typeof(AudioCueId), cueId))
        {
            Debug.LogWarning($"[AnimationAudioCueEventReceiver] 無効なAudioCueIdです: {cueId}", this);
            return false;
        }

        return true;
    }

    /// <summary>
    /// 自身または親階層からAudioEmitterを取得します。
    /// </summary>
    /// <param name="emitter">取得したAudioEmitter。</param>
    /// <returns>true：取得成功。false：取得失敗。</returns>
    private bool TryGetEmitter(out AudioEmitter emitter)
    {
        if (m_audioEmitter == null)
        {
            m_audioEmitter = GetComponentInParent<AudioEmitter>(true);
        }

        emitter = m_audioEmitter;
        if (emitter != null)
        {
            return true;
        }

        Debug.LogWarning("[AnimationAudioCueEventReceiver] AudioEmitterが見つかりません。", this);
        return false;
    }

    /// <summary>
    /// 無効化時に指定された音声とState終了待ちの音声を停止します。
    /// MANUALの音声は強制停止せず購読だけ解除します。
    /// </summary>
    private void OnDisable()
    {
        for (int i = m_activeRecords.Count - 1; i >= 0; i--)
        {
            PlaybackRecord record = m_activeRecords[i];
            RemoveRecordAt(i);

            if (record.StopTiming != AnimationAudioCueStopTiming.MANUAL)
            {
                // Animatorが破棄されてOnStateExitが呼ばれない場合の後片付け。
                StopRecord(record, record.StopMode == AnimationAudioCueStopMode.IMMEDIATE);
            }
        }
    }
}
