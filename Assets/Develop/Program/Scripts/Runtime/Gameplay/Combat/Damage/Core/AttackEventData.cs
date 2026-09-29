using UnityEngine;

/// <summary>
/// 攻撃アニメーションイベントの情報を保持します。
/// </summary>
[CreateAssetMenu(
    fileName = "AttackEventData",
    menuName = "Game/Attack/Attack Event Data")]
public sealed class AttackEventData : ScriptableObject
{

    // 攻撃アニメーションイベントの種類
    [SerializeField, Header("種類")]
    private AttackEventType m_attackEventType;
    /// <summary>
    /// 攻撃アニメーションイベントの種類を取得します。
    /// </summary>
    public AttackEventType AttackEventType => m_attackEventType;
}
