using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 攻撃用Colliderの衝突判定を管理します。
/// 単発ヒットモードと多段ヒットモードの両方をサポートします。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class AttackHitbox : MonoBehaviour
{
    /// <summary>
    /// 攻撃が対象に命中したときに通知されます。
    /// </summary>
    public event Action<IDamageable> AttackHit;

    // 攻撃判定に使用するCollider
    [SerializeField, Header("攻撃判定")]
    private Collider m_hitboxCollider;

    // 攻撃対象のレイヤー
    [SerializeField]
    private LayerMask m_targetLayerMask;

    // パラメータ未設定時に使用する基本ダメージ量
    [SerializeField, Header("デフォルト設定ダメージ")]
    [Min(0)]
    private int m_defaultDamage = 10;

    [SerializeField, Header("現在のダメージ")]
    // 現在の攻撃で使用するダメージ量
    private int m_currentDamage;

    [SerializeField, Header("外部からダメージが設定されているか")]
    // 外部からダメージが設定されているか
    private bool m_hasDamageOverride;

    // 現在の攻撃で命中済みの対象ID（単発ヒットモード用）
    private readonly HashSet<int> m_hitTargetIds = new();

    // 現在Triggerに重なっている対象と、その対象側のCollider（多段ヒットモード用）
    // 貫通量計算（Physics.ComputePenetration）にも使用する
    private readonly Dictionary<IDamageable, Collider> m_overlappingTargets = new();

    // 多段ヒットモードで判定中かどうか
    private bool m_isContinuousHitMode;

    /// <summary>
    /// 現在のダメージ量を取得します。
    /// </summary>
    public int CurrentDamage => m_currentDamage;

    /// <summary>
    /// 多段ヒットモード中、現在1体以上の対象と重なっているかどうかを取得します。
    /// 攻撃中の前進を止めるかどうかの判定などに使用します。
    /// </summary>
    public bool HasOverlappingTargets =>
        m_isContinuousHitMode &&
        m_overlappingTargets.Count > 0;

    /// <summary>
    /// 初期化します。
    /// </summary>
    private void Awake()
    {
        ResolveCollider();

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

        ResetDamage();
    }

    /// <summary>
    /// 攻撃判定を単発ヒットモードで有効にします。
    /// 同一攻撃中、同じ対象には1回のみ命中します。
    /// </summary>
    public void EnableHitbox()
    {
        if (m_hitboxCollider == null)
        {
            Debug.LogWarning(
                $"{nameof(Collider)}が設定されていません。",
                this);
            return;
        }

        if (!m_hasDamageOverride)
        {
            m_currentDamage = m_defaultDamage;
        }

        // 新しい攻撃判定として命中履歴を初期化します。
        m_hitTargetIds.Clear();
        m_isContinuousHitMode = false;
        m_hitboxCollider.enabled = true;
        gameObject.SetActive(true);
    }

    /// <summary>
    /// ダメージ量を指定して攻撃判定を単発ヒットモードで有効にします。
    /// </summary>
    /// <param name="damage">与えるダメージ量。</param>
    public void EnableHitbox(int damage)
    {
        SetDamage(damage);
        EnableHitbox();
    }

    /// <summary>
    /// 攻撃判定を多段ヒットモードで有効にします。
    /// Trigger内に留まっている対象を継続的に記録し、
    /// <see cref="ApplyContinuousDamage"/>が呼ばれるたびに
    /// まとめてダメージを与える方式です。
    /// </summary>
    public void EnableContinuousHitbox()
    {
        if (m_hitboxCollider == null)
        {
            Debug.LogWarning(
                $"{nameof(Collider)}が設定されていません。",
                this);
            return;
        }

        if (!m_hasDamageOverride)
        {
            m_currentDamage = m_defaultDamage;
        }

        m_hitTargetIds.Clear();
        m_overlappingTargets.Clear();
        m_isContinuousHitMode = true;
        m_hitboxCollider.enabled = true;
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 現在の攻撃で使用するダメージ量を設定します。
    /// </summary>
    /// <param name="damage">設定するダメージ量。</param>
    public void SetDamage(int damage)
    {
        m_currentDamage = Mathf.Max(0, damage);
        m_hasDamageOverride = true;
    }

    /// <summary>
    /// ダメージ量をInspectorの初期値へ戻します。
    /// </summary>
    public void ResetDamage()
    {
        m_currentDamage = Mathf.Max(0, m_defaultDamage);
        m_hasDamageOverride = false;
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

        m_isContinuousHitMode = false;
        m_overlappingTargets.Clear();
    }

    /// <summary>
    /// 多段ヒットモード中、現在重なっている全対象へダメージを与えます。
    /// 一定周期ごとに外部（<see cref="PlayerAttackController"/>等）から
    /// 呼び出されることを想定しています。
    /// </summary>
    /// <returns>
    /// true：1体以上の対象にダメージを与えた（命中した）。
    /// false：重なっている対象がなく、命中しなかった。
    /// </returns>
    public bool ApplyContinuousDamage()
    {
        if (!m_isContinuousHitMode ||
            m_overlappingTargets.Count == 0)
        {
            return false;
        }

        // ダメージ適用中に対象が破棄されコレクションを操作する
        // 可能性があるため、キーのコピーを取ってから反復します。
        List<IDamageable> targets =
            new List<IDamageable>(m_overlappingTargets.Keys);

        bool hasHitAnyTarget = false;

        foreach (IDamageable damageReceiver in targets)
        {
            Component receiverComponent =
                damageReceiver as Component;

            // 対象が破棄されている場合は追跡から除去します。
            if (receiverComponent == null)
            {
                m_overlappingTargets.Remove(damageReceiver);
                continue;
            }

            damageReceiver.TakeDamage(m_currentDamage);
            AttackHit?.Invoke(damageReceiver);
            hasHitAnyTarget = true;
        }

        return hasHitAnyTarget;
    }

    /// <summary>
    /// 現在重なっている対象のうち、最も深く貫通している対象について、
    /// 貫通を解消するための方向・距離を取得します。
    /// <see cref="Physics.ComputePenetration"/>を使用するため、
    /// 対象側のColliderが凸形状（Box・Sphere・Capsule・Convex Mesh等）である必要があります。
    /// </summary>
    /// <param name="direction">貫通を解消する方向（正規化済み）。</param>
    /// <param name="distance">貫通している距離。</param>
    /// <returns>
    /// true：貫通している対象があり、direction・distanceが有効です。
    /// false：貫通している対象がありません。
    /// </returns>
    public bool TryGetMaxPenetration(
        out Vector3 direction,
        out float distance)
    {
        direction = Vector3.zero;
        distance = 0.0f;

        if (!m_isContinuousHitMode ||
            m_overlappingTargets.Count == 0 ||
            m_hitboxCollider == null)
        {
            return false;
        }

        bool hasFoundPenetration = false;

        foreach (Collider otherCollider in m_overlappingTargets.Values)
        {
            if (otherCollider == null)
            {
                continue;
            }

            bool isOverlapping =
                Physics.ComputePenetration(
                    m_hitboxCollider,
                    m_hitboxCollider.transform.position,
                    m_hitboxCollider.transform.rotation,
                    otherCollider,
                    otherCollider.transform.position,
                    otherCollider.transform.rotation,
                    out Vector3 penetrationDirection,
                    out float penetrationDistance);

            if (!isOverlapping)
            {
                continue;
            }

            if (!hasFoundPenetration ||
                penetrationDistance > distance)
            {
                direction = penetrationDirection;
                distance = penetrationDistance;
                hasFoundPenetration = true;
            }
        }

        return hasFoundPenetration;
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

        if (m_isContinuousHitMode)
        {
            // 多段ヒットモードでは即時ダメージを与えず、
            // 重なっている対象・対象側Colliderとして記録するのみに留めます。
            m_overlappingTargets[damageReceiver] = other;
            return;
        }

        Component receiverComponent = damageReceiver as Component;

        if (receiverComponent == null)
        {
            return;
        }

        int targetId = receiverComponent.GetInstanceID();

        // 同じ攻撃判定中に同一対象へ複数回命中することを防ぎます。
        if (!m_hitTargetIds.Add(targetId))
        {
            return;
        }

        damageReceiver.TakeDamage(m_currentDamage);
        AttackHit?.Invoke(damageReceiver);
    }

    /// <summary>
    /// Triggerから離脱した対象を処理します（多段ヒットモード用）。
    /// </summary>
    /// <param name="other">離脱したCollider。</param>
    private void OnTriggerExit(Collider other)
    {
        if (!m_isContinuousHitMode || other == null)
        {
            return;
        }

        IDamageable damageReceiver =
            other.GetComponentInParent<IDamageable>();

        if (damageReceiver == null)
        {
            return;
        }

        m_overlappingTargets.Remove(damageReceiver);
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
    /// Collider参照を取得します。
    /// </summary>
    private void ResolveCollider()
    {
        if (m_hitboxCollider != null)
        {
            return;
        }

        m_hitboxCollider = GetComponent<Collider>();
    }

    /// <summary>
    /// Inspector設定時にColliderを自動取得します。
    /// </summary>
    private void Reset()
    {
        ResolveCollider();

        if (m_hitboxCollider == null)
        {
            return;
        }

        m_hitboxCollider.isTrigger = true;
        m_hitboxCollider.enabled = false;
    }
}