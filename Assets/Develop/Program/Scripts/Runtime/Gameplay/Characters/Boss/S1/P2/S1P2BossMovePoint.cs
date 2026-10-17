using UnityEngine;

/// <summary>
/// S1P2ボスの移動経路生成に使用する候補地点です。
/// </summary>
[DisallowMultipleComponent]
public sealed class S1P2BossMovePoint : MonoBehaviour
{
    /// <summary>
    /// 移動候補地点の位置を取得します。
    /// </summary>
    public Vector3 Position =>
        transform.position;
}
