using UnityEngine;

/// <summary>PlayerStateMachineComponentの被弾設定を保持します。</summary>
[CreateAssetMenu(fileName = "PlayerStateMachineParameter", menuName = "Game/Parameters/Player/State Machine")]
public sealed class PlayerStateMachineParameterAsset : ScriptableObject
{
    [SerializeField] private PlayerRailJumpParameterAsset m_railJumpParameterAsset;
    [SerializeField, Min(0.0f)] private float m_knockbackSpeed = 12.0f;
    [SerializeField, Min(0.0f)] private float m_knockbackRate = 1.0f;
    [SerializeField] private PlayerKnockbackProfile m_defaultKnockbackProfile;
    [SerializeField, Min(0.0f)] private float m_knockbackLiftSpeed = 5.0f;
    [SerializeField, Min(0.01f)] private float m_knockbackDuration = 0.8f;
    [SerializeField, Min(0.0f)] private float m_hitRecoveryDuration = 0.2f;
    [SerializeField] private PlayerInvincibilityParameterAsset m_invincibilityParameterAsset;
    [SerializeField] private LayerMask m_hitEnvironmentLayerMask = ~0;

    public PlayerRailJumpParameterAsset RailJumpParameterAsset => m_railJumpParameterAsset;
    public float KnockbackSpeed => Mathf.Max(0.0f, m_knockbackSpeed);
    public float KnockbackRate => Mathf.Max(0.0f, m_knockbackRate);
    public PlayerKnockbackProfile DefaultKnockbackProfile => m_defaultKnockbackProfile;
    public float KnockbackLiftSpeed => Mathf.Max(0.0f, m_knockbackLiftSpeed);
    public float KnockbackDuration => Mathf.Max(0.01f, m_knockbackDuration);
    public float HitRecoveryDuration => Mathf.Max(0.0f, m_hitRecoveryDuration);
    public PlayerInvincibilityParameterAsset InvincibilityParameterAsset => m_invincibilityParameterAsset;
    public LayerMask HitEnvironmentLayerMask => m_hitEnvironmentLayerMask;
}
