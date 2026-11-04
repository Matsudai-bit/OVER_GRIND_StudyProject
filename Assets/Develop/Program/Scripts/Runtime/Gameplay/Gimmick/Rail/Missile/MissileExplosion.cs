using UnityEngine;

/// <summary>
/// ミサイルの爆発処理を管理し、爆発音の再生およびカメラ振動の呼び出しを行います。
/// </summary>
[DisallowMultipleComponent]
public sealed class MissileExplosion : MonoBehaviour
{
    [SerializeField, Header("爆発")]
    private GameObject m_explosionPrefab;

    [SerializeField, Header("Audio Settings")]
    private AudioID m_explosionSoundId;

    [SerializeField, Header("Camera Shake Settings")]
    private ShakePreset m_explosionShakePreset;

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

        // 爆発音を AudioManager に依頼して再生
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.Play3DAtPosition(m_explosionSoundId, transform.position);
        }

        // 爆発時のカメラ振動を CameraShakeManager に依頼（距離減衰対応）
        if (CameraShakeManager.Instance != null && m_explosionShakePreset != null)
        {
            CameraShakeManager.Instance.Play(m_explosionShakePreset, transform.position);
        }

        var explosion = Instantiate(
            m_explosionPrefab,
            transform.position,
            Quaternion.identity,
            parent);

        if (!explosion.TryGetComponent(out AttackHitbox hitbox))
        {
            Debug.LogError("ヒットボックスがアタッチされていません", this);
        }
        else
        {
            AttackDamageData damageData = new AttackDamageData(
                hitbox.CurrentDamage,
                attackIdentifier);

            hitbox.EnableHitbox(damageData);
        }

        Destroy(gameObject);
    }
}