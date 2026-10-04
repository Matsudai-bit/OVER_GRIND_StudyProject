using UnityEngine;

/// <summary>プレイヤーの標準ノックバックと、攻撃IDごとの設定をまとめて定義します。</summary>
[CreateAssetMenu(fileName = "PlayerKnockbackProfile", menuName = "Player/Knockback Profile")]
public sealed class PlayerKnockbackProfile : ScriptableObject
{
    [Header("標準設定（攻撃IDが未設定・未登録の場合）")]
    [SerializeField, Min(0.0f), Tooltip("速度に掛ける倍率。0で吹き飛びなし、1で標準です。")]
    private float m_knockbackRate = 1.0f;
    [SerializeField, Min(0.0f)] private float m_horizontalSpeed = 12.0f;
    [SerializeField, Min(0.0f)] private float m_liftSpeed = 5.0f;
    [SerializeField, Min(0.01f)] private float m_duration = 0.8f;
    [SerializeField, Min(0.0f)] private float m_recoveryDuration = 0.2f;

    [SerializeField, Header("攻撃ごとの設定"), Tooltip("攻撃アセットとノックバックを登録します。同じ攻撃が重複した場合は先頭を使用します。")]
    private PlayerAttackKnockbackSettings[] m_attackSettings = new PlayerAttackKnockbackSettings[0];

    /// <summary>同一の攻撃アセットが登録された設定を取得します。</summary>
    /// <param name="attackIdentifier">受けた攻撃のID。</param>
    /// <param name="settings">一致した設定。見つからない場合はnull。</param>
    /// <returns>登録済みならtrue、未設定・未登録ならfalse。</returns>
    public bool TryGetSettings(AttackIdentifier attackIdentifier, out PlayerAttackKnockbackSettings settings)
    {
        settings = null;
        if (attackIdentifier == null || m_attackSettings == null)
        {
            return false;
        }

        foreach (PlayerAttackKnockbackSettings entry in m_attackSettings)
        {
            if (entry != null && entry.AttackIdentifier == attackIdentifier)
            {
                settings = entry;
                return true;
            }
        }

        return false;
    }

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
