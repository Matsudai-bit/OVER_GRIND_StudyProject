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

    private S1P2BossMoveStateParameters m_moveParameters;

    // 移動制御
    private S1P2BossMoveController m_moveController;

    // ミサイル制御
    private S1P2BossMissileExecutor m_missileExecutor;

    // 専用Animation Trigger ID
    private int m_animationTriggerID;

    // 移動完了
    private bool m_isMoveCompleted;

    // ミサイル完了
    private bool m_isMissileCompleted;

    /// <summary>
    /// 移動ミサイル攻撃を開始します。
    /// </summary>
    protected override void OnStartState()
    {
        m_isMoveCompleted = false;
        m_isMissileCompleted = false;

        if (!TryGetReferences())
        {
            SetFailed();

            return;
        }

        // 先に経路を生成する
        if (!m_moveReferences.RoutePlanner.TryCreateRoute(
                m_moveParameters,
                out BossMoveRoute route))
        {
            Debug.LogError(
                "移動ミサイル攻撃用の経路を生成できませんでした。");

            SetFailed();

            return;
        }

        // 専用Animationを取得する
        if (!TryGetAnimationSetting())
        {
            Debug.LogError(
                "移動ミサイル攻撃用Animationを取得できませんでした。");

            SetFailed();

            return;
        }

        m_moveController =
            m_moveReferences.MoveController;

        // 移動を準備・開始
        if (!m_moveController.StartMove(
                route,
                m_moveParameters))
        {
            Debug.LogError(
                "移動ミサイル攻撃の移動を開始できませんでした。");

            SetFailed();

            return;
        }

        // ミサイル攻撃を準備
        if (!m_missileExecutor.PrepareAttack(
                m_missileReferences,
                m_missileParameters,
                m_commonReferences.PlayerTransform))
        {
            Debug.LogError(
                "移動ミサイル攻撃のミサイルを準備できませんでした。");

            SetFailed();

            return;
        }

        /*
         * 専用Animationとミサイル発射を
         * 移動開始と同一State開始フレームで実行する。
         */
        Owner.AnimationController.SetTrigger(
            m_animationTriggerID);

        if (!m_missileExecutor.BeginLaunch())
        {
            Debug.LogError(
                "移動ミサイル攻撃の発射を開始できませんでした。");

            SetFailed();

            return;
        }

        m_isMissileCompleted =
            m_missileExecutor.IsCompleted;

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.RUNNING);

        TryCompleteState();
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
        if (Owner.GetStateExecutionStatus() !=
            StateExecutionStatus.RUNNING)
        {
            return;
        }

        if (m_isMissileCompleted)
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

        // ミサイルだけ終了した場合、移動は止めない
        m_isMissileCompleted = true;

        TryCompleteState();
    }

    /// <summary>
    /// 経路移動を更新します。
    /// </summary>
    protected override void OnFixedUpdate()
    {
        if (Owner.GetStateExecutionStatus() !=
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
         * 移動だけ先に終了した場合はBossを停止する。
         * ミサイル攻撃はOnUpdateでそのまま継続する。
         */
        m_isMoveCompleted = true;

        Owner.Motor?.StopHorizontalMovement();

        TryCompleteState();
    }

    /// <summary>
    /// 移動ミサイル攻撃を終了します。
    /// </summary>
    protected override void OnExitState()
    {
        m_moveController?.Cancel();

        m_missileExecutor?.Cancel();

        Owner.Motor?.StopHorizontalMovement();

        if (Owner.GetStateExecutionStatus() ==
            StateExecutionStatus.RUNNING)
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);
        }

        m_references = null;
        m_commonReferences = null;

        m_moveReferences = null;
        m_missileReferences = null;
        m_missileParameters = null;

        m_moveController = null;
        m_missileExecutor = null;
    }

    /// <summary>
    /// 移動とミサイルの両方が終了した場合に
    /// Stateを正常終了します。
    /// </summary>
    private void TryCompleteState()
    {
        if (!m_isMoveCompleted ||
            !m_isMissileCompleted)
        {
            return;
        }

        Owner.Motor?.StopHorizontalMovement();

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

        if (!Owner.PhaseController.TryGetCurrentPhaseComponent(
                out m_references))
        {
            return false;
        }

        if (!Owner.PhaseController.TryGetCurrentPhaseComponent(
                out m_commonReferences))
        {
            return false;
        }

        // 移動
        m_moveReferences =
            m_references.MoveStateReferences;

        if (!m_references.StateParameterAsset.TryGetMoveMissileParameters(out S1P2BossMoveMissileStateParameters  moveMissileStateParameters))
        {
            return false;
        }

        m_moveParameters = moveMissileStateParameters.Move;

        if (m_moveReferences == null ||
            m_moveParameters == null ||
            m_moveReferences.RoutePlanner == null ||
            m_moveReferences.MoveController == null)
        {
            return false;
        }

        // ミサイル
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

        m_missileParameters = moveMissileStateParameters.Missile;

        if (m_missileParameters == null ||
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

        if (m_missileExecutor == null)
        {
            return false;
        }

        return true;
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

        m_animationTriggerID =
            Animator.StringToHash(
                animationTriggerName);

        return true;
    }

    /// <summary>
    /// Stateを失敗終了します。
    /// </summary>
    private void SetFailed()
    {
        m_moveController?.Cancel();

        m_missileExecutor?.Cancel();

        Owner?.Motor?.StopHorizontalMovement();

        if (Owner == null)
        {
            return;
        }

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.FAILED);
    }
}