using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>デバッグキーでプレイヤーの被弾状態を再現します。</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerHealth), typeof(PlayerStateMachineComponent), typeof(PlayerInputReader))]
public sealed class PlayerHitDebugInput : MonoBehaviour
{
    [SerializeField] private Key m_hitKey = Key.F8;
    [SerializeField, Tooltip("未設定の場合はプレイヤーの前方を攻撃中心にします。")]
    private Transform m_attackCenter;
    [SerializeField] private Vector3 m_localAttackCenterOffset = new Vector3(0.0f, 0.0f, 2.0f);
    [SerializeField, Tooltip("OFFならHPを減らさず、繰り返し被弾動作を確認できます。")]
    private bool m_applyDamage;
    [SerializeField, Min(1)] private int m_damage = 1;
    [SerializeField, Tooltip("再現する攻撃の設定。未設定時はプレイヤーの標準設定を使用します。")]
    private PlayerKnockbackProfile m_knockbackProfile;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private PlayerHealth m_health;
    private PlayerStateMachineComponent m_stateMachine;

    /// <summary>同じGameObjectの被弾コンポーネントを取得します。</summary>
    private void ResolveReferences()
    {
        m_health = GetComponent<PlayerHealth>();
        m_stateMachine = GetComponent<PlayerStateMachineComponent>();
    }

    /// <summary>設定したキーの押下を検知し、被弾再現を要求します。</summary>
    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || m_hitKey == Key.None)
        {
            return;
        }

        if (!keyboard[m_hitKey].wasPressedThisFrame)
        {
            return;
        }

        SimulateHit();
    }

    /// <summary>キー入力またはコンテキストメニューから被弾を再現し、結果を通知します。</summary>
    [ContextMenu("Debug/Simulate Hit")]
    private void SimulateHit()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[PlayerHitDebugInput] Play中に実行してください。", this);
            return;
        }

        // Play中のスクリプト再読み込み後にも参照を取り直します。
        ResolveReferences();
        if (Time.timeScale <= 0.0f)
        {
            Debug.LogWarning("[PlayerHitDebugInput] ポーズ中です。解除してから再実行してください。", this);
            return;
        }

        if (m_stateMachine == null || !m_stateMachine.IsInitialized || !m_stateMachine.isActiveAndEnabled)
        {
            Debug.LogWarning("[PlayerHitDebugInput] PlayerStateMachineComponentが未初期化または無効です。" +
                "PlayerRoot配下のPlayerに取り付け、Playを再開始してください。", this);
            return;
        }

        if (m_stateMachine.IsHitReacting)
        {
            Debug.Log("[PlayerHitDebugInput] 既に被弾中です。復帰してから再実行してください。", this);
            return;
        }

        Vector3 attackCenter = m_attackCenter != null
            ? m_attackCenter.position
            : transform.TransformPoint(m_localAttackCenterOffset);

        bool accepted;
        if (m_applyDamage)
        {
            accepted = m_health != null && m_health.TryTakeDamage(Mathf.Max(1, m_damage), attackCenter, m_knockbackProfile);
        }
        else
        {
            // 動作だけの確認は通常入力の有効状態やHPに依存させません。
            accepted = m_stateMachine.TryStartHitReaction(attackCenter, m_knockbackProfile);
        }

        if (accepted)
        {
            Debug.Log($"[PlayerHitDebugInput] 被弾を再現しました。Key={m_hitKey}, Damage={m_applyDamage}", this);
        }
        else
        {
            Debug.LogWarning("[PlayerHitDebugInput] 被弾が拒否されました。" +
                "HPの初期化・死亡状態・Hurtboxの受付設定を確認してください。", this);
        }
    }
#endif
}
