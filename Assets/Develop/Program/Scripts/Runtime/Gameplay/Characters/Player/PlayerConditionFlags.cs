using System;

/// <summary>
/// プレイヤーの現在状況を表すフラグです。
/// 複数の状態を同時に保持できます。
/// </summary>
[Flags]
public enum PlayerConditionFlags
{
    NONE = 0,

    // 物理・移動状態
    GROUNDED = 1 << 0,
    AIRBORNE = 1 << 1,
    MOVING = 1 << 2,
    RISING = 1 << 3,
    FALLING = 1 << 4,

    // StateMachine状態
    IDLING = 1 << 5,
    WALKING = 1 << 6,
    JUMPING = 1 << 7,
    ATTACKING = 1 << 8,
    BOOST_CHARGING = 1 << 9,
    BOOSTING = 1 << 10,
    GRINDING = 1 << 11,
    RAIL_JUMPING = 1 << 12,
    RAIL_EXITING = 1 << 13,
    HIT_REACTION = 1 << 14,

    // Stateが明示的にON/OFFするアニメーション・演出用状態
    CHARGE_MOVING = 1 << 15,
    CHARGE_ATTACKING = 1 << 16
}
