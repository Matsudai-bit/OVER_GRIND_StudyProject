using System;
using UnityEngine;

/// <summary>
/// ゲーム設定に関するセーブデータです。
/// </summary>
[Serializable]
public sealed class GameSettingsSaveData
{
    // BGM音量
    [SerializeField, Range(0.0f, 1.0f)]
    private float m_bgmVolume = 1.0f;

    // SE音量
    [SerializeField, Range(0.0f, 1.0f)]
    private float m_seVolume = 1.0f;

    // マスター音量
    [SerializeField, Range(0.0f, 1.0f)]
    private float m_masterVolume = 1.0f;

    // 画面サイズ設定
    [SerializeField]
    private int m_screenSize = 1;

    // カメラ感度
    [SerializeField, Min(0.0f)]
    private float m_cameraSensitivity = 1.0f;

    /// <summary>
    /// BGM音量を取得します。
    /// </summary>
    public float BgmVolume => m_bgmVolume;

    /// <summary>
    /// SE音量を取得します。
    /// </summary>
    public float SeVolume => m_seVolume;

    /// <summary>
    /// マスター音量を取得します。
    /// </summary>
    public float MasterVolume => m_masterVolume;

    /// <summary>
    /// 画面サイズ設定を取得します。
    /// </summary>
    public int ScreenSize => m_screenSize;

    /// <summary>
    /// カメラ感度を取得します。
    /// </summary>
    public float CameraSensitivity => m_cameraSensitivity;

    /// <summary>
    /// BGM音量を設定します。
    /// </summary>
    public bool SetBgmVolume(float volume)
    {
        float newVolume = Mathf.Clamp01(volume);

        if (Mathf.Approximately(m_bgmVolume, newVolume))
        {
            return false;
        }

        m_bgmVolume = newVolume;

        return true;
    }

    /// <summary>
    /// SE音量を設定します。
    /// </summary>
    public bool SetSeVolume(float volume)
    {
        float newVolume = Mathf.Clamp01(volume);

        if (Mathf.Approximately(m_seVolume, newVolume))
        {
            return false;
        }

        m_seVolume = newVolume;

        return true;
    }

    /// <summary>
    /// マスター音量を設定します。
    /// </summary>
    public bool SetMasterVolume(float volume)
    {
        float newVolume = Mathf.Clamp01(volume);

        if (Mathf.Approximately(m_masterVolume, newVolume))
        {
            return false;
        }

        m_masterVolume = newVolume;

        return true;
    }

    /// <summary>
    /// 画面サイズ設定を設定します。
    /// </summary>
    public bool SetScreenSize(int screenSize)
    {
        if (m_screenSize == screenSize)
        {
            return false;
        }

        m_screenSize = screenSize;

        return true;
    }

    /// <summary>
    /// カメラ感度を設定します。
    /// </summary>
    public bool SetCameraSensitivity(float sensitivity)
    {
        float newSensitivity = Mathf.Max(0.0f, sensitivity);

        if (Mathf.Approximately(
            m_cameraSensitivity,
            newSensitivity))
        {
            return false;
        }

        m_cameraSensitivity = newSensitivity;

        return true;
    }
}