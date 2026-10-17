using System;
using UnityEngine;

/// <summary>
/// S1P2ボスの移動状態で使用する参照を保持します。
/// </summary>
[Serializable]
public sealed class S1P2MoveStateReferences : MonoBehaviour
{

    // 移動経路生成
    [SerializeField, Header("参照")]
    private S1P2BossMoveRoutePlanner m_routePlanner;

    // 経路追従
    [SerializeField]
    private S1P2BossMoveController m_moveController;

    /// <summary>
    /// 移動経路生成を取得します。
    /// </summary>
    public S1P2BossMoveRoutePlanner RoutePlanner =>
        m_routePlanner;

    /// <summary>
    /// 移動制御を取得します。
    /// </summary>
    public S1P2BossMoveController MoveController =>
        m_moveController;
}