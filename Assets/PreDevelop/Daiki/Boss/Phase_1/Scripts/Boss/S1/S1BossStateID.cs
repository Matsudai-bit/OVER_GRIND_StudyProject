using System;
using Unity.Behavior;

/// <summary>
/// ステージ1ボスのBehaviorから要求する状態を表します。
/// </summary>
[BlackboardEnum, Serializable]
public enum S1BossStateID
{
    IDLE,
    P1_WALK,
    P1_ATTACK_RIGHT,
    P1_ATTACK_LEFT,
    P1_TURN,
    P2_ATTACK_CHARGING, // 突進攻撃
}
