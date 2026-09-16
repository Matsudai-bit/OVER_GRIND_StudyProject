using System;
using UnityEngine;

/// <summary>
/// オブジェクトのHPを管理します。
/// </summary>
[DisallowMultipleComponent]
public sealed class Health : MonoBehaviour, IDamageable
{
    // HPの最小値
    private const int MIN_HEALTH = 0;

    // 最大HP
    [SerializeField, Min(1), Header("最大HP")]
    private int m_maxHealth = 100;

    // 現在のHP
    [SerializeField, Header("現在のHP")]
    private int m_currentHealth;

    // 初期化が完了しているか
    private bool m_isInitialized;

    /// <summary>
    /// HPが変更されたときに通知します。
    /// </summary>
    public event Action<int, int> HealthChanged;

    /// <summary>
    /// HPが0になったときに通知します。
    /// </summary>
    public event Action Died;

    /// <summary>
    /// 現在のHPを取得します。
    /// </summary>
    public int CurrentHealth => m_currentHealth;

    /// <summary>
    /// 最大HPを取得します。
    /// </summary>
    public int MaxHealth => m_maxHealth;

    /// <summary>
    /// 初期化が完了しているか取得します。
    /// </summary>
    public bool IsInitialized => m_isInitialized;

    /// <summary>
    /// HPが0か取得します。
    /// </summary>
    public bool IsDead =>
        m_isInitialized &&
        m_currentHealth <= MIN_HEALTH;

    private void Awake()
    {
        InitializeHealth();
    }

    /// <summary>
    /// ダメージを受けます。
    /// </summary>
    /// <param name="damage">受けるダメージ量。</param>
    public void TakeDamage(int damage)
    {
        if (damage <= 0 || IsDead)
        {
            return;
        }

        SetCurrentHealth(m_currentHealth - damage);
    }

    /// <summary>
    /// HPを回復します。
    /// </summary>
    /// <param name="healAmount">回復するHP量。</param>
    public void Heal(int healAmount)
    {
        if (healAmount <= 0 || IsDead)
        {
            return;
        }

        SetCurrentHealth(m_currentHealth + healAmount);
    }

    /// <summary>
    /// 最大HPを変更します。
    /// </summary>
    /// <param name="maxHealth">変更後の最大HP。</param>
    /// <param name="restoreHealth">HPを最大まで回復するか。</param>
    public void SetMaxHealth(
        int maxHealth,
        bool restoreHealth)
    {
        if (maxHealth <= MIN_HEALTH)
        {
            Debug.LogWarning(
                $"{nameof(Health)}: 最大HPは1以上である必要があります。",
                this);

            return;
        }

        m_maxHealth = maxHealth;

        if (restoreHealth)
        {
            m_currentHealth = m_maxHealth;
        }
        else
        {
            m_currentHealth = Mathf.Clamp(
                m_currentHealth,
                MIN_HEALTH,
                m_maxHealth);
        }

        NotifyHealthChanged();
    }

    /// <summary>
    /// HPを最大まで回復します。
    /// </summary>
    public void RestoreHealth()
    {
        SetCurrentHealth(m_maxHealth);
    }

    /// <summary>
    /// 死亡状態から復帰します。
    /// </summary>
    /// <param name="health">復帰後のHP。</param>
    public void Revive(int health)
    {
        if (!IsDead || health <= MIN_HEALTH)
        {
            return;
        }

        SetCurrentHealth(health);
    }

    /// <summary>
    /// HPを初期化します。
    /// </summary>
    private void InitializeHealth()
    {
        m_currentHealth = m_maxHealth;
        m_isInitialized = true;

        NotifyHealthChanged();
    }

    /// <summary>
    /// 現在のHPを変更します。
    /// </summary>
    /// <param name="health">変更後のHP。</param>
    private void SetCurrentHealth(int health)
    {
        int clampedHealth = Mathf.Clamp(
            health,
            MIN_HEALTH,
            m_maxHealth);

        if (m_currentHealth == clampedHealth)
        {
            return;
        }

        bool wasAlive = !IsDead;

        m_currentHealth = clampedHealth;

        NotifyHealthChanged();

        if (wasAlive && IsDead)
        {
            Died?.Invoke();
        }
    }

    /// <summary>
    /// HP変更を通知します。
    /// </summary>
    private void NotifyHealthChanged()
    {
        HealthChanged?.Invoke(
            m_currentHealth,
            m_maxHealth);
    }
  
#if UNITY_EDITOR
    private void OnValidate()
    {
        m_maxHealth = Mathf.Max(1, m_maxHealth);
    }

#endif
}