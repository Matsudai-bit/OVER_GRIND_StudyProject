/// <summary>
/// Playerの状態・行動に対応するシーケンス音声IDです。
/// AudioSequentialAssetそのものではなく、
/// 「どの行動の音声か」を識別する論理IDとして使用します。
/// </summary>
public enum PlayerAudioActionID
{
    NONE = 0,

    // PlayerIdlingState
    IDLING = 1,

    // PlayerWalkingState
    WALKING = 2,

    // PlayerJumpingState
    JUMPING = 3,

    // PlayerAttackingState
    ATTACKING_NORMAL =13,
    ATTACKING_BOOST = 14,

    // PlayerBoostChargingState
    BOOST_CHARGING_MOVE = 5,
    BOOST_CHARGING_STAY = 13,

    // PlayerVRunningState
    V_RUNNING = 6,

    // PlayerGrindingState
    RAIL_GRINDING = 7,

    // PlayerRailJumpingState
    RAIL_JUMPING = 8,

    // PlayerRailExitingState
    RAIL_EXITING = 9,

    // 攻撃ヒット無効（壁とか）
    HIT_INACTIVE = 10,
    // 攻撃ヒット有効（ダメージ与えれた）
    HIT_ACTIVE = 11,
        HIT_DAMAGE = 12
}
