using UnityEngine;

/// <summary>
/// ステージ1フェーズ2の尻尾叩きつけ攻撃を実行します。
/// </summary>
public sealed class S1P2BossTailSlamState :
    StateBase<BossController>
{
    // 尻尾叩きつけ攻撃のクールタイム
    private const float COOLDOWN_DURATION = 2.0f;

    // 尻尾叩きつけ攻撃ID
    private AttackIdentifier m_tailSlamAttackIdentifier;

    // 衝撃波攻撃ID
    //private AttackIdentifier m_impactAttackIdentifier;

    // Animator Trigger ID
    private int m_animationTriggerID;

    // 次に攻撃可能になる時刻
    private float m_cooldownEndTime;

    // AnimationEventを購読しているか
    private bool m_isAnimationEventSubscribed;

    // 地面衝突イベントを購読しているか
    private bool m_isGroundCollisionSubscribed;

    // フェーズ固有参照
    private S1P2BossReferences m_references;

    // 尻尾の地面衝突検知
    private CollisionSensor m_groundCollisionSensor;

    // 地面衝突済みか
    private bool m_hasGroundCollision;

    /// <summary>
    /// 尻尾叩きつけ攻撃を開始します。
    /// </summary>
    protected override void OnStartState()
    {
        m_hasGroundCollision = false;

        if (!TryGetReferences())
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);

            return;
        }

        if (IsCooldown())
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);

            return;
        }

        // 攻撃中は移動を停止する
        Owner.Motor?.StopHorizontalMovement();

        if (!ApplyAttackSetting())
        {
            Debug.LogError(
                "尻尾叩きつけ攻撃を開始できませんでした。");

            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);

            return;
        }

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.RUNNING);
    }

    /// <summary>
    /// 尻尾叩きつけ攻撃を終了します。
    /// </summary>
    protected override void OnExitState()
    {
        UnsubscribeAnimationEvent();
        UnsubscribeGroundCollision();

        // State途中終了時にも攻撃判定を残さない
        if (m_tailSlamAttackIdentifier != null)
        {
            Owner.AttackHitboxRegistry?.DisableHitboxes(
                m_tailSlamAttackIdentifier);
        }

        //if (m_impactAttackIdentifier != null)
        //{
        //    Owner.AttackHitboxRegistry?.DisableHitboxes(
        //        m_impactAttackIdentifier);
        //}

        if (Owner.GetStateExecutionStatus() ==
            StateExecutionStatus.RUNNING)
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);
        }

        m_references = null;
        m_groundCollisionSensor = null;
    }

    /// <summary>
    /// フェーズ固有参照を取得します。
    /// </summary>
    /// <returns>
    /// true：必要な参照を取得できました。
    /// false：取得できませんでした。
    /// </returns>
    private bool TryGetReferences()
    {
        if (Owner.PhaseController == null)
        {
            return false;
        }

        if (!Owner.PhaseController
                .TryGetCurrentPhaseComponent(
                    out m_references))
        {
            return false;
        }

        m_groundCollisionSensor =
            m_references.TailSlamReference.TailGroundCollisionSensor;

        if (m_groundCollisionSensor == null)
        {
            Debug.LogError(
                $"{nameof(CollisionSensor)}が設定されていません。");

            return false;
        }

        return true;
    }

    /// <summary>
    /// 尻尾が地面に衝突した際の処理を実行します。
    /// </summary>
    /// <param name="contactPosition">地面との接触位置。</param>
    private void HandleTailGroundCollisionDetected(
        Vector3 contactPosition)
    {
        if (m_hasGroundCollision)
        {
            return;
        }

        m_hasGroundCollision = true;

        // 尻尾本体の攻撃判定を終了する
        Owner.AttackHitboxRegistry?.DisableHitboxes(
            m_tailSlamAttackIdentifier);

        //// 衝撃波を開始する
        Object.Instantiate(m_references.TailSlamReference.ImpactEffect, contactPosition, Owner.transform.rotation);

        Debug.Log(
            $"尻尾が地面に衝突しました。位置: {contactPosition}");

        // 一度検知したらそれ以降の地面衝突は不要
        UnsubscribeGroundCollision();
    }

    /// <summary>
    /// 攻撃設定を適用します。
    /// </summary>
    /// <returns>
    /// true：設定できました。
    /// false：設定できませんでした。
    /// </returns>
    private bool ApplyAttackSetting()
    {
        S1P2BossAttackSettings attackSettings =
            Owner.GetComponentInChildren<
                S1P2BossAttackSettings>(true);

        if (attackSettings == null)
        {
            Debug.LogError(
                $"{nameof(S1P2BossAttackSettings)}が見つかりません。");

            return false;
        }

        // 尻尾叩きつけ攻撃
        if (!attackSettings.TryGetAttackSetting(
                S1P2BossAttackType.TAIL_SLAM,
                out m_tailSlamAttackIdentifier,
                out string animationTriggerName))
        {
            Debug.LogError(
                $"{S1P2BossAttackType.TAIL_SLAM}" +
                "の攻撃設定がありません。");

            return false;
        }

        // 衝撃波攻撃
        //if (!attackSettings.TryGetAttackSetting(
        //        S1P2BossAttackType.TAIL_SLAM_IMPACT,
        //        out m_impactAttackIdentifier,
        //        out _))
        //{
        //    Debug.LogError(
        //        $"{S1P2BossAttackType.TAIL_SLAM_IMPACT}" +
        //        "の攻撃設定がありません。");

        //    return false;
        //}

        if (m_tailSlamAttackIdentifier == null ||
            //m_impactAttackIdentifier == null ||
            string.IsNullOrEmpty(animationTriggerName) ||
            Owner.AnimationController == null ||
            Owner.AnimationController
                .CurrentAnimationEventReceiver == null)
        {
            return false;
        }

        m_animationTriggerID =
            Animator.StringToHash(
                animationTriggerName);

        SubscribeAnimationEvent();

        Owner.AnimationController.SetTrigger(
            m_animationTriggerID);

        return true;
    }

    /// <summary>
    /// 攻撃AnimationEventを処理します。
    /// </summary>
    /// <param name="attackEventData">攻撃イベント情報。</param>
    private void HandleAttackEvent(
        AttackEventData attackEventData)
    {
        switch (attackEventData.AttackEventType)
        {
            case AttackEventType.ANIMATION_START:
                SubscribeGroundCollision();
                break;

            case AttackEventType.HITBOX_ENABLE:
                Owner.EnableAttackHitboxes(
                    m_tailSlamAttackIdentifier);
                break;

            case AttackEventType.HITBOX_DISABLE:
                Owner.AttackHitboxRegistry?.DisableHitboxes(
                    m_tailSlamAttackIdentifier);
                break;

            case AttackEventType.IMPACT_START:
                //Owner.AttackHitboxRegistry?.DisableHitboxes(
                //    m_impactAttackIdentifier);
                break;

            case AttackEventType.ANIMATION_END:
                FinishAttack();
                break;
        }
    }

    /// <summary>
    /// 尻尾叩きつけ攻撃を正常終了します。
    /// </summary>
    private void FinishAttack()
    {
        UnsubscribeGroundCollision();

        Owner.AttackHitboxRegistry?.DisableHitboxes(
            m_tailSlamAttackIdentifier);

        //Owner.AttackHitboxRegistry?.DisableHitboxes(
        //    m_impactAttackIdentifier);

        StartCooldown();

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.SUCCEEDED);
    }

    /// <summary>
    /// クールタイムを開始します。
    /// </summary>
    private void StartCooldown()
    {
        m_cooldownEndTime =
            Time.time + COOLDOWN_DURATION;
    }

    /// <summary>
    /// クールタイム中か確認します。
    /// </summary>
    /// <returns>
    /// true：クールタイム中です。
    /// false：攻撃可能です。
    /// </returns>
    private bool IsCooldown()
    {
        return Time.time < m_cooldownEndTime;
    }

    /// <summary>
    /// AnimationEventを購読します。
    /// </summary>
    private void SubscribeAnimationEvent()
    {
        if (m_isAnimationEventSubscribed)
        {
            return;
        }

        Owner.AnimationController
            .CurrentAnimationEventReceiver
            .AttackEventReceived +=
            HandleAttackEvent;

        m_isAnimationEventSubscribed = true;
    }

    /// <summary>
    /// AnimationEventの購読を解除します。
    /// </summary>
    private void UnsubscribeAnimationEvent()
    {
        if (!m_isAnimationEventSubscribed ||
            Owner.AnimationController == null ||
            Owner.AnimationController
                .CurrentAnimationEventReceiver == null)
        {
            return;
        }

        Owner.AnimationController
            .CurrentAnimationEventReceiver
            .AttackEventReceived -=
            HandleAttackEvent;

        m_isAnimationEventSubscribed = false;
    }

    /// <summary>
    /// 地面衝突イベントを購読します。
    /// </summary>
    private void SubscribeGroundCollision()
    {
        if (m_isGroundCollisionSubscribed ||
            m_groundCollisionSensor == null)
        {
            return;
        }

        m_groundCollisionSensor.CollisionDetected +=
            HandleTailGroundCollisionDetected;

        m_isGroundCollisionSubscribed = true;
    }

    /// <summary>
    /// 地面衝突イベントの購読を解除します。
    /// </summary>
    private void UnsubscribeGroundCollision()
    {
        if (!m_isGroundCollisionSubscribed ||
            m_groundCollisionSensor == null)
        {
            return;
        }

        m_groundCollisionSensor.CollisionDetected -=
            HandleTailGroundCollisionDetected;

        m_isGroundCollisionSubscribed = false;
    }
}