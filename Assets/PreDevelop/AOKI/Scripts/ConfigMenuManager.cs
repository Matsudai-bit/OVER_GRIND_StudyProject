using UnityEngine;

/// <summary>
/// 設定画面とセーブデータを繋ぐ中継クラス
/// </summary>
public class ConfigMenuManager : MonoBehaviour
{
    [Header("セーブデータ管理")]
    [SerializeField]
    private SaveDataController m_saveDataController;

    [Header("設定項目のUIパーツ (選択式 / int型)")]
    [SerializeField] private ConfigSelectParamater m_screenSizeSelect;

    [Header("設定項目のUIパーツ (ゲージ式 / float型)")]
    [SerializeField] private ConfigGaugeParamater m_masterVolumeGauge;
    [SerializeField] private ConfigGaugeParamater m_bgmVolumeGauge;
    [SerializeField] private ConfigGaugeParamater m_seVolumeGauge;
    [SerializeField] private ConfigGaugeParamater m_cameraSensitivityGauge;

    private void Start()
    {
        // --------------------------------------------------------
        // 1. セーブデータから設定画面にパラメータを反映させる
        // --------------------------------------------------------
        ApplySaveDataToUI();
    }

    /// <summary>
    /// 現在のセーブデータを読み取り、UIの初期表示に反映します。
    /// </summary>
    private void ApplySaveDataToUI()
    {
        if (m_saveDataController == null || m_saveDataController.Settings == null)
        {
            return;
        }

        // 実行中の設定データを取得
        GameSettingsSaveData settings = m_saveDataController.Settings;

        // 【画面サイズの反映】(int型)
        if (m_screenSizeSelect != null)
        {
            m_screenSizeSelect.SetDefaultIndex(settings.ScreenSize);
        }

        // 【各種音量・カメラ感度の反映】(float型)
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
    }

    // --------------------------------------------------------
    // 2. 設定画面の設定をセーブデータに反映する
    // --------------------------------------------------------

    /// <summary>
    /// 画面サイズが変更された時 (ConfigSelectParamaterから呼ばれる)
    /// </summary>
    public void OnScreenSizeChanged(int index)
    {
        if (m_saveDataController != null)
        {
            m_saveDataController.SetScreenSize(index);
        }
    }

    /// <summary>
    /// マスター音量が変更された時 (ConfigGaugeParamaterから呼ばれる)
    /// </summary>
    public void OnMasterVolumeChanged(float volume)
    {
        if (m_saveDataController != null)
        {
            m_saveDataController.SetMasterVolume(volume);
        }
    }

    /// <summary>
    /// BGM音量が変更された時 (ConfigGaugeParamaterから呼ばれる)
    /// </summary>
    public void OnBgmVolumeChanged(float volume)
    {
        if (m_saveDataController != null)
        {
            m_saveDataController.SetBgmVolume(volume);
        }
    }

    /// <summary>
    /// SE音量が変更された時 (ConfigGaugeParamaterから呼ばれる)
    /// </summary>
    public void OnSeVolumeChanged(float volume)
    {
        if (m_saveDataController != null)
        {
            m_saveDataController.SetSeVolume(volume);
        }
    }

    /// <summary>
    /// カメラ感度が変更された時 (ConfigGaugeParamaterから呼ばれる)
    /// </summary>
    public void OnCameraSensitivityChanged(float sensitivity)
    {
        if (m_saveDataController != null)
        {
            m_saveDataController.SetCameraSensitivity(sensitivity);
        }
    }

    // --------------------------------------------------------
    // 3. 書き込みの通知を送る動作
    // --------------------------------------------------------

    /// <summary>
    /// 変更があればファイルに保存し、通知イベントを発行します。
    /// </summary>
    public void SaveConfigToDisk()
    {
        if (m_saveDataController != null)
        {
            // 未保存の変更が存在する場合のみ保存処理が走る
            m_saveDataController.SaveIfDirty();
        }
    }

    /// <summary>
    /// ★追加：右側の詳細設定から左側のカテゴリメニューに戻った時に呼ぶ処理
    /// </summary>
    public void OnReturnToCategoryMenu()
    {
        // メニューに戻ったタイミングで、変更状態をチェックしてセーブを走らせる
        SaveConfigToDisk();
    }

    private void OnEnable()
    {
        // 画面が開く（表示される）たびにセーブデータの値をUIへ反映する
        ApplySaveDataToUI();
    }
}