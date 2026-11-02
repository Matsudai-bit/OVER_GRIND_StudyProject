/// <summary>
/// 複数段階で構成されるボス行動の実行フェーズです。
/// </summary>
public enum BossActionPhase
{
    // 予備動作・方向調整
    PREPARATION = 0,

    // 狙い確定
    TARGET_LOCK = 1,

    // 行動本体
    EXECUTION = 2,

    // 終了動作
    RECOVERY = 3
}
/// <summary>
/// 突進開始前の旋回方向です。
/// </summary>
public enum BossChargeTurnDirection
{
    LEFT = -1,
    RIGHT = 1
}