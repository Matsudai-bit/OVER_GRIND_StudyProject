using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 一定時間だけ攻撃判定を有効化し、終了後にプールへ返却します。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(AttackHitbox))]
public sealed class ExplosionHitboxInstance : MonoBehaviour
{
    [SerializeField, Header("攻撃判定（未設定なら自身）")]
    private AttackHitbox m_hitbox;

    // 返却先とタイマー
    private Action<ExplosionHitboxInstance> m_returnCallback;
    private Coroutine m_lifetimeCoroutine;
    private bool m_isActive;

    private void Awake()
    {
        if (m_hitbox == null)
        {
            m_hitbox = GetComponent<AttackHitbox>();
        }
    }

    /// <summary>プールの返却先を登録します。</summary>
    public void Prepare(Action<ExplosionHitboxInstance> returnCallback)
    {
        m_returnCallback = returnCallback;
    }

    /// <summary>ダメージ判定を指定時間だけ有効にします。</summary>
    /// <returns>true：開始成功。false：参照または引数不正。</returns>
    public bool Activate(Vector3 position, AttackIdentifier attackIdentifier, float duration)
    {
        if (m_hitbox == null || m_returnCallback == null || m_isActive ||
            attackIdentifier == null || duration <= 0.0f)
        {
            return false;
        }

        m_isActive = true;
        transform.SetPositionAndRotation(position, Quaternion.identity);

        // ミサイルPrefabの寿命と切り離して配置します。
        gameObject.SetActive(true);

        // 既存AttackHitboxの攻撃情報を利用します。
        AttackDamageData damageData = new(
            m_hitbox.CurrentDamage, attackIdentifier);
        m_hitbox.EnableHitbox(damageData);
        m_lifetimeCoroutine = StartCoroutine(ReleaseAfterDuration(duration));
        return true;
    }

    /// <summary>期間終了前でも攻撃判定を終了します。</summary>
    public void Release()
    {
        if (!m_isActive)
        {
            return;
        }
        m_isActive = false;

        if (m_lifetimeCoroutine != null)
        {
            StopCoroutine(m_lifetimeCoroutine);
            m_lifetimeCoroutine = null;
        }

        // AttackHitboxの実装が不明なため、GameObject単位で判定を無効化します。
        gameObject.SetActive(false);
        m_returnCallback?.Invoke(this);
    }

    private IEnumerator ReleaseAfterDuration(float duration)
    {
        yield return new WaitForSeconds(duration);
        m_lifetimeCoroutine = null;
        Release();
    }

    private void OnDisable()
    {
        if (m_lifetimeCoroutine != null)
        {
            StopCoroutine(m_lifetimeCoroutine);
            m_lifetimeCoroutine = null;
        }
    }
}
