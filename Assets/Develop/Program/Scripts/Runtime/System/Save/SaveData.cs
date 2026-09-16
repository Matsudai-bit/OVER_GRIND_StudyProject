using UnityEngine;

/// <summary>
/// ゲーム全体のセーブデータです。
/// </summary>
[CreateAssetMenu(
    fileName = "SaveData",
    menuName = "Game/Save/Save Data")]
public sealed class SaveData : ScriptableObject
{
    public const int CURRENT_VERSION = 1;

    // セーブデータバージョン
    [SerializeField, HideInInspector]
    private int m_version = CURRENT_VERSION;

    // ゲーム進行データ
    [SerializeField, Header("ゲーム進行")]
    private GameProgressSaveData m_progress = new();

    // ゲーム設定データ
    [SerializeField, Header("ゲーム設定")]
    private GameSettingsSaveData m_settings = new();

    /// <summary>
    /// セーブデータバージョンを取得します。
    /// </summary>
    public int Version => m_version;

    /// <summary>
    /// ゲーム進行データを取得します。
    /// </summary>
    public GameProgressSaveData Progress => m_progress;

    /// <summary>
    /// ゲーム設定データを取得します。
    /// </summary>
    public GameSettingsSaveData Settings => m_settings;

    /// <summary>
    /// データが使用可能な状態になるよう初期化します。
    /// </summary>
    public void EnsureInitialized()
    {
        if (m_progress == null)
        {
            m_progress = new GameProgressSaveData();
        }

        if (m_settings == null)
        {
            m_settings = new GameSettingsSaveData();
        }
    }

    /// <summary>
    /// コード上の初期状態へリセットします。
    /// </summary>
    public void ResetData()
    {
        m_version = CURRENT_VERSION;
        m_progress = new GameProgressSaveData();
        m_settings = new GameSettingsSaveData();
    }
}