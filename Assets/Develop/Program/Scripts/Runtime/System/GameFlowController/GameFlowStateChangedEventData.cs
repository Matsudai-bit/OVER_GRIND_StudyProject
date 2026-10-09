/// <summary>
/// ゲーム進行状態変更時の情報です。
/// </summary>
public readonly struct GameFlowStateChangedEventData
{
    /// <summary>
    /// 変更前の状態を取得します。
    /// </summary>
    public GameFlowStateType PreviousState { get; }

    /// <summary>
    /// 変更後の状態を取得します。
    /// </summary>
    public GameFlowStateType CurrentState { get; }

    public GameFlowStateChangedEventData(
        GameFlowStateType previousState,
        GameFlowStateType currentState)
    {
        PreviousState = previousState;
        CurrentState = currentState;
    }
}