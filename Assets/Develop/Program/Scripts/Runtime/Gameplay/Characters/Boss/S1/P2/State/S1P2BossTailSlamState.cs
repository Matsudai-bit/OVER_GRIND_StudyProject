using UnityEngine;

/// <summary>
/// ステージ1フェーズ2の尻尾叩きつけ攻撃を
/// 単独のStateとして実行します。
/// </summary>
public sealed class S1P2BossTailSlamState :
    StateBase<BossController>
{
    // フェーズ2固有参照
    private S1P2BossReferences m_references;

    // 尻尾叩きつけ攻撃実行制御
    private S1P2BossTailSlamExecutor m_tailSlamExecutor;

    /// <summary>
    /// 尻尾叩きつけ攻撃を開始します。
    /// </summary>
    protected override void OnStartState()
    {
        if (!TryGetReferences())
        {
            SetFailed();

            return;
        }

        /*
         * 単独の尻尾攻撃Stateでは
         * 攻撃中のBoss移動を停止します。
         *
         * MoveStateからExecutorを使用する場合は、
         * MoveState側が移動を継続するため、
         * Executor自身は移動を停止しません。
         */
        Owner.Motor?
            .StopHorizontalMovement();

        m_tailSlamExecutor =
            new S1P2BossTailSlamExecutor(
                Owner);

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.RUNNING);

        if (m_tailSlamExecutor.Start(
                m_references))
        {
            return;
        }

        Debug.LogError(
            $"[{nameof(S1P2BossTailSlamState)}] " +
            "尻尾叩きつけ攻撃を開始できませんでした.");

        SetFailed();
    }

    /// <summary>
    /// 尻尾叩きつけ攻撃の完了状態を監視します。
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

        CompleteTailSlam();
    }

    /// <summary>
    /// 尻尾叩きつけ攻撃を終了します。
    /// </summary>
    protected override void OnExitState()
    {
        /*
         * 参照を破棄する前に
         * クールタイムを開始します。
         */
        StartCoolTimeIfSucceeded();

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

        m_tailSlamExecutor =
            null;

        m_references =
            null;
    }

    /// <summary>
    /// 正常終了した尻尾叩きつけ攻撃の
    /// クールタイムを開始します。
    /// </summary>
    private void StartCoolTimeIfSucceeded()
    {
        if (Owner == null ||
            Owner.GetStateExecutionStatus() !=
                StateExecutionStatus.SUCCEEDED ||
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

        coolTimeManager
            .StartCoolTime<S1P2BossTailSlamState>(
                m_references
                    .DecisionParameterAsset
                    .TailSlam
                    .CoolTime);
    }

    /// <summary>
    /// フェーズ2固有参照を取得します。
    /// </summary>
    /// <returns>
    /// true：必要な参照を取得できました。
    /// false：参照が不足しています。
    /// </returns>
    private bool TryGetReferences()
    {
        if (Owner == null ||
            Owner.PhaseController == null ||
            Owner.Motor == null ||
            Owner.AnimationController == null)
        {
            return false;
        }

        if (!Owner.PhaseController
                .TryGetCurrentPhaseComponent(
                    out m_references))
        {
            Debug.LogError(
                $"[{nameof(S1P2BossTailSlamState)}] " +
                $"{nameof(S1P2BossReferences)}を" +
                "取得できませんでした.");

            return false;
        }

        if (m_references.TailSlamReference == null)
        {
            Debug.LogError(
                $"[{nameof(S1P2BossTailSlamState)}] " +
                $"{nameof(S1P2BossTailSlamReference)}が" +
                "設定されていません.");

            return false;
        }

        if (m_references
                .TailSlamReference
                .TailGroundCollisionSensor == null)
        {
            Debug.LogError(
                $"[{nameof(S1P2BossTailSlamState)}] " +
                "尻尾の接地判定Sensorが設定されていません.");

            return false;
        }

        return true;
    }

    /// <summary>
    /// 尻尾叩きつけ攻撃を正常終了します。
    /// </summary>
    private void CompleteTailSlam()
    {
        if (Owner == null ||
            Owner.GetStateExecutionStatus() !=
                StateExecutionStatus.RUNNING)
        {
            return;
        }

        Owner.Motor?
            .StopHorizontalMovement();

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.SUCCEEDED);
    }

    /// <summary>
    /// Stateを失敗終了します。
    /// </summary>
    private void SetFailed()
    {
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
}