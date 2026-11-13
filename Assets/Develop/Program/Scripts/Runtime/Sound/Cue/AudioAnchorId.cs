/// <summary>
/// オーディオ発生位置の論理IDです。数値は変更・再利用しないでください。
/// </summary>
public enum AudioAnchorId
{
    // 基本位置
    ROOT = 0,
    BODY = 1,
    HEAD = 2,
    GROUND = 3,

    // 攻撃用
    ATTACK_A = 50,
    ATTACK_B = 51,
    ATTACK_C = 52,

    // 部位・装備
    FOOT_R = 100,
    FOOT_L = 101,
    FOOT_JET_R = 102,
    FOOT_JET_L = 103,
    ARM_R = 104,
    ARM_L = 105,
    MOUTH = 106,
    WEAPON = 107,
    TAIL = 108,
    TOE_R = 109,
    TOE_L = 110,

    // 個別用途
    CUSTOM_01 = 150,
    CUSTOM_02 = 151,
    CUSTOM_03 = 152,
    CUSTOM_04 = 153
}
