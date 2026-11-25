using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 保存されている音量設定をAudioMixerへ反映します。
/// </summary>
[DisallowMultipleComponent]
public sealed class AudioSettingsApplier : MonoBehaviour
{
    [SerializeField, Header("設定変更イベント")]
    private GameSettingsChangedEvent
        m_settingsChangedEvent;

    [SerializeField, Header("セーブデータ")]
    private SaveDataController
        m_saveDataController;

    [SerializeField, Header("オーディオパラメータ")]
    private AudioSettingsParameterAsset
        m_audioParameterAsset;

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
    /// 現在保存されているすべての音量設定を反映します。
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

        if (!ValidateAudioParameter())
        {
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

        if (!ValidateAudioParameter())
        {
            return;
        }

        switch (eventData.ChangeType)
        {
            case GameSettingsChangeType.ALL:
                ApplyAll(
                    eventData.Settings);
                break;

            case GameSettingsChangeType.MASTER_VOLUME:
                ApplyMasterVolume(
                    eventData.Settings);
                break;

            case GameSettingsChangeType.BGM_VOLUME:
                ApplyBgmVolume(
                    eventData.Settings);
                break;

            case GameSettingsChangeType.SE_VOLUME:
                ApplySeVolume(
                    eventData.Settings);
                break;
        }
    }

    /// <summary>
    /// すべての音量設定を反映します。
    /// </summary>
    /// <param name="settings">
    /// 反映するゲーム設定。
    /// </param>
    private void ApplyAll(
        GameSettingsSaveData settings)
    {
        ApplyMasterVolume(settings);
        ApplyBgmVolume(settings);
        ApplySeVolume(settings);
    }

    /// <summary>
    /// Master音量を反映します。
    /// </summary>
    /// <param name="settings">
    /// 反映するゲーム設定。
    /// </param>
    private void ApplyMasterVolume(
        GameSettingsSaveData settings)
    {
        ApplyVolume(
            m_audioParameterAsset
                .MasterVolumeParameterName,
            settings.MasterVolume);
    }

    /// <summary>
    /// BGM音量を反映します。
    /// </summary>
    /// <param name="settings">
    /// 反映するゲーム設定。
    /// </param>
    private void ApplyBgmVolume(
        GameSettingsSaveData settings)
    {
        ApplyVolume(
            m_audioParameterAsset
                .BgmVolumeParameterName,
            settings.BgmVolume);
    }

    /// <summary>
    /// SE音量を反映します。
    /// </summary>
    /// <param name="settings">
    /// 反映するゲーム設定。
    /// </param>
    private void ApplySeVolume(
        GameSettingsSaveData settings)
    {
        ApplyVolume(
            m_audioParameterAsset
                .SeVolumeParameterName,
            settings.SeVolume);
    }

    /// <summary>
    /// 指定されたAudioMixerパラメータへ音量を反映します。
    /// </summary>
    /// <param name="parameterName">
    /// AudioMixerのExposed Parameter名。
    /// </param>
    /// <param name="normalizedVolume">
    /// 0～1の音量。
    /// </param>
    private void ApplyVolume(
        string parameterName,
        float normalizedVolume)
    {
        if (string.IsNullOrWhiteSpace(
            parameterName))
        {
            Debug.LogWarning(
                "AudioMixerのParameter名が設定されていません。",
                this);

            return;
        }

        AudioMixer audioMixer =
            m_audioParameterAsset.AudioMixer;

        float volumeDb =
            m_audioParameterAsset
                .ConvertNormalizedVolumeToDb(
                    normalizedVolume);

        if (audioMixer.SetFloat(
            parameterName,
            volumeDb))
        {
            return;
        }

        Debug.LogWarning(
            $"AudioMixer Parameter "
            + $"'{parameterName}' "
            + "へ値を設定できませんでした。\n"
            + "AudioMixer側でExposeされているか確認してください。",
            this);
    }

    /// <summary>
    /// Audio設定に必要な参照を確認します。
    /// </summary>
    /// <returns>
    /// true：設定を適用できます。
    /// false：必要な設定が不足しています。
    /// </returns>
    private bool ValidateAudioParameter()
    {
        if (m_audioParameterAsset == null)
        {
            Debug.LogWarning(
                $"{nameof(AudioSettingsParameterAsset)} "
                + "が設定されていません。",
                this);

            return false;
        }

        if (m_audioParameterAsset.AudioMixer == null)
        {
            Debug.LogWarning(
                $"{nameof(AudioMixer)} "
                + "が設定されていません。",
                this);

            return false;
        }

        return true;
    }
}