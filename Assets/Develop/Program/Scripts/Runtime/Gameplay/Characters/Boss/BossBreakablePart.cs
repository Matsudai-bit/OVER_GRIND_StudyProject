using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// ボスの破壊可能部位を管理します。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Health))]
public sealed class BossBreakablePart : MonoBehaviour, IDamageable
{
    // 部位の描画Renderer
    [SerializeField, Header("表示")]
    private Renderer m_renderer;

    // 被弾時のMaterial
    [SerializeField]
    private Material m_hitMaterial;

    // 破壊後のMaterial
    [SerializeField]
    private Material m_brokenMaterial;

    // 被弾表示時間
    [SerializeField, Min(0.0f)]
    private float m_hitFeedbackDuration = 0.5f;

    // HP管理
    private Health m_health;

    // 通常時のMaterial
    private Material m_initialMaterial;

    // 現在の部位状態
    private BossBreakablePartState m_currentState;

    // 被弾表示コルーチン
    private Coroutine m_damageFeedbackCoroutine;

    /// <summary>
    /// 部位が破壊されたときに通知されます。
    /// </summary>
    public event Action<BossBreakablePart> Broken;

    /// <summary>
    /// 現在の状態を取得します。
    /// </summary>
    public BossBreakablePartState CurrentState => m_currentState;

    /// <summary>
    /// 部位が破壊済みか取得します。
    /// </summary>
    public bool IsBroken =>
        m_currentState == BossBreakablePartState.BROKEN ||
        m_currentState == BossBreakablePartState.DESTROYED;

    /// <summary>
    /// 初期化します。
    /// </summary>
    private void Awake()
    {
        m_health = GetComponent<Health>();

        if (m_renderer == null)
        {
            m_renderer = GetComponentInChildren<Renderer>();
        }

        if (m_renderer != null)
        {
            m_initialMaterial = m_renderer.sharedMaterial;
        }

        m_currentState = BossBreakablePartState.NORMAL;
    }

    /// <summary>
    /// ダメージを受け取ります。
    /// </summary>
    /// <param name="damage">受けるダメージ量。</param>
    public void TakeDamage(int damage)
    {
        if (damage <= 0 ||
            m_health == null ||
            m_health.IsDead ||
            m_currentState != BossBreakablePartState.NORMAL)
        {
            return;
        }

        m_health.TakeDamage(damage);

        bool willBreak = m_health.IsDead;
        StartDamageFeedback(willBreak);
    }

    /// <summary>
    /// 部位を非表示にして破壊済み状態へ変更します。
    /// </summary>
    public void DestroyPart()
    {
        if (m_currentState == BossBreakablePartState.DESTROYED)
        {
            return;
        }

        m_currentState = BossBreakablePartState.DESTROYED;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 被弾表示を開始します。
    /// </summary>
    /// <param name="willBreak">表示後に破壊状態へ移行するか。</param>
    private void StartDamageFeedback(bool willBreak)
    {
        if (m_damageFeedbackCoroutine != null)
        {
            StopCoroutine(m_damageFeedbackCoroutine);
        }

        m_damageFeedbackCoroutine = StartCoroutine(
            DamageFeedbackCoroutine(willBreak));
    }

    /// <summary>
    /// 被弾表示を行います。
    /// </summary>
    /// <param name="willBreak">表示後に破壊状態へ移行するか。</param>
    /// <returns>コルーチン。</returns>
    private IEnumerator DamageFeedbackCoroutine(bool willBreak)
    {
        if (m_renderer != null && m_hitMaterial != null)
        {
            m_renderer.material = m_hitMaterial;
        }

        if (m_hitFeedbackDuration > 0.0f)
        {
            yield return new WaitForSeconds(m_hitFeedbackDuration);
        }

        if (willBreak)
        {
            BreakPart();
        }
        else if (m_renderer != null && m_initialMaterial != null)
        {
            m_renderer.material = m_initialMaterial;
        }

        m_damageFeedbackCoroutine = null;
    }

    /// <summary>
    /// 部位を破壊状態へ変更します。
    /// </summary>
    private void BreakPart()
    {
        if (m_currentState != BossBreakablePartState.NORMAL)
        {
            return;
        }

        if (m_renderer != null && m_brokenMaterial != null)
        {
            m_renderer.material = m_brokenMaterial;
        }

        m_currentState = BossBreakablePartState.BROKEN;
        Broken?.Invoke(this);
    }
}
