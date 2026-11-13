/// <summary>
/// Animation Eventから発生させるエフェクトの配置方式です。
/// </summary>
public enum EffectAnimationSpawnMode
{
    // EffectEmitterの標準の再生方式を使用します（ローカル優先・Constraint対応）。
    DEFAULT,

    // 登録アンカーの現在位置に共通プール型を発生させます（追従なし）。
    WORLD_AT_ANCHOR,

    // EffectEmitterの現在位置に共通プール型を発生させます（追従なし）。
    WORLD_AT_EMITTER
}
