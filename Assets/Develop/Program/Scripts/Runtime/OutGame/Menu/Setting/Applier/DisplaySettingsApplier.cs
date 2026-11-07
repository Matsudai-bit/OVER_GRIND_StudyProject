using UnityEngine;

/// <summary>
/// 保存されている画面設定を実際のゲーム画面へ反映します。
/// </summary>
[DisallowMultipleComponent]
public sealed class DisplaySettingsApplier : MonoBehaviour
{
    private readonly struct ResolutionData
    {
        public int Width { get; }
        public int Height { get; }

        public ResolutionData(
            int width,
            int height)
        {
            Width = width;
            Height = height;
        }
    }

    // 対応解像度
    private static readonly ResolutionData[] RESOLUTIONS =
    {
        new ResolutionData(1920, 1080),
        new ResolutionData(1280, 720),
        new ResolutionData(800, 600)
    };

    [SerializeField, Header("設定変更イベント")]
    private GameSettingsChangedEvent m_settingsChangedEvent;

    [SerializeField, Header("セーブデータ")]
    private SaveDataController m_saveDataController;

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
    /// 現在保存されている画面設定を反映します。
    /// </summary>
    private void ApplyCurrentSettings()
    {
        if (m_saveDataController == null
            || m_saveDataController.Settings == null)
        {
            return;
        }

        ApplyResolution(
            m_saveDataController.Settings.ScreenSize);
    }

    /// <summary>
    /// ゲーム設定変更を受け取ります。
    /// </summary>
    private void OnSettingsChanged(
        GameSettingsChangedEventData eventData)
    {
        if (eventData.Settings == null)
        {
            return;
        }

        switch (eventData.ChangeType)
        {
            case GameSettingsChangeType.ALL:
            case GameSettingsChangeType.SCREEN_SIZE:
                ApplyResolution(
                    eventData.Settings.ScreenSize);
                break;
        }
    }

    /// <summary>
    /// 指定された解像度設定を反映します。
    /// </summary>
    private void ApplyResolution(int index)
    {
        if (index < 0
            || index >= RESOLUTIONS.Length)
        {
            Debug.LogWarning(
                $"不正な解像度Indexです: {index}",
                this);

            return;
        }

        ResolutionData resolution =
            RESOLUTIONS[index];

        Screen.SetResolution(
            resolution.Width,
            resolution.Height,
            Screen.fullScreenMode);
    }
}