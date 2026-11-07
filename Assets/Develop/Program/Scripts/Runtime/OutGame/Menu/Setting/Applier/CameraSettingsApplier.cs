using UnityEngine;

/// <summary>
/// 保存されているカメラ設定を
/// プレイヤーカメラパラメータへ反映します。
/// </summary>
[DisallowMultipleComponent]
public sealed class CameraSettingsApplier : MonoBehaviour
{
    // ゲーム設定変更イベント
    [SerializeField, Header("設定変更イベント")]
    private GameSettingsChangedEvent m_settingsChangedEvent;

    // 現在の設定取得元
    [SerializeField, Header("セーブデータ")]
    private SaveDataController m_saveDataController;

    // カメラ設定反映対象
    [SerializeField, Header("カメラ設定反映対象")]
    private PlayerCameraParameterAsset
        m_cameraParameterAsset;

    private void OnEnable()
    {
        if (m_settingsChangedEvent != null)
        {
            m_settingsChangedEvent.RegisterListener(
                OnSettingsChanged);
        }
    }

    private void Start()
    {
        ApplyCurrentSettings();
    }

    private void OnDisable()
    {
        if (m_settingsChangedEvent != null)
        {
            m_settingsChangedEvent.UnregisterListener(
                OnSettingsChanged);
        }
    }

    /// <summary>
    /// 現在保存されているカメラ設定を反映します。
    /// </summary>
    private void ApplyCurrentSettings()
    {
        if (m_saveDataController == null)
        {
            Debug.LogWarning(
                $"{nameof(SaveDataController)} "
                + "が設定されていません。",
                this);

            return;
        }

        if (m_saveDataController.Settings == null)
        {
            return;
        }

        if (m_cameraParameterAsset == null)
        {
            Debug.LogWarning(
                $"{nameof(PlayerCameraParameterAsset)} "
                + "が設定されていません。",
                this);

            return;
        }

        ApplyAll(
            m_saveDataController.Settings);
    }

    /// <summary>
    /// ゲーム設定変更を受け取ります。
    /// </summary>
    /// <param name="eventData">
    /// ゲーム設定変更情報。
    /// </param>
    private void OnSettingsChanged(
        GameSettingsChangedEventData eventData)
    {
        if (eventData.Settings == null)
        {
            return;
        }

        if (m_cameraParameterAsset == null)
        {
            Debug.LogWarning(
                $"{nameof(PlayerCameraParameterAsset)} "
                + "が設定されていません。",
                this);

            return;
        }

        switch (eventData.ChangeType)
        {
            case GameSettingsChangeType.ALL:
                ApplyAll(
                    eventData.Settings);
                break;

            case GameSettingsChangeType.CAMERA_SENSITIVITY:
                ApplySensitivity(
                    eventData.Settings);
                break;

            case GameSettingsChangeType.CAMERA_REVERSE:
                ApplyReverse(
                    eventData.Settings);
                break;
        }
    }

    /// <summary>
    /// 全カメラ設定を反映します。
    /// </summary>
    /// <param name="settings">
    /// 反映するゲーム設定。
    /// </param>
    private void ApplyAll(
        GameSettingsSaveData settings)
    {
        ApplySensitivity(settings);
        ApplyReverse(settings);
    }

    /// <summary>
    /// カメラ感度を反映します。
    /// </summary>
    /// <param name="settings">
    /// 反映するゲーム設定。
    /// </param>
    private void ApplySensitivity(
        GameSettingsSaveData settings)
    {
        float sensitivity =
            Mathf.Clamp01(
                settings.CameraSensitivity);

        m_cameraParameterAsset
            .SetCameraSensitivityRatioX(
                sensitivity);

        m_cameraParameterAsset
            .SetCameraSensitivityRatioY(
                sensitivity);
    }

    /// <summary>
    /// カメラ反転設定を反映します。
    /// </summary>
    /// <param name="settings">
    /// 反映するゲーム設定。
    /// </param>
    private void ApplyReverse(
        GameSettingsSaveData settings)
    {
        m_cameraParameterAsset
            .SetCameraReverseY(
                settings.IsCameraReverse);
    }
}