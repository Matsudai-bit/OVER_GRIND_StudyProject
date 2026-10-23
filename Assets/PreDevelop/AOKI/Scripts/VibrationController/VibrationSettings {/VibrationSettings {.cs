using UnityEngine;

public static class VibrationSettings
{
    private const string KeyEnabled = "Vibration_Enabled";
    private const string KeyVolume = "Vibration_Volume";

    // 振動の有効/無効（デフォルト: ON）
    public static bool IsEnabled
    {
        get => PlayerPrefs.GetInt(KeyEnabled, 1) == 1;
        set
        {
            PlayerPrefs.SetInt(KeyEnabled, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    // 振動の全体強度 0.0 ～ 1.0（デフォルト: 1.0 = 100%）
    public static float MasterVolume
    {
        get => PlayerPrefs.GetFloat(KeyVolume, 1.0f);
        set
        {
            PlayerPrefs.SetFloat(KeyVolume, Mathf.Clamp01(value));
            PlayerPrefs.Save();
        }
    }
}