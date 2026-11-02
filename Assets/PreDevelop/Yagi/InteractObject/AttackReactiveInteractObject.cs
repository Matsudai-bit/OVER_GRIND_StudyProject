using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 攻撃ダメージを蓄積し、規定値に到達したときに物理的に吹き飛びます。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
public sealed class AttackReactiveInteractObject : MonoBehaviour,
    IDirectionalDamageable,
    IAttackContactReceiver,
    IAttackContactDamageReceiver,
    IAttackMovementBlocker
{
    private const float DIRECTION_EPSILON = 0.0001f;
    private const float GROUND_CHECK_START_OFFSET = 0.02f;
    private const int GROUND_HIT_BUFFER_SIZE = 8;
    private static readonly int BASE_COLOR_PROPERTY_ID =
        Shader.PropertyToID("_BaseColor");
    private static readonly int COLOR_PROPERTY_ID =
        Shader.PropertyToID("_Color");

    [SerializeField, Header("Damage")]
    [Min(1)]
    private int m_requiredDamage = 100;

    [SerializeField]
    [Tooltip("有効な場合は通常攻撃でもダメージを蓄積します。")]
    private bool m_canBeLaunchedByNormalAttack = true;

    [SerializeField, Header("Launch")]
    [Min(0.0f)]
    private float m_launchImpulse = 12.0f;

    [SerializeField, Range(0.0f, 1.0f)]
    [Tooltip("吹き飛び方向へ加える上向き成分です。")]
    private float m_upwardDirectionRate = 0.35f;

    [SerializeField, Min(0.0f)]
    [Tooltip("RigidbodyのMassによる影響を維持したまま、吹き飛びの強さに掛ける倍率です。")]
    private float m_launchImpulseMultiplier = 1.0f;

    [SerializeField, Range(0.0f, 1.0f)]
    [Tooltip("Playerの体の向きへ吹き飛び方向を補正する割合です。0は従来方向、1はPlayerの正面方向です。")]
    private float m_playerFacingCorrectionRate = 0.35f;

    [SerializeField, Header("Physics")]
    [Tooltip("最初に吹き飛ぶまでは物理的に動かない状態にします。")]
    private bool m_startAsKinematic = true;

    [SerializeField]
    [Tooltip("親階層のTransform同期で物理移動が上書きされないよう、物理開始時に親から切り離します。")]
    private bool m_detachFromParentForPhysics = true;

    [SerializeField, Min(0.0f)]
    [Tooltip("底面4点の接地を確定するまでの時間です。")]
    private float m_settleDuration = 0.1f;

    [SerializeField, Min(0.0f)]
    [Tooltip("攻撃待機へ戻る際に許容する最大移動速度です。")]
    private float m_maxSettleLinearSpeed = 0.15f;

    [SerializeField, Min(0.0f)]
    [Tooltip("攻撃待機へ戻る際に許容する最大回転速度（rad/s）です。")]
    private float m_maxSettleAngularSpeed = 0.35f;

    [SerializeField, Header("Ground Check")]
    [Tooltip("底面判定に使用する地面レイヤーです。")]
    private LayerMask m_groundLayerMask = ~0;

    [SerializeField, Min(0.01f)]
    [Tooltip("底面4点から地面を確認する距離です。")]
    private float m_groundCheckDistance = 0.12f;

    [SerializeField, Min(0.001f)]
    [Tooltip("底面4点の接地判定に使用するSphereCastの半径です。")]
    private float m_groundCheckRadius = 0.06f;

    [SerializeField, Range(0.0f, 0.45f)]
    [Tooltip("底面判定点をColliderの角から内側へ寄せる割合です。")]
    private float m_groundCheckInsetRate = 0.1f;

    [SerializeField, Range(0.0f, 1.0f)]
    [Tooltip("地面として扱う面の法線とワールド上方向の最小一致度です。")]
    private float m_minGroundNormalDot = 0.7f;

    [SerializeField, Header("Hit Shake")]
    [Tooltip("命中中に揺らす見た目のTransform。未設定時は最初の子を使用します。")]
    private Transform m_visualRoot;

    [SerializeField, Min(0.0f)]
    [Tooltip("命中中の位置揺れ幅です。")]
    private float m_shakePositionAmplitude = 0.04f;

    [SerializeField, Min(0.0f)]
    [Tooltip("命中中の回転揺れ幅（度）です。")]
    private float m_shakeRotationAngle = 3.0f;

    [SerializeField, Min(0.0f)]
    [Tooltip("命中中の揺れ速度です。")]
    private float m_shakeFrequency = 28.0f;

    [SerializeField, Header("Debug")]
    [Tooltip("攻撃判定との接触中、Visual Root配下のRendererを指定色で表示します。")]
    private bool m_showHitDebugColor = true;

    [SerializeField]
    [Tooltip("攻撃判定との接触中に使用するデバッグ色です。")]
    private Color m_hitDebugColor = Color.red;

    private Rigidbody m_rigidbody;
    private BoxCollider m_boxCollider;
    private int m_accumulatedDamage;
    private bool m_hasBeenLaunched;
    private bool m_isRecoveringFromLaunch;
    private bool m_hasAcceptedDamageDuringContact;
    private float m_settleTimer;
    private float m_lastGroundContactTime = float.NegativeInfinity;
    private Vector3 m_visualBaseLocalPosition;
    private Quaternion m_visualBaseLocalRotation;
    private Renderer[] m_visualRenderers;
    private MaterialPropertyBlock[] m_originalPropertyBlocks;
    private readonly HashSet<int> m_attackContactSourceIds = new();
    private readonly RaycastHit[] m_groundHits =
        new RaycastHit[GROUND_HIT_BUFFER_SIZE];
    private AttackReactiveInteractObjectGroup m_group;

    /// <summary>現在蓄積しているダメージを取得します。</summary>
    public int AccumulatedDamage => m_accumulatedDamage;

    /// <summary>一度以上吹き飛ばされたか取得します。</summary>
    public bool HasBeenLaunched => m_hasBeenLaunched;

    /// <summary>必要な参照を取得し、初期物理状態を設定します。</summary>
    private void Awake()
    {
        m_rigidbody = GetComponent<Rigidbody>();
        m_boxCollider = GetComponent<BoxCollider>();

        if (m_rigidbody == null || m_boxCollider == null)
        {
            Debug.LogError(
                $"{nameof(Rigidbody)}または{nameof(BoxCollider)}が設定されていません。",
                this);
            enabled = false;
            return;
        }

        DetachFromParentForPhysics();
        m_rigidbody.isKinematic = m_startAsKinematic;
        ResolveVisualRoot();

        if (m_visualRoot != null)
        {
            m_visualBaseLocalPosition = m_visualRoot.localPosition;
            m_visualBaseLocalRotation = m_visualRoot.localRotation;
            CacheVisualRenderers();
        }
    }

    /// <summary>
    /// 物理対象を親階層から切り離し、現在のワールド姿勢を維持します。
    /// </summary>
    private void DetachFromParentForPhysics()
    {
        if (!m_detachFromParentForPhysics || transform.parent == null)
        {
            return;
        }

        transform.SetParent(null, true);
    }

    /// <summary>攻撃判定が接触している間、モデルだけを振動させます。</summary>
    private void LateUpdate()
    {
        if (m_isRecoveringFromLaunch ||
            !m_hasAcceptedDamageDuringContact ||
            m_attackContactSourceIds.Count == 0 ||
            m_visualRoot == null)
        {
            return;
        }

        float phase = Time.time * m_shakeFrequency;
        Vector3 positionOffset = new Vector3(
            Mathf.Sin(phase),
            Mathf.Sin(phase * 1.37f) * 0.5f,
            Mathf.Cos(phase * 1.73f)) * m_shakePositionAmplitude;
        Vector3 rotationOffset = new Vector3(
            Mathf.Sin(phase * 1.19f),
            Mathf.Cos(phase * 1.43f),
            Mathf.Sin(phase * 1.67f)) * m_shakeRotationAngle;

        m_visualRoot.localPosition =
            m_visualBaseLocalPosition + positionOffset;
        m_visualRoot.localRotation =
            m_visualBaseLocalRotation * Quaternion.Euler(rotationOffset);
    }

    /// <summary>攻撃判定との接触開始を記録します。</summary>
    /// <param name="attackSourceId">接触した攻撃判定のInstance ID。</param>
    public void BeginAttackContact(int attackSourceId)
    {
        if (m_group != null)
        {
            m_group.BeginAttackContact(this, attackSourceId);
            return;
        }

        BeginAttackContactLocally(attackSourceId);
    }

    /// <summary>このドラム缶に攻撃接触開始を適用します。</summary>
    /// <param name="attackSourceId">接触した攻撃判定のInstance ID。</param>
    internal void BeginAttackContactLocally(int attackSourceId)
    {
        if (m_attackContactSourceIds.Add(attackSourceId) &&
            m_attackContactSourceIds.Count == 1)
        {
            ApplyHitDebugColor();
        }
    }

    /// <summary>攻撃判定との接触終了を記録します。</summary>
    /// <param name="attackSourceId">離れた攻撃判定のInstance ID。</param>
    public void EndAttackContact(int attackSourceId)
    {
        if (m_group != null)
        {
            m_group.EndAttackContact(this, attackSourceId);
            return;
        }

        EndAttackContactLocally(attackSourceId);
    }

    /// <summary>このドラム缶に攻撃接触終了を適用します。</summary>
    /// <param name="attackSourceId">離れた攻撃判定のInstance ID。</param>
    internal void EndAttackContactLocally(int attackSourceId)
    {
        m_attackContactSourceIds.Remove(attackSourceId);

        if (m_attackContactSourceIds.Count == 0)
        {
            m_hasAcceptedDamageDuringContact = false;
            RestoreVisualTransform();
            RestoreVisualColors();
        }
    }

    /// <summary>吹き飛び後の停止状態を監視し、再攻撃待ちへ戻します。</summary>
    private void FixedUpdate()
    {
        if (!m_isRecoveringFromLaunch || m_rigidbody == null)
        {
            return;
        }

        float deltaTime = Time.fixedDeltaTime;

        bool hasSettled =
            HasSupportedGroundContact() &&
            HasSlowedDownForNextAttack();

        m_settleTimer = hasSettled
            ? m_settleTimer + deltaTime
            : 0.0f;

        if (hasSettled && m_settleTimer >= m_settleDuration)
        {
            LockForNextAttack();
        }
    }

    /// <summary>実際の床接触と底面4点の支持を確認します。</summary>
    /// <returns>true：床が底面を支持している。false：空中または支持不足。</returns>
    private bool HasSupportedGroundContact()
    {
        if (m_rigidbody == null) return false;

        // 近接検出だけで空中固定されないよう、物理接触の記録も必要とします。
        bool hasGroundContact = !float.IsNegativeInfinity(m_lastGroundContactTime) &&
            (Time.fixedTime - m_lastGroundContactTime <= Time.fixedDeltaTime * 1.5f ||
             m_rigidbody.IsSleeping());
        return hasGroundContact && IsBottomFullyGrounded();
    }

    /// <summary>攻撃待機へ戻しても不自然でない程度まで物理速度が低下したか確認します。</summary>
    /// <returns>true：移動・回転速度が許容値以下。false：まだ移動中。</returns>
    private bool HasSlowedDownForNextAttack()
    {
        if (m_rigidbody == null)
        {
            return false;
        }

        if (m_rigidbody.IsSleeping())
        {
            return true;
        }

        float maxLinearSpeedSqr =
            m_maxSettleLinearSpeed * m_maxSettleLinearSpeed;
        float maxAngularSpeedSqr =
            m_maxSettleAngularSpeed * m_maxSettleAngularSpeed;

        return m_rigidbody.linearVelocity.sqrMagnitude <= maxLinearSpeedSqr &&
               m_rigidbody.angularVelocity.sqrMagnitude <= maxAngularSpeedSqr;
    }

    /// <summary>現在下向きのBoxCollider面の4点が地面に接しているか確認します。</summary>
    /// <returns>true：底面4点が接地している。false：1点以上が浮いている。</returns>
    private bool IsBottomFullyGrounded()
    {
        if (m_boxCollider == null)
        {
            return false;
        }

        Vector3 halfSize = m_boxCollider.size * 0.5f;
        float insetMultiplier = 1.0f - m_groundCheckInsetRate;
        Vector3 localDown = m_boxCollider.transform.InverseTransformDirection(Vector3.down);
        int axis = 0;
        if (Mathf.Abs(localDown.y) > Mathf.Abs(localDown[axis])) axis = 1;
        if (Mathf.Abs(localDown.z) > Mathf.Abs(localDown[axis])) axis = 2;
        int firstTangent = (axis + 1) % 3;
        int secondTangent = (axis + 2) % 3;
        Vector3 faceCenter = m_boxCollider.center;
        faceCenter[axis] += Mathf.Sign(localDown[axis]) * halfSize[axis];

        for (int firstSign = -1; firstSign <= 1; firstSign += 2)
        {
            for (int secondSign = -1; secondSign <= 1; secondSign += 2)
            {
                Vector3 point = faceCenter;
                point[firstTangent] += firstSign * halfSize[firstTangent] * insetMultiplier;
                point[secondTangent] += secondSign * halfSize[secondTangent] * insetMultiplier;
                if (!IsGroundedAtLocalPoint(point)) return false;
            }
        }

        return true;
    }

    /// <summary>着地した物理接触を記録します。</summary>
    private void OnCollisionEnter(Collision collision) => RecordGroundContact(collision);

    /// <summary>接地中の物理接触を更新します。</summary>
    private void OnCollisionStay(Collision collision) => RecordGroundContact(collision);

    /// <summary>床レイヤーの上向き接触面を検出し、時刻を記録します。</summary>
    /// <param name="collision">物理衝突情報。</param>
    private void RecordGroundContact(Collision collision)
    {
        if (IsColliderFromSameGroup(collision.collider)) return;
        if ((m_groundLayerMask.value & (1 << collision.gameObject.layer)) == 0) return;
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (Vector3.Dot(collision.GetContact(i).normal, Vector3.up) >= m_minGroundNormalDot)
            {
                m_lastGroundContactTime = Time.fixedTime;
                return;
            }
        }
    }

    /// <summary>指定した底面上の点から地面を検出します。</summary>
    /// <param name="localPoint">BoxCollider基準のローカル座標。</param>
    /// <returns>true：地面を検出した。false：地面を検出しなかった。</returns>
    private bool IsGroundedAtLocalPoint(Vector3 localPoint)
    {
        Vector3 up = Vector3.up;
        Vector3 origin =
            m_boxCollider.transform.TransformPoint(localPoint) +
            up * (m_groundCheckRadius + GROUND_CHECK_START_OFFSET);
        float rayDistance =
            m_groundCheckDistance +
            GROUND_CHECK_START_OFFSET;

        int hitCount = Physics.SphereCastNonAlloc(
            origin,
            m_groundCheckRadius,
            -up,
            m_groundHits,
            rayDistance,
            m_groundLayerMask,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = m_groundHits[i];

            if (hit.collider == null ||
                hit.collider.attachedRigidbody == m_rigidbody ||
                IsColliderFromSameGroup(hit.collider))
            {
                continue;
            }

            if (Vector3.Dot(hit.normal, up) >= m_minGroundNormalDot)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 攻撃中心を使用できない場合の既定方向でダメージを受け取ります。
    /// </summary>
    /// <param name="damageData">受けるダメージと攻撃情報。</param>
    public void TakeDamage(AttackDamageData damageData)
    {
        Vector3 attackCenter =
            transform.position - transform.forward;

        if (m_group != null)
        {
            m_group.TryApplyDamage(this, damageData, attackCenter);
            return;
        }

        ApplyDamage(damageData, attackCenter);
    }

    /// <summary>
    /// 攻撃中心を基準に吹き飛び方向を決定してダメージを受け取ります。
    /// </summary>
    /// <param name="damageData">受けるダメージと攻撃情報。</param>
    /// <param name="attackCenter">攻撃中心のワールド座標。</param>
    /// <returns>true：ダメージを受け付けた。false：攻撃条件を満たさず拒否した。</returns>
    public bool TryTakeDamage(
        AttackDamageData damageData,
        Vector3 attackCenter)
    {
        return m_group != null
            ? m_group.TryApplyDamage(this, damageData, attackCenter)
            : ApplyDamage(damageData, attackCenter);
    }

    /// <summary>グループからこのドラム缶だけへダメージを適用します。</summary>
    /// <param name="damageData">受けるダメージと攻撃情報。</param>
    /// <param name="attackCenter">攻撃中心のワールド座標。</param>
    /// <returns>true：ダメージを蓄積した。false：対象外または吹き飛び中だった。</returns>
    internal bool TryApplyGroupDamage(
        AttackDamageData damageData,
        Vector3 attackCenter)
    {
        return ApplyDamage(damageData, attackCenter);
    }

    /// <summary>壁グループへ登録します。</summary>
    /// <param name="group">登録先のグループ。</param>
    /// <returns>true：登録した。false：別のグループに登録済み。</returns>
    internal bool TryRegisterGroup(AttackReactiveInteractObjectGroup group)
    {
        if (group == null)
        {
            return false;
        }

        if (m_group != null && m_group != group)
        {
            return false;
        }

        m_group = group;
        return true;
    }

    /// <summary>指定グループから登録解除します。</summary>
    /// <param name="group">解除するグループ。</param>
    internal void UnregisterGroup(AttackReactiveInteractObjectGroup group)
    {
        if (m_group == group)
        {
            m_group = null;
        }
    }

    /// <summary>指定Colliderが同一壁グループのドラム缶か確認します。</summary>
    /// <param name="targetCollider">確認するCollider。</param>
    /// <returns>true：同一グループの別ドラム缶。false：それ以外。</returns>
    private bool IsColliderFromSameGroup(Collider targetCollider)
    {
        if (m_group == null || targetCollider == null)
        {
            return false;
        }

        AttackReactiveInteractObject target =
            targetCollider.GetComponentInParent<AttackReactiveInteractObject>();

        return target != null &&
               target != this &&
               target.m_group == m_group;
    }

    /// <summary>
    /// 攻撃条件を確認してダメージを蓄積し、規定値到達時に即座に吹き飛ばします。
    /// </summary>
    /// <param name="damageData">受けるダメージと攻撃情報。</param>
    /// <param name="attackCenter">攻撃中心のワールド座標。</param>
    /// <returns>true：ダメージを蓄積した。false：対象外または吹き飛び中だった。</returns>
    private bool ApplyDamage(
        AttackDamageData damageData,
        Vector3 attackCenter)
    {
        if (damageData.Damage <= 0 ||
            (!m_canBeLaunchedByNormalAttack &&
             damageData.PowerType != AttackPowerType.CHARGED))
        {
            return false;
        }

        if (m_isRecoveringFromLaunch)
        {
            // 着地済みの最初の一撃を接地確定タイマーやFixedUpdate順序で捨てないよう、
            // 同じ呼び出し内で復帰を確定してからダメージを適用します。
            if (!HasSupportedGroundContact()) return false;
            LockForNextAttack();
        }

        // Collider接触だけではなく、実際に有効なダメージを受けた場合のみ揺らします。
        m_hasAcceptedDamageDuringContact = true;
        m_accumulatedDamage += damageData.Damage;

        if (m_accumulatedDamage >= m_requiredDamage)
        {
            // 超過分は持ち越さず、次回分の体力を全回復した状態にします。
            m_accumulatedDamage = 0;
            Launch(attackCenter, damageData);
        }

        return true;
    }

    /// <summary>
    /// 攻撃中心から離れる方向へRigidbodyを吹き飛ばします。
    /// </summary>
    /// <param name="attackCenter">攻撃中心のワールド座標。</param>
    /// <param name="damageData">攻撃元の方向情報を含むダメージ。</param>
    private void Launch(
        Vector3 attackCenter,
        AttackDamageData damageData)
    {
        if (m_rigidbody == null)
        {
            return;
        }

        RestoreVisualTransform();
        m_hasAcceptedDamageDuringContact = false;

        Vector3 launchDirection =
            Vector3.ProjectOnPlane(
                m_rigidbody.worldCenterOfMass - attackCenter,
                Vector3.up);

        if (launchDirection.sqrMagnitude <= DIRECTION_EPSILON)
        {
            launchDirection = transform.forward;
        }

        launchDirection.Normalize();

        if (damageData.TryGetSourceFacingDirection(
                out Vector3 sourceFacingDirection))
        {
            // 従来の放射方向を残しつつ、Playerの体の正面へ緩やかに寄せる。
            Vector3 correctedDirection = Vector3.Slerp(
                launchDirection,
                sourceFacingDirection,
                m_playerFacingCorrectionRate);

            if (correctedDirection.sqrMagnitude > DIRECTION_EPSILON)
            {
                launchDirection = correctedDirection.normalized;
            }
        }

        launchDirection =
            (launchDirection +
             Vector3.up * m_upwardDirectionRate).normalized;

        m_rigidbody.isKinematic = false;
        m_rigidbody.AddForce(
            launchDirection *
            m_launchImpulse *
            m_launchImpulseMultiplier,
            ForceMode.Impulse);

        m_settleTimer = 0.0f;
        m_lastGroundContactTime = float.NegativeInfinity;
        m_isRecoveringFromLaunch = true;
        m_hasBeenLaunched = true;
    }

    /// <summary>物理速度を止め、次の攻撃まで動かない状態へ戻します。</summary>
    private void LockForNextAttack()
    {
        m_rigidbody.linearVelocity = Vector3.zero;
        m_rigidbody.angularVelocity = Vector3.zero;
        m_rigidbody.isKinematic = true;
        m_accumulatedDamage = 0;
        m_isRecoveringFromLaunch = false;
        m_settleTimer = 0.0f;
    }

    /// <summary>揺らしたモデルのローカルTransformを元に戻します。</summary>
    private void RestoreVisualTransform()
    {
        if (m_visualRoot == null)
        {
            return;
        }

        m_visualRoot.localPosition = m_visualBaseLocalPosition;
        m_visualRoot.localRotation = m_visualBaseLocalRotation;
    }

    /// <summary>未設定時はルート直下の最初の子を見た目として取得します。</summary>
    private void ResolveVisualRoot()
    {
        // 本体自身を見た目として扱うと、次回のLaunch前に
        // RestoreVisualTransform() が物理移動後の本体位置を初期位置へ戻してしまう。
        if (m_visualRoot == transform)
        {
            m_visualRoot = null;
        }

        if (m_visualRoot == null && transform.childCount > 0)
        {
            m_visualRoot = transform.GetChild(0);
        }
    }

    /// <summary>Visual Root配下のRendererと既存のPropertyBlockを保存します。</summary>
    private void CacheVisualRenderers()
    {
        m_visualRenderers = m_visualRoot.GetComponentsInChildren<Renderer>(true);
        m_originalPropertyBlocks =
            new MaterialPropertyBlock[m_visualRenderers.Length];

        for (int i = 0; i < m_visualRenderers.Length; i++)
        {
            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            m_visualRenderers[i].GetPropertyBlock(propertyBlock);
            m_originalPropertyBlocks[i] = propertyBlock;
        }
    }

    /// <summary>攻撃接触中のデバッグ色をRendererへ適用します。</summary>
    private void ApplyHitDebugColor()
    {
        if (!m_showHitDebugColor || m_visualRenderers == null)
        {
            return;
        }

        foreach (Renderer visualRenderer in m_visualRenderers)
        {
            if (visualRenderer == null)
            {
                continue;
            }

            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            visualRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BASE_COLOR_PROPERTY_ID, m_hitDebugColor);
            propertyBlock.SetColor(COLOR_PROPERTY_ID, m_hitDebugColor);
            visualRenderer.SetPropertyBlock(propertyBlock);
        }
    }

    /// <summary>Rendererの表示を攻撃接触前のPropertyBlockへ戻します。</summary>
    private void RestoreVisualColors()
    {
        if (m_visualRenderers == null || m_originalPropertyBlocks == null)
        {
            return;
        }

        for (int i = 0; i < m_visualRenderers.Length; i++)
        {
            if (m_visualRenderers[i] != null)
            {
                m_visualRenderers[i].SetPropertyBlock(
                    m_originalPropertyBlocks[i]);
            }
        }
    }

    /// <summary>無効化時にモデルの揺れを解除します。</summary>
    private void OnDisable()
    {
        RestoreVisualTransform();
        RestoreVisualColors();
        m_hasAcceptedDamageDuringContact = false;
        m_attackContactSourceIds.Clear();
    }

    /// <summary>Inspector設定値を有効範囲へ制限します。</summary>
    private void OnValidate()
    {
        m_requiredDamage = Mathf.Max(1, m_requiredDamage);
        m_launchImpulse = Mathf.Max(0.0f, m_launchImpulse);
        m_launchImpulseMultiplier =
            Mathf.Max(0.0f, m_launchImpulseMultiplier);
        m_playerFacingCorrectionRate =
            Mathf.Clamp01(m_playerFacingCorrectionRate);
        m_upwardDirectionRate = Mathf.Clamp01(m_upwardDirectionRate);
        m_settleDuration = Mathf.Max(0.0f, m_settleDuration);
        m_maxSettleLinearSpeed = Mathf.Max(0.0f, m_maxSettleLinearSpeed);
        m_maxSettleAngularSpeed = Mathf.Max(0.0f, m_maxSettleAngularSpeed);
        m_groundCheckDistance = Mathf.Max(0.01f, m_groundCheckDistance);
        m_groundCheckRadius = Mathf.Max(0.001f, m_groundCheckRadius);
        m_groundCheckInsetRate = Mathf.Clamp(m_groundCheckInsetRate, 0.0f, 0.45f);
        m_minGroundNormalDot = Mathf.Clamp01(m_minGroundNormalDot);
        m_shakePositionAmplitude = Mathf.Max(0.0f, m_shakePositionAmplitude);
        m_shakeRotationAngle = Mathf.Max(0.0f, m_shakeRotationAngle);
        m_shakeFrequency = Mathf.Max(0.0f, m_shakeFrequency);
    }
}
