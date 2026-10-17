using System;
using UnityEngine;

/// <summary>
/// ボスの破壊可能部位を管理します。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Health))]
public sealed class BossBreakablePart :
    MonoBehaviour,
    IDamageable
{
    // 見た目制御
    [SerializeField, Header("表示")]
    private BossBreakablePartVisual m_visual;

    // HP管理
    private Health m_health;

    // 現在の部位状態
    private BossBreakablePartState m_currentState;

    // 被弾演出終了後に破壊するか
    private bool m_isBreakPending;

    /// <summary>
    /// ダメージを受けたときに通知されます。
    /// </summary>
    public event Action<
        BossBreakablePart,
        AttackDamageData> Damaged;

    /// <summary>
    /// 部位が破壊されたときに通知されます。
    /// </summary>
    public event Action<
        BossBreakablePart> Broken;

    /// <summary>
    /// 現在の状態を取得します。
    /// </summary>
    public BossBreakablePartState CurrentState =>
        m_currentState;

    /// <summary>
    /// 部位が破壊済みか取得します。
    /// </summary>
    public bool IsBroken =>
        m_currentState ==
            BossBreakablePartState.BROKEN ||
        m_currentState ==
            BossBreakablePartState.DESTROYED;

    /// <summary>
    /// 初期化します。
    /// </summary>
    private void Awake()
    {
        m_health =
            GetComponent<Health>();

        if (m_visual == null)
        {
            m_visual =
                GetComponentInChildren<
                    BossBreakablePartVisual>();
        }

        if (m_visual != null)
        {
            m_visual.FeedbackCompleted +=
                HandleFeedbackCompleted;
        }

        m_currentState =
            BossBreakablePartState.NORMAL;
    }

    /// <summary>
    /// 終了処理を行います。
    /// </summary>
    private void OnDestroy()
    {
        if (m_visual != null)
        {
            m_visual.FeedbackCompleted -=
                HandleFeedbackCompleted;
        }
    }

    /// <summary>
    /// ダメージを受け取ります。
    /// </summary>
    /// <param name="damageData">
    /// 受けるダメージと攻撃識別情報。
    /// </param>
    public void TakeDamage(
        AttackDamageData damageData)
    {
        if (damageData.Damage <= 0 ||
            m_health == null ||
            m_health.IsDead ||
            m_currentState !=
                BossBreakablePartState.NORMAL)
        {
            return;
        }

        // HPへダメージを反映
        m_health.TakeDamage(
            damageData.Damage);

        // ダメージ受領を通知
        Damaged?.Invoke(
            this,
            damageData);

        // HPが0なら演出終了後に破壊する
        if (m_health.IsDead)
        {
            m_isBreakPending =
                true;
        }

        if (m_visual == null)
        {
            if (m_isBreakPending)
            {
                BreakPart();
            }

            return;
        }

        /*
         * 再被弾時はVisual側で現在の演出を停止し、
         * Emissiveを通常値へ戻して最初から再生する。
         */
        m_visual.PlayDamageFeedback();
    }

    /// <summary>
    /// 部位を非表示にして破壊済み状態へ変更します。
    /// </summary>
    public void DestroyPart()
    {
        if (m_currentState ==
            BossBreakablePartState.DESTROYED)
        {
            return;
        }

        m_currentState =
            BossBreakablePartState.DESTROYED;

        gameObject.SetActive(
            false);
    }

    /// <summary>
    /// 被弾演出終了を処理します。
    /// </summary>
    private void HandleFeedbackCompleted()
    {
        if (!m_isBreakPending)
        {
            return;
        }

        BreakPart();
    }

    /// <summary>
    /// 部位を破壊状態へ変更します。
    /// </summary>
    private void BreakPart()
    {
        if (m_currentState !=
            BossBreakablePartState.NORMAL)
        {
            return;
        }

        m_isBreakPending =
            false;

        m_currentState =
            BossBreakablePartState.BROKEN;

        m_visual?.SetBrokenVisual();

        Broken?.Invoke(
            this);
    }
}