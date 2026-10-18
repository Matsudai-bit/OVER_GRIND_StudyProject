using UnityEngine;
using TMPro;

public class SettingPanelController : MonoBehaviour
{
    [Header("セーブ管理の参照")]
    [SerializeField] private SaveDataController m_saveDataController;

    [Header("数値表示用のテキスト (TMP_Text型)")]
    [SerializeField] private TMP_Text m_bgmValueText;
    [SerializeField] private TMP_Text m_seValueText;
    [SerializeField] private TMP_Text m_masterValueText;
    [SerializeField] private TMP_Text m_screenSizeText; // 追加：画面サイズ用
    [SerializeField] private TMP_Text m_cameraSensitivityText;

    [Header("表示設定")]
    [SerializeField] private string m_floatFormat = "F2";

    // 変更：StartではなくOnEnableを使う（設定画面を開くたびに数値を最新化するため）
    private void OnEnable()
    {
        ApplySaveDataToUI();
    }

    public void ApplySaveDataToUI()
    {
        if (m_saveDataController == null || m_saveDataController.Settings == null) return;

        var settings = m_saveDataController.Settings;

        UpdateBgmText(settings.BgmVolume);
        UpdateSeText(settings.SeVolume);
        UpdateMasterText(settings.MasterVolume);
        UpdateScreenSizeText(settings.ScreenSize);
        UpdateCameraSensitivityText(settings.CameraSensitivity);
    }

    // --- BGM音量 ---
    public void OnClickBgmIncrease() => ChangeBgmVolume(0.05f);
    public void OnClickBgmDecrease() => ChangeBgmVolume(-0.05f);
    private void ChangeBgmVolume(float delta)
    {
        float current = m_saveDataController.Settings.BgmVolume;
        float newValue = Mathf.Clamp01(current + delta);
        if (m_saveDataController.SetBgmVolume(newValue)) UpdateBgmText(newValue);
    }
    private void UpdateBgmText(float volume)
    {
        if (m_bgmValueText != null) m_bgmValueText.text = volume.ToString(m_floatFormat);
    }

    // --- SE音量 ---
    public void OnClickSeIncrease() => ChangeSeVolume(0.05f);
    public void OnClickSeDecrease() => ChangeSeVolume(-0.05f);
    private void ChangeSeVolume(float delta)
    {
        float current = m_saveDataController.Settings.SeVolume;
        float newValue = Mathf.Clamp01(current + delta);
        if (m_saveDataController.SetSeVolume(newValue)) UpdateSeText(newValue);
    }
    private void UpdateSeText(float volume)
    {
        if (m_seValueText != null) m_seValueText.text = volume.ToString(m_floatFormat);
    }

    // --- マスター音量 ---
    public void OnClickMasterIncrease() => ChangeMasterVolume(0.05f);
    public void OnClickMasterDecrease() => ChangeMasterVolume(-0.05f);
    private void ChangeMasterVolume(float delta)
    {
        float current = m_saveDataController.Settings.MasterVolume;
        float newValue = Mathf.Clamp01(current + delta);
        if (m_saveDataController.SetMasterVolume(newValue)) UpdateMasterText(newValue);
    }
    private void UpdateMasterText(float volume)
    {
        if (m_masterValueText != null) m_masterValueText.text = volume.ToString(m_floatFormat);
    }

    // --- 画面サイズ (int) ---
    public void OnClickScreenSizeIncrease()
    {
        int current = m_saveDataController.Settings.ScreenSize;
        if (m_saveDataController.SetScreenSize(current + 1)) UpdateScreenSizeText(current + 1);
    }
    public void OnClickScreenSizeDecrease()
    {
        int current = m_saveDataController.Settings.ScreenSize;
        if (m_saveDataController.SetScreenSize(Mathf.Max(1, current - 1))) UpdateScreenSizeText(Mathf.Max(1, current - 1));
    }
    private void UpdateScreenSizeText(int size)
    {
        if (m_screenSizeText != null) m_screenSizeText.text = $"Size {size}";
    }

    // --- カメラ感度 ---
    public void OnClickCameraSensitivityIncrease() => ChangeCameraSensitivity(0.1f);
    public void OnClickCameraSensitivityDecrease() => ChangeCameraSensitivity(-0.1f);
    private void ChangeCameraSensitivity(float delta)
    {
        float current = m_saveDataController.Settings.CameraSensitivity;
        float newValue = Mathf.Max(0.0f, current + delta);
        if (m_saveDataController.SetCameraSensitivity(newValue)) UpdateCameraSensitivityText(newValue);
    }
    private void UpdateCameraSensitivityText(float sensitivity)
    {
        if (m_cameraSensitivityText != null) m_cameraSensitivityText.text = sensitivity.ToString(m_floatFormat);
    }

    // --- 保存処理 ---
    public void OnCloseSettingsPanel()
    {
        if (m_saveDataController == null) return;
        m_saveDataController.SaveIfDirty(); // 変更があった場合のみ保存と通知が行われます
    }
}