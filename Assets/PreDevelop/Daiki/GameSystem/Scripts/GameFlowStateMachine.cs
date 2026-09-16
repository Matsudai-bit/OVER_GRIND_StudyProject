using System;

/// <summary>
/// インゲーム全体の状態遷移を管理します。
/// </summary>
public sealed class GameFlowStateMachine
{
    // 現在の状態
    private GameFlowStateType m_currentState = GameFlowStateType.NONE;

    // 初期化済みか
    private bool m_isInitialized = false;

    /// <summary>
    /// 状態変更時に通知されます。
    /// </summary>
    public event Action<GameFlowStateChangedEventData> StateChanged;

    /// <summary>
    /// 現在の状態を取得します。
    /// </summary>
    public GameFlowStateType CurrentState => m_currentState;

    /// <summary>
    /// 初期化済みか取得します。
    /// </summary>
    public bool IsInitialized => m_isInitialized;

    /// <summary>
    /// ステートマシンを初期化します。
    /// </summary>
    /// <param name="initialState">初期状態。</param>
    /// <returns>
    /// true：初期化しました。
    /// false：初期化できませんでした。
    /// </returns>
    public bool Initialize(GameFlowStateType initialState)
    {
        if (m_isInitialized)
        {
            return false;
        }

        if (initialState == GameFlowStateType.NONE)
        {
            return false;
        }

        GameFlowStateType previousState = m_currentState;

        m_currentState = initialState;
        m_isInitialized = true;

        NotifyStateChanged(previousState, m_currentState);

        return true;
    }

    /// <summary>
    /// 指定された状態へ遷移します。
    /// </summary>
    /// <param name="nextState">遷移先の状態。</param>
    /// <returns>
    /// true：状態を変更しました。
    /// false：状態を変更できませんでした。
    /// </returns>
    public bool TryChangeState(GameFlowStateType nextState)
    {
        if (!m_isInitialized)
        {
            return false;
        }

        if (m_currentState == nextState)
        {
            return false;
        }

        if (!CanChangeState(nextState))
        {
            return false;
        }

        GameFlowStateType previousState = m_currentState;

        m_currentState = nextState;

        NotifyStateChanged(previousState, m_currentState);

        return true;
    }

    /// <summary>
    /// 指定された状態へ遷移可能か確認します。
    /// </summary>
    /// <param name="nextState">遷移先の状態。</param>
    /// <returns>
    /// true：遷移可能です。
    /// false：遷移できません。
    /// </returns>
    public bool CanChangeState(GameFlowStateType nextState)
    {
        switch (m_currentState)
        {
            case GameFlowStateType.INTRO:
                return nextState == GameFlowStateType.PLAYING;

            case GameFlowStateType.PLAYING:
                return nextState == GameFlowStateType.ENDING;

            case GameFlowStateType.ENDING:
                return nextState == GameFlowStateType.RESULT;

            default:
                return false;
        }
    }

    /// <summary>
    /// 状態変更を通知します。
    /// </summary>
    /// <param name="previousState">変更前の状態。</param>
    /// <param name="currentState">変更後の状態。</param>
    private void NotifyStateChanged(
        GameFlowStateType previousState,
        GameFlowStateType currentState)
    {
        GameFlowStateChangedEventData eventData =
            new GameFlowStateChangedEventData(
                previousState,
                currentState);

        StateChanged?.Invoke(eventData);
    }
}