using System;
using UnityEngine;

/// <summary>
/// ゲーム設定に関するセーブデータです。
/// </summary>
[Serializable]
public sealed class GameSettingsSaveData
{
    [Header("音量設定")]
    /// <summary>BGM音量</summary>
    [SerializeField, Range(0.0f, 1.0f)]
    private float m_bgmVolume = 1.0f;

    /// <summary>SE音量</summary>
    [SerializeField, Range(0.0f, 1.0f)]
    private float m_seVolume = 1.0f;

    /// <summary>マスター音量</summary>
    [SerializeField, Range(0.0f, 1.0f)]
    private float m_masterVolume = 1.0f;

    [Header("画面設定")]
    /// <summary>画面サイズ設定</summary>
    [SerializeField]
    private int m_screenSize = 1;

    [Header("カメラ設定")]
    /// <summary>カメラ感度</summary>
    [SerializeField, Min(0.0f)]
    private float m_cameraSensitivity = 1.0f;

    /// <summary>カメラを反転させるかどうか</summary>
    [SerializeField]
    private bool m_isCameraReverse = false;

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
    /// カメラの反転設定を取得します。
    /// </summary>
    public bool IsCameraReverse => m_isCameraReverse;

    /// <summary>
    /// BGM音量を設定します。
    /// </summary>
    /// <param name="volume">設定するBGM音量。</param>
    /// <returns>
    /// true：値が変更されました。
    /// false：値は変更されませんでした（既に同じ値です）。
    /// </returns>
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
    /// <param name="volume">設定するSE音量。</param>
    /// <returns>
    /// true：値が変更されました。
    /// false：値は変更されませんでした（既に同じ値です）。
    /// </returns>
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
    /// <param name="volume">設定するマスター音量。</param>
    /// <returns>
    /// true：値が変更されました。
    /// false：値は変更されませんでした（既に同じ値です）。
    /// </returns>
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
    /// <param name="screenSize">設定する画面サイズのインデックス。</param>
    /// <returns>
    /// true：値が変更されました。
    /// false：値は変更されませんでした（既に同じ値です）。
    /// </returns>
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
    /// <param name="sensitivity">設定するカメラ感度。</param>
    /// <returns>
    /// true：値が変更されました。
    /// false：値は変更されませんでした（既に同じ値です）。
    /// </returns>
    public bool SetCameraSensitivity(float sensitivity)
    {
        float newSensitivity = Mathf.Max(0.0f, sensitivity);

        if (Mathf.Approximately(m_cameraSensitivity, newSensitivity))
        {
            return false;
        }

        m_cameraSensitivity = newSensitivity;
        return true;
    }

    /// <summary>
    /// カメラの反転設定を設定します。
    /// </summary>
    /// <param name="isReverse">反転させるかどうか。</param>
    /// <returns>
    /// true：値が変更されました。
    /// false：値は変更されませんでした（既に同じ値です）。
    /// </returns>
    public bool SetCameraReverse(bool isReverse)
    {
        if (m_isCameraReverse == isReverse)
        {
            return false;
        }

        m_isCameraReverse = isReverse;
        return true;
    }
}