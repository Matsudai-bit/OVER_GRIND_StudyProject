/// <summary>
/// 振動の競合解決方式
/// </summary>
public enum GamepadRumbleConflictMode
{
    /// <summary> 固定優先度 (Priority) を比較 </summary>
    Priority,

    /// <summary> 現在の再生強度と新要求の強さをリアルタイム比較 </summary>
    Strongest
}