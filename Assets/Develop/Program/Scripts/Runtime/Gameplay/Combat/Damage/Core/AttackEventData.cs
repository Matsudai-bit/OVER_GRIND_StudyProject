using UnityEngine;

/// <summary>
/// 攻撃アニメーションイベントの情報を保持します。
/// </summary>
[CreateAssetMenu(
    fileName = "AttackEventData",
    menuName = "Game/Attack/Attack Event Data")]
public sealed class AttackEventData : ScriptableObject
{
    // 攻撃ID
    [SerializeField, Header("攻撃情報")]
    private AttackIdentifier m_attackIdentifier;

    // 攻撃アニメーションイベントの種類
    [SerializeField]
    private AttackEventType m_attackEventType;

    /// <summary>
    /// 攻撃IDを取得します。
    /// </summary>
    public AttackIdentifier AttackIdentifier => m_attackIdentifier;

    /// <summary>
    /// 攻撃アニメーションイベントの種類を取得します。
    /// </summary>
    public AttackEventType AttackEventType => m_attackEventType;
}
