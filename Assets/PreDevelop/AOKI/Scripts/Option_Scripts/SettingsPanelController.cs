using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 設定画面のUIとSaveDataControllerを仲介・同期するコントローラーです。
/// </summary>
public class SettingsPanelController : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private SaveDataController m_saveDataController;

    [Header("UI要素 (必要に応じて割り当て)")]
    [SerializeField] private Slider m_bgmSlider;
    [SerializeField] private Slider m_seSlider;
    [SerializeField] private Slider m_masterSlider;
    [SerializeField] private Slider m_cameraSensitivitySlider;
    // 画面サイズ切り替え用（Dropdown や カスタムボタン数値管理など）
    [SerializeField] private Dropdown m_screenSizeDropdown;

    private void Start()
    {
        // ① セーブデータの値をUI要素に反映させる
        ApplySaveDataToUI();

        // UI操作イベントのリスナー登録
        RegisterUIListeners();
    }

    /// <summary>
    /// 【要件1】SaveData上の現在のパラメータを取得してUI要素へ反映します。
    /// </summary>
    public void ApplySaveDataToUI()
    {
        if (m_saveDataController == null || m_saveDataController.Settings == null)
        {
            Debug.LogWarning("SaveDataController または Settings がアタッチされていません。", this);
            return;
        }

        var settings = m_saveDataController.Settings;

        // SetValueWithoutNotify を使うことで、反映時に無駄なUIイベントが発火するのを防ぎます
        if (m_bgmSlider != null)
            m_bgmSlider.SetValueWithoutNotify(settings.BgmVolume);

        if (m_seSlider != null)
            m_seSlider.SetValueWithoutNotify(settings.SeVolume);

        if (m_masterSlider != null)
            m_masterSlider.SetValueWithoutNotify(settings.MasterVolume);

        if (m_cameraSensitivitySlider != null)
            m_cameraSensitivitySlider.SetValueWithoutNotify(settings.CameraSensitivity);

        if (m_screenSizeDropdown != null)
            m_screenSizeDropdown.SetValueWithoutNotify(settings.ScreenSize);
    }

    /// <summary>
    /// UI要素が操作された時のイベントリスナーを登録します。
    /// </summary>
    private void RegisterUIListeners()
    {
        if (m_bgmSlider != null)
            m_bgmSlider.onValueChanged.AddListener(OnBgmSliderChanged);

        if (m_seSlider != null)
            m_seSlider.onValueChanged.AddListener(OnSeSliderChanged);

        if (m_masterSlider != null)
            m_masterSlider.onValueChanged.AddListener(OnMasterSliderChanged);

        if (m_cameraSensitivitySlider != null)
            m_cameraSensitivitySlider.onValueChanged.AddListener(OnCameraSensitivityChanged);

        if (m_screenSizeDropdown != null)
            m_screenSizeDropdown.onValueChanged.AddListener(OnScreenSizeChanged);
    }

    // --- 【要件2】UI変更時にSaveDataの変更用メソッドを呼び出す ---

    public void OnBgmSliderChanged(float value)
    {
        m_saveDataController?.SetBgmVolume(value);
    }

    public void OnSeSliderChanged(float value)
    {
        m_saveDataController?.SetSeVolume(value);
    }

    public void OnMasterSliderChanged(float value)
    {
        m_saveDataController?.SetMasterVolume(value);
    }

    public void OnCameraSensitivityChanged(float value)
    {
        m_saveDataController?.SetCameraSensitivity(value);
    }

    public void OnScreenSizeChanged(int index)
    {
        m_saveDataController?.SetScreenSize(index);
    }

    /// <summary>
    /// 【要件2の仕上げ】設定画面を抜ける（「戻る」ボタン押下など）タイミングで呼び出します。
    /// 変更が存在する場合のみ書き込み（Save）と通知（SaveChangedEvent）が発生します。
    /// </summary>
    public void OnCloseSettingsPanel()
    {
        if (m_saveDataController == null) return;

        // 未保存の変更（m_isDirty）がある場合のみ、書き込み処理と通知イベントを実行
        bool wasSaved = m_saveDataController.SaveIfDirty();

        if (wasSaved)
        {
            Debug.Log("[Settings] 変更があったためセーブデータを保存し、書き込み通知を発行しました。");
        }
        else
        {
            Debug.Log("[Settings] 変更がないためセーブ処理をスキップしました。");
        }
    }
}