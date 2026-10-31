using UnityEngine;

/// <summary>
/// ステージ1フェーズ1の方向転換を実行します。
/// </summary>
public sealed class S1P1BossTurnState :
    StateBase<BossController>
{
    // 1回の方向転換で回転する角度
    private const float TURN_ANGLE = 45.0f;

    // Animator Trigger名
    private const string TURN_RIGHT_PARAMETER_NAME =
        "Turn_Right";

    private const string TURN_LEFT_PARAMETER_NAME =
        "Turn_Left";

    // Animator Trigger ID
    private static readonly int TURN_RIGHT_PARAMETER_ID =
        Animator.StringToHash(
            TURN_RIGHT_PARAMETER_NAME);

    private static readonly int TURN_LEFT_PARAMETER_ID =
        Animator.StringToHash(
            TURN_LEFT_PARAMETER_NAME);

    /// <summary>
    /// 方向転換の左右を表します。
    /// </summary>
    private enum TurnDirection
    {
        RIGHT,
        LEFT
    }

    // 開始回転
    private Quaternion m_startRotation;

    // 目標回転
    private Quaternion m_targetRotation;

    // 回転方向
    private TurnDirection m_turnDirection;

    // Animation経過時間
    private float m_animationElapsedTime;

    // Animation時間
    private float m_animationDuration;

    // Animationが開始されたか
    private bool m_isAnimationStarted;

    // S1P1固有参照
    private S1P1BossReferences m_references;

    // フェーズ共通参照
    private BossPhaseReferences m_commonReferences;

    // 方向転換パラメータ
    private S1P1BossTurnStateParameters m_parameters;

    // AnimationEvent受信
    private AnimationEventReceiver m_animationEventReceiver;

    /// <summary>
    /// 方向転換を開始します。
    /// </summary>
    protected override void OnStartState()
    {
        ResetRuntimeState();

        if (!TryGetReferences() ||
            Owner.Motor == null ||
            Owner.AnimationController == null)
        {
            SetImmediateFailed();
            return;
        }

        Vector3 targetDirection =
            GetTurnDirection();

        if (targetDirection.sqrMagnitude <=
            Mathf.Epsilon)
        {
            SetImmediateFailed();
            return;
        }

        m_startRotation =
            Owner.transform.rotation;

        if (!TryCalculateTargetRotation(
                targetDirection))
        {
            SetImmediateFailed();
            return;
        }

        if (!PrepareAnimation())
        {
            SetImmediateFailed();
            return;
        }

        Owner.Motor.StopHorizontalMovement();

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.RUNNING);

        PlayTurnAnimation();
    }

    /// <summary>
    /// Animationに同期して方向転換を更新します。
    /// </summary>
    protected override void OnFixedUpdate()
    {
        if (!m_isAnimationStarted ||
            m_parameters == null ||
            Owner.Motor == null ||
            m_animationDuration <= Mathf.Epsilon)
        {
            return;
        }

        m_animationElapsedTime +=
            Time.fixedDeltaTime;

        float normalizedTime =
            Mathf.Clamp01(
                m_animationElapsedTime /
                m_animationDuration);

        float easedTime =
            Mathf.Clamp01(
                m_parameters
                    .RotationEasingCurve
                    .Evaluate(
                        normalizedTime));

        Quaternion rotation =
            Quaternion.Slerp(
                m_startRotation,
                m_targetRotation,
                easedTime);

        Owner.Motor.MoveRotation(
            rotation);

        // 補間が完了してもStateは終了しない。
        // ANIMATION_ENDまでRUNNINGを維持する。
    }

    /// <summary>
    /// 方向転換を終了します。
    /// </summary>
    protected override void OnExitState()
    {
        StartCoolTimeIfSucceeded();

        Owner.Motor?.StopHorizontalMovement();

        if (m_animationEventReceiver != null)
        {
            m_animationEventReceiver.AttackEventReceived -=
                HandleAnimationEvent;
        }

        // 強制的にStateを抜けた場合は失敗として扱う
        if (Owner.GetStateExecutionStatus() ==
            StateExecutionStatus.RUNNING)
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);
        }

        m_references = null;
        m_commonReferences = null;
        m_parameters = null;
        m_animationEventReceiver = null;

        ResetRuntimeState();
    }

    /// <summary>
    /// 使用する方向転換Animationを準備します。
    /// </summary>
    /// <returns>
    /// true：準備できました。
    /// false：必要な設定がありません。
    /// </returns>
    private bool PrepareAnimation()
    {
        AnimationClip animationClip =
            m_turnDirection ==
            TurnDirection.RIGHT
                ? m_parameters.RightTurnAnimationClip
                : m_parameters.LeftTurnAnimationClip;

        if (animationClip == null ||
            animationClip.length <=
            Mathf.Epsilon)
        {
            return false;
        }

        m_animationDuration =
            animationClip.length;

        m_animationEventReceiver =
            Owner.AnimationController
                .CurrentAnimationEventReceiver;

        if (m_animationEventReceiver == null)
        {
            return false;
        }

        m_animationEventReceiver.AttackEventReceived +=
            HandleAnimationEvent;

        return true;
    }

    /// <summary>
    /// 左右に対応した方向転換Animationを再生します。
    /// </summary>
    private void PlayTurnAnimation()
    {
        int triggerID =
            m_turnDirection ==
            TurnDirection.RIGHT
                ? TURN_RIGHT_PARAMETER_ID
                : TURN_LEFT_PARAMETER_ID;

        Owner.AnimationController.SetTrigger(
            triggerID);
    }

    /// <summary>
    /// AnimationEventを処理します。
    /// </summary>
    /// <param name="attackEventData">
    /// AnimationEvent情報。
    /// </param>
    private void HandleAnimationEvent(
        AttackEventData attackEventData)
    {
        if (attackEventData == null)
        {
            return;
        }

        switch (attackEventData.AttackEventType)
        {
            case AttackEventType.ANIMATION_START:
                StartTurning();
                break;

            case AttackEventType.ANIMATION_END:
                FinishTurning();
                break;
        }
    }

    /// <summary>
    /// Animationに同期した方向転換を開始します。
    /// </summary>
    private void StartTurning()
    {
        m_animationElapsedTime = 0.0f;
        m_isAnimationStarted = true;
    }

    /// <summary>
    /// Animation終了時に方向転換を完了します。
    /// </summary>
    private void FinishTurning()
    {
        if (!m_isAnimationStarted)
        {
            return;
        }

        m_isAnimationStarted = false;

        // FixedUpdateとAnimation終了タイミングの誤差を補正し、
        // 最終的に必ず45度回転した状態にする
        Owner.Motor?.MoveRotation(
            m_targetRotation);

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.SUCCEEDED);

        // 方向転換後は歩行へ移行
        Machine.ChangeState<S1P1BossWalkState>();
    }

    /// <summary>
    /// 必要な参照とパラメータを取得します。
    /// </summary>
    /// <returns>
    /// true：取得できました。
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
                    out m_references) ||
            !Owner.PhaseController
                .TryGetCurrentPhaseComponent(
                    out m_commonReferences))
        {
            return false;
        }

        if (m_references.StateParameterAsset == null)
        {
            return false;
        }

        m_parameters =
            m_references.StateParameterAsset.Turn;

        return m_parameters != null &&
               m_parameters.HasRequiredParameters() &&
               m_commonReferences.Origin != null;
    }

    /// <summary>
    /// 正常終了した方向転換のクールタイムを開始します。
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

        coolTimeManager.StartCoolTime<S1P1BossTurnState>(
            m_references.DecisionParameterAsset
                .Turn
                .CoolTime);
    }

    /// <summary>
    /// 方向転換する対象方向を取得します。
    /// </summary>
    /// <returns>
    /// 方向転換の基準となる方向。
    /// </returns>
    private Vector3 GetTurnDirection()
    {
        BossNavMeshFootprint bossNavMeshFootprint =
            m_commonReferences.NavMeshFootprint;

        Transform playerTransform =
            m_commonReferences.PlayerTransform;

        Transform originTransform =
            m_commonReferences.Origin;

        if (originTransform == null)
        {
            return Vector3.zero;
        }

        // NavMeshからはみ出している場合は
        // NavMesh内側への方向を優先する
        if (bossNavMeshFootprint != null &&
            Owner.Navigation != null &&
            !Owner.Navigation.IsFootprintInsideNavMesh(
                bossNavMeshFootprint))
        {
            if (Owner.Navigation.TryGetNavMeshInsideDirection(
                    bossNavMeshFootprint,
                    out Vector3 insideDirection))
            {
                return insideDirection;
            }
        }

        // 通常時はPlayer方向を使用する
        if (playerTransform == null)
        {
            return Vector3.zero;
        }

        Vector3 playerDirection =
            playerTransform.position -
            originTransform.position;

        playerDirection.y = 0.0f;

        if (playerDirection.sqrMagnitude <=
            Mathf.Epsilon)
        {
            return Vector3.zero;
        }

        return playerDirection.normalized;
    }

    /// <summary>
    /// 対象がいる左右を判定し、
    /// 固定45度の目標回転を計算します。
    /// </summary>
    /// <param name="targetDirection">
    /// 回転方向を決定する対象方向。
    /// </param>
    /// <returns>
    /// true：目標回転を計算できました。
    /// false：方向を決定できませんでした。
    /// </returns>
    private bool TryCalculateTargetRotation(
        Vector3 targetDirection)
    {
        Vector3 currentForward =
            m_commonReferences.Origin.forward;

        currentForward.y = 0.0f;
        targetDirection.y = 0.0f;

        if (currentForward.sqrMagnitude <=
                Mathf.Epsilon ||
            targetDirection.sqrMagnitude <=
                Mathf.Epsilon)
        {
            return false;
        }

        currentForward.Normalize();
        targetDirection.Normalize();

        float signedAngle =
            Vector3.SignedAngle(
                currentForward,
                targetDirection,
                Vector3.up);

        if (Mathf.Abs(signedAngle) <=
            Mathf.Epsilon)
        {
            return false;
        }

        m_turnDirection =
            signedAngle > 0.0f
                ? TurnDirection.RIGHT
                : TurnDirection.LEFT;

        float turnAngle =
            m_turnDirection ==
            TurnDirection.RIGHT
                ? TURN_ANGLE
                : -TURN_ANGLE;

        m_targetRotation =
            Quaternion.AngleAxis(
                turnAngle,
                Vector3.up) *
            m_startRotation;

        return true;
    }

    /// <summary>
    /// Runtime状態を初期化します。
    /// </summary>
    private void ResetRuntimeState()
    {
        m_startRotation =
            Quaternion.identity;

        m_targetRotation =
            Quaternion.identity;

        m_animationElapsedTime =
            0.0f;

        m_animationDuration =
            0.0f;

        m_isAnimationStarted =
            false;
    }

    /// <summary>
    /// Animation開始前の設定不備として失敗させます。
    /// </summary>
    private void SetImmediateFailed()
    {
        Owner.Motor?.StopHorizontalMovement();

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.FAILED);
    }
}