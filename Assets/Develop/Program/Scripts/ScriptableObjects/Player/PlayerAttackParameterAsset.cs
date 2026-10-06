using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerAttackParamater",
    menuName = "Game/Parameters/Player/Attack Parameter")]
public class PlayerAttackParameterAsset : ScriptableObject
{
    [SerializeField, Header("攻撃パラメータ")]
    [Tooltip("アニメーションイベントの受信元")]
    private PlayerAnimationEventReceiver m_animationEventReceiver;

    [SerializeField]
    [Tooltip("攻撃用ヒットボックス")]
    private List<AttackHitbox> m_attackHitboxes = new();

    [SerializeField]
    [Tooltip("多段ヒット設定"), Min(0.1f)]
    private float m_baseHitsPerSecond = 40.0f;

    /// <summary>
    /// 攻撃パラメータを生成します。
    /// </summary>
    /// <returns>攻撃パラメータ。</returns>
    public PlayerAttackParameter CreateAttackParameter()
    {
        return new PlayerAttackParameter(
            m_animationEventReceiver,
            m_attackHitboxes,
            m_baseHitsPerSecond
        );
    }
}
