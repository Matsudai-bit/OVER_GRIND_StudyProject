/// <summary>
/// 振動の競合解決方式。
/// </summary>
public enum GamepadRumbleConflictMode
{
    /// <summary>固定優先度を比較します。</summary>
    PRIORITY,

    /// <summary>現在の再生強度と新要求の強さをリアルタイム比較します。</summary>
    STRONGEST
}