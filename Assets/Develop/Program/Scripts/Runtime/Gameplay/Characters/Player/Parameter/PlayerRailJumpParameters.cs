using System;
using UnityEngine;

/// <summary>レール上ジャンプと方向指定ジャンプの調整値を保持します。</summary>
[Serializable]
public sealed class PlayerRailJumpParameters
{
    [SerializeField, Min(0.01f), Tooltip("無入力ジャンプの上向き初速（m/s）。滑走速度は別途引き継ぎます。")]
    private float m_onRailUpSpeed = 8.0f;

    [SerializeField, Min(0.01f), Tooltip("方向指定ジャンプの合成初速（m/s）。")]
    private float m_launchSpeed = 25.0f;

    [SerializeField, Range(1.0f, 89.0f), Tooltip("方向指定ジャンプの水平面からの発射角度（度）。")]
    private float m_launchAngle = 10.0f;

    [SerializeField, Range(0.0f, 0.95f), Tooltip("この大きさ以下の方向入力は無入力として扱います。")]
    private float m_directionDeadZone = 0.2f;

    [SerializeField, Min(0.0f), Tooltip("離陸直後の接地・再搭乗判定を無視する時間（秒）。")]
    private float m_landingDelay = 0.1f;

    [SerializeField, Min(0.01f), Tooltip("方向指定ジャンプで突進を続ける水平距離（m）。着地までの総距離ではありません。")]
    private float m_dashDistance = 8.0f;

    [SerializeField, Range(0.0f, 1.0f), Tooltip("突進距離に達した後に残す水平速度の割合。0で水平停止、1で維持します。")]
    private float m_dashEndSpeedRate = 0.2f;

    [SerializeField, Min(0.0f), Tooltip("方向指定ジャンプで離陸元レールとの衝突を無視する最低時間（秒）。重なりが残る場合は離れるまで延長します。")]
    private float m_railCollisionIgnoreDuration = 0.1f;

    /// <summary>離陸元レールとの衝突を無視する最低時間を取得します。</summary>
    public float RailCollisionIgnoreDuration => Mathf.Max(0.0f, m_railCollisionIgnoreDuration);

    /// <summary>突進する水平距離を取得します。</summary>
    public float DashDistance => Mathf.Max(0.01f, m_dashDistance);

    /// <summary>突進終了後に残す水平速度の割合を取得します。</summary>
    public float DashEndSpeedRate => Mathf.Clamp01(m_dashEndSpeedRate);

    /// <summary>無入力ジャンプの上向き初速を取得します。</summary>
    public float OnRailUpSpeed => Mathf.Max(0.01f, m_onRailUpSpeed);

    /// <summary>方向指定ジャンプの合成初速を取得します。</summary>
    public float LaunchSpeed => Mathf.Max(0.01f, m_launchSpeed);

    /// <summary>水平面からの発射角度を取得します。</summary>
    public float LaunchAngle => Mathf.Clamp(m_launchAngle, 1.0f, 89.0f);

    /// <summary>方向入力のデッドゾーンを取得します。</summary>
    public float DirectionDeadZone => Mathf.Clamp(m_directionDeadZone, 0.0f, 0.95f);

    /// <summary>離陸後の着地判定待ち時間を取得します。</summary>
    public float LandingDelay => Mathf.Max(0.0f, m_landingDelay);

    /// <summary>水平入力方向と角度から、方向指定ジャンプの初速を計算します。</summary>
    /// <param name="worldDirection">ワールド空間の入力方向。</param>
    /// <returns>水平方向と上方向を合成した初速。</returns>
    public Vector3 CalculateLaunchVelocity(Vector3 worldDirection)
    {
        Vector3 horizontalDirection = Vector3.ProjectOnPlane(worldDirection, Vector3.up).normalized;
        float angle = LaunchAngle * Mathf.Deg2Rad;
        return (horizontalDirection * Mathf.Cos(angle) + Vector3.up * Mathf.Sin(angle)) * LaunchSpeed;
    }
}
