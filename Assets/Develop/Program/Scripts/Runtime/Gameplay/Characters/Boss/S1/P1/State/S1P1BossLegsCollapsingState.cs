using UnityEngine;

/// <summary>
/// ステージ1フェーズ1の脚崩壊とフェーズ移行を実行します。
/// </summary>
public sealed class S1P1BossLegsCollapsingState :
    StateBase<BossController>
{
    // Sequence完了通知が来ない場合の最低待機上限
    private const float MIN_SEQUENCE_TIMEOUT = 15.0f;

    // 脚管理
    private readonly S1P1BossLegsController m_legsController;

    // S1P1固有参照
    private S1P1BossReferences m_references;

    // フェーズ移行待機時間
    private float m_transitionDuration;

    // 経過時間
    private float m_elapsedTime;

    // Sequenceイベントを購読中か
    private bool m_isSequenceEventRegistered;

    /// <summary>
    /// 脚崩壊ステートを生成します。
    /// </summary>
    /// <param name="legsController">脚管理。</param>
    public S1P1BossLegsCollapsingState(
        S1P1BossLegsController legsController)
    {
        m_legsController =
            legsController;
    }

    /// <summary>
    /// 脚崩壊を開始します。
    /// </summary>
    protected override void OnStartState()
    {
        ResetRuntimeState();

        if (!TryGetReferences())
        {
            SetFailed();

            return;
        }

        /*
         * Raiseより先に購読します。
         * Sequence側が同期的に状態通知を発行しても取りこぼしません。
         */
        RegisterSequenceEvent();

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.RUNNING);

        m_references.SequenceRequestEvent.Raise(
            m_references.DestructionSequenceAsset);
    }

    /// <summary>
    /// Sequence完了待機を更新します。
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

        m_elapsedTime +=
            Mathf.Max(
                0.0f,
                deltaTime);

        /*
         * Sequence完了通知が欠落しても
         * Stateが永久にRUNNINGにならないようにします。
         */
        float timeoutDuration =
            Mathf.Max(
                MIN_SEQUENCE_TIMEOUT,
                m_transitionDuration);

        if (m_elapsedTime <
            timeoutDuration)
        {
            return;
        }

        Debug.LogWarning(
            $"[{nameof(S1P1BossLegsCollapsingState)}] " +
            "崩壊Sequenceの完了通知を待機中にタイムアウトしたため、" +
            "フェーズ移行を続行します。");

        CompleteTransition();
    }

    /// <summary>
    /// 脚崩壊状態を終了します。
    /// </summary>
    protected override void OnExitState()
    {
        UnregisterSequenceEvent();

        if (Owner != null &&
            Owner.GetStateExecutionStatus() ==
                StateExecutionStatus.RUNNING)
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);
        }

        m_references =
            null;
    }

    /// <summary>
    /// Sequence変更通知を処理します。
    /// </summary>
    /// <param name="eventData">Sequence変更情報。</param>
    private void HandleSequenceChanged(
        GameplaySequenceChangedEventData eventData)
    {
        if (Owner == null ||
            m_references == null ||
            Owner.GetStateExecutionStatus() !=
                StateExecutionStatus.RUNNING ||
            eventData.Sequence !=
                m_references.DestructionSequenceAsset)
        {
            return;
        }

        switch (eventData.EventType)
        {
            case GameplaySequenceEventType.FINISHED:
                CompleteTransition();
                break;

            case GameplaySequenceEventType.CANCELED:
                SetFailed();
                break;
        }
    }

    /// <summary>
    /// 次フェーズへ移行します。
    /// </summary>
    private void CompleteTransition()
    {
        if (Owner == null ||
            Owner.PhaseController == null ||
            Owner.GetStateExecutionStatus() !=
                StateExecutionStatus.RUNNING)
        {
            return;
        }

        /*
         * AdvancePhaseで現在Phaseが無効化される可能性があるため、
         * 先にイベント購読を解除します。
         */
        UnregisterSequenceEvent();

        if (!Owner.PhaseController.AdvancePhase())
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);

            return;
        }

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
        if (m_legsController == null ||
            Owner == null ||
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
            m_references.StateParameterAsset.LegsCollapsing == null ||
            m_references.SequenceRequestEvent == null ||
            m_references.SequenceChangedEvent == null ||
            m_references.DestructionSequenceAsset == null)
        {
            return false;
        }

        m_transitionDuration =
            m_references
                .StateParameterAsset
                .LegsCollapsing
                .TransitionDuration;

        return true;
    }

    /// <summary>
    /// Sequenceイベントを購読します。
    /// </summary>
    private void RegisterSequenceEvent()
    {
        if (m_isSequenceEventRegistered ||
            m_references?.SequenceChangedEvent == null)
        {
            return;
        }

        m_references.SequenceChangedEvent.RegisterListener(
            HandleSequenceChanged);

        m_isSequenceEventRegistered =
            true;
    }

    /// <summary>
    /// Sequenceイベントの購読を解除します。
    /// </summary>
    private void UnregisterSequenceEvent()
    {
        if (!m_isSequenceEventRegistered ||
            m_references?.SequenceChangedEvent == null)
        {
            return;
        }

        m_references.SequenceChangedEvent.UnregisterListener(
            HandleSequenceChanged);

        m_isSequenceEventRegistered =
            false;
    }

    /// <summary>
    /// Stateを失敗状態にします。
    /// </summary>
    private void SetFailed()
    {
        UnregisterSequenceEvent();

        if (Owner == null)
        {
            return;
        }

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.FAILED);
    }

    /// <summary>
    /// Runtime状態を初期化します。
    /// </summary>
    private void ResetRuntimeState()
    {
        m_references =
            null;

        m_transitionDuration =
            0.0f;

        m_elapsedTime =
            0.0f;

        m_isSequenceEventRegistered =
            false;
    }
}
