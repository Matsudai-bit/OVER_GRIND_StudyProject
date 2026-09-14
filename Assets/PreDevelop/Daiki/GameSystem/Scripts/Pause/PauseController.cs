using UnityEngine;

/// <summary>
/// ゲームのポーズ状態を管理します。
/// </summary>
[DisallowMultipleComponent]
public sealed class PauseController : MonoBehaviour
{
    // ポーズ要求イベント
    [SerializeField, Header("ポーズ要求イベント")]
    private PauseRequestEvent m_pauseRequestEvent;

    // ポーズ状態変更イベント
    [SerializeField, Header("ポーズ状態変更イベント")]
    private PauseChangedEvent m_pauseChangedEvent;

    // ゲーム進行状態変更イベント
    [SerializeField, Header("ゲーム進行イベント")]
    private GameFlowStateChangedEvent m_gameFlowStateChangedEvent;

    // 現在ポーズ中か
    private bool m_isPaused = false;

    // 現在のゲーム進行状態
    private GameFlowStateType m_currentGameFlowState =
        GameFlowStateType.NONE;

    /// <summary>
    /// 現在ポーズ中か取得します。
    /// </summary>
    public bool IsPaused => m_isPaused;

    /// <summary>
    /// 現在ポーズ可能か取得します。
    /// </summary>
    public bool CanPause =>
        m_currentGameFlowState == GameFlowStateType.PLAYING;

    private void OnEnable()
    {
        if (m_pauseRequestEvent != null)
        {
            m_pauseRequestEvent.RegisterListener(
                OnPauseRequested);
        }
        else
        {
            Debug.LogWarning(
                $"{nameof(PauseRequestEvent)} が設定されていません。",
                this);
        }

        if (m_gameFlowStateChangedEvent != null)
        {
            m_gameFlowStateChangedEvent.RegisterListener(
                OnGameFlowStateChanged);
        }
        else
        {
            Debug.LogWarning(
                $"{nameof(GameFlowStateChangedEvent)} が設定されていません。",
                this);
        }
    }

    private void OnDisable()
    {
        if (m_pauseRequestEvent != null)
        {
            m_pauseRequestEvent.UnregisterListener(
                OnPauseRequested);
        }

        if (m_gameFlowStateChangedEvent != null)
        {
            m_gameFlowStateChangedEvent.UnregisterListener(
                OnGameFlowStateChanged);
        }
    }

    /// <summary>
    /// ゲームをポーズします。
    /// </summary>
    /// <returns>
    /// true：ポーズしました。
    /// false：ポーズできませんでした。
    /// </returns>
    public bool Pause()
    {
        if (!CanPause)
        {
            return false;
        }

        return SetPaused(true);
    }

    /// <summary>
    /// ポーズを解除します。
    /// </summary>
    /// <returns>
    /// true：ポーズを解除しました。
    /// false：状態変更されませんでした。
    /// </returns>
    public bool Resume()
    {
        return SetPaused(false);
    }

    /// <summary>
    /// ポーズ状態を切り替えます。
    /// </summary>
    /// <returns>
    /// true：状態を変更しました。
    /// false：状態変更できませんでした。
    /// </returns>
    public bool TogglePause()
    {
        if (m_isPaused)
        {
            return Resume();
        }

        return Pause();
    }

    /// <summary>
    /// ポーズ要求を処理します。
    /// </summary>
    /// <param name="eventData">ポーズ要求情報。</param>
    private void OnPauseRequested(PauseRequestEventData eventData)
    {
        switch (eventData.RequestType)
        {
            case PauseRequestType.PAUSE:
                Pause();
                break;

            case PauseRequestType.RESUME:
                Resume();
                break;

            case PauseRequestType.TOGGLE:
                TogglePause();
                break;
        }
    }

    /// <summary>
    /// ゲーム進行状態変更を処理します。
    /// </summary>
    /// <param name="eventData">ゲーム進行状態変更情報。</param>
    private void OnGameFlowStateChanged(
        GameFlowStateChangedEventData eventData)
    {
        m_currentGameFlowState = eventData.CurrentState;

        // PLAYING終了時は必ずポーズを解除
        if (eventData.PreviousState == GameFlowStateType.PLAYING
            && eventData.CurrentState != GameFlowStateType.PLAYING)
        {
            Resume();
        }
    }

    /// <summary>
    /// ポーズ状態を変更します。
    /// </summary>
    /// <param name="isPaused">変更後のポーズ状態。</param>
    /// <returns>
    /// true：状態を変更しました。
    /// false：既に同じ状態です。
    /// </returns>
    private bool SetPaused(bool isPaused)
    {
        if (m_isPaused == isPaused)
        {
            return false;
        }

        bool previousIsPaused = m_isPaused;

        m_isPaused = isPaused;

        RaisePauseChangedEvent(
            previousIsPaused,
            m_isPaused);

        return true;
    }

    /// <summary>
    /// ポーズ状態変更を通知します。
    /// </summary>
    /// <param name="previousIsPaused">変更前の状態。</param>
    /// <param name="isPaused">変更後の状態。</param>
    private void RaisePauseChangedEvent(
        bool previousIsPaused,
        bool isPaused)
    {
        if (m_pauseChangedEvent == null)
        {
            Debug.LogWarning(
                $"{nameof(PauseChangedEvent)} が設定されていません。",
                this);

            return;
        }

        PauseChangedEventData eventData =
            new PauseChangedEventData(
                previousIsPaused,
                isPaused);

        m_pauseChangedEvent.Raise(eventData);
    }
}