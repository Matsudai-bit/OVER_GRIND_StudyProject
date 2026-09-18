using UnityEngine;

/// <summary>
/// プレイヤー固有のHP処理を管理します。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Health))]
public sealed class PlayerHealth : MonoBehaviour, IDirectionalDamageable
{
    // 共通HP
    private Health m_health;
    private PlayerStateMachineComponent m_stateMachine;

    [SerializeField]
    private Hurtbox m_hurtbox;

    /// <summary>
    /// 共通HPを取得します。
    /// </summary>
    public Health Health => m_health;

    /// <summary>HPと被弾状態の管理コンポーネントを取得します。</summary>
    private void Awake()
    {
        m_health = GetComponent<Health>();
        m_stateMachine = GetComponent<PlayerStateMachineComponent>();

    }

    /// <summary>死亡通知を購読します。</summary>
    private void OnEnable()
    {
        if (m_health == null)
        {
            return;
        }

        m_health.Died += HandleDied;
    }

    /// <summary>死亡通知の購読を解除します。</summary>
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

    /// <summary>攻撃位置を持たないダメージを受け、前方からの被弾として処理します。</summary>
    public void TakeDamage(int damage)
    {
        TryTakeDamage(damage, transform.position + transform.forward);
    }

    /// <summary>無敵・死亡中を除き、HPを減らして攻撃中心から吹き飛ばします。</summary>
    public bool TryTakeDamage(int damage, Vector3 attackCenter)
    {
        if (damage <= 0 || m_health == null || !m_health.IsInitialized || m_health.IsDead ||
            (m_hurtbox != null && !m_hurtbox.CanReceiveDamage) ||
            (m_stateMachine != null && m_stateMachine.IsHitReacting))
        {
            return false;
        }

        // HP変更イベントから別の攻撃が発生しても、先に無敵化して多重被弾を防ぎます。
        if (m_stateMachine != null)
        {
            m_stateMachine.TryStartHitReaction(attackCenter);
        }
        m_health.TakeDamage(damage);
        return true;
    }
}