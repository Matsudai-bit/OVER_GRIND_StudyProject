using System;
using Unity.Behavior;

/// <summary>
/// ステージ1ボスのBehaviorから要求する状態を表します。
/// </summary>
[BlackboardEnum, Serializable]
public enum S1BossStateID
{
    IDLE,
    S1P1_WALK,
    S1P1_ATTACK_RIGHT,
    S1P1_ATTACK_LEFT,
    S1P1_TURN
}
