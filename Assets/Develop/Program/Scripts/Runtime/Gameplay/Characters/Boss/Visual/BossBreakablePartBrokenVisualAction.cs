using System;

/// <summary>
/// ボス破壊可能部位の破壊時に適用する見た目処理です。
/// </summary>
[Flags]
public enum BossBreakablePartBrokenVisualAction
{
    // 破壊時処理なし
    NONE = 0,

    // 通常メッシュを非表示
    DISABLE_NORMAL_MESH = 1 << 0,

    // 破壊メッシュを表示
    ENABLE_BROKEN_MESH = 1 << 1,

    // 破壊時のEmissive値を適用
    APPLY_BROKEN_EMISSIVE = 1 << 2
}