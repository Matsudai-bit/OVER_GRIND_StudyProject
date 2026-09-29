using UnityEngine;

/// <summary>
/// ステージ1フェーズ1の歩行を実行します。
/// </summary>
public sealed class S1P1BossWalkState :
    StateBase<BossController>
{
    // Animatorパラメータ名
    private const string WALK_PARAMETER_NAME = "Walk";

    // AnimatorパラメータID
    private static readonly int WALK_PARAMETER_ID =
        Animator.StringToHash(WALK_PARAMETER_NAME);

    // S1P1固有参照
    private S1P1BossReferences m_references;

    // 歩行状態パラメータ
    private S1P1BossWalkStateParameters m_parameters;

    // 歩行経過時間
    private float m_elapsedTime;

    // 停止状態への変更を要求したか
    private bool m_isIdleRequested;

    // 攻撃ID
    private AttackIdentifier m_attackIdentifier;

    // Animator Trigger ID
    private int m_animationTriggerID;

    // 使用中のAnimationEventReceiver
    private AnimationEventReceiver m_animationEventReceiver;

    /// <summary>
    /// 歩行を開始します。
    /// </summary>
    protected override void OnStartState()
    {
        m_elapsedTime = 0.0f;
        m_isIdleRequested = false;

        if (!TryGetParameters() ||
            Owner.Navigation == null ||
            Owner.Motor == null)
        {
            RequestFailedIdleState();
            return;
        }

        // 開始時点で前方へ進めるか確認します。
        if (!CanMoveForward())
        {
            RequestIdleState();
            return;
        }

        Owner.AnimationController?.SetBool(
            WALK_PARAMETER_ID,
            true);

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.RUNNING);

        if (!ApplyAttackSetting())
        {
            Debug.LogError(
                "歩行攻撃の設定を取得できませんでした。");

            RequestFailedIdleState();
        }
    }

    /// <summary>
    /// 歩行時間を更新します。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過時間。</param>
    protected override void OnUpdate(float deltaTime)
    {
        if (m_isIdleRequested ||
            m_parameters == null)
        {
            return;
        }

        m_elapsedTime += deltaTime;

        if (m_elapsedTime <
            m_parameters.WalkDuration)
        {
            return;
        }

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.SUCCEEDED);
    }

    /// <summary>
    /// 前方への物理移動を実行します。
    /// </summary>
    protected override void OnFixedUpdate()
    {
        if (m_isIdleRequested ||
            m_parameters == null)
        {
            return;
        }

        if (!CanMoveForward())
        {
            RequestIdleState();
            return;
        }

        Owner.Motor.MoveForward(
            Time.fixedDeltaTime);
    }

    /// <summary>
    /// 歩行を終了します。
    /// </summary>
    protected override void OnExitState()
    {
        StartCoolTimeIfSucceeded();

        Owner.Motor?.StopHorizontalMovement();

        if (m_animationEventReceiver != null)
        {
            m_animationEventReceiver.AttackEventReceived -=
                HandleAttackEvent;
        }

        if (m_attackIdentifier != null)
        {
            Owner.AttackHitboxRegistry?.DisableHitboxes(
                m_attackIdentifier);
        }

        Owner.AnimationController?.SetBool(
            WALK_PARAMETER_ID,
            false);

        if (Owner.GetStateExecutionStatus() ==
            StateExecutionStatus.RUNNING)
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);
        }

        m_references = null;
        m_parameters = null;
        m_animationEventReceiver = null;
    }

    /// <summary>
    /// 現在フェーズの歩行パラメータを取得します。
    /// </summary>
    /// <returns>
    /// true：取得できました。
    /// false：取得できませんでした。
    /// </returns>
    private bool TryGetParameters()
    {
        if (Owner.PhaseController == null ||
            !Owner.PhaseController.TryGetCurrentPhaseComponent(
                out m_references) ||
            m_references.StateParameterAsset == null ||
            !m_references.StateParameterAsset.HasRequiredParameters())
        {
            return false;
        }

        m_parameters =
            m_references.StateParameterAsset.Walk;

        return m_parameters != null;
    }

    /// <summary>
    /// 正常終了した歩行のクールタイムを開始します。
    /// </summary>
    private void StartCoolTimeIfSucceeded()
    {
        if (Owner.GetStateExecutionStatus() !=
            StateExecutionStatus.SUCCEEDED ||
            m_references == null ||
            m_references.DecisionParameterAsset == null)
        {
            return;
        }

        BossStateCoolTimeManager coolTimeManager =
            Owner.GetComponent<BossStateCoolTimeManager>();

        if (coolTimeManager == null)
        {
            return;
        }

        coolTimeManager.StartCoolTime<S1P1BossWalkState>(
            m_references.DecisionParameterAsset.Walk.CoolTime);
    }

    /// <summary>
    /// ボスの前方へ直進できるか確認します。
    /// </summary>
    /// <returns>
    /// true：前方へ直進できます。
    /// false：前方へ直進できません。
    /// </returns>
    private bool CanMoveForward()
    {
        if (Owner.Navigation == null ||
            m_parameters == null)
        {
            return false;
        }

        return Owner.Navigation.CanMoveStraight(
            Owner.transform.forward,
            m_parameters.ForwardCheckDistance);
    }

    /// <summary>
    /// 停止状態への変更を要求します。
    /// </summary>
    private void RequestIdleState()
    {
        if (m_isIdleRequested)
        {
            return;
        }

        m_isIdleRequested = true;

        Owner.Motor?.StopHorizontalMovement();

        float idleDuration =
            m_parameters?.BlockedIdleDuration ?? 0.0f;

        Machine.ChangeState<BossIdleState>(
            idleDuration);
    }

    /// <summary>
    /// 失敗状態として停止状態への変更を要求します。
    /// </summary>
    private void RequestFailedIdleState()
    {
        Owner.SetStateExecutionStatus(
            StateExecutionStatus.FAILED);

        RequestIdleState();
    }

    /// <summary>
    /// 歩行攻撃の設定を適用します。
    /// </summary>
    /// <returns>
    /// true：設定できました。
    /// false：設定できませんでした。
    /// </returns>
    private bool ApplyAttackSetting()
    {
        S1P1BossAttackSettings attackSettings =
            Owner.GetComponentInChildren<
                S1P1BossAttackSettings>(true);

        if (attackSettings == null)
        {
            return false;
        }

        if (!attackSettings.TryGetAttackSetting(
                S1P1BossAttackType.WALKING,
                out m_attackIdentifier,
                out string animationTriggerName))
        {
            return false;
        }

        if (string.IsNullOrEmpty(animationTriggerName) ||
            m_attackIdentifier == null ||
            Owner.AnimationController == null)
        {
            return false;
        }

        m_animationEventReceiver =
            Owner.AnimationController.CurrentAnimationEventReceiver;

        if (m_animationEventReceiver == null)
        {
            return false;
        }

        m_animationTriggerID =
            Animator.StringToHash(
                animationTriggerName);

        m_animationEventReceiver.AttackEventReceived +=
            HandleAttackEvent;

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
                Owner.SetStateExecutionStatus(
                    StateExecutionStatus.SUCCEEDED);
                break;
        }
    }
}
