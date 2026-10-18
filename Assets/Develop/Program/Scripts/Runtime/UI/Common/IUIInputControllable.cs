/// <summary>
/// 入力の有効状態を切り替えるインターフェースです。
/// </summary>
public interface IUIInputControllable
{
    /// <summary>
    /// 入力の有効状態を変更します。
    /// </summary>
    /// <param name="isEnabled">入力を有効にするか。</param>
    void SetInputEnabled(bool isEnabled);
}
