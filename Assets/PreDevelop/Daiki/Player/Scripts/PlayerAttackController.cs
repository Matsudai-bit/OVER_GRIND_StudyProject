using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// プレイヤーの攻撃判定とアニメーションイベントを管理します。
/// </summary>
public class PlayerAttackController : MonoBehaviour
{
    /// <summary>
    /// 攻撃アニメーションから通知されるイベント種別です。
    /// </summary>
    public enum AttackAnimationEventType
    {
        ENABLE_HITBOX,
        DISABLE_HITBOX,
        START_ANIMATION,
        FINISH_ANIMATION
    }

    // アニメーションイベントの受信元
    [SerializeField]
    private PlayerAnimationEventReceiver m_animationEventReceiver;

    // 攻撃時に有効化するヒットボックス
    [SerializeField]
    [Header("攻撃用ヒットボックス")]
    private List<AttackHitbox> m_attackHitboxes = new();

    // 多段ヒット攻撃の基本ヒットレート（1秒あたりのヒット回数）
    [SerializeField]
    [Header("多段ヒット設定")]
    [Min(0.1f)]
    private float m_baseHitsPerSecond = 40.0f;

    // コンボ段階ごとに受信済みイベントを記録する
    private readonly Dictionary<int, HashSet<AttackAnimationEventType>> m_eventMap = new();

    // 攻撃ヒットボックスの有効化要求
    private bool m_requestEnableHitbox;

    /// <summary>
    /// 攻撃ヒットボックスの有効化要求を取得します。
    /// </summary>
    public bool IsAttackHitboxEnableRequested => m_requestEnableHitbox;

    /// <summary>
    /// 多段ヒット攻撃の基本ヒットレート（1秒あたりのヒット回数）を取得します。
    /// </summary>
    public float BaseHitsPerSecond => m_baseHitsPerSecond;

    /// <summary>
    /// 多段ヒット攻撃の基本ヒット間隔（秒）を取得します。
    /// </summary>
    public float BaseHitIntervalSeconds =>
        1.0f / Mathf.Max(m_baseHitsPerSecond, 0.0001f);

    private void OnEnable()
    {
        SubscribeAnimationEvents();
    }

    private void OnDisable()
    {
        UnsubscribeAnimationEvents();
        DisableAttackHitboxes();
    }

    /// <summary>
    /// すべての攻撃ヒットボックスを単発ヒットモードで有効化します。
    /// </summary>
    public void EnableAttackHitboxes()
    {
        foreach (AttackHitbox attackHitbox in m_attackHitboxes)
        {
            if (attackHitbox == null)
            {
                continue;
            }

            attackHitbox.EnableHitbox();
        }
    }

    /// <summary>
    /// すべての攻撃ヒットボックスを多段ヒットモードで有効化します。
    /// 地上攻撃など、押しっぱなしで継続ヒットさせる攻撃で使用します。
    /// </summary>
    public void EnableContinuousAttackHitboxes()
    {
        foreach (AttackHitbox attackHitbox in m_attackHitboxes)
        {
            if (attackHitbox == null)
            {
                continue;
            }

            attackHitbox.EnableContinuousHitbox();
        }
    }

    /// <summary>
    /// すべての攻撃ヒットボックスについて、
    /// 多段ヒット判定（現在重なっている対象へのダメージ適用）を1回分実行します。
    /// 一定周期（<see cref="BaseHitIntervalSeconds"/>）ごとに呼び出してください。
    /// </summary>
    /// <returns>
    /// true：いずれかのヒットボックスが1体以上の対象に命中した。
    /// false：どのヒットボックスも対象に命中しなかった（空振り）。
    /// </returns>
    public bool ApplyContinuousHitTick()
    {
        bool hasHitAnyTarget = false;

        foreach (AttackHitbox attackHitbox in m_attackHitboxes)
        {
            if (attackHitbox == null)
            {
                continue;
            }

            // 短絡評価でApplyContinuousDamage()自体が
            // 呼ばれなくなることを避けるため、
            // 先に呼び出してから結果をOR合成する
            bool hasHit = attackHitbox.ApplyContinuousDamage();
            hasHitAnyTarget = hasHitAnyTarget || hasHit;
        }

        return hasHitAnyTarget;
    }

    /// <summary>
    /// すべての攻撃ヒットボックスを無効化します。
    /// </summary>
    public void DisableAttackHitboxes()
    {
        foreach (AttackHitbox attackHitbox in m_attackHitboxes)
        {
            if (attackHitbox == null)
            {
                continue;
            }

            attackHitbox.DisableHitbox();
        }
    }

    /// <summary>
    /// 指定したコンボ段階でイベントを受信済みか判定します。
    /// </summary>
    /// <param name="comboStage">確認するコンボ段階。</param>
    /// <param name="eventType">確認するイベント種別。</param>
    /// <returns>
    /// true：指定したイベントを受信済みです。
    /// false：指定したイベントを受信していません。
    /// </returns>
    public bool HasReceivedEvent(
        int comboStage,
        AttackAnimationEventType eventType)
    {
        if (!m_eventMap.TryGetValue(comboStage, out HashSet<AttackAnimationEventType> eventTypes))
        {
            return false;
        }

        return eventTypes.Contains(eventType);
    }

    /// <summary>
    /// 指定したコンボ段階のイベント履歴を削除します。
    /// </summary>
    /// <param name="comboStage">削除するコンボ段階。</param>
    public void ClearEventHistory(int comboStage)
    {
        m_eventMap.Remove(comboStage);
    }
    /// <summary>
    /// 指定したコンボ段階の種類イベント履歴を削除します。
    /// </summary>
    /// <param name="comboStage">削除するコンボ段階。</param>
    public void ClearEventHistory(int comboStage, AttackAnimationEventType eventType)
    {
        if (HasReceivedEvent(comboStage, eventType))
        {
            m_eventMap[comboStage].Remove(eventType);
        }
    }

    /// <summary>
    /// すべてのイベント履歴を削除します。
    /// </summary>
    public void ClearAllEventHistory()
    {
        m_eventMap.Clear();
    }

    /// <summary>
    /// アニメーションイベントを購読します。
    /// </summary>
    private void SubscribeAnimationEvents()
    {
        if (m_animationEventReceiver == null)
        {
            Debug.LogError(
                $"{nameof(PlayerAttackController)}: " +
                $"{nameof(PlayerAnimationEventReceiver)}が設定されていません。",
                this);

            return;
        }

        m_animationEventReceiver.AttackHitboxStarted += HandleAttackHitboxStarted;
        m_animationEventReceiver.AttackHitboxEnded += HandleAttackHitboxEnded;
        m_animationEventReceiver.AttackAnimationStarted += HandleAttackAnimationStarted;
        m_animationEventReceiver.AttackAnimationFinished += HandleAttackAnimationFinished;
    }

    /// <summary>
    /// アニメーションイベントの購読を解除します。
    /// </summary>
    private void UnsubscribeAnimationEvents()
    {
        if (m_animationEventReceiver == null)
        {
            return;
        }

        m_animationEventReceiver.AttackHitboxStarted -= HandleAttackHitboxStarted;
        m_animationEventReceiver.AttackHitboxEnded -= HandleAttackHitboxEnded;
        m_animationEventReceiver.AttackAnimationStarted -= HandleAttackAnimationStarted;
        m_animationEventReceiver.AttackAnimationFinished -= HandleAttackAnimationFinished;
    }

    /// <summary>
    /// 攻撃判定開始イベントを処理します。
    /// </summary>
    /// <param name="comboStage">コンボ段階。</param>
    private void HandleAttackHitboxStarted(int comboStage)
    {
        m_requestEnableHitbox = true;

        RecordAnimationEvent(
            comboStage,
            AttackAnimationEventType.ENABLE_HITBOX);

        Debug.Log(
            $"{nameof(PlayerAttackController)}: " +
            $"攻撃判定開始通知を受信しました。ComboStage={comboStage}",
            this);
    }

    /// <summary>
    /// 攻撃判定終了イベントを処理します。
    /// </summary>
    /// <param name="comboStage">コンボ段階。</param>
    private void HandleAttackHitboxEnded(int comboStage)
    {
        m_requestEnableHitbox = false;

        RecordAnimationEvent(
            comboStage,
            AttackAnimationEventType.DISABLE_HITBOX);

        Debug.Log(
            $"{nameof(PlayerAttackController)}: " +
            $"攻撃判定終了通知を受信しました。ComboStage={comboStage}",
            this);
    }

    /// <summary>
    /// 攻撃アニメーション開始イベントを処理します。
    /// </summary>
    /// <param name="comboStage">コンボ段階。</param>
    private void HandleAttackAnimationStarted(int comboStage)
    {
        RecordAnimationEvent(
            comboStage,
            AttackAnimationEventType.START_ANIMATION);

        Debug.Log(
            $"{nameof(PlayerAttackController)}: " +
            $"攻撃アニメーション開始通知を受信しました。ComboStage={comboStage}",
            this);
    }

    /// <summary>
    /// 攻撃アニメーション終了イベントを処理します。
    /// </summary>
    /// <param name="comboStage">コンボ段階。</param>
    private void HandleAttackAnimationFinished(int comboStage)
    {
        RecordAnimationEvent(
            comboStage,
            AttackAnimationEventType.FINISH_ANIMATION);

        Debug.Log(
            $"{nameof(PlayerAttackController)}: " +
            $"攻撃アニメーション終了通知を受信しました。ComboStage={comboStage}",
            this);
    }

    /// <summary>
    /// コンボ段階ごとのイベント履歴を記録します。
    /// </summary>
    /// <param name="comboStage">コンボ段階。</param>
    /// <param name="eventType">記録するイベント種別。</param>
    private void RecordAnimationEvent(
        int comboStage,
        AttackAnimationEventType eventType)
    {
        if (!m_eventMap.TryGetValue(
                comboStage,
                out HashSet<AttackAnimationEventType> eventTypes))
        {
            eventTypes = new HashSet<AttackAnimationEventType>();
            m_eventMap.Add(comboStage, eventTypes);
        }

        eventTypes.Add(eventType);
    }
}