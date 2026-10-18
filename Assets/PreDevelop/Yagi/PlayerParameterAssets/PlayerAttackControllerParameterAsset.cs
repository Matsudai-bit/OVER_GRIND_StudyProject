using UnityEngine;

/// <summary>PlayerAttackControllerの攻撃設定を保持します。</summary>
[CreateAssetMenu(fileName = "PlayerAttackControllerParameter", menuName = "Game/Parameters/Player/Attack Controller")]
public sealed class PlayerAttackControllerParameterAsset : ScriptableObject
{
    [SerializeField, Min(0.0f)] private float m_airAttachHoldDuration = 0.15f;
    [SerializeField, Min(0.01f)] private float m_airAttachFallAcceleration = 0.75f;
    [SerializeField, Min(0.0f)] private float m_airAttachContactGrace = 0.2f;
    [SerializeField, Min(0.01f)] private float m_airAttachMaxDistance = 1.0f;
    [SerializeField, Min(0.1f)] private float m_airAttackFallSpeed = 2.0f;
    [SerializeField, Min(0.0f)] private float m_airHitStopDuration = 0.08f;
    [SerializeField, Min(0.0f)] private float m_airHitFallSpeed = 0.5f;
    [SerializeField, Min(0.1f)] private float m_baseHitsPerSecond = 40.0f;
    [SerializeField, Min(0.1f)] private float m_chargedHitsPerSecond = 60.0f;

    public float AirAttachHoldDuration => Mathf.Max(0.0f, m_airAttachHoldDuration);
    public float AirAttachFallAcceleration => Mathf.Max(0.01f, m_airAttachFallAcceleration);
    public float AirAttachContactGrace => Mathf.Max(0.0f, m_airAttachContactGrace);
    public float AirAttachMaxDistance => Mathf.Max(0.01f, m_airAttachMaxDistance);
    public float AirAttackFallSpeed => Mathf.Max(0.1f, m_airAttackFallSpeed);
    public float AirHitStopDuration => Mathf.Max(0.0f, m_airHitStopDuration);
    public float AirHitFallSpeed => Mathf.Max(0.0f, m_airHitFallSpeed);
    public float BaseHitsPerSecond => Mathf.Max(0.1f, m_baseHitsPerSecond);
    public float ChargedHitsPerSecond => Mathf.Max(0.1f, m_chargedHitsPerSecond);
}
