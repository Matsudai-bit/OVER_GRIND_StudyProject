using UnityEngine;

/// <summary>
/// ステージ1フェーズ2の経路移動を実行します。
/// 移動中は接触攻撃判定を有効化し、
/// 条件成立時には尻尾叩きつけ攻撃を並行して実行します。
/// </summary>
public sealed class S1P2BossMoveState :
    StateBase<BossController>
{
    // S1P2固有参照
    private S1P2BossReferences m_references;

    // 移動状態参照
    private S1P2MoveStateReferences m_moveReferences;

    // 移動制御
    private S1P2BossMoveController m_moveController;

    // 移動パラメータ
    private S1P2BossMoveStateParameters m_parameter;

    // 尻尾攻撃実行制御
    private S1P2BossTailSlamExecutor m_tailSlamExecutor;

    // 移動中の攻撃ID
    private AttackIdentifier m_moveAttackIdentifier;

    // 移動完了
    private bool m_isMoveCompleted;

    // 今回の移動で尻尾攻撃を行うか
    private bool m_shouldExecuteTailSlam;

    // 尻尾攻撃完了
    private bool m_isTailSlamCompleted;

    // 移動攻撃判定が有効か
    private bool m_isMoveAttackEnabled;

    /// <summary>
    /// 経路移動を開始します。
    /// </summary>
    protected override void OnStartState()
    {
        ResetRuntimeState();

        if (!TryGetReferences())
        {
            SetFailed();

            return;
        }

        if (!ApplyMoveAttackSetting())
        {
            Debug.LogError(
                $"[{nameof(S1P2BossMoveState)}] " +
                "移動中の攻撃設定を取得できませんでした。");

            SetFailed();

            return;
        }

        if (!m_moveReferences.RoutePlanner.TryCreateRoute(
                m_parameter,
                out BossMoveRoute route))
        {
            Debug.LogWarning(
                $"[{nameof(S1P2BossMoveState)}] " +
                "移動経路を生成できませんでした。");

            SetFailed();

            return;
        }

        m_moveController =
            m_moveReferences.MoveController;

        if (!m_moveController.StartMove(
                route,
                m_parameter))
        {
            Debug.LogError(
                $"[{nameof(S1P2BossMoveState)}] " +
                "経路移動を開始できませんでした。");

            SetFailed();

            return;
        }

        /*
         * MoveState開始時に一度だけ
         * 尻尾攻撃を実行するか判定します。
         */
        m_shouldExecuteTailSlam =
            ShouldExecuteTailSlam();

        m_isTailSlamCompleted =
            !m_shouldExecuteTailSlam;

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.RUNNING);

        /*
         * 移動中は接触攻撃判定を有効にします。
         */
        EnableMoveAttack();

        if (m_shouldExecuteTailSlam)
        {
            StartTailSlam();
        }
    }

    /// <summary>
    /// 移動中の追加攻撃を更新します。
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

        if (!m_shouldExecuteTailSlam ||
            m_isTailSlamCompleted)
        {
            return;
        }

        if (m_tailSlamExecutor == null)
        {
            SetFailed();

            return;
        }

        if (m_tailSlamExecutor.HasFailed)
        {
            SetFailed();

            return;
        }

        if (!m_tailSlamExecutor.IsCompleted)
        {
            return;
        }

        m_isTailSlamCompleted =
            true;

        StartTailSlamCoolTime();

        TryCompleteState();
    }

    /// <summary>
    /// 経路移動を物理更新します。
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

        UpdateMoveAttackState();

        if (m_moveController.HasFailed)
        {
            SetFailed();

            return;
        }

        if (!m_moveController.IsCompleted)
        {
            return;
        }

        m_isMoveCompleted =
            true;

        Owner.Motor?
            .StopHorizontalMovement();

        /*
         * 尻尾攻撃の完了監視は
         * OnUpdate側で行います。
         */
        TryCompleteState();
    }

    /// <summary>
    /// 移動状態を終了します。
    /// </summary>
    protected override void OnExitState()
    {
        /*
         * State終了時には必ず
         * 移動中の攻撃判定を無効化します。
         */
        DisableMoveAttack();

        m_moveController?.Cancel();

        m_tailSlamExecutor?.Cancel();

        Owner?.Motor?
            .StopHorizontalMovement();

        if (Owner != null &&
            Owner.GetStateExecutionStatus() ==
                StateExecutionStatus.RUNNING)
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);
        }

        m_moveAttackIdentifier =
            null;

        m_tailSlamExecutor =
            null;

        m_moveController =
            null;

        m_moveReferences =
            null;

        m_parameter =
            null;

        m_references =
            null;
    }

    /// <summary>
    /// 今回の移動中に尻尾叩きつけ攻撃を
    /// 実行するか判定します。
    /// </summary>
    /// <returns>
    /// true：実行します。
    /// false：実行しません。
    /// </returns>
    private bool ShouldExecuteTailSlam()
    {
        if (m_parameter == null ||
            !m_parameter.EnableTailSlam ||
            m_references == null ||
            m_references.DecisionParameterAsset == null)
        {
            return false;
        }

        return S1P2TailSlamDecisionEvaluator
            .ShouldExecute(
                Owner,
                m_references.DecisionParameterAsset);
    }

    /// <summary>
    /// 尻尾叩きつけ攻撃を開始します。
    /// </summary>
    private void StartTailSlam()
    {
        m_tailSlamExecutor =
            new S1P2BossTailSlamExecutor(
                Owner);

        if (m_tailSlamExecutor.Start(
                m_references))
        {
            return;
        }

        Debug.LogError(
            $"[{nameof(S1P2BossMoveState)}] " +
            "移動中の尻尾叩きつけ攻撃を開始できませんでした。");

        SetFailed();
    }

    /// <summary>
    /// 移動中に実行した尻尾攻撃の
    /// クールタイムを開始します。
    /// </summary>
    private void StartTailSlamCoolTime()
    {
        if (Owner == null ||
            m_references?.DecisionParameterAsset == null)
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

        /*
         * 単独TailSlamStateと同じキーを使用することで、
         * Move中の尻尾攻撃とクールタイムを共有します。
         */
        coolTimeManager
            .StartCoolTime<S1P2BossTailSlamState>(
                m_references
                    .DecisionParameterAsset
                    .TailSlam
                    .CoolTime);
    }

    /// <summary>
    /// 移動中の攻撃設定を取得します。
    /// </summary>
    /// <returns>
    /// true：取得できました。
    /// false：取得できませんでした。
    /// </returns>
    private bool ApplyMoveAttackSetting()
    {
        if (Owner == null)
        {
            return false;
        }

        S1P2BossAttackSettings attackSettings =
            Owner.GetComponentInChildren<
                S1P2BossAttackSettings>(
                    true);

        if (attackSettings == null)
        {
            Debug.LogError(
                $"[{nameof(S1P2BossMoveState)}] " +
                $"{nameof(S1P2BossAttackSettings)}が" +
                "見つかりません。");

            return false;
        }

        if (!attackSettings.TryGetAttackSetting(
                S1P2BossAttackType.MOVE,
                out m_moveAttackIdentifier,
                out _))
        {
            Debug.LogError(
                $"[{nameof(S1P2BossMoveState)}] " +
                $"{S1P2BossAttackType.MOVE}の" +
                "攻撃設定がありません。");

            return false;
        }

        if (m_moveAttackIdentifier == null)
        {
            Debug.LogError(
                $"[{nameof(S1P2BossMoveState)}] " +
                "移動用AttackIdentifierが設定されていません。");

            return false;
        }

        return true;
    }

    /// <summary>
    /// 移動中の攻撃判定を有効化します。
    /// </summary>
    private void EnableMoveAttack()
    {
        if (Owner == null ||
            m_moveAttackIdentifier == null ||
            m_isMoveAttackEnabled)
        {
            return;
        }

        Owner.EnableAttackHitboxes(
            m_moveAttackIdentifier);

        m_isMoveAttackEnabled =
            true;
    }

    /// <summary>
    /// 移動中の攻撃判定を無効化します。
    /// </summary>
    private void DisableMoveAttack()
    {
        if (Owner == null ||
            m_moveAttackIdentifier == null ||
            !m_isMoveAttackEnabled)
        {
            return;
        }

        Owner.AttackHitboxRegistry?
            .DisableHitboxes(
                m_moveAttackIdentifier);

        m_isMoveAttackEnabled =
            false;
    }

    /// <summary>
    /// 移動と追加攻撃が完了した場合に
    /// Stateを正常終了します。
    /// </summary>
    private void TryCompleteState()
    {
        if (!m_isMoveCompleted ||
            !m_isTailSlamCompleted)
        {
            return;
        }

        /*
         * 正常終了が確定した時点で
         * 移動中の攻撃判定を停止します。
         */
        DisableMoveAttack();

        Owner.Motor?
            .StopHorizontalMovement();

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.SUCCEEDED);
    }

    /// <summary>
    /// 移動状態に必要な参照を取得します。
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
            Debug.LogError(
                $"[{nameof(S1P2BossMoveState)}] " +
                $"{nameof(S1P2BossReferences)}を" +
                "取得できませんでした。");

            return false;
        }

        if (m_references == null ||
            m_references.StateParameterAsset == null)
        {
            return false;
        }

        m_moveReferences =
            m_references.MoveStateReferences;

        if (m_moveReferences == null)
        {
            Debug.LogError(
                $"[{nameof(S1P2BossMoveState)}] " +
                $"{nameof(S1P2MoveStateReferences)}が" +
                "設定されていません。");

            return false;
        }

        if (m_moveReferences.RoutePlanner == null)
        {
            Debug.LogError(
                $"[{nameof(S1P2BossMoveState)}] " +
                $"{nameof(S1P2BossMoveRoutePlanner)}が" +
                "設定されていません。");

            return false;
        }

        if (m_moveReferences.MoveController == null)
        {
            Debug.LogError(
                $"[{nameof(S1P2BossMoveState)}] " +
                $"{nameof(S1P2BossMoveController)}が" +
                "設定されていません。");

            return false;
        }

        m_parameter =
            m_references
                .StateParameterAsset
                .Move;

        if (m_parameter == null)
        {
            Debug.LogError(
                $"[{nameof(S1P2BossMoveState)}] " +
                $"{nameof(S1P2BossMoveStateParameters)}が" +
                "設定されていません。");

            return false;
        }

        /*
         * 尻尾攻撃を使用するときだけ
         * TailSlam関連参照を必須にします。
         */
        if (m_parameter.EnableTailSlam)
        {
            if (m_references.TailSlamReference == null)
            {
                Debug.LogError(
                    $"[{nameof(S1P2BossMoveState)}] " +
                    $"{nameof(S1P2BossTailSlamReference)}が" +
                    "設定されていません。");

                return false;
            }

            if (m_references.TailTransform == null)
            {
                Debug.LogError(
                    $"[{nameof(S1P2BossMoveState)}] " +
                    "TailTransformが設定されていません。");

                return false;
            }

            if (m_references.DecisionParameterAsset == null)
            {
                Debug.LogError(
                    $"[{nameof(S1P2BossMoveState)}] " +
                    $"{nameof(S1P2BossDecisionParameterAsset)}が" +
                    "設定されていません。");

                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Runtime状態を初期化します。
    /// </summary>
    private void ResetRuntimeState()
    {
        m_moveAttackIdentifier =
            null;

        m_tailSlamExecutor =
            null;

        m_isMoveCompleted =
            false;

        m_shouldExecuteTailSlam =
            false;

        m_isTailSlamCompleted =
            true;
        m_isMoveAttackEnabled =
             false;
    }

    /// <summary>
    /// Stateを失敗終了します。
    /// </summary>
    private void SetFailed()
    {
        DisableMoveAttack();

        m_moveController?.Cancel();

        m_tailSlamExecutor?.Cancel();

        Owner?.Motor?
            .StopHorizontalMovement();

        if (Owner == null)
        {
            return;
        }

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.FAILED);
    }
    /// <summary>
    /// 移動速度に応じて接触攻撃判定を更新します。
    /// </summary>
    private void UpdateMoveAttackState()
    {
        if (Owner?.Motor == null ||
            m_moveAttackIdentifier == null)
        {
            return;
        }

        float currentSpeed =
            Owner.Motor.HorizontalSpeed;

        if (currentSpeed >=
            m_parameter.AttackEnableSpeed)
        {
            EnableMoveAttack();

            return;
        }

        DisableMoveAttack();
    }


}