using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ステージ1フェーズ2の崩壊演出を再生し、
/// 自身へミサイルを連続発射してから次フェーズへ移行します。
/// </summary>
public sealed class S1P2BossCollapsingState :
    StateBase<BossController>
{
    // 崩壊時に発射するミサイル数
    private const int COLLAPSE_MISSILE_COUNT = 6;

    // S1P2固有参照
    private S1P2BossReferences m_references;

    // ミサイル攻撃参照
    private S1BossMissileReferences m_missileReferences;

    // ミサイル実行制御
    private S1P2BossMissileExecutor m_missileExecutor;

    // 1発発射用ミサイルパラメータ
    private S1P2BossMissileStateParameters
        m_singleShotMissileParameters;

    // 次に使用する自己攻撃対象Index
    private int m_nextTargetIndex;

    // 発射済みミサイル数
    private int m_launchedMissileCount;

    // 次弾発射までの経過時間
    private float m_launchElapsedTime;

    // Executorが1発分を実行中か
    private bool m_isMissileExecuting;

    // 全ミサイルの発射が完了したか
    private bool m_isMissileSequenceCompleted;

    // 崩壊Sequenceが完了したか
    private bool m_isSequenceCompleted;

    // Sequence変更イベントを購読中か
    private bool m_isSequenceEventRegistered;

    /// <summary>
    /// 崩壊演出を開始します。
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
         * 通常のP2ミサイル設定を流用しつつ、
         * Executor1回あたりの発射数だけ1発に固定します。
         */
        m_singleShotMissileParameters =
            m_references
                .StateParameterAsset
                .Missile
                .CreateCopyWithLaunchCount(
                    1);

        if (m_singleShotMissileParameters == null)
        {
            Debug.LogError(
                $"[{nameof(S1P2BossCollapsingState)}] " +
                "崩壊用ミサイルパラメータを生成できませんでした。");

            SetFailed();

            return;
        }

        /*
         * Sequence開始通知を取りこぼさないよう、
         * Raiseより先に購読します。
         */
        m_references.SequenceChangedEvent.RegisterListener(
            HandleSequenceChanged);

        m_isSequenceEventRegistered =
            true;

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.RUNNING);

        // 崩壊演出を開始します。
        m_references.SequenceRequestEvent.Raise(
            m_references.DestructionSequenceAsset);

        /*
         * 崩壊演出開始と同時に
         * 1発目の自己攻撃ミサイルを発射します。
         */
        if (!StartNextMissile())
        {
            SetFailed();
        }
    }

    /// <summary>
    /// 自己攻撃ミサイルの連続発射を更新します。
    /// </summary>
    /// <param name="deltaTime">経過時間。</param>
    protected override void OnUpdate(
        float deltaTime)
    {
        if (Owner == null ||
            Owner.GetStateExecutionStatus() !=
                StateExecutionStatus.RUNNING ||
            m_isMissileSequenceCompleted)
        {
            return;
        }

        if (m_missileExecutor == null)
        {
            SetFailed();

            return;
        }

        /*
         * 現在の1発分のExecutorが動作中なら
         * 完了するまで更新します。
         */
        if (m_isMissileExecuting)
        {
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

            m_isMissileExecuting =
                false;

            /*
             * 指定数を撃ち終えていれば
             * ミサイル側の処理は完了です。
             */
            if (m_launchedMissileCount >=
                COLLAPSE_MISSILE_COUNT)
            {
                m_isMissileSequenceCompleted =
                    true;

                TryCompleteCollapse();

                return;
            }

            m_launchElapsedTime =
                0.0f;
        }

        /*
         * 次の発射までは通常ミサイルと同じ
         * LaunchIntervalを使用します。
         */
        m_launchElapsedTime +=
            Mathf.Max(
                0.0f,
                deltaTime);

        if (m_launchElapsedTime <
            m_singleShotMissileParameters.LaunchInterval)
        {
            return;
        }

        m_launchElapsedTime =
            0.0f;

        if (!StartNextMissile())
        {
            SetFailed();
        }
    }

    /// <summary>
    /// State終了処理を行います。
    /// </summary>
    protected override void OnExitState()
    {
        UnregisterSequenceEvent();

        m_missileExecutor?.Cancel();

        if (Owner != null &&
            Owner.GetStateExecutionStatus() ==
                StateExecutionStatus.RUNNING)
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);
        }

        m_references =
            null;

        m_missileReferences =
            null;

        m_missileExecutor =
            null;

        m_singleShotMissileParameters =
            null;
    }

    /// <summary>
    /// 次の自己攻撃ミサイルを1発開始します。
    /// </summary>
    /// <returns>
    /// true：開始できました。
    /// false：開始できませんでした。
    /// </returns>
    private bool StartNextMissile()
    {
        if (m_launchedMissileCount >=
            COLLAPSE_MISSILE_COUNT)
        {
            m_isMissileSequenceCompleted =
                true;

            TryCompleteCollapse();

            return true;
        }

        if (!TryGetNextMissileTarget(
                out Transform target))
        {
            Debug.LogError(
                $"[{nameof(S1P2BossCollapsingState)}] " +
                "自己攻撃ミサイルの対象を取得できませんでした。");

            return false;
        }

        /*
         * Executor自体には手を加えず、
         * 1発ごとに対象Transformを指定して再利用します。
         */
        if (!m_missileExecutor.PrepareAttack(
                m_missileReferences,
                m_singleShotMissileParameters,
                target))
        {
            Debug.LogError(
                $"[{nameof(S1P2BossCollapsingState)}] " +
                "自己攻撃ミサイルを準備できませんでした。");

            return false;
        }

        if (!m_missileExecutor.BeginLaunch())
        {
            Debug.LogError(
                $"[{nameof(S1P2BossCollapsingState)}] " +
                "自己攻撃ミサイルを発射できませんでした。");

            return false;
        }

        m_launchedMissileCount++;

        m_isMissileExecuting =
            true;

        return true;
    }

    /// <summary>
    /// 次にミサイルを当てるボス部位を取得します。
    /// </summary>
    /// <param name="target">取得した対象。</param>
    /// <returns>
    /// true：取得できました。
    /// false：取得できませんでした。
    /// </returns>
    private bool TryGetNextMissileTarget(
        out Transform target)
    {
        target =
            null;

        IReadOnlyList<Transform> targets =
            m_references?
                .CollapseMissileTargets;

        if (targets == null ||
            targets.Count == 0)
        {
            return false;
        }

        /*
         * nullを飛ばしつつ、
         * リスト末尾まで到達したら先頭へ戻ります。
         */
        int searchCount =
            0;

        while (searchCount <
               targets.Count)
        {
            int targetIndex =
                m_nextTargetIndex %
                targets.Count;

            m_nextTargetIndex =
                (targetIndex + 1) %
                targets.Count;

            searchCount++;

            Transform candidate =
                targets[targetIndex];

            if (candidate == null)
            {
                continue;
            }

            target =
                candidate;

            return true;
        }

        return false;
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
                StateExecutionStatus.RUNNING)
        {
            return;
        }

        if (eventData.Sequence !=
            m_references.DestructionSequenceAsset)
        {
            return;
        }

        switch (eventData.EventType)
        {
            case GameplaySequenceEventType.FINISHED:
                HandleSequenceFinished();
                break;

            case GameplaySequenceEventType.CANCELED:
                SetFailed();
                break;
        }
    }

    /// <summary>
    /// 崩壊Sequence完了を処理します。
    /// </summary>
    private void HandleSequenceFinished()
    {
        m_isSequenceCompleted =
            true;

        TryCompleteCollapse();
    }

    /// <summary>
    /// Sequenceとミサイルの両方が完了した場合に
    /// 次フェーズへ移行します。
    /// </summary>
    private void TryCompleteCollapse()
    {
        if (!m_isSequenceCompleted ||
            !m_isMissileSequenceCompleted)
        {
            return;
        }

        /*
         * AdvancePhaseで現在Phase Rootが
         * 無効化される可能性があるため、
         * 先に購読を解除します。
         */
        UnregisterSequenceEvent();

        m_missileExecutor?.Cancel();

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

        if (m_references == null ||
            m_references.SequenceRequestEvent == null ||
            m_references.SequenceChangedEvent == null ||
            m_references.DestructionSequenceAsset == null ||
            m_references.MissileReferences == null ||
            m_references.MissileExecutor == null ||
            m_references.StateParameterAsset == null ||
            m_references.StateParameterAsset.Missile == null)
        {
            return false;
        }

        IReadOnlyList<Transform> targets =
            m_references.CollapseMissileTargets;

        if (targets == null ||
            targets.Count == 0)
        {
            Debug.LogError(
                $"[{nameof(S1P2BossCollapsingState)}] " +
                "崩壊ミサイルの対象地点が設定されていません。");

            return false;
        }

        bool hasValidTarget =
            false;

        foreach (Transform target in targets)
        {
            if (target == null)
            {
                continue;
            }

            hasValidTarget =
                true;

            break;
        }

        if (!hasValidTarget)
        {
            Debug.LogError(
                $"[{nameof(S1P2BossCollapsingState)}] " +
                "有効な崩壊ミサイル対象地点がありません。");

            return false;
        }

        m_missileReferences =
            m_references.MissileReferences;

        m_missileExecutor =
            m_references.MissileExecutor;

        return true;
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

        m_missileExecutor?.Cancel();

        if (Owner == null)
        {
            return;
        }

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.FAILED);
    }

    /// <summary>
    /// 実行時状態を初期化します。
    /// </summary>
    private void ResetRuntimeState()
    {
        m_nextTargetIndex =
            0;

        m_launchedMissileCount =
            0;

        m_launchElapsedTime =
            0.0f;

        m_isMissileExecuting =
            false;

        m_isMissileSequenceCompleted =
            false;

        m_isSequenceCompleted =
            false;

        m_isSequenceEventRegistered =
            false;
    }
}