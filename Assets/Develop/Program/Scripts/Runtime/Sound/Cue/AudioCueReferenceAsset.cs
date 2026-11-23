using UnityEngine;

/// <summary>
/// Animation Eventに渡す音声IDと停止条件を定義します。
/// </summary>
[CreateAssetMenu(fileName = "AudioCueReference", menuName = "Game/Audio/Cue Reference")]
public sealed class AudioCueReferenceAsset : ScriptableObject
{
    // Animation EventのObject Parameterから参照する音声演出ID。
    [SerializeField, Header("音声演出ID")]
    private AudioCueId m_cueId = AudioCueId.NONE;

    // 再生元アニメーションが終了したときの自動停止条件。
    [SerializeField, Header("自動停止するタイミング")]
    private AnimationAudioCueStopTiming m_stopTiming = AnimationAudioCueStopTiming.MANUAL;

    // シーケンスをENDへ遷移させるか、即時停止するか。
    [SerializeField, Header("停止方法")]
    private AnimationAudioCueStopMode m_stopMode = AnimationAudioCueStopMode.GRACEFUL;

    /// <summary>音声演出IDを取得します。</summary>
    public AudioCueId CueId => m_cueId;

    /// <summary>自動停止タイミングを取得します。</summary>
    public AnimationAudioCueStopTiming StopTiming => m_stopTiming;

    /// <summary>音声の停止方法を取得します。</summary>
    public AnimationAudioCueStopMode StopMode => m_stopMode;
}
