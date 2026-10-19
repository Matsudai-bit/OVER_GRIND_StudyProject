using UnityEngine;

/// <summary>SplineGrindControllerの走行・離脱設定を保持します。</summary>
[CreateAssetMenu(fileName = "SplineGrindControllerParameter", menuName = "Game/Parameters/Player/Spline Grind Controller")]
public sealed class SplineGrindControllerParameterAsset : ScriptableObject
{
    [SerializeField, Min(0.0f)] private float m_baseGrindSpeed = 15.0f;
    [SerializeField, Min(0.0f)] private float m_maxGrindSpeed = 40.0f;
    [SerializeField, Min(0.0f)] private float m_exitSpeedScale = 1.0f;
    [SerializeField, Min(0.0f)] private float m_collisionExitUpSpeed = 3.0f;
    [SerializeField, Min(0.0f)] private float m_collisionExitSideSpeed = 4.0f;
    [SerializeField, Min(0.0f)] private float m_collisionExitAwaySpeed = 0.75f;
    [SerializeField, Min(0.0f)] private float m_collisionExitLandingDelay = 0.15f;
    [SerializeField, Min(0.0f)] private float m_collisionExitSideDistance = 2.0f;
    [SerializeField, Min(0.0f)] private float m_collisionExitAssistDuration = 1.0f;
    [SerializeField, Min(0.0f)] private float m_rideHeight = 2.4f;

    public float BaseGrindSpeed => Mathf.Max(0.0f, m_baseGrindSpeed);
    public float MaxGrindSpeed => Mathf.Max(BaseGrindSpeed, m_maxGrindSpeed);
    public float ExitSpeedScale => Mathf.Max(0.0f, m_exitSpeedScale);
    public float CollisionExitUpSpeed => Mathf.Max(0.0f, m_collisionExitUpSpeed);
    public float CollisionExitSideSpeed => Mathf.Max(0.0f, m_collisionExitSideSpeed);
    public float CollisionExitAwaySpeed => Mathf.Max(0.0f, m_collisionExitAwaySpeed);
    public float CollisionExitLandingDelay => Mathf.Max(0.0f, m_collisionExitLandingDelay);
    public float CollisionExitSideDistance => Mathf.Max(0.0f, m_collisionExitSideDistance);
    public float CollisionExitAssistDuration => Mathf.Max(0.0f, m_collisionExitAssistDuration);
    public float RideHeight => Mathf.Max(0.0f, m_rideHeight);
}
