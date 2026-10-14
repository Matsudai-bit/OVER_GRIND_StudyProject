using UnityEngine;

/// <summary>
/// 設定画面のUIとセーブデータを繋ぐ中継クラス
/// </summary>
public class ConfigMenuManager : MonoBehaviour
{
    [Header("セーブデータ管理")]
    /// <summary>セーブデータ管理クラスの参照</summary>
    [SerializeField]
    private SaveDataController m_saveDataController;

    [Header("設定項目のUIパーツ (選択式 / int型)")]
    /// <summary>画面サイズ設定のUIパーツ</summary>
    [SerializeField]
    private ConfigSelectParamater m_screenSizeSelect;

    [Header("設定項目のUIパーツ (ゲージ式 / float型)")]
    /// <summary>マスター音量設定のUIパーツ</summary>
    [SerializeField]
    private ConfigGaugeParamater m_masterVolumeGauge;

    /// <summary>BGM音量設定のUIパーツ</summary>
    [SerializeField]
    private ConfigGaugeParamater m_bgmVolumeGauge;

    /// <summary>SE音量設定のUIパーツ</summary>
    [SerializeField]
    private ConfigGaugeParamater m_seVolumeGauge;

    /// <summary>カメラ感度設定のUIパーツ</summary>
    [SerializeField]
    private ConfigGaugeParamater m_cameraSensitivityGauge;

    [Header("UI参照 (スイッチ式 / bool型)")]
    /// <summary>カメラ反転設定のUIパーツ</summary>
    [SerializeField]
    private ConfigSwitchParamater m_cameraReverseSwitch; 

    [Header("その他の設定")]
    /// <summary>カメラを反転させるかどうか</summary>
    [SerializeField]
    private bool m_isCameraReverse = false;

    private void Start()
    {
        ApplySaveDataToUI();
    }

    private void OnEnable()
    {
        ApplySaveDataToUI();
    }

    /// <summary>
    /// 現在のセーブデータを読み取り、UIの初期表示に反映します。
    /// </summary>
    private void ApplySaveDataToUI()
    {
        if (m_saveDataController == null || m_saveDataController.CurrentData == null || m_saveDataController.CurrentData.Settings == null)
        {
            return;
        }

        var settings = m_saveDataController.CurrentData.Settings;

        if (m_screenSizeSelect != null)
        {
            m_screenSizeSelect.SetDefaultIndex(settings.ScreenSize);
        }

        if (m_masterVolumeGauge != null)
        {
            m_masterVolumeGauge.SetDefaultValue(settings.MasterVolume);
        }

        if (m_bgmVolumeGauge != null)
        {
            m_bgmVolumeGauge.SetDefaultValue(settings.BgmVolume);
        }

        if (m_seVolumeGauge != null)
        {
            m_seVolumeGauge.SetDefaultValue(settings.SeVolume);
        }

        if (m_cameraSensitivityGauge != null)
        {
            m_cameraSensitivityGauge.SetDefaultValue(settings.CameraSensitivity);
        }

        // セーブデータからカメラ反転フラグを変数に読み込む
        m_isCameraReverse = settings.IsCameraReverse;

        // UIにセーブデータの初期値を反映させる
        if (m_cameraReverseSwitch != null)
        {
            m_cameraReverseSwitch.SetDefaultValue(m_isCameraReverse);
        }
    }

    /// <summary>
    /// 画面サイズが変更された時に呼び出されます。
    /// </summary>
    public void OnScreenSizeChanged(int index)
    {
        if (m_saveDataController != null)
        {
            m_saveDataController.SetScreenSize(index);
        }
    }

    /// <summary>
    /// マスター音量が変更された時に呼び出されます。
    /// </summary>
    public void OnMasterVolumeChanged(float volume)
    {
        if (m_saveDataController != null)
        {
            m_saveDataController.SetMasterVolume(volume);
        }
    }

    /// <summary>
    /// BGM音量が変更された時に呼び出されます。
    /// </summary>
    public void OnBgmVolumeChanged(float volume)
    {
        if (m_saveDataController != null)
        {
            m_saveDataController.SetBgmVolume(volume);
        }
    }

    /// <summary>
    /// SE音量が変更された時に呼び出されます。
    /// </summary>
    public void OnSeVolumeChanged(float volume)
    {
        if (m_saveDataController != null)
        {
            m_saveDataController.SetSeVolume(volume);
        }
    }

    /// <summary>
    /// カメラ感度が変更された時に呼び出されます。
    /// </summary>
    public void OnCameraSensitivityChanged(float sensitivity)
    {
        if (m_saveDataController != null)
        {
            m_saveDataController.SetCameraSensitivity(sensitivity);
        }
    }

    /// <summary>
    /// カメラの反転設定が変更された時に呼び出されます。
    /// </summary>
    /// <param name="isReverse">反転させるかどうか。</param>
    public void OnCameraReverseChanged(bool isReverse)
    {

        m_isCameraReverse = isReverse;

        if (m_saveDataController != null)
        {
            m_saveDataController.SetCameraReverse(isReverse);
        }
    }

    /// <summary>
    /// 設定画面からカテゴリメニューに戻った時に呼び出されます。
    /// </summary>
    public void OnReturnToCategoryMenu()
    {
        SaveConfigToDisk();
    }

    /// <summary>
    /// 変更があればファイルに保存し、通知イベントを発行します。
    /// </summary>
    public void SaveConfigToDisk()
    {
        if (m_saveDataController != null)
        {
            m_saveDataController.SaveIfDirty();
        }
    }
}