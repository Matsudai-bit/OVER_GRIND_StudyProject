using UnityEngine;

/// <summary>
/// 被攻撃判定を管理します。
/// </summary>
[DisallowMultipleComponent]
public sealed class Hurtbox : MonoBehaviour, IDirectionalDamageable
{
    /// <summary>
    /// 実際にダメージを処理するコンポーネント。
    /// </summary>
    [SerializeField]
    private MonoBehaviour m_damageReceiverComponent;

    /// <summary>
    /// 受けるダメージの倍率。
    /// </summary>
    [SerializeField]
    [Min(0.0f)]
    private float m_damageMultiplier = 1.0f;

    /// <summary>
    /// ダメージを受け付けるか。
    /// </summary>
    [SerializeField]
    private bool m_canReceiveDamage = true;

    /// <summary>
    /// 実際にダメージを処理する対象。
    /// </summary>
    private IDamageable m_damageReceiver;

    /// <summary>
    /// ダメージを受け付けるか取得します。
    /// </summary>
    public bool CanReceiveDamage => m_canReceiveDamage;

    /// <summary>
    /// ダメージ倍率を取得します。
    /// </summary>
    public float DamageMultiplier => m_damageMultiplier;

    /// <summary>
    /// 初期化します。
    /// </summary>
    private void Awake()
    {
        CacheDamageReceiver();
    }

    /// <summary>
    /// 攻撃ダメージを受け取ります。
    /// </summary>
    /// <param name="damageData">受けるダメージと攻撃識別情報。</param>
    public void TakeDamage(AttackDamageData damageData)
    {
        ApplyDamage(damageData, null);
    }

    /// <summary>攻撃中心と倍率適用後のダメージを実際の被弾先へ転送します。</summary>
    public bool TryTakeDamage(AttackDamageData damageData, Vector3 attackCenter)
    {
        return ApplyDamage(damageData, attackCenter);
    }

    /// <summary>受付状態と倍率を確認し、攻撃情報を維持して被弾先へ転送します。</summary>
    private bool ApplyDamage(AttackDamageData damageData, Vector3? attackCenter)
    {
        if (!m_canReceiveDamage || damageData.Damage <= 0 ||
            (m_damageReceiver == null && !CacheDamageReceiver()))
        {
            return false;
        }

        int adjustedDamage = Mathf.Max(
            0,
            Mathf.RoundToInt(damageData.Damage * m_damageMultiplier));

        if (adjustedDamage <= 0)
        {
            return false;
        }

        AttackDamageData adjustedDamageData =
            damageData.WithDamage(adjustedDamage);

        if (attackCenter.HasValue && m_damageReceiver is IDirectionalDamageable directionalReceiver)
        {
            return directionalReceiver.TryTakeDamage(
                adjustedDamageData,
                attackCenter.Value);
        }

        m_damageReceiver.TakeDamage(adjustedDamageData);
        return true;
    }

    /// <summary>
    /// ダメージ受付状態を設定します。
    /// </summary>
    /// <param name="canReceiveDamage">ダメージを受け付けるか。</param>
    public void SetDamageEnabled(bool canReceiveDamage)
    {
        m_canReceiveDamage = canReceiveDamage;
    }

    /// <summary>
    /// ダメージ倍率を設定します。
    /// </summary>
    /// <param name="damageMultiplier">設定するダメージ倍率。</param>
    public void SetDamageMultiplier(float damageMultiplier)
    {
        m_damageMultiplier = Mathf.Max(0.0f, damageMultiplier);
    }

    /// <summary>
    /// ダメージ受付対象のIDを取得します。
    /// </summary>
    /// <returns>ダメージ受付対象のInstance ID。</returns>
    public int GetDamageReceiverInstanceId()
    {
        if (m_damageReceiverComponent == null)
        {
            return GetInstanceID();
        }

        return m_damageReceiverComponent.GetInstanceID();
    }

    /// <summary>
    /// ダメージ受付対象を保持します。
    /// </summary>
    /// <returns>
    /// true：ダメージ受付対象を取得できました。
    /// false：ダメージ受付対象を取得できませんでした。
    /// </returns>
    private bool CacheDamageReceiver()
    {
        // Inspectorで未設定の場合は親階層から検索します。
        if (m_damageReceiverComponent == null)
        {
            m_damageReceiverComponent = FindDamageReceiverComponent();
        }

        if (m_damageReceiverComponent == null)
        {
            Debug.LogError(
                $"{nameof(IDamageable)}を実装したコンポーネントが見つかりません。",
                this);

            m_damageReceiver = null;
            return false;
        }

        if (m_damageReceiverComponent == this)
        {
            Debug.LogError(
                $"{nameof(Hurtbox)}自身をダメージ受付対象には設定できません。",
                this);

            m_damageReceiver = null;
            return false;
        }

        m_damageReceiver =
            m_damageReceiverComponent as IDamageable;

        if (m_damageReceiver == null)
        {
            Debug.LogError(
                $"{m_damageReceiverComponent.GetType().Name}は" +
                $"{nameof(IDamageable)}を実装していません。",
                m_damageReceiverComponent);

            return false;
        }

        return true;
    }

    /// <summary>
    /// 親階層からダメージ受付対象を検索します。
    /// </summary>
    /// <returns>見つかったダメージ受付コンポーネント。</returns>
    private MonoBehaviour FindDamageReceiverComponent()
    {
        MonoBehaviour[] components =
            GetComponentsInParent<MonoBehaviour>(true);

        // PlayerHealthなど、攻撃方向も処理できる受付先を優先します。
        foreach (MonoBehaviour component in components)
        {
            if (IsValidDamageReceiver<IDirectionalDamageable>(component))
            {
                return component;
            }
        }

        foreach (MonoBehaviour component in components)
        {
            if (IsValidDamageReceiver<IDamageable>(component))
            {
                return component;
            }
        }

        return null;
    }

    /// <summary>
    /// コンポーネントが指定したダメージ受付型として有効か確認します。
    /// </summary>
    /// <typeparam name="TReceiver">確認するダメージ受付型。</typeparam>
    /// <param name="component">確認するコンポーネント。</param>
    /// <returns>true：受付先として有効。false：受付先として無効。</returns>
    private bool IsValidDamageReceiver<TReceiver>(MonoBehaviour component)
        where TReceiver : class, IDamageable
    {
        return component != null &&
               component != this &&
               component is not Hurtbox &&
               component is TReceiver;
    }

    /// <summary>
    /// Inspector設定を検証します。
    /// </summary>
    private void OnValidate()
    {
        m_damageMultiplier = Mathf.Max(
            0.0f,
            m_damageMultiplier);

        if (m_damageReceiverComponent == this)
        {
            Debug.LogWarning(
                $"{nameof(Hurtbox)}自身は設定できません。",
                this);

            m_damageReceiverComponent = null;
            return;
        }

        if (m_damageReceiverComponent != null &&
            m_damageReceiverComponent is not IDamageable)
        {
            Debug.LogWarning(
                $"{m_damageReceiverComponent.GetType().Name}は" +
                $"{nameof(IDamageable)}を実装していません。",
                m_damageReceiverComponent);
        }
    }

    /// <summary>
    /// Inspector設定時に受付対象を自動取得します。
    /// </summary>
    private void Reset()
    {
        m_damageMultiplier = 1.0f;
        m_canReceiveDamage = true;
        m_damageReceiverComponent = FindDamageReceiverComponent();
    }
}
