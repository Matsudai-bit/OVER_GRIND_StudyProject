using UnityEngine;

/// <summary>
/// 振動パラメータを定義する ScriptableObject です。
/// </summary>
[CreateAssetMenu(fileName = "ShakePreset", menuName = "Camera/Shake Preset")]
public class ShakePreset : ScriptableObject
{
    [SerializeField, Header("継続時間（秒）")]
    private float m_duration = 0.3f;

    [SerializeField, Header("基準の振動強度")]
    private float m_magnitude = 1.0f;

    [SerializeField, Header("既存の振動を上書きするか")]
    private bool m_isOverwrite = false;

    [Header("距離減衰設定")]
    [SerializeField, Header("距離減衰を有効にするか")]
    private bool m_useDistanceAttenuation = false;

    [SerializeField, Header("最大強度を維持する最小距離")]
    private float m_minDistance = 2.0f;

    [SerializeField, Header("振動が完全に消える最大距離")]
    private float m_maxDistance = 20.0f;

    [SerializeField, Header("距離に応じた減衰カーブ (0=近, 1=遠)")]
    private AnimationCurve m_attenuationCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

    public float Duration => m_duration;
    public float Magnitude => m_magnitude;
    public bool IsOverwrite => m_isOverwrite;
    public bool UseDistanceAttenuation => m_useDistanceAttenuation;

    /// <summary>
    /// 発生源からの距離に応じて実際の振動強度を計算します。
    /// </summary>
    /// <param name="distance">カメラ（またはプレイヤー）からの距離</param>
    /// <returns>減衰後の振動強度</returns>
    public float CalculateMagnitudeAtDistance(float distance)
    {
        // 距離減衰が無効の場合は基準強度をそのまま返す
        if (!m_useDistanceAttenuation)
        {
            return m_magnitude;
        }

        // 最小距離以内なら100%の強さ
        if (distance <= m_minDistance)
        {
            return m_magnitude;
        }

        // 最大距離以上なら揺らさない
        if (distance >= m_maxDistance)
        {
            return 0f;
        }

        // 最小距離～最大距離の間を 0.0 ～ 1.0 に正規化
        float t = Mathf.InverseLerp(m_minDistance, m_maxDistance, distance);

        // カーブに基づいて減衰率（1.0 ～ 0.0）を取得して強度に掛ける
        float attenuation = m_attenuationCurve.Evaluate(t);
        return m_magnitude * attenuation;
    }
}