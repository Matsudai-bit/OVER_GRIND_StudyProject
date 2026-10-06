using UnityEngine;

/// <summary>
/// プレイヤーの被ダメージ時に使用するパラメータを保持します。
/// </summary>
public readonly struct PlayerHurtParameter
{
    /// <summary>
    /// 被ダメージ時パラメータを生成します。
    /// </summary>
    /// <param name="damageMultiplier">         受けるダメージの倍率。                      </param>
    /// <param name="knockbackSpeed">           ノックバック速度。                          </param>
    /// <param name="knockbackRate">            ノックバック倍率。                          </param>
    /// <param name="defaultKnockBackProfile">  攻撃別設定が渡されなかった場合の設定。      </param>
    /// <param name="knockbackLiftSpeed">       被弾時に上方向へ加算する吹き飛び速度。      </param>
    /// <param name="knockbackDuration">        吹き飛び状態を維持する時間。                </param>
    /// <param name="hitRecoveryDuration">      被弾後に操作可能になるまでの硬直時間（秒）。</param>
    /// <param name="hitEnvironmentLayerMask">  吹き飛びを終了させる地形・壁のレイヤー。    </param>
    public PlayerHurtParameter(
        float damageMultiplier,
        float knockbackSpeed,
        float knockbackRate,
        PlayerKnockbackProfile defaultKnockBackProfile,
        float knockbackLiftSpeed,
        float knockbackDuration,
        float hitRecoveryDuration,
        LayerMask hitEnvironmentLayerMask)
    {
        DamageMultiplier = damageMultiplier;
        KnockbackSpeed = knockbackSpeed;
        KnockBackRate = knockbackRate;
        DefaultKnockBackProfile = defaultKnockBackProfile;
        KnockBackLiftSpeed = knockbackLiftSpeed;
        KnockBackDuration = knockbackDuration;
        HitRecoveryDuration = hitRecoveryDuration;
        HitEnvironmentLayerMask = hitEnvironmentLayerMask;
    }

    // ============================================================
    // ダメージ
    // ============================================================

    /// <summary>
    /// 受けるダメージの倍率を取得します。
    /// </summary>
    public float DamageMultiplier { get; }

    // ============================================================
    // ノックバック
    // ============================================================

    /// <summary>
    /// ノックバック速度を取得します。
    /// </summary>
    public float KnockbackSpeed { get; }

    /// <summary>
    /// ノックバック倍率を取得します。
    /// </summary>
    public float KnockBackRate { get; }

    /// <summary>
    /// 攻撃別設定が渡されなかった場合の設定を取得します。
    /// </summary>
    public PlayerKnockbackProfile DefaultKnockBackProfile { get; }

    /// <summary>
    /// 被弾時に上方向へ加算する吹き飛び速度を取得します。
    /// </summary>
    public float KnockBackLiftSpeed { get; }

    /// <summary>
    /// 吹き飛び状態を維持する時間を取得します。
    /// </summary>
    public float KnockBackDuration { get; }

    /// <summary>
    /// 被弾後に操作可能になるまでの硬直時間（秒）を取得します。
    /// </summary>
    public float HitRecoveryDuration { get; }

    /// <summary>
    /// 吹き飛びを終了させる地形・壁のレイヤーを取得します。
    /// </summary>
    public LayerMask HitEnvironmentLayerMask { get; }
}
