using UnityEngine;

/// <summary>
/// ステージ1ボス用の攻撃ダメージパラメータを保持します。
/// </summary>
[CreateAssetMenu(
    fileName = "S1BossAttackDamageParameter",
    menuName = "Game/Parameters/Boss/S1 Boss Attack Damage Parameter")]
public sealed class S1BossAttackDamageParameterAsset
    : AttackDamageParameterAsset<S1BossHitboxId>
{
}
