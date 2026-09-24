using UnityEngine;

/// <summary>
/// ステージ1フェーズ2の尻尾叩きつけ攻撃を実行します。
/// </summary>
public sealed class S1P2BossTailSlamState :
    StateBase<BossController>
{
    // 尻尾叩きつけ攻撃のクールタイム
    private const float COOLDOWN_DURATION = 2.0f;

    // 攻撃ID
    private AttackIdentifier m_attackIdentifier;

    // Animator Trigger ID
    private int m_animationTriggerID;

    // 次に攻撃可能になる時刻
    private float m_cooldownEndTime;

    // AnimationEventを購読しているか
    private bool m_isEventSubscribed;

    /// <summary>
    /// 尻尾叩きつけ攻撃を開始します。
    /// </summary>
    protected override void OnStartState()
    {
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

        // Stateが途中で終了しても攻撃判定を残さない
        if (m_attackIdentifier != null)
        {
            Owner.AttackHitboxRegistry?.DisableHitboxes(
                m_attackIdentifier);
        }

        if (Owner.GetStateExecutionStatus() ==
            StateExecutionStatus.RUNNING)
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);
        }
    }

    /// <summary>
    /// 尻尾叩きつけ攻撃の設定を適用します。
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

        if (!attackSettings.TryGetAttackSetting(
                S1P2BossAttackType.TAIL_SLAM,
                out m_attackIdentifier,
                out string animationTriggerName))
        {
            Debug.LogError(
                $"{S1P2BossAttackType.TAIL_SLAM}" +
                "の攻撃設定がありません。");

            return false;
        }

        if (m_attackIdentifier == null ||
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
            case AttackEventType.HITBOX_ENABLE:
                Owner.EnableAttackHitboxes(
                    m_attackIdentifier);
                break;

            case AttackEventType.HITBOX_DISABLE:
                Owner.AttackHitboxRegistry?.DisableHitboxes(
                    m_attackIdentifier);
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
        // 念のため攻撃終了時にも判定を無効化する
        Owner.AttackHitboxRegistry?.DisableHitboxes(
            m_attackIdentifier);

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
        if (m_isEventSubscribed)
        {
            return;
        }

        Owner.AnimationController
            .CurrentAnimationEventReceiver
            .AttackEventReceived +=
            HandleAttackEvent;

        m_isEventSubscribed = true;
    }

    /// <summary>
    /// AnimationEventの購読を解除します。
    /// </summary>
    private void UnsubscribeAnimationEvent()
    {
        if (!m_isEventSubscribed ||
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

        m_isEventSubscribed = false;
    }
}