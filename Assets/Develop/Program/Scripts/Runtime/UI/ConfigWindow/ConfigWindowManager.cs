using UnityEngine;

/// <summary>
/// ゲーム全体のコンフィグを管理します。
/// </summary>
public class ConfigWindowManager : SingletonMonoBehaviour<ConfigWindowManager>
{


    #region PlayerPrefs Key

    private const string MASTER_VOLUME_KEY = "MasterVolume";
    private const string BGM_VOLUME_KEY = "BgmVolume";
    private const string SE_VOLUME_KEY = "SeVolume";
    private const string CAMERA_SENSITIVITY_KEY = "CameraSensitivity";
    private const string WINDOW_SIZE_KEY = "WindowSize";

    #endregion

    #region Default Value

    private const float DEFAULT_MASTER_VOLUME = 1.0f;
    private const float DEFAULT_BGM_VOLUME = 1.0f;
    private const float DEFAULT_SE_VOLUME = 1.0f;
    private const float DEFAULT_CAMERA_SENSITIVITY = 1.0f;
    private const int DEFAULT_WINDOW_SIZE = 2;

    #endregion

    /// <summary>
    /// マスターボリューム
    /// </summary>
    public float MasterVolume { get; private set; }

    /// <summary>
    /// BGMボリューム
    /// </summary>
    public float BgmVolume { get; private set; }

    /// <summary>
    /// SEボリューム
    /// </summary>
    public float SeVolume { get; private set; }

    /// <summary>
    /// カメラ感度
    /// </summary>
    public float CameraSensitivity { get; private set; }

    /// <summary>
    /// ウィンドウサイズ
    /// </summary>
    public int WindowSizeIndex { get; private set; }

  
    /// <summary>
    /// コンフィグを読み込みます。
    /// </summary>
    public void Load()
    {
        MasterVolume = PlayerPrefs.GetFloat(
            MASTER_VOLUME_KEY,
            DEFAULT_MASTER_VOLUME);

        BgmVolume = PlayerPrefs.GetFloat(
            BGM_VOLUME_KEY,
            DEFAULT_BGM_VOLUME);

        SeVolume = PlayerPrefs.GetFloat(
            SE_VOLUME_KEY,
            DEFAULT_SE_VOLUME);

        CameraSensitivity = PlayerPrefs.GetFloat(
            CAMERA_SENSITIVITY_KEY,
            DEFAULT_CAMERA_SENSITIVITY);

        WindowSizeIndex = PlayerPrefs.GetInt(
            WINDOW_SIZE_KEY,
            DEFAULT_WINDOW_SIZE);
    }

    /// <summary>
    /// コンフィグを保存します。
    /// </summary>
    public void Save()
    {
        PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, MasterVolume);
        PlayerPrefs.SetFloat(BGM_VOLUME_KEY, BgmVolume);
        PlayerPrefs.SetFloat(SE_VOLUME_KEY, SeVolume);
        PlayerPrefs.SetFloat(CAMERA_SENSITIVITY_KEY, CameraSensitivity);
        PlayerPrefs.SetInt(WINDOW_SIZE_KEY, WindowSizeIndex);

        PlayerPrefs.Save();
    }

    /// <summary>
    /// マスターボリュームを設定します。
    /// </summary>
    /// <param name="volume">ボリューム。</param>
    public void SetMasterVolume(float volume)
    {
        MasterVolume = volume;
        Save();
    }

    /// <summary>
    /// BGMボリュームを設定します。
    /// </summary>
    /// <param name="volume">ボリューム。</param>
    public void SetBgmVolume(float volume)
    {
        BgmVolume = volume;
        Save();
    }

    /// <summary>
    /// SEボリュームを設定します。
    /// </summary>
    /// <param name="volume">ボリューム。</param>
    public void SetSeVolume(float volume)
    {
        SeVolume = volume;
        Save();
    }

    /// <summary>
    /// カメラ感度を設定します。
    /// </summary>
    /// <param name="sensitivity">感度。</param>
    public void SetCameraSensitivity(float sensitivity)
    {
        CameraSensitivity = sensitivity;
        Save();
    }

    /// <summary>
    /// ウィンドウサイズを設定します。
    /// </summary>
    /// <param name="index">サイズ番号。</param>
    public void SetWindowSize(int index)
    {
        WindowSizeIndex = index;
        Save();
    }
}