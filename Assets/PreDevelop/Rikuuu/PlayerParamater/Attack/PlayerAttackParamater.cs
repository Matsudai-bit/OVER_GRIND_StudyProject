using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerAttackParamater",
    menuName = "Game/Parameters/Player/Attack Parameter"
)]
public class PlayerAttackParamater : ScriptableObject
{
    // 多段ヒット攻撃の基本ヒットレート（1秒あたりのヒット回数）
    [SerializeField, Header("多段ヒット設定")]
    [Tooltip("多段ヒット攻撃の1秒間の基本ヒットレートです。")]
    [Min(0.1f)]
    private float m_baseHitsPerSecond = 40.0f;
}