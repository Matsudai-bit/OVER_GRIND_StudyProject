using UnityEngine;

/// <summary>プレイヤーの無敵時間設定を保持します。</summary>
[CreateAssetMenu(
    fileName = "PlayerInvincibilityParameter",
    menuName = "Game/Parameters/Player/Invincibility Parameter")]
public sealed class PlayerInvincibilityParameterAsset : ScriptableObject
{
    [SerializeField, Min(0.0f), Tooltip("被弾状態終了後に無敵となる時間（秒）。")]
    private float m_postHitDuration = 1.0f;

    /// <summary>被弾状態終了後の無敵時間を秒単位で取得します。</summary>
    public float PostHitDuration => Mathf.Max(0.0f, m_postHitDuration);
}
