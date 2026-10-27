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

    /// <summary>
    /// 歩行を開始します。
    /// </summary>
    protected override void OnStartState()
    {
        ResetRuntimeState();

        if (!TryGetParameters() ||
            Owner.Navigation == null ||
            Owner.Motor == null ||
            Owner.AnimationController == null)
        {
            SetImmediateFailed();
            return;
        }

        if (!TryGetAnimationDuration())
        {
            Debug.LogError(
                $"[{nameof(S1P1BossWalkState)}] " +
                "歩行Animationの再生時間を取得できませんでした.");

            SetImmediateFailed();
            return;
        }

        if (!ApplyAttackSetting())
        {
            Debug.LogError(
                $"[{nameof(S1P1BossWalkState)}] " +
                "歩行攻撃の設定を取得できませんでした.");

            SetImmediateFailed();
            return;
        }

        Owner.AnimationController.SetBool(
            WALK_PARAMETER_ID,
            true);

        // State開始後はAnimation終了までRUNNINGを維持する
        Owner.SetStateExecutionStatus(
            StateExecutionStatus.RUNNING);
    }

    /// <summary>
    /// 前方への物理移動を実行します。
    /// </summary>
    protected override void OnFixedUpdate()
    {
        if (!m_isAnimationStarted ||
            m_parameters == null ||
            m_hasMovementFailed)
        {
            return;
        }

        // Animation中に進行不能になっても、
        // State自体はAnimation終了までRUNNINGを維持する
        if (!CanMoveForward())
        {
            m_hasMovementFailed = true;

            Owner.Motor.StopHorizontalMovement();

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
                m_parameters.MoveEasingCurve.Evaluate(
                    normalizedTime));

        Owner.Motor.MoveForwardWithSpeedRate(
            speedRate);
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

        // 外部要因によってAnimation終了前に
        // Stateを抜けた場合のみFAILEDにする
        if (Owner.GetStateExecutionStatus() ==
            StateExecutionStatus.RUNNING)
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);
        }

        m_references = null;
        m_parameters = null;
        m_animationEventReceiver = null;

        ResetRuntimeState();
    }

    /// <summary>
    /// Runtime状態を初期化します。
    /// </summary>
    private void ResetRuntimeState()
    {
        m_animationElapsedTime = 0.0f;
        m_animationDuration = 0.0f;
        m_isAnimationStarted = false;
        m_hasMovementFailed = false;
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
        if (m_parameters.WalkAnimationClip == null)
        {
            return false;
        }

        m_animationDuration =
            m_parameters.WalkAnimationClip.length;

        return m_animationDuration >
               Mathf.Epsilon;
    }

    /// <summary>
    /// 現在フェーズの歩行パラメータを取得します。
    /// </summary>
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
            m_references.DecisionParameterAsset
                .Walk
                .CoolTime);
    }

    /// <summary>
    /// ボスの前方へ直進できるか確認します。
    /// </summary>
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
    /// 歩行攻撃の設定を適用します。
    /// </summary>
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

        if (string.IsNullOrEmpty(
                animationTriggerName) ||
            m_attackIdentifier == null ||
            Owner.AnimationController == null)
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

        m_animationEventReceiver.AttackEventReceived +=
            HandleAttackEvent;

        Owner.AnimationController.SetTrigger(
            m_animationTriggerID);

        return true;
    }

    /// <summary>
    /// 攻撃AnimationEventを処理します。
    /// </summary>
    private void HandleAttackEvent(
        AttackEventData attackEventData)
    {
        if (attackEventData == null)
        {
            return;
        }

        switch (attackEventData.AttackEventType)
        {
            case AttackEventType.ANIMATION_START:
                StartMovement();
                break;

            case AttackEventType.HITBOX_ENABLE:
                Owner.EnableAttackHitboxes(
                    m_attackIdentifier);
                break;

            case AttackEventType.HITBOX_DISABLE:
                Owner.AttackHitboxRegistry?.DisableHitboxes(
                    m_attackIdentifier);
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
        m_animationElapsedTime = 0.0f;
        m_isAnimationStarted = true;

        Owner.Motor?.StopHorizontalMovement();
    }

    /// <summary>
    /// Animation終了に合わせて歩行状態を終了します。
    /// </summary>
    private void FinishWalk()
    {
        if (!m_isAnimationStarted)
        {
            return;
        }

        m_isAnimationStarted = false;

        Owner.Motor?.StopHorizontalMovement();

        // Animation終了時点で初めて結果を確定する
        StateExecutionStatus result =
            m_hasMovementFailed
                ? StateExecutionStatus.FAILED
                : StateExecutionStatus.SUCCEEDED;

        Owner.SetStateExecutionStatus(
            result);
    }

    /// <summary>
    /// Animation開始前の致命的な設定不備として終了します。
    /// </summary>
    private void SetImmediateFailed()
    {
        Owner.Motor?.StopHorizontalMovement();

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.FAILED);
    }
}