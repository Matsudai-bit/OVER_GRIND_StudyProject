/// <summary>
/// エフェクトを識別します。
/// 一度割り当てた数値は変更・再利用しないでください。
/// </summary>
public enum EffectId
{
    NONE = 0,

    // ==============================
    // 共通エフェクト（1～49）
    // ==============================

    COMMON_EXPLOSION = 1,
    COMMON_HIT = 2,


    // ==============================
    // プレイヤー（50～99）
    // ==============================

    PLAYER_MOVE_WALK_I = 50,
    PLAYER_ATTACK_NORMAL_I = 51,
    PLAYER_ATTACK_NORMAL_L = 52,
    PLAYER_ATTACK_NORMAL_O = 53,
    PLAYER_MOVE_WALK_L = 54,   // 右足歩行
    PLAYER_BOOST_LOOP = 55,
    PLAYER_SP_VCharge_L_V0 = 56,
    PLAYER_SP_VCharge_L_V1 = 57,
    PLAYER_SP_VCharge_L_V2 = 58,
    PLAYER_SP_VCharge_L_V3 = 59,
    PLAYER_MOVE_VBOOST_I = 60,
    PLAYER_MOVE_VBOOST_L = 61,
    PLAYER_MOVE_VBOOST_O = 62,


    // ==============================
    // ボス（100以上）
    // ==============================

    S1P1_BOSS_ATTACK = 100,
    S1P1_BOSS_CHARGE = 101
}