using UnityEngine;

/// <summary>
/// 振動の全般設定（ON/OFF・全体強度）を PlayerPrefs で保存・管理するクラス
/// </summary>
public static class VibrationSettings
{
    private const string KeyEnabled = "Vibration_Enabled";
    private const string KeyVolume = "Vibration_Volume";

    /// <summary>
    /// 振動機能の有効/無効（デフォルト: ON）
    /// </summary>
    public static bool IsEnabled
    {
        get => PlayerPrefs.GetInt(KeyEnabled, 1) == 1;
        set
        {
            PlayerPrefs.SetInt(KeyEnabled, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// 振動の全体強度 0.0 ～ 1.0（デフォルト: 1.0 = 100%）
    /// </summary>
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