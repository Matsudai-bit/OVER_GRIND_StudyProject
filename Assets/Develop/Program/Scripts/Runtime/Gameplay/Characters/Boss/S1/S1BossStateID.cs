using System;
using Unity.Behavior;

/// <summary>
/// ステージ1ボスのBehaviorから要求する状態を表します。
/// </summary>
[BlackboardEnum, Serializable]
public enum S1BossStateID
{
    IDLE,                   // 行動要求がない待機状態

    // Phase 1
    P1_STOP,                // 停止
    P1_WALK,                // 歩行
    P1_STOMP,               // 踏みつけ攻撃
    P1_TURN,                // 方向転換
    P1_HEAT_EXHAUST,        // 排熱攻撃
    P1_MISSILE,             // ミサイル攻撃

    // Phase 2
    P2_STOP,                // 停止
    P2_MOVE,                // 通常移動
    P2_CHARGE,              // 突進攻撃
    P2_TAIL_SLAM,           // 尻尾の叩きつけ攻撃
    P2_HEAT_EXHAUST,        // 排熱攻撃
    P2_MISSILE,             // ミサイル攻撃
    P2_NAPE_JET,            // うなじ噴射
    P2_STUN,                // ダウン状態


    // Phase 3
    P3_ENERGY_CANNON,       // エネルギー砲攻撃
    P3_DREAD_ATTACK,        // ドレット攻撃
    P3_CHARGE,              // 突進攻撃
    P3_DOWN,                // ダウン状態
    P3_BITE_CRUSH,          // 噛み潰し攻撃
}