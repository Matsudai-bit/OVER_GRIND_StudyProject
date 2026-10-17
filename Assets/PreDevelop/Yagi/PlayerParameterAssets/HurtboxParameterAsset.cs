using UnityEngine;

/// <summary>Hurtboxのダメージ受付設定を保持します。</summary>
[CreateAssetMenu(fileName = "HurtboxParameter", menuName = "Game/Parameters/Player/Hurtbox")]
public sealed class HurtboxParameterAsset : ScriptableObject
{
    [SerializeField, Min(0.0f)] private float m_damageMultiplier = 1.0f;
    [SerializeField] private bool m_canReceiveDamage = true;
    public float DamageMultiplier => Mathf.Max(0.0f, m_damageMultiplier);
    public bool CanReceiveDamage => m_canReceiveDamage;
}
