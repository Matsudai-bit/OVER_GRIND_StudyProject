/// <summary>
/// ステージ1フェーズ2の崩壊演出を再生し、
/// 終了後に次のフェーズへ移行します。
/// </summary>
public sealed class S1P2BossCollapsingState :
    StateBase<BossController>
{
    // S1P2固有参照
    private S1P2BossReferences m_references;

    // Sequence変更イベントを購読中か
    private bool m_isSequenceEventRegistered;

    /// <summary>
    /// 崩壊演出を開始します。
    /// </summary>
    protected override void OnStartState()
    {
        if (!TryGetReferences())
        {
            SetFailed();

            return;
        }

        /*
         * Sequence開始通知を取りこぼさないよう、
         * Raiseより先にイベントを購読します。
         */
        m_references.SequenceChangedEvent.RegisterListener(
            HandleSequenceChanged);

        m_isSequenceEventRegistered =
            true;

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.RUNNING);

        m_references.SequenceRequestEvent.Raise(
            m_references.DestructionSequenceAsset);
    }

    /// <summary>
    /// State終了処理を行います。
    /// </summary>
    protected override void OnExitState()
    {
        UnregisterSequenceEvent();

        m_references =
            null;

        if (Owner != null &&
            Owner.GetStateExecutionStatus() ==
                StateExecutionStatus.RUNNING)
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);
        }
    }

    /// <summary>
    /// Sequence変更通知を処理します。
    /// </summary>
    /// <param name="eventData">
    /// Sequence変更情報。
    /// </param>
    private void HandleSequenceChanged(
        GameplaySequenceChangedEventData eventData)
    {
        if (Owner == null ||
            m_references == null ||
            Owner.GetStateExecutionStatus() !=
                StateExecutionStatus.RUNNING)
        {
            return;
        }

        // このStateが開始したSequence以外は無視する
        if (eventData.Sequence !=
            m_references.DestructionSequenceAsset)
        {
            return;
        }

        switch (eventData.EventType)
        {
            case GameplaySequenceEventType.FINISHED:
                CompleteCollapse();
                break;

            case GameplaySequenceEventType.CANCELED:
                SetFailed();
                break;
        }
    }

    /// <summary>
    /// 崩壊演出終了後に次のフェーズへ移行します。
    /// </summary>
    private void CompleteCollapse()
    {
        /*
         * AdvancePhaseで現在フェーズRootが
         * 無効化される可能性があるため、
         * 先に購読を解除します。
         */
        UnregisterSequenceEvent();

        if (!Owner.PhaseController.AdvancePhase())
        {
            SetFailed();

            return;
        }

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.SUCCEEDED);
    }

    /// <summary>
    /// 必要な参照を取得します。
    /// </summary>
    /// <returns>
    /// true：取得できました。
    /// false：取得できませんでした。
    /// </returns>
    private bool TryGetReferences()
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

        return
            m_references.SequenceRequestEvent != null &&
            m_references.SequenceChangedEvent != null &&
            m_references.DestructionSequenceAsset != null;
    }

    /// <summary>
    /// Sequence変更イベントの購読を解除します。
    /// </summary>
    private void UnregisterSequenceEvent()
    {
        if (!m_isSequenceEventRegistered ||
            m_references == null ||
            m_references.SequenceChangedEvent == null)
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
}