/// <summary>
/// Actorの制御状態を変更できることを表します。
/// </summary>
public interface IActorControllable
{
    /// <summary>
    /// Actorの制御が有効か取得します。
    /// </summary>
    public bool IsControlEnabled { get; }

    /// <summary>
    /// Actorの制御状態を変更します。
    /// </summary>
    /// <param name="isEnabled">制御を有効にするか。</param>
    public void SetControlEnabled(bool isEnabled);
}
