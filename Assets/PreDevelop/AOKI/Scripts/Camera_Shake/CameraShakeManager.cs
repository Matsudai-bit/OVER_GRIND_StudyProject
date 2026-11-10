using UnityEngine;

/// <summary>
/// カメラ振動を一括管理するマネージャークラスです。
/// シーン内の GameObject にアタッチして使用します。
/// </summary>
public class CameraShakeManager : MonoBehaviour
{
    public static CameraShakeManager Instance { get; private set; }

    private CinemachineShake m_cinemachineShake;
    private CameraShake m_cameraShake;
    private Transform m_mainCameraTransform;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CacheComponents();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void CacheComponents()
    {
        m_cinemachineShake = FindFirstObjectByType<CinemachineShake>();
        m_cameraShake = FindFirstObjectByType<CameraShake>();

        if (Camera.main != null)
        {
            m_mainCameraTransform = Camera.main.transform;
        }
    }

    /// <summary>
    /// 距離減衰なしで振動を実行します（自機被弾やUI揺れなど）。
    /// </summary>
    public void Play(ShakePreset preset)
    {
        if (preset == null) return;
        PlayInternal(preset.Duration, preset.Magnitude, preset.IsOverwrite);
    }

    /// <summary>
    /// 発生源の位置を指定し、カメラとの距離に応じて減衰させた振動を実行します（爆発など）。
    /// </summary>
    /// <param name="preset">使用するプリセット</param>
    /// <param name="sourcePosition">爆発などの発生位置（ワールド座標）</param>
    public void Play(ShakePreset preset, Vector3 sourcePosition)
    {
        if (preset == null) return;

        EnsureCameraCached();

        if (preset.UseDistanceAttenuation && m_mainCameraTransform != null)
        {
            // カメラと発生源の距離を計算
            float distance = Vector3.Distance(sourcePosition, m_mainCameraTransform.position);

            // 距離に応じた強さを算出
            float calculatedMagnitude = preset.CalculateMagnitudeAtDistance(distance);

            // 強度が実質0なら処理しない
            if (calculatedMagnitude <= 0.001f) return;

            PlayInternal(preset.Duration, calculatedMagnitude, preset.IsOverwrite);
        }
        else
        {
            Play(preset);
        }
    }

    private void EnsureCameraCached()
    {
        if (m_mainCameraTransform == null && Camera.main != null)
        {
            m_mainCameraTransform = Camera.main.transform;
        }

        if (m_cinemachineShake == null && m_cameraShake == null)
        {
            CacheComponents();
        }
    }

    private void PlayInternal(float duration, float magnitude, bool isOverwrite)
    {
        EnsureCameraCached();

        if (m_cinemachineShake != null)
        {
            m_cinemachineShake.Shake(duration, magnitude, isOverwrite);
        }
        else if (m_cameraShake != null)
        {
            m_cameraShake.Shake(duration, magnitude, isOverwrite);
        }
    }
}