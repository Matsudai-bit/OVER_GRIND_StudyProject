using UnityEngine;

/// <summary>
/// ステージ1フェーズ2の移動しながら行う
/// ミサイル攻撃を実行します。
/// </summary>
public sealed class S1P2BossMoveMissileState :
    StateBase<BossController>
{
    // S1P2固有参照
    private S1P2BossReferences m_references;

    // 共通フェーズ参照
    private BossPhaseReferences m_commonReferences;

    // 移動参照
    private S1P2MoveStateReferences m_moveReferences;

    // ミサイル参照
    private S1BossMissileReferences m_missileReferences;

    // ミサイルパラメータ
    private S1P2BossMissileStateParameters m_missileParameters;

    // 移動パラメータ
    private S1P2BossMoveStateParameters m_moveParameters;

    // 移動制御
    private S1P2BossMoveController m_moveController;

    // ミサイル制御
    private S1P2BossMissileExecutor m_missileExecutor;

    // 専用Animation Trigger ID
    private int m_animationTriggerID;

    // AnimationEvent受信
    private AnimationEventReceiver m_animationEventReceiver;

    // 移動完了
    private bool m_isMoveCompleted;

    // ミサイル発射開始済み
    private bool m_hasStartedMissileLaunch;

    // ミサイル発射完了
    private bool m_isMissileCompleted;

    // Animation完了
    private bool m_isAnimationCompleted;

    /// <summary>
    /// 移動ミサイル攻撃を開始します。
    /// </summary>
    protected override void OnStartState()
    {
        ResetRuntimeState();

        if (!TryGetReferences())
        {
            SetFailed();

            return;
        }

        if (!m_moveReferences.RoutePlanner.TryCreateRoute(
                m_moveParameters,
                out BossMoveRoute route))
        {
            Debug.LogWarning(
                $"[{nameof(S1P2BossMoveMissileState)}] " +
                "移動経路を生成できませんでした。");

            SetFailed();

            return;
        }

        /*
         * Triggerを発火する前に
         * AnimationEventを購読します。
         */
        if (!TryGetAnimationSetting())
        {
            Debug.LogError(
                $"[{nameof(S1P2BossMoveMissileState)}] " +
                "Animation設定を取得できませんでした。");

            SetFailed();

            return;
        }

        m_moveController =
            m_moveReferences.MoveController;

        if (!m_moveController.StartMove(
                route,
                m_moveParameters))
        {
            Debug.LogError(
                $"[{nameof(S1P2BossMoveMissileState)}] " +
                "移動を開始できませんでした。");

            SetFailed();

            return;
        }

        /*
         * ミサイルはここでは準備のみ行います。
         * 実際の発射開始はAnimationEventを待ちます。
         */
        if (!m_missileExecutor.PrepareAttack(
                m_missileReferences,
                m_missileParameters,
                m_commonReferences.PlayerTransform))
        {
            Debug.LogError(
                $"[{nameof(S1P2BossMoveMissileState)}] " +
                "ミサイル攻撃を準備できませんでした。");

            SetFailed();

            return;
        }

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.RUNNING);

        Owner.AnimationController.SetTrigger(
            m_animationTriggerID);
    }

    /// <summary>
    /// ミサイル連続発射を更新します。
    /// </summary>
    /// <param name="deltaTime">
    /// 前フレームからの経過時間。
    /// </param>
    protected override void OnUpdate(
        float deltaTime)
    {
        if (Owner == null ||
            Owner.GetStateExecutionStatus() !=
                StateExecutionStatus.RUNNING)
        {
            return;
        }

        /*
         * AnimationEventで発射が許可されるまでは
         * Executorを更新しません。
         */
        if (!m_hasStartedMissileLaunch ||
            m_isMissileCompleted)
        {
            return;
        }

        if (m_missileExecutor == null)
        {
            SetFailed();

            return;
        }

        m_missileExecutor.UpdateAttack(
            deltaTime);

        if (m_missileExecutor.HasFailed)
        {
            SetFailed();

            return;
        }

        if (!m_missileExecutor.IsCompleted)
        {
            return;
        }

        m_isMissileCompleted =
            true;

        TryCompleteState();
    }

    /// <summary>
    /// 経路移動を更新します。
    /// </summary>
    protected override void OnFixedUpdate()
    {
        if (Owner == null ||
            Owner.GetStateExecutionStatus() !=
                StateExecutionStatus.RUNNING)
        {
            return;
        }

        if (m_isMoveCompleted)
        {
            return;
        }

        if (m_moveController == null)
        {
            SetFailed();

            return;
        }

        m_moveController.FixedUpdateMove(
            Time.fixedDeltaTime);

        if (m_moveController.HasFailed)
        {
            SetFailed();

            return;
        }

        if (!m_moveController.IsCompleted)
        {
            return;
        }

        /*
         * 移動のみ終了させます。
         * ミサイル発射やAnimationは継続します。
         */
        m_isMoveCompleted =
            true;

        Owner.Motor?
            .StopHorizontalMovement();

        TryCompleteState();
    }

    /// <summary>
    /// 移動ミサイル攻撃を終了します。
    /// </summary>
    protected override void OnExitState()
    {
        UnregisterAnimationEvent();

        m_moveController?.Cancel();

        m_missileExecutor?.Cancel();

        Owner?.Motor?
            .StopHorizontalMovement();

        if (Owner != null &&
            Owner.GetStateExecutionStatus() ==
                StateExecutionStatus.RUNNING)
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);
        }

        m_references =
            null;

        m_commonReferences =
            null;

        m_moveReferences =
            null;

        m_missileReferences =
            null;

        m_missileParameters =
            null;

        m_moveParameters =
            null;

        m_moveController =
            null;

        m_missileExecutor =
            null;

        m_animationEventReceiver =
            null;
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
                StartMissileLaunch();
                break;

            case AttackEventType.HITBOX_DISABLE:
                break;

            case AttackEventType.ANIMATION_END:
                HandleAnimationEnd();
                break;
        }
    }

    /// <summary>
    /// AnimationEventを起点に
    /// 全ミサイルの連続発射を開始します。
    /// </summary>
    private void StartMissileLaunch()
    {
        /*
         * HITBOX_ENABLEが複数回発生しても
         * 発射処理は1回しか開始しません。
         */
        if (m_hasStartedMissileLaunch)
        {
            return;
        }

        if (m_missileExecutor == null)
        {
            SetFailed();

            return;
        }

        if (!m_missileExecutor.BeginLaunch())
        {
            SetFailed();

            return;
        }

        m_hasStartedMissileLaunch =
            true;

        /*
         * LaunchSiteが1つだけの場合など、
         * BeginLaunch直後に完了する場合にも対応します。
         */
        if (!m_missileExecutor.IsCompleted)
        {
            return;
        }

        m_isMissileCompleted =
            true;

        TryCompleteState();
    }

    /// <summary>
    /// 攻撃Animation終了を処理します。
    /// </summary>
    private void HandleAnimationEnd()
    {
        m_isAnimationCompleted =
            true;

        TryCompleteState();
    }

    /// <summary>
    /// 移動・ミサイル・Animationが
    /// すべて終了した場合にStateを完了します。
    /// </summary>
    private void TryCompleteState()
    {
        if (!m_isMoveCompleted ||
            !m_isMissileCompleted ||
            !m_isAnimationCompleted)
        {
            return;
        }

        Owner.Motor?
            .StopHorizontalMovement();

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.SUCCEEDED);
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
        if (Owner == null ||
            Owner.PhaseController == null ||
            Owner.Motor == null)
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

        if (m_references.StateParameterAsset == null)
        {
            return false;
        }

        /*
         * 移動
         */
        m_moveReferences =
            m_references.MoveStateReferences;

        if (!m_references.StateParameterAsset
                .TryGetMoveMissileParameters(
                    out S1P2BossMoveMissileStateParameters
                        moveMissileParameters))
        {
            return false;
        }

        m_moveParameters =
            moveMissileParameters.Move;

        if (m_moveReferences == null ||
            m_moveParameters == null ||
            m_moveReferences.RoutePlanner == null ||
            m_moveReferences.MoveController == null)
        {
            return false;
        }

        /*
         * ミサイル
         */
        m_missileReferences =
            m_references.MissileReferences;

        m_missileParameters =
            moveMissileParameters.Missile;

        if (m_missileReferences == null ||
            !m_missileReferences.HasRequiredReferences() ||
            m_missileParameters == null ||
            !m_missileParameters.HasRequiredParameters())
        {
            return false;
        }

        if (m_commonReferences.PlayerTransform == null)
        {
            return false;
        }

        m_missileExecutor =
            m_references.MissileExecutor;

        return m_missileExecutor != null;
    }

    /// <summary>
    /// 移動ミサイル専用Animation設定を取得します。
    /// </summary>
    /// <returns>
    /// true：取得できました。
    /// false：取得できませんでした。
    /// </returns>
    private bool TryGetAnimationSetting()
    {
        if (Owner.AnimationController == null)
        {
            return false;
        }

        S1P2BossAttackSettings attackSettings =
            Owner.GetComponentInChildren<
                S1P2BossAttackSettings>(
                    true);

        if (attackSettings == null)
        {
            return false;
        }

        if (!attackSettings.TryGetAttackSetting(
                S1P2BossAttackType.MOVE_MISSILE,
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

        /*
         * Triggerより先にAnimationEventを購読します。
         */
        m_animationEventReceiver.AttackEventReceived +=
            HandleAttackEvent;

        return true;
    }

    /// <summary>
    /// AnimationEventの購読を解除します。
    /// </summary>
    private void UnregisterAnimationEvent()
    {
        if (m_animationEventReceiver == null)
        {
            return;
        }

        m_animationEventReceiver.AttackEventReceived -=
            HandleAttackEvent;
    }

    /// <summary>
    /// Runtime状態を初期化します。
    /// </summary>
    private void ResetRuntimeState()
    {
        m_isMoveCompleted =
            false;

        m_hasStartedMissileLaunch =
            false;

        m_isMissileCompleted =
            false;

        m_isAnimationCompleted =
            false;
    }

    /// <summary>
    /// Stateを失敗終了します。
    /// </summary>
    private void SetFailed()
    {
        m_moveController?.Cancel();

        m_missileExecutor?.Cancel();

        Owner?.Motor?
            .StopHorizontalMovement();

        if (Owner == null)
        {
            return;
        }

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.FAILED);
    }
}