using UnityEngine;

/// <summary>
/// Animation EventやTimelineから再生する単発音声の設定です。
/// </summary>
[CreateAssetMenu(fileName = "AudioOneShotAsset", menuName = "Game/Audio/One Shot Audio Asset")]
public sealed class AudioOneShotAsset : ScriptableObject
{
    [SerializeField, Header("再生する音声")]
    private AudioID m_audioID;

    [SerializeField, Header("再生位置")]
    private AudioSequentialSpatialMode m_spatialMode = AudioSequentialSpatialMode.THREE_DIMENSIONAL_FOLLOWING;

    /// <summary>単発音声のIDを取得します。</summary>
    public AudioID AudioID => m_audioID;

    /// <summary>再生位置の種類を取得します。</summary>
    public AudioSequentialSpatialMode SpatialMode => m_spatialMode;
}
