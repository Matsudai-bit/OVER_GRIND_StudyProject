using UnityEngine;

/// <summary>
/// ミサイル爆発時に、独立したプールへ攻撃判定を要求します。
/// </summary>
[DisallowMultipleComponent]
public sealed class MissileExplosion : MonoBehaviour
{
    // 爆発Hitboxの共有プール
    private ExplosionHitboxPool m_hitboxPool;

    /// <summary>爆発の攻撃判定プールを指定します。</summary>
    public void SetHitboxPool(ExplosionHitboxPool hitboxPool)
    {
        m_hitboxPool = hitboxPool;
    }

    /// <summary>爆発位置へ一時的な攻撃判定を生成します。</summary>
    /// <returns>true：生成成功。false：プール不足またはID不正。</returns>
    public bool Explode(Vector3 position, AttackIdentifier attackIdentifier)
    {
        if (m_hitboxPool == null)
        {
            Debug.LogError("[MissileExplosion] ExplosionHitboxPoolがありません。", this);
            return false;
        }

        return m_hitboxPool.TryActivate(position, attackIdentifier);
    }
}
