using UnityEngine;

/// <summary>
/// 設定画面とSaveDataControllerの受け渡しを管理するスクリプトです。
/// </summary>
public sealed class SettingsUIController : MonoBehaviour
{
    [Header("コントローラー参照")]
    [SerializeField]
    private SaveDataController m_saveDataController;

    [Header("各ゲージの参照")]
    [SerializeField]
    private ConfigGaugeParamater m_masterVolumeGauge;

    [SerializeField]
    private ConfigGaugeParamater m_bgmVolumeGauge;

    [SerializeField]
    private ConfigGaugeParamater m_seVolumeGauge;

    private void OnEnable()
    {
        // 設定画面が開いたときにセーブデータの値をゲージに反映します
        LoadToUI();
    }

    /// <summary>
    /// ① セーブデータから数値を取得して各ゲージに反映させる（データの受け取り）
    /// </summary>
    public void LoadToUI()
    {
        if (m_saveDataController == null)
        {
            m_saveDataController = FindFirstObjectByType<SaveDataController>();
        }

        if (m_saveDataController == null || m_saveDataController.Settings == null)
        {
            return;
        }

        var settings = m_saveDataController.Settings;

        // ※ ConfigGaugeParamater 側に値をセットするメソッド（例: SetValue や UpdateValue）がある場合、
        // 以下のようにセーブデータの値を渡します。
        // if (m_masterVolumeGauge != null) m_masterVolumeGauge.SetValue(settings.MasterVolume);
        // if (m_bgmVolumeGauge != null) m_bgmVolumeGauge.SetValue(settings.BgmVolume);
        // if (m_seVolumeGauge != null) m_seVolumeGauge.SetValue(settings.SeVolume);
    }

    /// <summary>
    /// ③ 変更があった場合のみ保存し、通知イベントを発行する（書き込み・通知）
    /// </summary>
    public void ApplyAndSave()
    {
        if (m_saveDataController != null)
        {
            // 変更（IsDirty）が存在するときだけファイル保存とイベント通知が実行されます
            m_saveDataController.SaveIfDirty();
        }
    }
}