using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerHurtParamater",
    menuName = "Game/Parameters/Player/Hurt Parameter")]
public class PlayerHurtParameterAsset : ScriptableObject
{
    // ============================================================
    // ダメージ
    // ============================================================

    [SerializeField, Header("ダメージ関連")]
    [Min(0.0f), Tooltip("受けるダメージの倍率\r\n")]
    private float m_damageMultiplier = 1.0f;

    // ============================================================
    // ノックバック
    // ============================================================

    [SerializeField, Header("被弾ノックバック関連"), Min(0.0f)]
    private float m_knockbackSpeed = 12.0f;

    [SerializeField, Min(0.0f)]
    [Tooltip("全攻撃共通のノックバック倍率。攻撃固有の倍率と乗算します。")]
    private float m_knockbackRate = 1.0f;

    [SerializeField]
    [Tooltip("攻撃別設定が渡されなかった場合の設定。未設定なら従来の速度・時間を使用します。")]
    private PlayerKnockbackProfile m_defaultKnockbackProfile;

    [SerializeField, Min(0.0f)]
    [Tooltip("被弾時に上方向へ加算する吹き飛び速度")]
    private float m_knockbackLiftSpeed = 5.0f;

    [SerializeField, Min(0.01f)]
    [Tooltip("吹き飛び状態を維持する時間")]
    private float m_knockbackDuration = 0.8f;

    [SerializeField, Min(0.0f)]
    [Tooltip("被弾後に操作可能になるまでの硬直時間（秒）")]
    private float m_hitRecoveryDuration = 0.2f;

    [SerializeField]
    [Tooltip("吹き飛びを終了させる地形・壁のレイヤー")]
    private LayerMask m_hitEnvironmentLayerMask = ~0;

    /// <summary>
    /// 被ダメージパラメータを生成します。
    /// </summary>
    /// <returns>被ダメージパラメータ。</returns>
    public PlayerHurtParameter CreateHurtParamater()
    {
        return new PlayerHurtParameter(
            m_damageMultiplier,
            m_knockbackSpeed,
            m_knockbackRate,
            m_defaultKnockbackProfile,
            m_knockbackLiftSpeed,
            m_knockbackDuration,
            m_hitRecoveryDuration,
            m_hitEnvironmentLayerMask
        );
    }
}
