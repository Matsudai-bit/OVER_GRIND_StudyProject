using TMPro;
using UnityEngine;

/// <summary>
/// コンフィグ項目を管理します。
/// </summary>
public class ConfigItem : MonoBehaviour
{
    /// <summary>
    /// コンフィグの種類
    /// </summary>
    public enum ConfigType
    {
        MASTER_VOLUME,
        BGM_VOLUME,
        SE_VOLUME,
        CAMERA_SENSITIVITY,
        WINDOW_SIZE
    }

    [Header("Config")]

    [SerializeField]
    private ConfigType m_type;

    [SerializeField]
    private TMP_Text m_valueText;

    [Header("Number Setting")]

    [SerializeField]
    private int m_minValue = 0;

    [SerializeField]
    private int m_maxValue = 100;

    [SerializeField]
    private int m_step = 5;

    [Header("Window Size")]

    [SerializeField]
    private string[] m_windowNames =
    {
        "1280 × 720",
        "1600 × 900",
        "1920 × 1080"
    };

    [SerializeField]
    private TMP_Text m_labelText;

    /// <summary>
    /// 現在の値
    /// </summary>
    private int m_value;

    /// <summary>
    /// 初期化
    /// </summary>
    public void Initialize()
    {
        switch (m_type)
        {
            case ConfigType.MASTER_VOLUME:
                m_value = Mathf.RoundToInt(ConfigManager.Instance.MasterVolume * 100);
                break;

            case ConfigType.BGM_VOLUME:
                m_value = Mathf.RoundToInt(ConfigManager.Instance.BgmVolume * 100);
                break;

            case ConfigType.SE_VOLUME:
                m_value = Mathf.RoundToInt(ConfigManager.Instance.SeVolume * 100);
                break;

            case ConfigType.CAMERA_SENSITIVITY:
                m_value = Mathf.RoundToInt(ConfigManager.Instance.CameraSensitivity * 10);
                break;

            case ConfigType.WINDOW_SIZE:
                m_value = ConfigManager.Instance.WindowSizeIndex;
                break;
        }

        UpdateText();
    }

    /// <summary>
    /// 値を増やします。
    /// </summary>
    public void Increase()
    {
        switch (m_type)
        {
            case ConfigType.WINDOW_SIZE:

                if (m_value < m_windowNames.Length - 1)
                {
                    m_value++;

                    ConfigManager.Instance.SetWindowSize(m_value);

                    ApplyWindowSize();
                }

                break;

            default:

                m_value += m_step;
                m_value = Mathf.Clamp(m_value, m_minValue, m_maxValue);

                ApplyValue();

                break;
        }

        UpdateText();
    }

    /// <summary>
    /// 値を減らします。
    /// </summary>
    public void Decrease()
    {
        switch (m_type)
        {
            case ConfigType.WINDOW_SIZE:

                if (m_value > 0)
                {
                    m_value--;

                    ConfigManager.Instance.SetWindowSize(m_value);

                    ApplyWindowSize();
                }

                break;

            default:

                m_value -= m_step;
                m_value = Mathf.Clamp(m_value, m_minValue, m_maxValue);

                ApplyValue();

                break;
        }

        UpdateText();
    }

    /// <summary>
    /// 値をConfigManagerへ反映します。
    /// </summary>
    private void ApplyValue()
    {
        float value = m_value / 100f;

        switch (m_type)
        {
            case ConfigType.MASTER_VOLUME:
                ConfigManager.Instance.SetMasterVolume(value);
                break;

            case ConfigType.BGM_VOLUME:
                ConfigManager.Instance.SetBgmVolume(value);
                break;

            case ConfigType.SE_VOLUME:
                ConfigManager.Instance.SetSeVolume(value);
                break;

            case ConfigType.CAMERA_SENSITIVITY:
                ConfigManager.Instance.SetCameraSensitivity(m_value / 10f);
                break;
        }
    }

    /// <summary>
    /// ウィンドウサイズを反映します。
    /// </summary>
    private void ApplyWindowSize()
    {
        switch (m_value)
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
    /// 表示を更新します。
    /// </summary>
    private void UpdateText()
    {
        if (m_type == ConfigType.WINDOW_SIZE)
        {
            m_valueText.text = $"< {m_windowNames[m_value]} >";
        }
        else
        {
            m_valueText.text = $"< {m_value} >";
        }
    }

    /// <summary>
    /// 選択状態を変更します。
    /// </summary>
    public void SetSelected(bool selected)
    {
        Color color = selected ? Color.yellow : Color.white;

        m_labelText.color = color;
        m_valueText.color = color;
    }
}