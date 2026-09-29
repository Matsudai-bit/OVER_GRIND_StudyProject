using UnityEngine;

/// <summary>
/// インゲーム全体の進行を制御します。
/// </summary>
[DisallowMultipleComponent]
public sealed class GameFlowController : MonoBehaviour
{
    // 初期状態
    [SerializeField, Header("ゲーム進行")]
    private GameFlowStateType m_initialState = GameFlowStateType.INTRO;

    // 状態変更イベント
    [SerializeField, Header("イベント")]
    private GameFlowStateChangedEvent m_stateChangedEvent;

    // ゲーム進行ステートマシン
    private GameFlowStateMachine m_stateMachine;

    /// <summary>
    /// 現在のゲーム進行状態を取得します。
    /// </summary>
    public GameFlowStateType CurrentState
    {
        get
        {
            if (m_stateMachine == null)
            {
                return GameFlowStateType.NONE;
            }

            return m_stateMachine.CurrentState;
        }
    }

    private void Awake()
    {
        m_stateMachine = new GameFlowStateMachine();

        m_stateMachine.StateChanged += OnStateChanged;
    }

    private void Start()
    {
        InitializeFlow();
    }

    private void OnDestroy()
    {
        if (m_stateMachine == null)
        {
            return;
        }

        m_stateMachine.StateChanged -= OnStateChanged;
    }

    /// <summary>
    /// ゲーム進行を初期化します。
    /// </summary>
    public void InitializeFlow()
    {
        if (m_stateMachine == null)
        {
            Debug.LogError(
                $"{nameof(GameFlowStateMachine)} が生成されていません。",
                this);

            return;
        }

        if (m_initialState == GameFlowStateType.NONE)
        {
            Debug.LogError(
                "ゲーム進行の初期状態に NONE は指定できません。",
                this);

            return;
        }

        if (!m_stateMachine.Initialize(m_initialState))
        {
            Debug.LogWarning(
                "ゲーム進行の初期化に失敗しました。",
                this);
        }
    }

    /// <summary>
    /// 指定された状態へ変更します。
    /// </summary>
    /// <param name="nextState">遷移先の状態。</param>
    /// <returns>
    /// true：状態を変更しました。
    /// false：状態を変更できませんでした。
    /// </returns>
    public bool ChangeState(GameFlowStateType nextState)
    {
        if (m_stateMachine == null)
        {
            return false;
        }

        if (m_stateMachine.TryChangeState(nextState))
        {
            return true;
        }

        Debug.LogWarning(
            $"ゲーム進行状態を変更できませんでした。"
            + $" Current: {m_stateMachine.CurrentState}"
            + $" Next: {nextState}",
            this);

        return false;
    }

    /// <summary>
    /// ゲームプレイ状態へ移行します。
    /// </summary>
    public void EnterPlaying()
    {
        ChangeState(GameFlowStateType.PLAYING);
    }

    /// <summary>
    /// 終了状態へ移行します。
    /// </summary>
    public void EnterEnding()
    {
        ChangeState(GameFlowStateType.ENDING);
    }

    /// <summary>
    /// リザルト状態へ移行します。
    /// </summary>
    public void EnterResult()
    {
        ChangeState(GameFlowStateType.RESULT);
    }

    /// <summary>
    /// 状態変更をゲーム全体へ通知します。
    /// </summary>
    /// <param name="eventData">状態変更情報。</param>
    private void OnStateChanged(
        GameFlowStateChangedEventData eventData)
    {
        if (m_stateChangedEvent == null)
        {
            Debug.LogWarning(
                $"{nameof(GameFlowStateChangedEvent)} が設定されていません。",
                this);

            return;
        }

        m_stateChangedEvent.Raise(eventData);
    }
}