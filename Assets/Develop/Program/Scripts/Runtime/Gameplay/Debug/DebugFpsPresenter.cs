using TMPro;
using UnityEngine;

/// <summary>
/// 現在のFPSを計測してUIへ表示します。
/// </summary>
[DisallowMultipleComponent]
public sealed class DebugFpsPresenter : MonoBehaviour
{
    // FPS更新間隔の最小値
    private const float MIN_UPDATE_INTERVAL = 0.1f;

    // FPS表示
    [SerializeField, Header("UI")]
    private TMP_Text m_fpsText;

    // FPS表示の更新間隔
    [SerializeField, Header("FPS設定"), Min(MIN_UPDATE_INTERVAL)]
    private float m_updateInterval = 0.5f;

    // 計測中の経過時間
    private float m_elapsedTime = 0.0f;

    // 計測中のフレーム数
    private int m_frameCount = 0;

    private void OnEnable()
    {
        ResetMeasurement();
    }

    private void Update()
    {
        m_elapsedTime += Time.unscaledDeltaTime;
        m_frameCount++;

        if (m_elapsedTime < m_updateInterval)
        {
            return;
        }

        UpdateFps();

        ResetMeasurement();
    }

    /// <summary>
    /// FPS表示を更新します。
    /// </summary>
    private void UpdateFps()
    {
        if (m_fpsText == null
            || m_elapsedTime <= 0.0f)
        {
            return;
        }

        float fps =
            m_frameCount / m_elapsedTime;

        m_fpsText.text =
            $"FPS : {fps:F1}";
    }

    /// <summary>
    /// FPS計測状態をリセットします。
    /// </summary>
    private void ResetMeasurement()
    {
        m_elapsedTime = 0.0f;
        m_frameCount = 0;
    }
}