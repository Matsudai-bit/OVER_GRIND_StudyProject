using UnityEngine;

/// <summary>
/// Animation Eventから参照する音声演出IDを定義します。
/// </summary>
[CreateAssetMenu(fileName = "AudioCueReference", menuName = "Game/Audio/Cue Reference")]
public sealed class AudioCueReferenceAsset : ScriptableObject
{
    // Animation EventのObject Parameterから間接的に指定するCueID。
    [SerializeField, Header("音声演出ID")]
    private AudioCueId m_cueId = AudioCueId.NONE;

    /// <summary>
    /// 再生または停止する音声演出IDを取得します。
    /// </summary>
    public AudioCueId CueId => m_cueId;
}
