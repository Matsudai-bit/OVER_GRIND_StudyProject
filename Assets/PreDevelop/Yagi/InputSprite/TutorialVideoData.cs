using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// チュートリアル動画の対象Cueと再生する動画を保持します。
/// </summary>
[CreateAssetMenu(fileName = "TutorialVideoData", menuName = "Game/UI/Tutorial Video Data")]
public sealed class TutorialVideoData : ScriptableObject
{
    [SerializeField, Header("対象Cue")]
    private GameplayCueAsset m_cue;

    [SerializeField, Header("再生する動画")]
    private VideoClip m_videoClip;

    /// <summary>対象Cueを取得します。</summary>
    public GameplayCueAsset Cue => m_cue;

    /// <summary>再生する動画を取得します。</summary>
    public VideoClip VideoClip => m_videoClip;
}
