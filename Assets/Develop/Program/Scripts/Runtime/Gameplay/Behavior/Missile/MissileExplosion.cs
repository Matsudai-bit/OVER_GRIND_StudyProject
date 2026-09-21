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
    public void Explode(Transform parent)
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
        hitbox.EnableHitbox();

        Destroy(gameObject);
    }
}