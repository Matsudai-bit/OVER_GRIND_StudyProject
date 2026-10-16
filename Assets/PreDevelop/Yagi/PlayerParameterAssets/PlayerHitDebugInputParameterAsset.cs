using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>PlayerHitDebugInputのデバッグ被弾設定を保持します。</summary>
[CreateAssetMenu(fileName = "PlayerHitDebugInputParameter", menuName = "Game/Parameters/Player/Hit Debug Input")]
public sealed class PlayerHitDebugInputParameterAsset : ScriptableObject
{
    [SerializeField] private Key m_hitKey = Key.F8;
    [SerializeField] private Vector3 m_localAttackCenterOffset = new Vector3(0.0f, 0.0f, 2.0f);
    [SerializeField] private bool m_applyDamage;
    [SerializeField, Min(1)] private int m_damage = 1;
    [SerializeField] private PlayerKnockbackProfile m_knockbackProfile;
    [SerializeField] private AttackIdentifier m_attackIdentifier;

    public Key HitKey => m_hitKey;
    public Vector3 LocalAttackCenterOffset => m_localAttackCenterOffset;
    public bool ApplyDamage => m_applyDamage;
    public int Damage => Mathf.Max(1, m_damage);
    public PlayerKnockbackProfile KnockbackProfile => m_knockbackProfile;
    public AttackIdentifier AttackIdentifier => m_attackIdentifier;
}
