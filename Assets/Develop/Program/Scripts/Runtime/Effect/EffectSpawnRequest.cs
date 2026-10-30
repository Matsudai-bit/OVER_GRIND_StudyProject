using UnityEngine;

/// <summary>エフェクトの位置と追従に必要な情報です。</summary>
public readonly struct EffectSpawnRequest
{
    /// <summary>ワールド位置。</summary>
    public Vector3 Position { get; }
    /// <summary>ワールド回転。</summary>
    public Quaternion Rotation { get; }
    /// <summary>ワールド位置を適用するか。</summary>
    public bool UseWorldPose { get; }
    /// <summary>追従アンカーの登録先。</summary>
    public EffectAnchorRegistry Anchors { get; }

    /// <summary>生成情報を指定します。</summary>
    /// <param name="position">再生位置。</param>
    /// <param name="rotation">再生回転。</param>
    /// <param name="useWorldPose">ワールド座標を指定するか。</param>
    /// <param name="anchors">必要な場合の追従先。</param>
    public EffectSpawnRequest(Vector3 position, Quaternion rotation, bool useWorldPose,
        EffectAnchorRegistry anchors = null)
    {
        Position = position;
        Rotation = rotation;
        UseWorldPose = useWorldPose;
        Anchors = anchors;
    }
}
