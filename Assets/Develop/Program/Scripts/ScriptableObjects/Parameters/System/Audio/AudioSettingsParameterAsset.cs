using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// オーディオ設定を実際のAudioMixerへ反映するための
/// パラメータを保持します。
/// </summary>
[CreateAssetMenu(
    fileName = "AudioSettingsParameter",
    menuName = "Game/Parameters/Audio/Audio Settings Parameter")]
public sealed class AudioSettingsParameterAsset : ScriptableObject
{
    private const float DEFAULT_MIN_VOLUME_DB = -80.0f;
    private const float DEFAULT_MAX_VOLUME_DB = 0.0f;

    [SerializeField, Header("Audio Mixer")]
    [Tooltip("音量設定を反映するAudioMixer。")]
    private AudioMixer m_audioMixer;

    [SerializeField, Header("Exposed Parameter")]
    [Tooltip("Master音量のExposed Parameter名。")]
    private string m_masterVolumeParameterName =
        "MasterVolume";

    [SerializeField]
    [Tooltip("BGM音量のExposed Parameter名。")]
    private string m_bgmVolumeParameterName =
        "BGMVolume";

    [SerializeField]
    [Tooltip("SE音量のExposed Parameter名。")]
    private string m_seVolumeParameterName =
        "SEVolume";

    [SerializeField, Header("音量範囲")]
    [Range(-80.0f, 0.0f)]
    [Tooltip("ミュート時に使用する最小音量[dB]。")]
    private float m_minVolumeDb =
        DEFAULT_MIN_VOLUME_DB;

    [SerializeField, Range(-20.0f, 20.0f)]
    [Tooltip("音量1.0時に使用する最大音量[dB]。")]
    private float m_maxVolumeDb =
        DEFAULT_MAX_VOLUME_DB;

    /// <summary>
    /// 設定対象のAudioMixerを取得します。
    /// </summary>
    public AudioMixer AudioMixer =>
        m_audioMixer;

    /// <summary>
    /// Master音量のExposed Parameter名を取得します。
    /// </summary>
    public string MasterVolumeParameterName =>
        m_masterVolumeParameterName;

    /// <summary>
    /// BGM音量のExposed Parameter名を取得します。
    /// </summary>
    public string BgmVolumeParameterName =>
        m_bgmVolumeParameterName;

    /// <summary>
    /// SE音量のExposed Parameter名を取得します。
    /// </summary>
    public string SeVolumeParameterName =>
        m_seVolumeParameterName;

    /// <summary>
    /// 最小音量を取得します。
    /// </summary>
    public float MinVolumeDb =>
        m_minVolumeDb;

    /// <summary>
    /// 最大音量を取得します。
    /// </summary>
    public float MaxVolumeDb =>
        m_maxVolumeDb;

    /// <summary>
    /// 0～1の音量をAudioMixer用のdB値へ変換します。
    /// </summary>
    /// <param name="normalizedVolume">
    /// 0～1の音量。
    /// </param>
    /// <returns>
    /// AudioMixerへ設定するdB値。
    /// </returns>
    public float ConvertNormalizedVolumeToDb(
        float normalizedVolume)
    {
        normalizedVolume =
            Mathf.Clamp01(normalizedVolume);

        // 0の場合はLog10を使用できないため
        // 明示的に最小dBを返す
        if (normalizedVolume <= 0.0f)
        {
            return m_minVolumeDb;
        }

        float volumeDb =
            m_maxVolumeDb
            + 20.0f * Mathf.Log10(normalizedVolume);

        return Mathf.Clamp(
            volumeDb,
            m_minVolumeDb,
            m_maxVolumeDb);
    }

#if UNITY_EDITOR

    private void OnValidate()
    {
        if (m_maxVolumeDb < m_minVolumeDb)
        {
            m_maxVolumeDb =
                m_minVolumeDb;
        }
    }

#endif
}