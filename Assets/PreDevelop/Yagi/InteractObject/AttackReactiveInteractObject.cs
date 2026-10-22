using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 攻撃ダメージを蓄積し、規定値に到達したときに物理的に吹き飛びます。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class AttackReactiveInteractObject : MonoBehaviour,
    IDirectionalDamageable,
    IAttackContactReceiver,
    IAttackMovementBlocker
{
    private const float DIRECTION_EPSILON = 0.0001f;

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

    [SerializeField, Header("Pending Launch Effect")]
    [Tooltip("待機中に揺らす見た目のTransform。未設定時は最初の子を使用します。")]
    private Transform m_visualRoot;

    [SerializeField, Min(0.0f)]
    [Tooltip("待機中の位置揺れ幅です。")]
    private float m_shakePositionAmplitude = 0.04f;

    [SerializeField, Min(0.0f)]
    [Tooltip("待機中の回転揺れ幅（度）です。")]
    private float m_shakeRotationAngle = 3.0f;

    [SerializeField, Min(0.0f)]
    [Tooltip("待機中の揺れ速度です。")]
    private float m_shakeFrequency = 28.0f;

    [SerializeField, Header("Physics")]
    [Tooltip("最初に吹き飛ぶまでは物理的に動かない状態にします。")]
    private bool m_startAsKinematic = true;

    private Rigidbody m_rigidbody;
    private int m_accumulatedDamage;
    private bool m_hasBeenLaunched;
    private bool m_isLaunchPending;
    private Vector3 m_pendingAttackCenter;
    private Vector3 m_visualBaseLocalPosition;
    private Quaternion m_visualBaseLocalRotation;
    private readonly HashSet<int> m_attackContactSourceIds = new();

    /// <summary>現在蓄積しているダメージを取得します。</summary>
    public int AccumulatedDamage => m_accumulatedDamage;

    /// <summary>一度以上吹き飛ばされたか取得します。</summary>
    public bool HasBeenLaunched => m_hasBeenLaunched;

    /// <summary>必要な参照を取得し、初期物理状態を設定します。</summary>
    private void Awake()
    {
        m_rigidbody = GetComponent<Rigidbody>();

        if (m_rigidbody == null)
        {
            Debug.LogError(
                $"{nameof(Rigidbody)}が設定されていません。",
                this);
            enabled = false;
            return;
        }

        m_rigidbody.isKinematic = m_startAsKinematic;
        ResolveVisualRoot();

        if (m_visualRoot != null)
        {
            m_visualBaseLocalPosition = m_visualRoot.localPosition;
            m_visualBaseLocalRotation = m_visualRoot.localRotation;
        }
    }

    /// <summary>吹き飛び待機中にモデルだけを振動させます。</summary>
    private void LateUpdate()
    {
        if (!m_isLaunchPending || m_visualRoot == null)
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

    /// <summary>
    /// 攻撃判定との接触開始を記録します。
    /// </summary>
    /// <param name="attackSourceId">接触した攻撃判定のInstance ID。</param>
    public void BeginAttackContact(int attackSourceId)
    {
        m_attackContactSourceIds.Add(attackSourceId);
    }

    /// <summary>
    /// 攻撃判定との接触終了を記録し、最後の接触終了時に吹き飛ばします。
    /// </summary>
    /// <param name="attackSourceId">離れた攻撃判定のInstance ID。</param>
    public void EndAttackContact(int attackSourceId)
    {
        m_attackContactSourceIds.Remove(attackSourceId);

        if (m_isLaunchPending && m_attackContactSourceIds.Count == 0)
        {
            LaunchPendingObject();
        }
    }

    /// <summary>
    /// 攻撃中心を使用できない場合の既定方向でダメージを受け取ります。
    /// </summary>
    /// <param name="damageData">受けるダメージと攻撃情報。</param>
    public void TakeDamage(AttackDamageData damageData)
    {
        ApplyDamage(
            damageData,
            transform.position - transform.forward);
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
        return ApplyDamage(damageData, attackCenter);
    }

    /// <summary>
    /// 攻撃条件を確認してダメージを蓄積し、規定値で吹き飛ばします。
    /// </summary>
    /// <param name="damageData">受けるダメージと攻撃情報。</param>
    /// <param name="attackCenter">攻撃中心のワールド座標。</param>
    /// <returns>true：ダメージを蓄積した。false：対象外の攻撃だった。</returns>
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

        if (m_isLaunchPending)
        {
            return true;
        }

        m_accumulatedDamage += damageData.Damage;

        if (m_accumulatedDamage < m_requiredDamage)
        {
            return true;
        }

        m_accumulatedDamage = 0;

        if (m_attackContactSourceIds.Count > 0)
        {
            m_pendingAttackCenter = attackCenter;
            m_isLaunchPending = true;
        }
        else
        {
            Launch(attackCenter);
        }

        return true;
    }

    /// <summary>待機演出を終了し、保存した攻撃位置から吹き飛ばします。</summary>
    private void LaunchPendingObject()
    {
        Vector3 attackCenter = m_pendingAttackCenter;
        m_isLaunchPending = false;
        RestoreVisualTransform();
        Launch(attackCenter);
    }

    /// <summary>
    /// 攻撃中心から離れる方向へRigidbodyを吹き飛ばします。
    /// </summary>
    /// <param name="attackCenter">攻撃中心のワールド座標。</param>
    private void Launch(Vector3 attackCenter)
    {
        if (m_rigidbody == null)
        {
            return;
        }

        Vector3 launchDirection =
            Vector3.ProjectOnPlane(
                m_rigidbody.worldCenterOfMass - attackCenter,
                Vector3.up);

        if (launchDirection.sqrMagnitude <= DIRECTION_EPSILON)
        {
            launchDirection = transform.forward;
        }

        launchDirection =
            (launchDirection.normalized +
             Vector3.up * m_upwardDirectionRate).normalized;

        m_rigidbody.isKinematic = false;
        m_rigidbody.AddForce(
            launchDirection * m_launchImpulse,
            ForceMode.Impulse);

        m_hasBeenLaunched = true;
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
        if (m_visualRoot == null && transform.childCount > 0)
        {
            m_visualRoot = transform.GetChild(0);
        }
    }

    /// <summary>無効化時にモデルの揺れを解除します。</summary>
    private void OnDisable()
    {
        RestoreVisualTransform();
        m_isLaunchPending = false;
        m_attackContactSourceIds.Clear();
    }

    /// <summary>Inspector設定値を有効範囲へ制限します。</summary>
    private void OnValidate()
    {
        m_requiredDamage = Mathf.Max(1, m_requiredDamage);
        m_launchImpulse = Mathf.Max(0.0f, m_launchImpulse);
        m_upwardDirectionRate = Mathf.Clamp01(m_upwardDirectionRate);
        m_shakePositionAmplitude = Mathf.Max(0.0f, m_shakePositionAmplitude);
        m_shakeRotationAngle = Mathf.Max(0.0f, m_shakeRotationAngle);
        m_shakeFrequency = Mathf.Max(0.0f, m_shakeFrequency);
    }
}
