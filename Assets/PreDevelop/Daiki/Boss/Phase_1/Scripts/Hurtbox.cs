using UnityEngine;

/// <summary>
/// 被攻撃判定を管理します。
/// </summary>
[DisallowMultipleComponent]
public sealed class Hurtbox : MonoBehaviour, IAttackDamageReceiver
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
    private IAttackDamageReceiver m_damageReceiver;

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
    /// <param name="damage">受けるダメージ量。</param>
    public void ReceiveAttackDamage(int damage)
    {
        if (!m_canReceiveDamage)
        {
            return;
        }

        if (damage <= 0)
        {
            return;
        }

        // 参照が失われている場合は再取得します。
        if (m_damageReceiver == null &&
            !CacheDamageReceiver())
        {
            return;
        }

        // Hurtbox固有の倍率を適用します。
        int adjustedDamage = Mathf.Max(
            0,
            Mathf.RoundToInt(damage * m_damageMultiplier));

        if (adjustedDamage <= 0)
        {
            return;
        }

        m_damageReceiver.ReceiveAttackDamage(adjustedDamage);
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
                $"{nameof(IAttackDamageReceiver)}を実装したコンポーネントが見つかりません。",
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
            m_damageReceiverComponent as IAttackDamageReceiver;

        if (m_damageReceiver == null)
        {
            Debug.LogError(
                $"{m_damageReceiverComponent.GetType().Name}は" +
                $"{nameof(IAttackDamageReceiver)}を実装していません。",
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

        foreach (MonoBehaviour component in components)
        {
            if (component == null || component == this)
            {
                continue;
            }

            // 別のHurtboxへの転送は行いません。
            if (component is Hurtbox)
            {
                continue;
            }

            if (component is IAttackDamageReceiver)
            {
                return component;
            }
        }

        return null;
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
            m_damageReceiverComponent is not IAttackDamageReceiver)
        {
            Debug.LogWarning(
                $"{m_damageReceiverComponent.GetType().Name}は" +
                $"{nameof(IAttackDamageReceiver)}を実装していません。",
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