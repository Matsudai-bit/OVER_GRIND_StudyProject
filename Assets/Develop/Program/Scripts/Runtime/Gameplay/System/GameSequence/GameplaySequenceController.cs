using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ゲームプレイ中のシーケンス実行を管理します。
/// </summary>
[DisallowMultipleComponent]
public sealed class GameplaySequenceController : MonoBehaviour
{
    // ゲームプレイモード管理
    [SerializeField, Header("ゲームプレイモード")]
    private GameplayModeController m_gameplayModeController;

    // シーケンス開始要求イベント
    [SerializeField, Header("シーケンス要求イベント")]
    private GameplaySequenceRequestEvent m_sequenceRequestEvent;

    // シーケンス変更イベント
    [SerializeField, Header("シーケンス変更イベント")]
    private GameplaySequenceChangedEvent m_sequenceChangedEvent;

    // シーン内で使用するシーケンスRunner
    [SerializeField, Header("シーケンスRunner")]
    private List<GameplaySequenceRunner> m_sequenceRunners = new();

    // 現在実行中のシーケンス
    private GameplaySequenceAsset m_currentSequence;

    // 現在実行中のRunner
    private GameplaySequenceRunner m_currentRunner;

    // Sequence開始時にGameplayModeをPushしたか
    private bool m_hasPushedGameplayMode = false;

    /// <summary>
    /// 現在実行中のシーケンスを取得します。
    /// </summary>
    public GameplaySequenceAsset CurrentSequence => m_currentSequence;

    /// <summary>
    /// シーケンス実行中か取得します。
    /// </summary>
    public bool IsRunning => m_currentSequence != null;

    private void OnEnable()
    {
        if (m_sequenceRequestEvent == null)
        {
            Debug.LogWarning(
                $"{nameof(GameplaySequenceRequestEvent)} が設定されていません。",
                this);

            return;
        }

        m_sequenceRequestEvent.RegisterListener(
            OnSequenceRequested);
    }

    private void OnDisable()
    {
        if (m_sequenceRequestEvent == null)
        {
            return;
        }

        m_sequenceRequestEvent.UnregisterListener(
            OnSequenceRequested);
    }

    /// <summary>
    /// 指定されたシーケンスを開始します。
    /// </summary>
    /// <param name="sequence">開始するシーケンス。</param>
    /// <returns>
    /// true：開始しました。
    /// false：開始できませんでした。
    /// </returns>
    public bool StartSequence(GameplaySequenceAsset sequence)
    {
        if (sequence == null)
        {
            Debug.LogWarning(
                "開始するシーケンスが指定されていません。",
                this);

            return false;
        }

        if (IsRunning)
        {
            Debug.LogWarning(
                $"シーケンス実行中のため開始できません。"
                + $" Current: {m_currentSequence.name}"
                + $" Request: {sequence.name}",
                this);

            return false;
        }

        GameplaySequenceRunner runner = FindRunner(sequence);

        if (runner == null)
        {
            Debug.LogWarning(
                $"{sequence.name} に対応するRunnerが見つかりません。",
                this);

            return false;
        }

        if (!TryEnterRequiredMode(sequence))
        {
            return false;
        }

        m_currentSequence = sequence;
        m_currentRunner = runner;

        m_currentRunner.Completed += OnSequenceCompleted;

        if (!m_currentRunner.StartSequence())
        {
            m_currentRunner.Completed -= OnSequenceCompleted;

            m_currentSequence = null;
            m_currentRunner = null;

            RestoreGameplayMode();

            Debug.LogWarning(
                $"{sequence.name} の開始に失敗しました。",
                this);

            return false;
        }

        RaiseSequenceChangedEvent(
            sequence,
            GameplaySequenceEventType.STARTED);

        return true;
    }

    /// <summary>
    /// 現在のシーケンスを中断します。
    /// </summary>
    /// <returns>
    /// true：中断しました。
    /// false：中断できませんでした。
    /// </returns>
    public bool CancelCurrentSequence()
    {
        if (!IsRunning || m_currentRunner == null)
        {
            return false;
        }

        GameplaySequenceAsset canceledSequence = m_currentSequence;
        GameplaySequenceRunner canceledRunner = m_currentRunner;

        canceledRunner.Completed -= OnSequenceCompleted;

        canceledRunner.StopSequence();

        ClearCurrentSequence();

        RestoreGameplayMode();

        RaiseSequenceChangedEvent(
            canceledSequence,
            GameplaySequenceEventType.CANCELED);

        return true;
    }

    /// <summary>
    /// シーケンス開始要求を受け取ります。
    /// </summary>
    /// <param name="eventData">シーケンス開始要求情報。</param>
    private void OnSequenceRequested(
        GameplaySequenceRequestEventData eventData)
    {
        if (eventData.Sequence == null)
        {
            return;
        }

        StartSequence(eventData.Sequence);
    }

    /// <summary>
    /// 指定されたシーケンスに対応するRunnerを取得します。
    /// </summary>
    /// <param name="sequence">対象のシーケンス。</param>
    /// <returns>対応するRunner。</returns>
    private GameplaySequenceRunner FindRunner(
        GameplaySequenceAsset sequence)
    {
        foreach (GameplaySequenceRunner runner in m_sequenceRunners)
        {
            if (runner == null)
            {
                continue;
            }

            if (runner.Sequence == sequence)
            {
                return runner;
            }
        }

        return null;
    }

    /// <summary>
    /// シーケンスに必要なGameplayModeへ移行します。
    /// </summary>
    /// <param name="sequence">開始するシーケンス。</param>
    /// <returns>
    /// true：必要なモードになりました。
    /// false：モードを変更できませんでした。
    /// </returns>
    private bool TryEnterRequiredMode(
        GameplaySequenceAsset sequence)
    {
        m_hasPushedGameplayMode = false;

        GameplayModeType requiredMode = sequence.RequiredMode;

        if (requiredMode == GameplayModeType.NONE)
        {
            return true;
        }

        if (m_gameplayModeController == null)
        {
            Debug.LogWarning(
                $"{nameof(GameplayModeController)} が設定されていません。",
                this);

            return false;
        }

        if (m_gameplayModeController.CurrentMode == requiredMode)
        {
            return true;
        }

        if (!m_gameplayModeController.EnterMode(requiredMode))
        {
            Debug.LogWarning(
                $"{requiredMode} へ移行できませんでした。",
                this);

            return false;
        }

        m_hasPushedGameplayMode = true;

        return true;
    }

    /// <summary>
    /// シーケンス開始前のGameplayModeへ復帰します。
    /// </summary>
    private void RestoreGameplayMode()
    {
        if (!m_hasPushedGameplayMode)
        {
            return;
        }

        if (m_gameplayModeController == null)
        {
            m_hasPushedGameplayMode = false;
            return;
        }

        m_gameplayModeController.ExitCurrentMode();

        m_hasPushedGameplayMode = false;
    }

    /// <summary>
    /// Runnerからシーケンス完了を受け取ります。
    /// </summary>
    /// <param name="runner">完了したRunner。</param>
    private void OnSequenceCompleted(
        GameplaySequenceRunner runner)
    {
        if (runner == null || runner != m_currentRunner)
        {
            return;
        }

        GameplaySequenceAsset finishedSequence = m_currentSequence;

        runner.Completed -= OnSequenceCompleted;

        ClearCurrentSequence();

        RestoreGameplayMode();

        RaiseSequenceChangedEvent(
            finishedSequence,
            GameplaySequenceEventType.FINISHED);
    }

    /// <summary>
    /// 現在のシーケンス情報をクリアします。
    /// </summary>
    private void ClearCurrentSequence()
    {
        m_currentSequence = null;
        m_currentRunner = null;
    }

    /// <summary>
    /// シーケンス変更を通知します。
    /// </summary>
    /// <param name="sequence">対象のシーケンス。</param>
    /// <param name="eventType">イベント種別。</param>
    private void RaiseSequenceChangedEvent(
        GameplaySequenceAsset sequence,
        GameplaySequenceEventType eventType)
    {
        if (m_sequenceChangedEvent == null)
        {
            Debug.LogWarning(
                $"{nameof(GameplaySequenceChangedEvent)} が設定されていません。",
                this);

            return;
        }

        GameplaySequenceChangedEventData eventData =
            new GameplaySequenceChangedEventData(
                sequence,
                eventType);

        m_sequenceChangedEvent.Raise(eventData);
    }
}