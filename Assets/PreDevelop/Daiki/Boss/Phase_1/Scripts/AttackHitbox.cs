using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 攻撃用Colliderの衝突判定を管理します。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class AttackHitbox : MonoBehaviour
{
    /// <summary>
    /// 攻撃が対象に命中したときに通知されます。
    /// </summary>
    public event Action<IDamageable> AttackHit;

    /// <summary>
    /// 攻撃判定に使用するCollider。
    /// </summary>
    [SerializeField]
    private Collider m_hitboxCollider;

    /// <summary>
    /// 攻撃対象のレイヤー。
    /// </summary>
    [SerializeField]
    private LayerMask m_targetLayerMask;

    /// <summary>
    /// 基本ダメージ量。
    /// </summary>
    [SerializeField]
    private int m_defaultDamage = 10;

    /// <summary>
    /// 現在の攻撃で使用するダメージ量。
    /// </summary>
    private int m_currentDamage;

    /// <summary>
    /// 現在の攻撃で命中済みの対象ID。
    /// </summary>
    private readonly HashSet<int> m_hitTargetIds = new();

    /// <summary>
    /// 初期化します。
    /// </summary>
    private void Awake()
    {
        if (m_hitboxCollider == null)
        {
            m_hitboxCollider = GetComponent<Collider>();
        }

        if (m_hitboxCollider == null)
        {
            Debug.LogError(
                $"{nameof(Collider)}が見つかりません。",
                this);

            enabled = false;
            return;
        }

        // 攻撃判定をTriggerとして使用します。
        m_hitboxCollider.isTrigger = true;
        m_hitboxCollider.enabled = false;

        m_currentDamage = m_defaultDamage;
    }

    /// <summary>
    /// 攻撃判定を有効にします。
    /// </summary>
    public void EnableHitbox()
    {
        EnableHitbox(m_defaultDamage);
    }

    /// <summary>
    /// ダメージ量を指定して攻撃判定を有効にします。
    /// </summary>
    /// <param name="damage">与えるダメージ量。</param>
    public void EnableHitbox(int damage)
    {
        if (m_hitboxCollider == null)
        {
            Debug.LogWarning(
                $"{nameof(Collider)}が設定されていません。",
                this);

            return;
        }

        // 新しい攻撃として命中履歴を初期化します。
        m_hitTargetIds.Clear();
        m_currentDamage = Mathf.Max(0, damage);
        m_hitboxCollider.enabled = true;
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 攻撃判定を無効にします。
    /// </summary>
    public void DisableHitbox()
    {
        if (m_hitboxCollider == null)
        {
            return;
        }

        m_hitboxCollider.enabled = false;
        gameObject.SetActive(false);

    }

    /// <summary>
    /// Triggerに侵入した対象を処理します。
    /// </summary>
    /// <param name="other">侵入したCollider。</param>
    private void OnTriggerEnter(Collider other)
    {
        if (other == null)
        {
            return;
        }

        if (!IsTargetLayer(other.gameObject.layer))
        {
            return;
        }

        // Colliderの親階層からダメージ受付コンポーネントを探します。
        IDamageable damageReceiver =
            other.GetComponentInParent<IDamageable>();

        if (damageReceiver == null)
        {
            return;
        }

        Component receiverComponent = damageReceiver as Component;

        if (receiverComponent == null)
        {
            return;
        }

        int targetId = receiverComponent.GetInstanceID();

        // 同じ攻撃中に同一対象へ複数回命中することを防ぎます。
        if (!m_hitTargetIds.Add(targetId))
        {
            return;
        }

        damageReceiver.TakeDamage(m_currentDamage);
        AttackHit?.Invoke(damageReceiver);
    }

    /// <summary>
    /// 対象レイヤーか確認します。
    /// </summary>
    /// <param name="layer">確認するレイヤー。</param>
    /// <returns>
    /// true：攻撃対象のレイヤーです。
    /// false：攻撃対象のレイヤーではありません。
    /// </returns>
    private bool IsTargetLayer(int layer)
    {
        int layerMask = 1 << layer;
        return (m_targetLayerMask.value & layerMask) != 0;
    }

    /// <summary>
    /// Inspector設定時にColliderを自動取得します。
    /// </summary>
    private void Reset()
    {
        m_hitboxCollider = GetComponent<Collider>();

        if (m_hitboxCollider != null)
        {
            m_hitboxCollider.isTrigger = true;
            m_hitboxCollider.enabled = false;
        }
    }
}