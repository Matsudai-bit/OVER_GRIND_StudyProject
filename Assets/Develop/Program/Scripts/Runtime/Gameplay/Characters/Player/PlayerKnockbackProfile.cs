using UnityEngine;

/// <summary>攻撃種類ごとにプレイヤーが受けるノックバック量を定義します。</summary>
[CreateAssetMenu(fileName = "PlayerKnockbackProfile", menuName = "Player/Knockback Profile")]
public sealed class PlayerKnockbackProfile : ScriptableObject
{
    [SerializeField, Min(0.0f), Tooltip("速度に掛ける倍率。0で吹き飛びなし、1で標準です。")]
    private float m_knockbackRate = 1.0f;
    [SerializeField, Min(0.0f)] private float m_horizontalSpeed = 12.0f;
    [SerializeField, Min(0.0f)] private float m_liftSpeed = 5.0f;
    [SerializeField, Min(0.01f)] private float m_duration = 0.8f;
    [SerializeField, Min(0.0f)] private float m_recoveryDuration = 0.2f;

    /// <summary>攻撃固有のノックバック倍率を取得します。</summary>
    public float KnockbackRate => Mathf.Max(0.0f, m_knockbackRate);
    /// <summary>倍率適用前の水平初速を取得します。</summary>
    public float HorizontalSpeed => Mathf.Max(0.0f, m_horizontalSpeed);
    /// <summary>倍率適用前の上向き初速を取得します。</summary>
    public float LiftSpeed => Mathf.Max(0.0f, m_liftSpeed);
    /// <summary>吹き飛びの最大時間を取得します。</summary>
    public float Duration => Mathf.Max(0.01f, m_duration);
    /// <summary>吹き飛び後の復帰待機時間を取得します。</summary>
    public float RecoveryDuration => Mathf.Max(0.0f, m_recoveryDuration);
}
