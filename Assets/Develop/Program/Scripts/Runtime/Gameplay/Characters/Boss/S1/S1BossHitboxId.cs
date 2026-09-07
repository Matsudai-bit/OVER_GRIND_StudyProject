/// <summary>
/// ステージ1ボスの攻撃Hitboxを識別します。
/// </summary>
public enum S1BossHitboxId
{
    LEFT_FOOT_SOLE,       // 左足裏の攻撃判定
    RIGHT_FOOT_SOLE,      // 右足裏の攻撃判定
    STOMP_SHOCKWAVE,      // 踏みつけ時に発生する衝撃波の攻撃判定
    MISSILE_EXPLOSION,    // ミサイル爆発時の攻撃判定
    HEAT_EXHAUST_AREA,    // 排熱攻撃の範囲判定
    FRONT_BODY,           // ボス正面の攻撃判定
    LEFT_CRAWLER,         // 左キャタピラーの攻撃判定
    RIGHT_CRAWLER,        // 右キャタピラーの攻撃判定
    TAIL,                 // 尻尾の攻撃判定
    ENERGY_CANNON,        // エネルギー砲の攻撃判定
    DREAD_PROJECTILE,     // ドレット攻撃で発射する飛翔物の攻撃判定
    UPPER_JAW,            // 上顎の攻撃判定
    LOWER_JAW             // 下顎の攻撃判定
}
