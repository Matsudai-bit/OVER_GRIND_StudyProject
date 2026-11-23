using UnityEngine;

/// <summary>
/// ステージ1フェーズ1の踏みつけ攻撃を実行します。
/// Playerに近い脚を選択し、
/// AnimationEventに合わせて攻撃判定を制御します。
/// </summary>
public sealed class S1P1BossStompState :
    StateBase<BossController>
{
    // 攻撃ID
    private AttackIdentifier m_attackIdentifier;

    // Animator Trigger ID
    private int m_animationTriggerID;

    // 使用中のAnimationEventReceiver
    private AnimationEventReceiver m_animationEventReceiver;

    // フェーズ1固有参照
    private S1P1BossReferences m_references;

    // フェーズ共通参照
    private BossPhaseReferences m_commonReferences;

    // AnimationEvent購読中か
    private bool m_isAnimationEventRegistered;

    /// <summary>
    /// 踏みつけ攻撃を開始します。
    /// </summary>
    protected override void OnStartState()
    {
        ResetRuntimeState();

        if (!TryGetReferences())
        {
            Debug.LogError(
                $"[{nameof(S1P1BossStompState)}] " +
                "必要な参照を取得できませんでした。");

            SetFailed();

            return;
        }

        S1P1BossAttackType attackType =
            GetAttackType();

        if (!TryApplyAttackSetting(
                attackType))
        {
            Debug.LogError(
                $"[{nameof(S1P1BossStompState)}] " +
                "踏みつけ攻撃の設定を取得できませんでした。");

            SetFailed();

            return;
        }

        /*
         * Trigger発火より先に購読することで、
         * Animation開始直後のEvent取りこぼしを防ぎます。
         */
        RegisterAnimationEvent();

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.RUNNING);

        /*
         * Animationの開始責務は
         * OnStartStateに集約します。
         */
        Owner.AnimationController.SetTrigger(
            m_animationTriggerID);
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
        if (attackEventData == null ||
            Owner == null ||
            Owner.GetStateExecutionStatus() !=
                StateExecutionStatus.RUNNING)
        {
            return;
        }

        switch (attackEventData.AttackEventType)
        {
            case AttackEventType.HITBOX_ENABLE:
                EnableAttackHitboxes();
                break;

            case AttackEventType.HITBOX_DISABLE:
                DisableAttackHitboxes();
                break;

            case AttackEventType.ANIMATION_END:
                CompleteAttack();
                break;
        }
    }

    /// <summary>
    /// 攻撃を終了します。
    /// </summary>
    protected override void OnExitState()
    {
        UnregisterAnimationEvent();

        /*
         * Stateが途中終了した場合でも
         * 攻撃判定が残らないよう必ず無効化します。
         */
        DisableAttackHitboxes();

        if (Owner != null &&
            Owner.GetStateExecutionStatus() ==
                StateExecutionStatus.RUNNING)
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);
        }

        m_attackIdentifier =
            null;

        m_animationEventReceiver =
            null;

        m_references =
            null;

        m_commonReferences =
            null;
    }

    /// <summary>
    /// Playerに近い脚の攻撃種別を取得します。
    /// </summary>
    /// <returns>
    /// 使用する踏みつけ攻撃種別。
    /// </returns>
    private S1P1BossAttackType GetAttackType()
    {
        Vector3 playerPosition =
            m_commonReferences
                .PlayerTransform
                .position;

        Vector3 leftDifference =
            m_references
                .LeftLegTransform
                .position -
            playerPosition;

        Vector3 rightDifference =
            m_references
                .RightLegTransform
                .position -
            playerPosition;

        /*
         * 距離そのものは不要なため、
         * sqrtを行わないsqrMagnitudeで比較します。
         */
        if (leftDifference.sqrMagnitude >
            rightDifference.sqrMagnitude)
        {
            return S1P1BossAttackType.RIGHT_LEG;
        }

        return S1P1BossAttackType.LEFT_LEG;
    }

    /// <summary>
    /// 踏みつけ攻撃の設定を取得します。
    /// </summary>
    /// <param name="attackType">
    /// 使用する踏みつけ攻撃種別。
    /// </param>
    /// <returns>
    /// true：必要な設定を取得できました。
    /// false：設定が不足しています。
    /// </returns>
    private bool TryApplyAttackSetting(
        S1P1BossAttackType attackType)
    {
        if (Owner == null ||
            Owner.AnimationController == null)
        {
            return false;
        }

        S1P1BossAttackSettings attackSettings =
            Owner.GetComponentInChildren<
                S1P1BossAttackSettings>(
                    true);

        if (attackSettings == null)
        {
            Debug.LogError(
                $"{nameof(S1P1BossAttackSettings)}が" +
                "見つかりません。");

            return false;
        }

        if (!attackSettings.TryGetAttackSetting(
                attackType,
                out m_attackIdentifier,
                out string animationTriggerName))
        {
            Debug.LogError(
                $"{attackType}の攻撃設定がありません。");

            return false;
        }

        if (m_attackIdentifier == null ||
            string.IsNullOrEmpty(
                animationTriggerName))
        {
            return false;
        }

        m_animationEventReceiver =
            Owner.AnimationController
                .CurrentAnimationEventReceiver;

        if (m_animationEventReceiver == null)
        {
            Debug.LogError(
                $"{nameof(AnimationEventReceiver)}が" +
                "設定されていません。");

            return false;
        }

        m_animationTriggerID =
            Animator.StringToHash(
                animationTriggerName);

        return true;
    }

    /// <summary>
    /// State実行に必要な参照を取得します。
    /// </summary>
    /// <returns>
    /// true：取得できました。
    /// false：必要な参照がありません。
    /// </returns>
    private bool TryGetReferences()
    {
        if (Owner == null ||
            Owner.PhaseController == null)
        {
            return false;
        }

        if (!Owner.PhaseController
                .TryGetCurrentPhaseComponent(
                    out m_references))
        {
            return false;
        }

        if (!Owner.PhaseController
                .TryGetCurrentPhaseComponent(
                    out m_commonReferences))
        {
            return false;
        }

        if (m_references == null ||
            m_commonReferences == null)
        {
            return false;
        }

        if (m_references.LeftLegTransform == null ||
            m_references.RightLegTransform == null)
        {
            Debug.LogError(
                "踏みつけ攻撃用の脚Transformが" +
                "設定されていません。");

            return false;
        }

        if (m_commonReferences.PlayerTransform == null)
        {
            Debug.LogError(
                "PlayerTransformが設定されていません。");

            return false;
        }

        return true;
    }

    /// <summary>
    /// 踏みつけ攻撃判定を有効化します。
    /// </summary>
    private void EnableAttackHitboxes()
    {
        if (Owner == null ||
            m_attackIdentifier == null)
        {
            return;
        }

        Owner.EnableAttackHitboxes(
            m_attackIdentifier);
    }

    /// <summary>
    /// 踏みつけ攻撃判定を無効化します。
    /// </summary>
    private void DisableAttackHitboxes()
    {
        if (Owner == null ||
            m_attackIdentifier == null)
        {
            return;
        }

        Owner.AttackHitboxRegistry?
            .DisableHitboxes(
                m_attackIdentifier);
    }

    /// <summary>
    /// 踏みつけ攻撃を正常終了します。
    /// </summary>
    private void CompleteAttack()
    {
        if (Owner == null ||
            Owner.GetStateExecutionStatus() !=
                StateExecutionStatus.RUNNING)
        {
            return;
        }

        /*
         * AnimationEvent順序に依存せず、
         * State終了時には確実に判定を停止します。
         */
        DisableAttackHitboxes();

        /*
         * SUCCEEDEDへ変更すると
         * Behavior側が次の行動判定へ進む可能性があるため、
         * 必ず先にクールタイムを登録します。
         */
        StartCoolTime();

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.SUCCEEDED);
    }

    /// <summary>
    /// AnimationEventを購読します。
    /// </summary>
    private void RegisterAnimationEvent()
    {
        if (m_isAnimationEventRegistered ||
            m_animationEventReceiver == null)
        {
            return;
        }

        m_animationEventReceiver.AttackEventReceived +=
            HandleAttackEvent;

        m_isAnimationEventRegistered =
            true;
    }

    /// <summary>
    /// AnimationEventの購読を解除します。
    /// </summary>
    private void UnregisterAnimationEvent()
    {
        if (!m_isAnimationEventRegistered ||
            m_animationEventReceiver == null)
        {
            return;
        }

        m_animationEventReceiver.AttackEventReceived -=
            HandleAttackEvent;

        m_isAnimationEventRegistered =
            false;
    }

    /// <summary>
    /// 踏みつけ攻撃のクールタイムを開始します。
    /// </summary>
    private void StartCoolTime()
    {
        if (Owner == null ||
            m_references == null ||
            m_references.DecisionParameterAsset == null)
        {
            return;
        }

        BossStateCoolTimeManager coolTimeManager =
            Owner.GetComponent<
                BossStateCoolTimeManager>();

        if (coolTimeManager == null)
        {
            return;
        }

        coolTimeManager
            .StartCoolTime<
                S1P1BossStompState>(
                m_references
                    .DecisionParameterAsset
                    .Stomp
                    .CoolTime);
    }

    /// <summary>
    /// Stateを失敗状態にします。
    /// </summary>
    private void SetFailed()
    {
        DisableAttackHitboxes();

        UnregisterAnimationEvent();

        if (Owner == null)
        {
            return;
        }

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.FAILED);
    }

    /// <summary>
    /// 実行時情報を初期化します。
    /// </summary>
    private void ResetRuntimeState()
    {
        m_attackIdentifier =
            null;

        m_animationEventReceiver =
            null;

        m_references =
            null;

        m_commonReferences =
            null;

        m_animationTriggerID =
            0;

        m_isAnimationEventRegistered =
            false;
    }
}