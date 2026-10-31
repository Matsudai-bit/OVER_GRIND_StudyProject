using UnityEngine;

/// <summary>
/// ステージ1フェーズ3のスタン状態を実行します。
/// </summary>
public sealed class S1P3BossStunnedState :
    StateBase<BossController>
{
    // 通常のスタン継続時間
    private const float STUN_DURATION =
        10.0f;

    // Animator Bool名
    private const string STUN_ANIMATION_BOOL_NAME =
        "Stun";

    // S1P3固有参照
    private S1P3BossReferences m_references;

    // 経過時間
    private float m_elapsedTime;

    // Animator Bool ID
    private int m_animationBoolID;

    // QTE開始要求済みか
    private bool m_hasRequestedQTE;

    // QTE実行中か
    private bool m_isQTERunning;

    // Sensorを購読中か
    private bool m_isSensorRegistered;

    // Sequenceイベントを購読中か
    private bool m_isSequenceEventRegistered;

    /// <summary>
    /// スタンを開始します。
    /// </summary>
    protected override void OnStartState()
    {
        ResetRuntimeState();

        if (!TryGetReferences())
        {
            SetFailed();

            return;
        }

        if (Owner.PhaseController.TryGetCurrentPhaseComponent(out S1P3BossStateTransitionRequest request))
        {
            request.ConsumeStunnedRequest();
        }

        m_animationBoolID =
            Animator.StringToHash(
                STUN_ANIMATION_BOOL_NAME);

        Owner.Motor?
            .StopHorizontalMovement();

        Owner.AnimationController.SetBool(
            m_animationBoolID,
            true);

        RegisterEvents();

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.RUNNING);
    }

    /// <summary>
    /// スタン状態を更新します。
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
         * QTE実行中は通常のスタン時間を
         * 消費しません。
         */
        if (m_isQTERunning)
        {
            return;
        }

        m_elapsedTime +=
            deltaTime;

        if (m_elapsedTime <
            STUN_DURATION)
        {
            return;
        }

        CompleteStun();
    }

    /// <summary>
    /// スタン状態を終了します。
    /// </summary>
    protected override void OnExitState()
    {
        UnregisterEvents();

        if (Owner?.AnimationController != null)
        {
            Owner.AnimationController.SetBool(
                m_animationBoolID,
                false);
        }

        if (Owner != null &&
            Owner.GetStateExecutionStatus() ==
                StateExecutionStatus.RUNNING)
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);
        }

        m_references =
            null;

        ResetRuntimeState();
    }

    /// <summary>
    /// QTE開始範囲への侵入を処理します。
    /// </summary>
    /// <param name="hitPosition">
    /// Sensor内へ侵入した位置。
    /// </param>
    private void HandleQTESensorDetected(
        Vector3 hitPosition)
    {
        if (Owner == null ||
            Owner.GetStateExecutionStatus() !=
                StateExecutionStatus.RUNNING ||
            m_hasRequestedQTE ||
            m_isQTERunning)
        {
            return;
        }

        StartQTE();
    }

    /// <summary>
    /// QTE Sequenceの開始を要求します。
    /// </summary>
    private void StartQTE()
    {
        if (m_references == null ||
            m_references.SequenceRequestEvent == null ||
            m_references.QTESequenceAsset == null)
        {
            Debug.LogError(
                $"[{nameof(S1P3BossStunnedState)}] " +
                "QTE Sequenceの設定が不足しています。");

            return;
        }

        /*
         * OnTriggerEnterが複数Colliderから
         * 発生しても1回だけ要求します。
         */
        m_hasRequestedQTE =
            true;

        m_references.SequenceRequestEvent.Raise(
            m_references.QTESequenceAsset);
    }

    /// <summary>
    /// Sequence変更イベントを処理します。
    /// </summary>
    /// <param name="eventData">
    /// Sequence変更情報。
    /// </param>
    private void HandleSequenceChanged(
        GameplaySequenceChangedEventData eventData)
    {
        if (m_references == null ||
            eventData.Sequence !=
                m_references.QTESequenceAsset)
        {
            return;
        }

        switch (eventData.EventType)
        {
            case GameplaySequenceEventType.STARTED:
                HandleQTEStarted();
                break;

            case GameplaySequenceEventType.FINISHED:
                HandleQTEFinished();
                break;

            case GameplaySequenceEventType.CANCELED:
                HandleQTECanceled();
                break;
        }
    }

    /// <summary>
    /// QTE開始を処理します。
    /// </summary>
    private void HandleQTEStarted()
    {
        m_isQTERunning =
            true;

        Owner?.Motor?
            .StopHorizontalMovement();
    }

    /// <summary>
    /// QTE完了を処理します。
    /// </summary>
    private void HandleQTEFinished()
    {
        m_isQTERunning =
            false;

        CompleteStun();

        AttackDamageData attackDamage = new(999999);
        m_references.BreakablePart.TakeDamage(attackDamage);
    }

    /// <summary>
    /// QTE中断を処理します。
    /// </summary>
    private void HandleQTECanceled()
    {
        m_isQTERunning =
            false;

        /*
         * 中断後は通常のスタン状態へ戻します。
         * Playerが一度Sensor外へ出て再侵入すれば
         * QTEを再要求できます。
         */
        m_hasRequestedQTE =
            false;
    }

    /// <summary>
    /// スタン状態を正常終了します。
    /// </summary>
    private void CompleteStun()
    {
        if (Owner == null ||
            Owner.GetStateExecutionStatus() !=
                StateExecutionStatus.RUNNING)
        {
            return;
        }

        Owner.AnimationController?.SetBool(
            m_animationBoolID,
            false);

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.SUCCEEDED);
    }

    /// <summary>
    /// 必要な参照を取得します。
    /// </summary>
    /// <returns>
    /// true：取得しました。
    /// false：取得できませんでした。
    /// </returns>
    private bool TryGetReferences()
    {
        if (Owner == null ||
            Owner.PhaseController == null ||
            Owner.AnimationController == null)
        {
            return false;
        }

        if (!Owner.PhaseController
                .TryGetCurrentPhaseComponent(
                    out m_references))
        {
            return false;
        }

        if (m_references.StunQTESensor == null)
        {
            Debug.LogError(
                $"[{nameof(S1P3BossStunnedState)}] " +
                $"{nameof(CollisionSensor)}が設定されていません.");

            return false;
        }

        if (m_references.QTESequenceAsset == null)
        {
            Debug.LogError(
                $"[{nameof(S1P3BossStunnedState)}] " +
                "QTE Sequence Assetが設定されていません.");

            return false;
        }

        if (m_references.SequenceRequestEvent == null ||
            m_references.SequenceChangedEvent == null)
        {
            Debug.LogError(
                $"[{nameof(S1P3BossStunnedState)}] " +
                "Sequence Eventが設定されていません.");

            return false;
        }

        return true;
    }

    /// <summary>
    /// QTE関連イベントを購読します。
    /// </summary>
    private void RegisterEvents()
    {
        if (!m_isSensorRegistered)
        {
            m_references.StunQTESensor.CollisionDetected +=
                HandleQTESensorDetected;

            m_isSensorRegistered =
                true;
        }

        if (!m_isSequenceEventRegistered)
        {
            m_references.SequenceChangedEvent.RegisterListener(
                HandleSequenceChanged);

            m_isSequenceEventRegistered =
                true;
        }
    }

    /// <summary>
    /// QTE関連イベントの購読を解除します。
    /// </summary>
    private void UnregisterEvents()
    {
        if (m_isSensorRegistered &&
            m_references?.StunQTESensor != null)
        {
            m_references.StunQTESensor.CollisionDetected -=
                HandleQTESensorDetected;

            m_isSensorRegistered =
                false;
        }

        if (m_isSequenceEventRegistered &&
            m_references?.SequenceChangedEvent != null)
        {
            m_references.SequenceChangedEvent.UnregisterListener(
                HandleSequenceChanged);

            m_isSequenceEventRegistered =
                false;
        }
    }

    /// <summary>
    /// Runtime状態を初期化します。
    /// </summary>
    private void ResetRuntimeState()
    {
        m_elapsedTime =
            0.0f;

        m_hasRequestedQTE =
            false;

        m_isQTERunning =
            false;

        m_isSensorRegistered =
            false;

        m_isSequenceEventRegistered =
            false;
    }

    /// <summary>
    /// スタン状態を失敗状態にします。
    /// </summary>
    private void SetFailed()
    {
        UnregisterEvents();

        if (Owner == null)
        {
            return;
        }

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.FAILED);
    }
}