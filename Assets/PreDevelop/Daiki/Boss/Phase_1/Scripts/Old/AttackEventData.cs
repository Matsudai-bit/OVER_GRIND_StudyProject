using UnityEngine;

/// <summary>
/// 攻撃アニメーションイベントの情報を保持します。
/// </summary>
[CreateAssetMenu(
    fileName = "AttackEventData",
    menuName = "Game/Attack/Attack Event Data")]
public sealed class AttackEventData : ScriptableObject
{
    /// <summary>
    /// 攻撃に使用する部位。
    /// </summary>
    [SerializeField]
    private P1AttackType m_attackType;

    /// <summary>
    /// 攻撃アニメーションイベントの種類。
    /// </summary>
    [SerializeField]
    private AttackEventType m_attackEventType;

    /// <summary>
    /// 攻撃に使用する部位を取得します。
    /// </summary>
    public P1AttackType AttackType => m_attackType;

    /// <summary>
    /// 攻撃アニメーションイベントの種類を取得します。
    /// </summary>
    public AttackEventType AttackEventType => m_attackEventType;
}