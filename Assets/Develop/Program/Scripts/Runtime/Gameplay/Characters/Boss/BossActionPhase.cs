/// <summary>
/// 複数段階で構成されるボス行動の実行フェーズです。
/// </summary>
public enum BossActionPhase
{
    // 予備動作
    PREPARATION = 0,

    // 行動本体
    EXECUTION = 1,

    // 終了動作
    RECOVERY = 2
}