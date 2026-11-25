using UnityEngine;

/// <summary>
/// 1回分の再生リクエスト（再生位置・倍率・フェードなど）です。
/// </summary>
public readonly struct SoundPlayRequest
{
    /// <summary>
    /// 3D 再生時に追従する対象を取得します。
    /// </summary>
    public Transform FollowTarget { get; }

    /// <summary>
    /// 固定位置が指定されているかを取得します。
    /// </summary>
    public bool HasPosition { get; }

    /// <summary>
    /// 固定位置（ワールド座標）を取得します。
    /// </summary>
    public Vector3 Position { get; }

    /// <summary>
    /// 音量倍率（0～1）を取得します。
    /// </summary>
    public float VolumeScale { get; }

    /// <summary>
    /// ピッチ倍率を取得します。
    /// </summary>
    public float PitchScale { get; }

    /// <summary>
    /// フェードイン秒数を取得します。
    /// </summary>
    public float FadeInSeconds { get; }

    /// <summary>
    /// 2D / 3D の指定を取得します。
    /// </summary>
    public SoundSpatialMode SpatialMode { get; }

    /// <summary>
    /// 再生リクエストを生成します。
    /// </summary>
    /// <param name="followTarget">追従する対象。</param>
    /// <param name="hasPosition">固定位置を使うか。</param>
    /// <param name="position">固定位置。</param>
    /// <param name="volumeScale">音量倍率。</param>
    /// <param name="pitchScale">ピッチ倍率。</param>
    /// <param name="fadeInSeconds">フェードイン秒数。</param>
    /// <param name="spatialMode">2D / 3D の指定。</param>
    public SoundPlayRequest(
        Transform followTarget,
        bool hasPosition,
        Vector3 position,
        float volumeScale,
        float pitchScale,
        float fadeInSeconds,
        SoundSpatialMode spatialMode)
    {
        FollowTarget = followTarget;
        HasPosition = hasPosition;
        Position = position;
        VolumeScale = volumeScale;
        PitchScale = pitchScale;
        FadeInSeconds = fadeInSeconds;
        SpatialMode = spatialMode;
    }
}