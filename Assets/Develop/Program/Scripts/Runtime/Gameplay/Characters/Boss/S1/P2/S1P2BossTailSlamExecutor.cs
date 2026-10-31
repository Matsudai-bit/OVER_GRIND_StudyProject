using UnityEngine;

/// <summary>
/// S1P2ボスの尻尾叩きつけ攻撃を実行します。
/// </summary>
public sealed class S1P2BossTailSlamExecutor
{
    // 実行対象のBoss
    private readonly BossController m_owner;

    // フェーズ2固有参照
    private S1P2BossReferences m_references;

    // 尻尾攻撃参照
    private S1P2BossTailSlamReference m_tailSlamReference;

    // 尻尾攻撃ID
    private AttackIdentifier m_attackIdentifier;

    // 尻尾接地Sensor
    private CollisionSensor m_groundCollisionSensor;

    // AnimationEvent受信
    private AnimationEventReceiver m_animationEventReceiver;

    // Animator Trigger ID
    private int m_animationTriggerID;

    // 尻尾が地面へ接触済みか
    private bool m_hasGroundCollision;

    // AnimationEvent購読中か
    private bool m_isAnimationEventSubscribed;

    // Ground Sensor購読中か
    private bool m_isGroundCollisionSubscribed;

    /// <summary>
    /// 攻撃実行中か取得します。
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// 攻撃が完了したか取得します。
    /// </summary>
    public bool IsCompleted { get; private set; }

    /// <summary>
    /// 攻撃が失敗したか取得します。
    /// </summary>
    public bool HasFailed { get; private set; }

    /// <summary>
    /// 尻尾叩きつけ実行機構を生成します。
    /// </summary>
    /// <param name="owner">
    /// 実行対象のBoss。
    /// </param>
    public S1P2BossTailSlamExecutor(
        BossController owner)
    {
        m_owner =
            owner;
    }

    /// <summary>
    /// 尻尾叩きつけ攻撃を開始します。
    /// </summary>
    /// <param name="references">
    /// S1P2固有参照。
    /// </param>
    /// <returns>
    /// true：開始しました。
    /// false：開始できませんでした。
    /// </returns>
    public bool Start(
        S1P2BossReferences references)
    {
        Cancel();

        ResetRuntimeState();

        if (m_owner == null ||
            references == null ||
            m_owner.AnimationController == null)
        {
            FailAttack();

            return false;
        }

        m_references =
            references;

        m_tailSlamReference =
            m_references.TailSlamReference;

        if (m_tailSlamReference == null)
        {
            Debug.LogError(
                $"[{nameof(S1P2BossTailSlamExecutor)}] " +
                $"{nameof(S1P2BossTailSlamReference)}が" +
                "設定されていません.");

            FailAttack();

            return false;
        }

        m_groundCollisionSensor =
            m_tailSlamReference
                .TailGroundCollisionSensor;

        if (m_groundCollisionSensor == null)
        {
            Debug.LogError(
                $"[{nameof(S1P2BossTailSlamExecutor)}] " +
                $"{nameof(CollisionSensor)}が" +
                "設定されていません.");

            FailAttack();

            return false;
        }

        if (!ApplyAttackSetting())
        {
            FailAttack();

            return false;
        }

        IsRunning =
            true;

        /*
         * AnimationEventを先に購読してから
         * Triggerを発火します。
         */
        SubscribeAnimationEvent();

        m_owner.AnimationController.SetTrigger(
            m_animationTriggerID);

        return true;
    }

    /// <summary>
    /// 尻尾叩きつけ攻撃をキャンセルします。
    /// </summary>
    public void Cancel()
    {
        UnsubscribeAnimationEvent();

        UnsubscribeGroundCollision();

        DisableAttackHitbox();

        IsRunning =
            false;

        m_references =
            null;

        m_tailSlamReference =
            null;

        m_attackIdentifier =
            null;

        m_groundCollisionSensor =
            null;

        m_animationEventReceiver =
            null;
    }

    /// <summary>
    /// 攻撃設定を取得します。
    /// </summary>
    /// <returns>
    /// true：取得できました。
    /// false：取得できませんでした。
    /// </returns>
    private bool ApplyAttackSetting()
    {
        S1P2BossAttackSettings attackSettings =
            m_owner.GetComponentInChildren<
                S1P2BossAttackSettings>(
                    true);

        if (attackSettings == null)
        {
            return false;
        }

        if (!attackSettings.TryGetAttackSetting(
                S1P2BossAttackType.TAIL_SLAM,
                out m_attackIdentifier,
                out string animationTriggerName))
        {
            return false;
        }

        if (m_attackIdentifier == null ||
            string.IsNullOrEmpty(
                animationTriggerName))
        {
            return false;
        }

        m_animationEventReceiver =
            m_owner.AnimationController
                .CurrentAnimationEventReceiver;

        if (m_animationEventReceiver == null)
        {
            return false;
        }

        m_animationTriggerID =
            Animator.StringToHash(
                animationTriggerName);

        return true;
    }

    /// <summary>
    /// 攻撃AnimationEventを処理します。
    /// </summary>
    /// <param name="attackEventData">
    /// 攻撃イベント情報。
    /// </param>
    private void HandleAttackEvent(
        AttackEventData attackEventData)
    {
        if (!IsRunning ||
            attackEventData == null)
        {
            return;
        }

        switch (attackEventData.AttackEventType)
        {
            case AttackEventType.ANIMATION_START:
                SubscribeGroundCollision();
                break;

            case AttackEventType.HITBOX_ENABLE:
                m_owner.EnableAttackHitboxes(
                    m_attackIdentifier);
                break;

            case AttackEventType.HITBOX_DISABLE:
                DisableAttackHitbox();
                break;

            case AttackEventType.IMPACT_START:
                break;

            case AttackEventType.ANIMATION_END:
                CompleteAttack();
                break;
        }
    }

    /// <summary>
    /// 尻尾の地面衝突を処理します。
    /// </summary>
    /// <param name="contactPosition">
    /// 地面との接触位置。
    /// </param>
    private void HandleGroundCollisionDetected(
        Vector3 contactPosition)
    {
        if (!IsRunning ||
            m_hasGroundCollision)
        {
            return;
        }

        m_hasGroundCollision =
            true;

        DisableAttackHitbox();

        if (m_tailSlamReference != null &&
            m_tailSlamReference.ImpactEffect != null)
        {
            Object.Instantiate(
                m_tailSlamReference.ImpactEffect,
                contactPosition,
                m_owner.transform.rotation);
        }

        /*
         * 一度接地した後は
         * 追加の接地通知は不要です。
         */
        UnsubscribeGroundCollision();
    }

    /// <summary>
    /// 尻尾攻撃を正常終了します。
    /// </summary>
    private void CompleteAttack()
    {
        if (!IsRunning)
        {
            return;
        }

        DisableAttackHitbox();

        UnsubscribeGroundCollision();

        UnsubscribeAnimationEvent();

        IsRunning =
            false;

        IsCompleted =
            true;
    }

    /// <summary>
    /// 尻尾攻撃を失敗終了します。
    /// </summary>
    private void FailAttack()
    {
        DisableAttackHitbox();

        UnsubscribeGroundCollision();

        UnsubscribeAnimationEvent();

        IsRunning =
            false;

        HasFailed =
            true;
    }

    /// <summary>
    /// 攻撃Hitboxを無効化します。
    /// </summary>
    private void DisableAttackHitbox()
    {
        if (m_attackIdentifier == null)
        {
            return;
        }

        m_owner?.AttackHitboxRegistry?
            .DisableHitboxes(
                m_attackIdentifier);
    }

    /// <summary>
    /// AnimationEventを購読します。
    /// </summary>
    private void SubscribeAnimationEvent()
    {
        if (m_isAnimationEventSubscribed ||
            m_animationEventReceiver == null)
        {
            return;
        }

        m_animationEventReceiver.AttackEventReceived +=
            HandleAttackEvent;

        m_isAnimationEventSubscribed =
            true;
    }

    /// <summary>
    /// AnimationEventの購読を解除します。
    /// </summary>
    private void UnsubscribeAnimationEvent()
    {
        if (!m_isAnimationEventSubscribed ||
            m_animationEventReceiver == null)
        {
            return;
        }

        m_animationEventReceiver.AttackEventReceived -=
            HandleAttackEvent;

        m_isAnimationEventSubscribed =
            false;
    }

    /// <summary>
    /// Ground Sensorを購読します。
    /// </summary>
    private void SubscribeGroundCollision()
    {
        if (m_isGroundCollisionSubscribed ||
            m_groundCollisionSensor == null)
        {
            return;
        }

        m_groundCollisionSensor.CollisionDetected +=
            HandleGroundCollisionDetected;

        m_isGroundCollisionSubscribed =
            true;
    }

    /// <summary>
    /// Ground Sensorの購読を解除します。
    /// </summary>
    private void UnsubscribeGroundCollision()
    {
        if (!m_isGroundCollisionSubscribed ||
            m_groundCollisionSensor == null)
        {
            return;
        }

        m_groundCollisionSensor.CollisionDetected -=
            HandleGroundCollisionDetected;

        m_isGroundCollisionSubscribed =
            false;
    }

    /// <summary>
    /// Runtime状態を初期化します。
    /// </summary>
    private void ResetRuntimeState()
    {
        m_hasGroundCollision =
            false;

        m_isAnimationEventSubscribed =
            false;

        m_isGroundCollisionSubscribed =
            false;

        IsRunning =
            false;

        IsCompleted =
            false;

        HasFailed =
            false;
    }
}