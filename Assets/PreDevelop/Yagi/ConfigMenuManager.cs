using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// コンフィグ画面を管理します。
/// </summary>
public class ConfigMenuManager : MonoBehaviour
{
    /// <summary>
    /// コンフィグを閉じた時の通知
    /// </summary>
    public Action OnClosed;

    [Header("Volume")]

    [SerializeField]
    private Slider m_masterVolumeSlider;

    [SerializeField]
    private Slider m_bgmVolumeSlider;

    [SerializeField]
    private Slider m_seVolumeSlider;

    [Header("Camera")]

    [SerializeField]
    private Slider m_cameraSensitivitySlider;

    [Header("Window")]

    [SerializeField]
    private TMP_Dropdown m_windowSizeDropdown;

    [Header("Button")]

    [SerializeField]
    private Button m_backButton;

    /// <summary>
    /// 初期化
    /// </summary>
    private void Start()
    {
        if (ConfigManager.Instance == null)
        {
            Debug.LogError("ConfigManagerが存在しません。");
            return;
        }

        Initialize();

        m_masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        m_bgmVolumeSlider.onValueChanged.AddListener(OnBgmVolumeChanged);
        m_seVolumeSlider.onValueChanged.AddListener(OnSeVolumeChanged);
        m_cameraSensitivitySlider.onValueChanged.AddListener(OnCameraSensitivityChanged);

        m_windowSizeDropdown.onValueChanged.AddListener(OnWindowSizeChanged);

        m_backButton.onClick.AddListener(CloseConfig);
    }

    /// <summary>
    /// 設定値をUIへ反映します。
    /// </summary>
    public void Initialize()
    {
        ConfigManager.Instance.Load();

        m_masterVolumeSlider.SetValueWithoutNotify(
            ConfigManager.Instance.MasterVolume);

        m_bgmVolumeSlider.SetValueWithoutNotify(
            ConfigManager.Instance.BgmVolume);

        m_seVolumeSlider.SetValueWithoutNotify(
            ConfigManager.Instance.SeVolume);

        m_cameraSensitivitySlider.SetValueWithoutNotify(
            ConfigManager.Instance.CameraSensitivity);

        m_windowSizeDropdown.SetValueWithoutNotify(
            ConfigManager.Instance.WindowSizeIndex);
    }

    /// <summary>
    /// マスターボリューム変更
    /// </summary>
    private void OnMasterVolumeChanged(float value)
    {
        ConfigManager.Instance.SetMasterVolume(value);
    }

    /// <summary>
    /// BGMボリューム変更
    /// </summary>
    private void OnBgmVolumeChanged(float value)
    {
        ConfigManager.Instance.SetBgmVolume(value);
    }

    /// <summary>
    /// SEボリューム変更
    /// </summary>
    private void OnSeVolumeChanged(float value)
    {
        ConfigManager.Instance.SetSeVolume(value);
    }

    /// <summary>
    /// カメラ感度変更
    /// </summary>
    private void OnCameraSensitivityChanged(float value)
    {
        ConfigManager.Instance.SetCameraSensitivity(value);
    }

    /// <summary>
    /// ウィンドウサイズ変更
    /// </summary>
    private void OnWindowSizeChanged(int index)
    {
        ConfigManager.Instance.SetWindowSize(index);

        switch (index)
        {
            case 0:
                Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
                break;

            case 1:
                Screen.SetResolution(1600, 900, FullScreenMode.Windowed);
                break;

            case 2:
                Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
                break;
        }
    }

    /// <summary>
    /// コンフィグを閉じます。
    /// </summary>
    public void CloseConfig()
    {
        gameObject.SetActive(false);

        OnClosed?.Invoke();
    }
}