/// <summary>
/// 通常ジャンプで使用するアニメーション種別です。
/// AnimatorのJumpType Intパラメータへそのまま渡します。
/// </summary>
public enum PlayerJumpAnimationType
{
    /// <summary>
    /// その場ジャンプ。
    /// </summary>
    STATIONARY = 0,

    /// <summary>
    /// 水平移動中のジャンプ。
    /// </summary>
    MOVING = 1,
    RAIL=2
}
