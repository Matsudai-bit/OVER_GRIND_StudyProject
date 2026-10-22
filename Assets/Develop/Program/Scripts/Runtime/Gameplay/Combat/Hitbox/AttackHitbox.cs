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

    [SerializeField, Tooltip("攻撃部の中心。未設定の場合は攻撃Colliderの中心を使用します。")]
    private Transform m_attackCenter;

    /// <summary>攻撃部の中心位置をワールド座標で取得します。</summary>
    public Vector3 AttackCenter => m_attackCenter != null
        ? m_attackCenter.position
        : m_hitboxCollider != null && m_hitboxCollider.enabled
            ? m_hitboxCollider.bounds.center
            : transform.position;

    /// <summary>攻撃部の中心を外部から設定します。nullでCollider中心へ戻します。</summary>
    public void SetAttackCenter(Transform attackCenter)
    {
        m_attackCenter = attackCenter;
    }

    /// <summary>攻撃中心を対応する受け手へ渡し、無敵中の命中通知を抑制します。</summary>
    private bool TryApplyDamage(IDamageable damageReceiver)
    {
        AttackDamageData damageData = m_currentDamageData;

        if (AttackPowerContext.TryGetCurrent(
                out AttackPowerType attackPowerType))
        {
            damageData =
                damageData.WithAttackPowerType(attackPowerType);
        }
        else if (AttackPowerSourceRegistry.TryGet(
                     transform,
                     out attackPowerType))
        {
            damageData =
                damageData.WithAttackPowerType(attackPowerType);
        }

        if (damageReceiver is IDirectionalDamageable directionalReceiver)
        {
            return directionalReceiver.TryTakeDamage(damageData, AttackCenter);
        }

        damageReceiver.TakeDamage(damageData);
        return true;
    }

    // 攻撃対象のレイヤー
    [SerializeField]
    private LayerMask m_targetLayerMask;

    // パラメータ未設定時に使用する基本ダメージ量
    [SerializeField, Header("デフォルト設定ダメージ")]
    [Min(0)]
    private int m_defaultDamage = 10;

    [SerializeField, Header("現在のダメージ")]
    // 現在の攻撃で使用するダメージ量
    private AttackDamageData m_currentDamageData;

    [SerializeField, Header("外部からダメージが設定されているか")]
    // 外部からダメージが設定されているか
    private bool m_hasDamageOverride;

    // 現在の攻撃で命中済みの対象ID（単発ヒットモード用）
    private readonly HashSet<int> m_hitTargetIds = new();

    // 現在Triggerに重なっている対象と、その対象側のCollider（多段ヒットモード用）
    // 貫通量計算（Physics.ComputePenetration）にも使用する
    private readonly Dictionary<IDamageable, Collider> m_overlappingTargets = new();
    // 同じHurtbox配下のColliderを個別に保持し、一部の離脱で対象を失わないようにします。
    private readonly Dictionary<Collider, IDamageable> m_contactColliders = new();

    // 接触回数方式の対象を周期ダメージから除外するために保持します。
    private readonly HashSet<IDamageable> m_contactDamageTargets = new();

    // 攻撃接触の通知対象と、対象ごとの接触Collider数を管理します。
    private readonly Dictionary<IAttackContactReceiver, int> m_contactReceiverCounts = new();

    // プレイヤーの前進を止める対象との接触数を管理します。
    private readonly Dictionary<IAttackMovementBlocker, int> m_movementBlockerCounts = new();

    // 多段ヒットモードで判定中かどうか
    private bool m_isContinuousHitMode;

    /// <summary>
    /// 現在のダメージ量を取得します。
    /// </summary>
    public int CurrentDamage => m_currentDamageData.Damage;

    /// <summary>
    /// 現在の攻撃情報を取得します。
    /// </summary>
    public AttackDamageData CurrentDamageData => m_currentDamageData;

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
            m_currentDamageData = new AttackDamageData(m_defaultDamage);
        }

        // 新しい攻撃判定として命中履歴と以前の接触状態を初期化します。
        ClearAttackContacts();
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
    /// 攻撃情報を指定して攻撃判定を単発ヒットモードで有効にします。
    /// </summary>
    /// <param name="damageData">使用するダメージと攻撃識別情報。</param>
    public void EnableHitbox(AttackDamageData damageData)
    {
        SetDamageData(damageData);
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
            m_currentDamageData = new AttackDamageData(m_defaultDamage);
        }

        ClearAttackContacts();
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
        SetDamageData(new AttackDamageData(damage));
    }

    /// <summary>
    /// 現在の攻撃で使用するダメージと攻撃識別情報を設定します。
    /// </summary>
    /// <param name="damageData">設定する攻撃情報。</param>
    public void SetDamageData(AttackDamageData damageData)
    {
        m_currentDamageData = damageData;
        m_hasDamageOverride = true;
    }

    /// <summary>
    /// ダメージ量をInspectorの初期値へ戻します。
    /// </summary>
    public void ResetDamage()
    {
        m_currentDamageData = new AttackDamageData(m_defaultDamage);
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
        ClearAttackContacts();
        m_isContinuousHitMode = false;
        m_overlappingTargets.Clear();
        gameObject.SetActive(false);
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
                m_contactDamageTargets.Remove(damageReceiver);
                continue;
            }

            if (m_contactDamageTargets.Contains(damageReceiver))
            {
                continue;
            }

            if (!TryApplyDamage(damageReceiver))
            {
                continue;
            }
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
        if (other == null || m_contactColliders.ContainsKey(other))
        {
            return;
        }

        if (!IsTargetLayer(other.gameObject.layer))
        {
            return;
        }

        // Colliderの親階層からダメージ受付コンポーネントを探します。
        IDamageable damageReceiver =
            (IDamageable)other.GetComponentInParent<Hurtbox>() ?? other.GetComponentInParent<IDamageable>();

        if (damageReceiver == null)
        {
            return;
        }

        bool wasAlreadyOverlapping =
            IsDamageReceiverAlreadyOverlapping(damageReceiver);

        m_contactColliders.Add(other, damageReceiver);
        RegisterAttackContact(other);
        RegisterMovementBlocker(other);

        if (m_isContinuousHitMode)
        {
            m_overlappingTargets[damageReceiver] = other;

            if (other.GetComponentInParent<IAttackContactDamageReceiver>() != null)
            {
                m_contactDamageTargets.Add(damageReceiver);

                // 同じ対象の複数Colliderへ同時に触れても、1回の接触として扱います。
                if (!wasAlreadyOverlapping &&
                    TryApplyDamage(damageReceiver))
                {
                    AttackHit?.Invoke(damageReceiver);
                }
            }

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

        if (TryApplyDamage(damageReceiver))
        {
            AttackHit?.Invoke(damageReceiver);
        }
    }

    /// <summary>判定の再有効化などでEnterを取り逃がした接触を復元します。</summary>
    /// <param name="other">接触中のCollider。</param>
    private void OnTriggerStay(Collider other)
    {
        if (m_isContinuousHitMode && !m_contactColliders.ContainsKey(other))
        {
            OnTriggerEnter(other);
        }
    }

    /// <summary>
    /// Triggerから離脱した対象を処理します（多段ヒットモード用）。
    /// </summary>
    /// <param name="other">離脱したCollider。</param>
    private void OnTriggerExit(Collider other)
    {
        if (other == null || !m_contactColliders.TryGetValue(other, out IDamageable damageReceiver))
        {
            return;
        }

        m_contactColliders.Remove(other);
        UnregisterAttackContact(other);
        UnregisterMovementBlocker(other);

        if (!m_isContinuousHitMode)
        {
            return;
        }

        foreach (KeyValuePair<Collider, IDamageable> contact in m_contactColliders)
        {
            if (contact.Value == damageReceiver && contact.Key != null &&
                contact.Key.enabled && contact.Key.gameObject.activeInHierarchy)
            {
                m_overlappingTargets[damageReceiver] = contact.Key;
                return;
            }
        }

        m_overlappingTargets.Remove(damageReceiver);
        m_contactDamageTargets.Remove(damageReceiver);
    }

    /// <summary>同じダメージ受付対象との接触が既に存在するか確認します。</summary>
    /// <param name="damageReceiver">確認するダメージ受付対象。</param>
    /// <returns>true：接触中。false：未接触。</returns>
    private bool IsDamageReceiverAlreadyOverlapping(
        IDamageable damageReceiver)
    {
        foreach (IDamageable trackedReceiver in m_contactColliders.Values)
        {
            if (trackedReceiver == damageReceiver)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 移動遮断対象との接触を登録します。
    /// </summary>
    /// <param name="other">接触した対象側Collider。</param>
    private void RegisterMovementBlocker(Collider other)
    {
        IAttackMovementBlocker movementBlocker =
            other.GetComponentInParent<IAttackMovementBlocker>();

        if (movementBlocker == null)
        {
            return;
        }

        m_movementBlockerCounts.TryGetValue(
            movementBlocker,
            out int blockerContactCount);
        m_movementBlockerCounts[movementBlocker] = blockerContactCount + 1;

        if (blockerContactCount == 0 && m_movementBlockerCounts.Count == 1)
        {
            AttackMovementBlockRegistry.Register(this);
        }
    }

    /// <summary>
    /// 移動遮断対象との接触を解除します。
    /// </summary>
    /// <param name="other">離れた対象側Collider。</param>
    private void UnregisterMovementBlocker(Collider other)
    {
        IAttackMovementBlocker movementBlocker =
            other.GetComponentInParent<IAttackMovementBlocker>();

        if (movementBlocker == null ||
            !m_movementBlockerCounts.TryGetValue(movementBlocker, out int blockerContactCount))
        {
            return;
        }

        if (blockerContactCount <= 1)
        {
            m_movementBlockerCounts.Remove(movementBlocker);

            if (m_movementBlockerCounts.Count == 0)
            {
                AttackMovementBlockRegistry.Unregister(this);
            }
        }
        else
        {
            m_movementBlockerCounts[movementBlocker] = blockerContactCount - 1;
        }
    }

    /// <summary>攻撃接触の開始を対象へ通知します。</summary>
    /// <param name="other">接触した対象側Collider。</param>
    private void RegisterAttackContact(Collider other)
    {
        IAttackContactReceiver contactReceiver =
            other.GetComponentInParent<IAttackContactReceiver>();

        if (contactReceiver == null)
        {
            return;
        }

        m_contactReceiverCounts.TryGetValue(
            contactReceiver,
            out int contactCount);
        m_contactReceiverCounts[contactReceiver] = contactCount + 1;

        if (contactCount == 0)
        {
            contactReceiver.BeginAttackContact(GetInstanceID());
        }
    }

    /// <summary>攻撃接触の終了を対象へ通知します。</summary>
    /// <param name="other">離れた対象側Collider。</param>
    private void UnregisterAttackContact(Collider other)
    {
        IAttackContactReceiver contactReceiver =
            other.GetComponentInParent<IAttackContactReceiver>();

        if (contactReceiver == null ||
            !m_contactReceiverCounts.TryGetValue(contactReceiver, out int contactCount))
        {
            return;
        }

        if (contactCount > 1)
        {
            m_contactReceiverCounts[contactReceiver] = contactCount - 1;
            return;
        }

        m_contactReceiverCounts.Remove(contactReceiver);
        contactReceiver.EndAttackContact(GetInstanceID());
    }

    /// <summary>
    /// 追跡中の攻撃接触と移動遮断状態をすべて解除します。
    /// </summary>
    private void ClearAttackContacts()
    {
        int attackSourceId = GetInstanceID();

        foreach (IAttackContactReceiver contactReceiver in m_contactReceiverCounts.Keys)
        {
            Component receiverComponent = contactReceiver as Component;

            if (receiverComponent != null)
            {
                contactReceiver.EndAttackContact(attackSourceId);
            }
        }

        m_contactReceiverCounts.Clear();
        m_contactColliders.Clear();
        m_overlappingTargets.Clear();
        m_contactDamageTargets.Clear();
        m_movementBlockerCounts.Clear();
        AttackMovementBlockRegistry.Unregister(this);
    }

    /// <summary>非アクティブ化時に残っている接触状態を解除します。</summary>
    private void OnDisable()
    {
        ClearAttackContacts();
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
