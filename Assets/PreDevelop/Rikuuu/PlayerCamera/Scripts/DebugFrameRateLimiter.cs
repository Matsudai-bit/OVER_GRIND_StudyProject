using UnityEngine;

/// <summary>検証用: FPSを固定して低FPS時の挙動を再現します。確認後に削除してください。</summary>
public class DebugFrameRateLimiter : MonoBehaviour
{
    [SerializeField, Min(1)] private int m_targetFrameRate = 30;

    private void Awake()
    {
        // VSyncが有効だとtargetFrameRateが無視されるため無効にする
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = m_targetFrameRate;
    }
}