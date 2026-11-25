/// <summary>
/// ゲーム設定変更時の情報です。
/// </summary>
public readonly struct GameSettingsChangedEventData
{
    /// <summary>
    /// 変更された設定種別を取得します。
    /// </summary>
    public GameSettingsChangeType ChangeType { get; }

    /// <summary>
    /// 現在のゲーム設定を取得します。
    /// </summary>
    public GameSettingsSaveData Settings { get; }

    /// <summary>
    /// ゲーム設定変更情報を生成します。
    /// </summary>
    /// <param name="changeType">変更種別。</param>
    /// <param name="settings">現在のゲーム設定。</param>
    public GameSettingsChangedEventData(
        GameSettingsChangeType changeType,
        GameSettingsSaveData settings)
    {
        ChangeType = changeType;
        Settings = settings;
    }
}
