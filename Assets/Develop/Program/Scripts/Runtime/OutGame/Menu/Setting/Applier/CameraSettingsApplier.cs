using UnityEngine;

/// <summary>
/// セーブデータに保存されているカメラ設定を
/// PlayerCameraParameterAssetへ反映します。
///
/// PlayerCameraへ直接値を書き込まず、ParameterAssetを唯一の
/// 設定反映先にすることで、設定変更経路を一本化します。
/// </summary>
[DisallowMultipleComponent]
public sealed class CameraSettingsApplier : MonoBehaviour
{
    // --------------------------------------------------------
    // Inspector参照
    // --------------------------------------------------------

    // ゲーム設定変更通知
    [SerializeField, Header("設定変更イベント")]
    private GameSettingsChangedEvent m_settingsChangedEvent;

    // 現在のゲーム設定を保持するセーブデータ
    [SerializeField, Header("セーブデータ")]
    private SaveDataController m_saveDataController;

    // セーブ設定を反映するカメラParameterAsset
    [SerializeField, Header("カメラ設定反映対象")]
    private PlayerCameraParameterAsset m_cameraParameterAsset;

    // --------------------------------------------------------
    // Unity Event
    // --------------------------------------------------------

    /// <summary>
    /// ゲーム設定変更通知を購読します。
    /// </summary>
    private void OnEnable()
    {
        if (m_settingsChangedEvent == null)
        {
            return;
        }

        m_settingsChangedEvent.RegisterListener(
            OnSettingsChanged);
    }

    /// <summary>
    /// 起動時に現在保存されているカメラ設定を反映します。
    /// </summary>
    private void Start()
    {
        ApplyCurrentSettings();
    }

    /// <summary>
    /// ゲーム設定変更通知の購読を解除します。
    /// </summary>
    private void OnDisable()
    {
        if (m_settingsChangedEvent == null)
        {
            return;
        }

        m_settingsChangedEvent.UnregisterListener(
            OnSettingsChanged);
    }

    // --------------------------------------------------------
    // Settings Apply
    // --------------------------------------------------------

    /// <summary>
    /// 現在保存されている全カメラ設定をParameterAssetへ反映します。
    /// PlayerCameraはParameterAssetの変更通知を受けて自動更新されます。
    /// </summary>
    private void ApplyCurrentSettings()
    {
        if (!TryGetCurrentSettings(
                out GameSettingsSaveData settings))
        {
            return;
        }

        ApplyAll(
            settings);
    }

    /// <summary>
    /// ゲーム設定変更を受け取り、変更された項目だけ反映します。
    /// </summary>
    /// <param name="eventData">
    /// ゲーム設定変更情報。
    /// </param>
    private void OnSettingsChanged(
        GameSettingsChangedEventData eventData)
    {
        if (eventData.Settings == null ||
            !CanApplyCameraSettings())
        {
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
    /// 現在のセーブデータからゲーム設定を取得します。
    /// </summary>
    /// <param name="settings">
    /// 取得したゲーム設定。
    /// </param>
    /// <returns>
    /// true：取得成功。
    /// false：必要な参照が不足しています。
    /// </returns>
    private bool TryGetCurrentSettings(
        out GameSettingsSaveData settings)
    {
        settings = null;

        if (m_saveDataController == null)
        {
            Debug.LogWarning(
                $"{nameof(SaveDataController)}"
                + "が設定されていません。",
                this);

            return false;
        }

        if (!CanApplyCameraSettings())
        {
            return false;
        }

        settings =
            m_saveDataController.Settings;

        return settings != null;
    }

    /// <summary>
    /// カメラ設定の反映先が設定されているか確認します。
    /// </summary>
    private bool CanApplyCameraSettings()
    {
        if (m_cameraParameterAsset != null)
        {
            return true;
        }

        Debug.LogWarning(
            $"{nameof(PlayerCameraParameterAsset)}"
            + "が設定されていません。",
            this);

        return false;
    }

    /// <summary>
    /// 感度と反転設定をまとめて反映します。
    /// ParameterChangedは1回だけ発行されます。
    /// </summary>
    /// <param name="settings">
    /// 反映するゲーム設定。
    /// </param>
    private void ApplyAll(
        GameSettingsSaveData settings)
    {
        m_cameraParameterAsset.ApplyRuntimeSettings(
            settings.CameraSensitivity,
            settings.IsCameraReverse);
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
        m_cameraParameterAsset
            .SetCameraSensitivityRatio(
                settings.CameraSensitivity);
    }

    /// <summary>
    /// Y軸カメラ反転設定を反映します。
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
