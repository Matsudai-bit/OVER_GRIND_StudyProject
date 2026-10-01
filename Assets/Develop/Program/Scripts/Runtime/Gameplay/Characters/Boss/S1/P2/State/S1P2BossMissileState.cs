using UnityEngine;

/// <summary>
/// ステージ1フェーズ2のミサイル攻撃を実行します。
/// </summary>
public sealed class S1P2BossMissileState :
    StateBase<BossController>
{
    // S1P2固有参照
    private S1P2BossReferences m_references;

    // ミサイル攻撃参照
    private S1BossMissileReferences m_missileReferences;

    // ミサイル状態パラメータ
    private S1P2BossMissileStateParameters m_parameters;

    // ミサイル攻撃対象
    private Transform m_playerTransform;

    // ミサイル実行制御
    private S1P2BossMissileExecutor m_missileExecutor;

    // 停止状態への変更を要求したか
    private bool m_isIdleRequested;

    // Animator Trigger ID
    private int m_animationTriggerID;

    // AnimationEvent受信
    private AnimationEventReceiver m_animationEventReceiver;

    /// <summary>
    /// ミサイル攻撃を開始します。
    /// </summary>
    protected override void OnStartState()
    {
        m_isIdleRequested = false;

        // 単体ミサイル攻撃ではBossを停止させる
        Owner.Motor?.StopHorizontalMovement();

        if (!TryGetReferencesAndParameters())
        {
            Debug.LogError(
                "ミサイル攻撃に必要な参照またはパラメータを取得できませんでした。");

            RequestFailedIdleState();

            return;
        }

        if (!m_missileExecutor.PrepareAttack(
                m_missileReferences,
                m_parameters,
                m_playerTransform))
        {
            Debug.LogError(
                "ミサイル攻撃を準備できませんでした。");

            RequestFailedIdleState();

            return;
        }

        if (!ApplyAttackSetting())
        {
            Debug.LogError(
                "ミサイル攻撃のAnimation設定を取得できませんでした。");

            RequestFailedIdleState();

            return;
        }

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.RUNNING);
    }

    /// <summary>
    /// ミサイル攻撃を更新します。
    /// </summary>
    /// <param name="deltaTime">
    /// 前フレームからの経過時間。
    /// </param>
    protected override void OnUpdate(
        float deltaTime)
    {
        if (Owner.GetStateExecutionStatus() !=
            StateExecutionStatus.RUNNING)
        {
            return;
        }

        if (m_isIdleRequested ||
            m_missileExecutor == null)
        {
            return;
        }

        m_missileExecutor.UpdateAttack(
            deltaTime);

        if (m_missileExecutor.HasFailed)
        {
            RequestFailedIdleState();

            return;
        }

        if (!m_missileExecutor.IsCompleted)
        {
            return;
        }

        CompleteMissileAttack();
    }

    /// <summary>
    /// ミサイル攻撃を終了します。
    /// </summary>
    protected override void OnExitState()
    {
        StartCoolTimeIfSucceeded();

        if (m_animationEventReceiver != null)
        {
            m_animationEventReceiver.AttackEventReceived -=
                HandleAttackEvent;
        }

        m_missileExecutor?.Cancel();

        Owner.Motor?.StopHorizontalMovement();

        if (Owner.GetStateExecutionStatus() ==
            StateExecutionStatus.RUNNING)
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);
        }

        m_references = null;
        m_missileReferences = null;
        m_parameters = null;
        m_playerTransform = null;
        m_missileExecutor = null;
        m_animationEventReceiver = null;
    }

    /// <summary>
    /// 正常終了したミサイル攻撃のクールタイムを開始します。
    /// </summary>
    private void StartCoolTimeIfSucceeded()
    {
        // 必要になった段階で実装
    }

    /// <summary>
    /// ミサイル攻撃を正常終了します。
    /// </summary>
    private void CompleteMissileAttack()
    {
        if (m_isIdleRequested)
        {
            return;
        }

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.SUCCEEDED);

        RequestIdleState();
    }

    /// <summary>
    /// 停止状態へ移行します。
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
            m_parameters?.IdleDuration ??
            0.0f;

        Machine.ChangeState<BossIdleState>(
            idleDuration);
    }

    /// <summary>
    /// 攻撃失敗として停止状態へ移行します。
    /// </summary>
    private void RequestFailedIdleState()
    {
        m_missileExecutor?.Cancel();

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.FAILED);

        RequestIdleState();
    }

    /// <summary>
    /// ミサイル攻撃に必要な参照とパラメータを取得します。
    /// </summary>
    /// <returns>
    /// true：取得できました。
    /// false：取得できませんでした。
    /// </returns>
    private bool TryGetReferencesAndParameters()
    {
        if (Owner == null ||
            Owner.PhaseController == null)
        {
            return false;
        }

        if (!Owner.PhaseController.TryGetCurrentPhaseComponent(
                out m_references))
        {
            return false;
        }

        if (!Owner.PhaseController.TryGetCurrentPhaseComponent(
                out BossPhaseReferences commonReferences))
        {
            return false;
        }

        m_missileReferences =
            m_references.MissileReferences;

        if (m_missileReferences == null ||
            !m_missileReferences.HasRequiredReferences())
        {
            return false;
        }

        if (m_references.StateParameterAsset == null ||
            !m_references.StateParameterAsset.HasRequiredParameters())
        {
            return false;
        }

        m_parameters =
            m_references.StateParameterAsset.Missile;

        if (m_parameters == null ||
            !m_parameters.HasRequiredParameters())
        {
            return false;
        }

        if (commonReferences.PlayerTransform == null)
        {
            return false;
        }

        m_playerTransform =
            commonReferences.PlayerTransform;

        m_missileExecutor =
            m_references.MissileExecutor;

        if (m_missileExecutor == null)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// ミサイル攻撃Animation設定を適用します。
    /// </summary>
    /// <returns>
    /// true：設定できました。
    /// false：設定できませんでした。
    /// </returns>
    private bool ApplyAttackSetting()
    {
        S1P2BossAttackSettings attackSettings =
            Owner.GetComponentInChildren<
                S1P2BossAttackSettings>(
                    true);

        if (attackSettings == null ||
            Owner.AnimationController == null)
        {
            return false;
        }

        if (!attackSettings.TryGetAttackSetting(
                S1P2BossAttackType.MISSILE,
                out _,
                out string animationTriggerName))
        {
            return false;
        }

        if (string.IsNullOrEmpty(
                animationTriggerName))
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

        // Triggerより先にイベントを購読する
        m_animationEventReceiver.AttackEventReceived +=
            HandleAttackEvent;

        Owner.AnimationController.SetTrigger(
            m_animationTriggerID);

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
        if (Owner.GetStateExecutionStatus() !=
            StateExecutionStatus.RUNNING)
        {
            return;
        }

        switch (attackEventData.AttackEventType)
        {
            case AttackEventType.HITBOX_ENABLE:
                StartMissileLaunch();
                break;

            case AttackEventType.HITBOX_DISABLE:
                break;

            case AttackEventType.ANIMATION_END:
                break;
        }
    }

    /// <summary>
    /// AnimationEventを起点にミサイル発射を開始します。
    /// </summary>
    private void StartMissileLaunch()
    {
        if (m_missileExecutor == null)
        {
            RequestFailedIdleState();

            return;
        }

        if (!m_missileExecutor.BeginLaunch())
        {
            RequestFailedIdleState();

            return;
        }

        /*
         * 1発×1Volleyなど、
         * BeginLaunch直後に終了する構成にも対応する。
         */
        if (m_missileExecutor.IsCompleted)
        {
            CompleteMissileAttack();
        }
    }
}