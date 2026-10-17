using UnityEngine;

/// <summary>
/// ミサイルの爆発処理を管理します。
/// </summary>
[DisallowMultipleComponent]
public sealed class MissileExplosion : MonoBehaviour
{
    [SerializeField, Header("爆発")]
    private GameObject m_explosionPrefab;

    /// <summary>
    /// 現在位置に爆発オブジェクトを生成します。
    /// </summary>
    /// <param name="parent">爆発を生成する親Transform。</param>
    /// <param name="attackIdentifier">爆発に引き継ぐ攻撃ID。</param>
    public void Explode(
        Transform parent,
        AttackIdentifier attackIdentifier = null)
    {
        if (m_explosionPrefab == null)
        {
            Debug.LogError(
                "爆発Prefabが設定されていません。",
                this);

            return;
        }



        var explosion = Instantiate(
            m_explosionPrefab,
            transform.position,
            Quaternion.identity,
            parent);

        if (!explosion.TryGetComponent(out AttackHitbox hitbox))
        {
            Debug.LogError("ヒットボックスがアタッチされていません");
        }
        AttackDamageData damageData = new AttackDamageData(
            hitbox.CurrentDamage,
            attackIdentifier);

        hitbox.EnableHitbox(damageData);

        Destroy(gameObject);
    }
}
