using UnityEngine;

/// <summary>
/// プレイヤー固有のHP処理を管理します。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Health))]
public sealed class PlayerHealth : MonoBehaviour, IDamageable
{
    // 共通HP
    private Health m_health;

    [SerializeField]
    private Hurtbox m_hurtbox;

    /// <summary>
    /// 共通HPを取得します。
    /// </summary>
    public Health Health => m_health;

    private void Awake()
    {
        m_health = GetComponent<Health>();

    }

    private void OnEnable()
    {
        if (m_health == null)
        {
            return;
        }

        m_health.Died += HandleDied;
    }

    private void OnDisable()
    {
        if (m_health == null)
        {
            return;
        }

        m_health.Died -= HandleDied;
    }


    /// <summary>
    /// プレイヤーのHPを回復します。
    /// </summary>
    /// <param name="healAmount">回復するHP量。</param>
    public void Heal(int healAmount)
    {
        m_health.Heal(healAmount);
    }

    /// <summary>
    /// プレイヤーが死亡したときの処理を実行します。
    /// </summary>
    private void HandleDied()
    {
        Debug.Log("プレイヤーが死亡しました。", this);

        // 入力無効化、死亡ステートへの変更などを実行
    }

    public void TakeDamage(int damage)
    {
        m_health.TakeDamage(damage);
    }
}