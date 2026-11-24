using UnityEngine;

/// <summary>
/// ステージ1フェーズ1の歩行を実行します。
/// AnimationEventに同期して移動と攻撃判定を制御します。
/// </summary>
public sealed class S1P1BossWalkState :
    StateBase<BossController>
{
    // Animatorパラメータ名
    private const string WALK_PARAMETER_NAME = "Walk";

    // AnimationEvent待機の猶予時間
    private const float ANIMATION_TIMEOUT_MARGIN = 2.0f;

    // AnimatorパラメータID
    private static readonly int WALK_PARAMETER_ID =
        Animator.StringToHash(
            WALK_PARAMETER_NAME);

    // S1P1固有参照
    private S1P1BossReferences m_references;

    // 歩行状態パラメータ
    private S1P1BossWalkStateParameters m_parameters;

    // 攻撃ID
    private AttackIdentifier m_attackIdentifier;

    // Animator Trigger ID
    private int m_animationTriggerID;

    // 使用中のAnimationEventReceiver
    private AnimationEventReceiver m_animationEventReceiver;

    // Animation開始からの経過時間
    private float m_animationElapsedTime;

    // Animationの再生時間
    private float m_animationDuration;

    // Animationが開始されたか
    private bool m_isAnimationStarted;

    // 移動中に進行不能になったか
    private bool m_hasMovementFailed;

    // AnimationEvent購読中か
    private bool m_isAnimationEventRegistered;

    // State開始からの経過時間
    private float m_stateElapsedTime;

    /// <summary>
    /// 歩行を開始します。
    /// </summary>
    protected override void OnStartState()
    {
        ResetRuntimeState();

        if (Owner == null ||
            Owner.Navigation == null ||
            Owner.Motor == null ||
            Owner.AnimationController == null)
        {
            SetImmediateFailed();

            return;
        }

        if (!TryGetParameters())
        {
            Debug.LogError(
                $"[{nameof(S1P1BossWalkState)}] " +
                "歩行パラメータを取得できませんでした。");

            SetImmediateFailed();

            return;
        }

        if (!TryGetAnimationDuration())
        {
            Debug.LogError(
                $"[{nameof(S1P1BossWalkState)}] " +
                "歩行Animationの再生時間を取得できませんでした。");

            SetImmediateFailed();

            return;
        }

        if (!TryApplyAttackSetting())
        {
            Debug.LogError(
                $"[{nameof(S1P1BossWalkState)}] " +
                "歩行攻撃の設定を取得できませんでした。");

            SetImmediateFailed();

            return;
        }

        /*
         * Animation開始前にEventを購読します。
         * 開始直後のAnimationEvent取りこぼしを防ぎます。
         */
        RegisterAnimationEvent();

        /*
         * AnimationEvent側ではRUNNINGのみ受け付けるため、
         * Animationを開始する前にRUNNINGへ変更します。
         */
        Owner.SetStateExecutionStatus(
            StateExecutionStatus.RUNNING);

        Owner.AnimationController.SetBool(
            WALK_PARAMETER_ID,
            true);

        Owner.AnimationController.SetTrigger(
            m_animationTriggerID);
    }

    /// <summary>
    /// AnimationEvent待機のタイムアウトを監視します。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過時間。</param>
    protected override void OnUpdate(
        float deltaTime)
    {
        if (Owner == null ||
            Owner.GetStateExecutionStatus() !=
                StateExecutionStatus.RUNNING)
        {
            return;
        }

        m_stateElapsedTime +=
            Mathf.Max(
                0.0f,
                deltaTime);

        if (m_stateElapsedTime <
            m_animationDuration +
            ANIMATION_TIMEOUT_MARGIN)
        {
            return;
        }

        Debug.LogError(
            $"[{nameof(S1P1BossWalkState)}] " +
            "歩行Animationがタイムアウトしました。");

        SetImmediateFailed();
    }

    /// <summary>
    /// 前方への物理移動を実行します。
    /// </summary>
    protected override void OnFixedUpdate()
    {
        if (Owner == null ||
            Owner.GetStateExecutionStatus() !=
                StateExecutionStatus.RUNNING ||
            !m_isAnimationStarted ||
            m_parameters == null ||
            m_hasMovementFailed)
        {
            return;
        }

        /*
         * Animation中に進行不能になった場合、
         * 移動だけを停止します。
         *
         * StateはAnimation終了までRUNNINGを維持し、
         * ANIMATION_END受信時にFAILEDへ変更します。
         */
        if (!CanMoveForward())
        {
            m_hasMovementFailed =
                true;

            Owner.Motor?
                .StopHorizontalMovement();

            return;
        }

        m_animationElapsedTime +=
            Time.fixedDeltaTime;

        float normalizedTime =
            Mathf.Clamp01(
                m_animationElapsedTime /
                m_animationDuration);

        float speedRate =
            Mathf.Clamp01(
                m_parameters
                    .MoveEasingCurve
                    .Evaluate(
                        normalizedTime));

        Owner.Motor?
            .MoveForwardWithSpeedRate(
                speedRate);
    }

    /// <summary>
    /// 歩行を終了します。
    /// </summary>
    protected override void OnExitState()
    {
        Owner?.Motor?
            .StopHorizontalMovement();

        UnregisterAnimationEvent();

        DisableAttackHitboxes();

        Owner?.AnimationController?
            .SetBool(
                WALK_PARAMETER_ID,
                false);

        /*
         * Animation終了前に外部要因で
         * Stateを抜けた場合のみFAILEDとします。
         */
        if (Owner != null &&
            Owner.GetStateExecutionStatus() ==
                StateExecutionStatus.RUNNING)
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);
        }

        m_references =
            null;

        m_parameters =
            null;

        m_attackIdentifier =
            null;

        m_animationEventReceiver =
            null;

        ResetRuntimeState();
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
            case AttackEventType.ANIMATION_START:
                StartMovement();
                break;

            case AttackEventType.HITBOX_ENABLE:
                EnableAttackHitboxes();
                break;

            case AttackEventType.HITBOX_DISABLE:
                DisableAttackHitboxes();
                break;

            case AttackEventType.ANIMATION_END:
                FinishWalk();
                break;
        }
    }

    /// <summary>
    /// Animationに同期した移動を開始します。
    /// </summary>
    private void StartMovement()
    {
        if (m_isAnimationStarted)
        {
            return;
        }

        m_animationElapsedTime =
            0.0f;

        m_isAnimationStarted =
            true;

        Owner?.Motor?
            .StopHorizontalMovement();
    }

    /// <summary>
    /// Animation終了に合わせて歩行状態を終了します。
    /// </summary>
    private void FinishWalk()
    {
        if (Owner == null ||
            Owner.GetStateExecutionStatus() !=
                StateExecutionStatus.RUNNING)
        {
            return;
        }

        if (!m_isAnimationStarted)
        {
            Debug.LogError(
                $"[{nameof(S1P1BossWalkState)}] " +
                "ANIMATION_STARTを受信する前にANIMATION_ENDを受信しました。");

            SetImmediateFailed();

            return;
        }

        m_isAnimationStarted =
            false;

        Owner.Motor?
            .StopHorizontalMovement();

        /*
         * HITBOX_DISABLEを取りこぼした場合でも
         * Animation終了時には必ず攻撃判定を停止します。
         */
        DisableAttackHitboxes();

        /*
         * 移動に失敗していた場合は
         * 正常終了ではないためクールタイムを開始しません。
         */
        if (m_hasMovementFailed)
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);

            return;
        }

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
    /// Runtime状態を初期化します。
    /// </summary>
    private void ResetRuntimeState()
    {
        m_animationElapsedTime =
            0.0f;

        m_stateElapsedTime =
            0.0f;

        m_animationDuration =
            0.0f;

        m_isAnimationStarted =
            false;

        m_hasMovementFailed =
            false;

        m_isAnimationEventRegistered =
            false;

        m_animationTriggerID =
            0;
    }

    /// <summary>
    /// Animationの再生時間を取得します。
    /// </summary>
    /// <returns>
    /// true：取得できました。
    /// false：取得できませんでした。
    /// </returns>
    private bool TryGetAnimationDuration()
    {
        if (m_parameters == null ||
            m_parameters.WalkAnimationClip == null)
        {
            return false;
        }

        m_animationDuration =
            m_parameters
                .WalkAnimationClip
                .length;

        return m_animationDuration >
               Mathf.Epsilon;
    }

    /// <summary>
    /// 現在フェーズの歩行パラメータを取得します。
    /// </summary>
    /// <returns>
    /// true：取得できました。
    /// false：必要な設定がありません。
    /// </returns>
    private bool TryGetParameters()
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

        if (m_references == null ||
            m_references.StateParameterAsset == null ||
            !m_references
                .StateParameterAsset
                .HasRequiredParameters())
        {
            return false;
        }

        m_parameters =
            m_references
                .StateParameterAsset
                .Walk;

        return m_parameters != null;
    }

    /// <summary>
    /// ボスの前方へ直進できるか確認します。
    /// </summary>
    /// <returns>
    /// true：直進できます。
    /// false：直進できません。
    /// </returns>
    private bool CanMoveForward()
    {
        if (Owner?.Navigation == null ||
            m_parameters == null)
        {
            return false;
        }

        return Owner.Navigation.CanMoveStraight(
            Owner.transform.forward,
            m_parameters.ForwardCheckDistance);
    }

    /// <summary>
    /// 歩行攻撃の設定を取得します。
    /// </summary>
    /// <returns>
    /// true：設定を取得できました。
    /// false：設定が不足しています。
    /// </returns>
    private bool TryApplyAttackSetting()
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
            return false;
        }

        if (!attackSettings.TryGetAttackSetting(
                S1P1BossAttackType.WALKING,
                out m_attackIdentifier,
                out string animationTriggerName))
        {
            return false;
        }

        if (string.IsNullOrEmpty(
                animationTriggerName) ||
            m_attackIdentifier == null)
        {
            return false;
        }

        m_animationEventReceiver =
            Owner.AnimationController
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
    /// AnimationEventを購読します。
    /// </summary>
    private void RegisterAnimationEvent()
    {
        if (m_isAnimationEventRegistered ||
            m_animationEventReceiver == null)
        {
            return;
        }

        m_animationEventReceiver
            .AttackEventReceived +=
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

        m_animationEventReceiver
            .AttackEventReceived -=
            HandleAttackEvent;

        m_isAnimationEventRegistered =
            false;
    }

    /// <summary>
    /// 歩行中の攻撃判定を有効化します。
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
    /// 歩行中の攻撃判定を無効化します。
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
    /// 正常終了した歩行のクールタイムを開始します。
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
                S1P1BossWalkState>(
                m_references
                    .DecisionParameterAsset
                    .Walk
                    .CoolTime);
    }

    /// <summary>
    /// Animation開始前の設定不備として終了します。
    /// </summary>
    private void SetImmediateFailed()
    {
        UnregisterAnimationEvent();

        DisableAttackHitboxes();

        Owner?.Motor?
            .StopHorizontalMovement();

        Owner?.AnimationController?
            .SetBool(
                WALK_PARAMETER_ID,
                false);

        if (Owner == null)
        {
            return;
        }

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.FAILED);
    }
}