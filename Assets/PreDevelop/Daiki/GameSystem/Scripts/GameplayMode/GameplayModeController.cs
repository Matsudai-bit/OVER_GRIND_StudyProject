using UnityEngine;

/// <summary>
/// ゲームプレイ中の動作モードを制御します。
/// </summary>
[DisallowMultipleComponent]
public sealed class GameplayModeController : MonoBehaviour
{
    // ゲーム進行状態変更イベント
    [SerializeField, Header("ゲーム進行イベント")]
    private GameFlowStateChangedEvent m_gameFlowStateChangedEvent;

    // ゲームプレイモード変更イベント
    [SerializeField, Header("ゲームプレイモードイベント")]
    private GameplayModeChangedEvent m_modeChangedEvent;

    // ゲームプレイモードのステートマシン
    private GameplayModeStateMachine m_stateMachine;

    /// <summary>
    /// 現在のゲームプレイモードを取得します。
    /// </summary>
    public GameplayModeType CurrentMode
    {
        get
        {
            if (m_stateMachine == null)
            {
                return GameplayModeType.NONE;
            }

            return m_stateMachine.CurrentMode;
        }
    }

    /// <summary>
    /// ゲームプレイモードが有効か取得します。
    /// </summary>
    public bool IsActive
    {
        get
        {
            return m_stateMachine != null
                && m_stateMachine.IsInitialized;
        }
    }

    private void Awake()
    {
        m_stateMachine = new GameplayModeStateMachine();

        m_stateMachine.ModeChanged += OnModeChanged;
    }

    private void OnEnable()
    {
        if (m_gameFlowStateChangedEvent == null)
        {
            Debug.LogWarning(
                $"{nameof(GameFlowStateChangedEvent)} が設定されていません。",
                this);

            return;
        }

        m_gameFlowStateChangedEvent.RegisterListener(
            OnGameFlowStateChanged);
    }

    private void OnDisable()
    {
        if (m_gameFlowStateChangedEvent == null)
        {
            return;
        }

        m_gameFlowStateChangedEvent.UnregisterListener(
            OnGameFlowStateChanged);
    }

    private void OnDestroy()
    {
        if (m_stateMachine == null)
        {
            return;
        }

        m_stateMachine.ModeChanged -= OnModeChanged;
    }

    /// <summary>
    /// ゲームプレイモードを開始します。
    /// </summary>
    /// <returns>
    /// true：開始しました。
    /// false：開始できませんでした。
    /// </returns>
    public bool ActivateGameplay()
    {
        if (m_stateMachine == null)
        {
            return false;
        }

        return m_stateMachine.Initialize(
            GameplayModeType.NORMAL);
    }

    /// <summary>
    /// 現在のモードを保持して指定モードへ移行します。
    /// </summary>
    /// <param name="nextMode">移行先のモード。</param>
    /// <returns>
    /// true：モードを変更しました。
    /// false：モードを変更できませんでした。
    /// </returns>
    public bool EnterMode(GameplayModeType nextMode)
    {
        if (m_stateMachine == null)
        {
            return false;
        }

        if (m_stateMachine.TryPushMode(nextMode))
        {
            return true;
        }

        Debug.LogWarning(
            $"ゲームプレイモードを変更できませんでした。"
            + $" Current: {m_stateMachine.CurrentMode}"
            + $" Next: {nextMode}",
            this);

        return false;
    }

    /// <summary>
    /// 直前のモードへ復帰します。
    /// </summary>
    /// <returns>
    /// true：前のモードへ復帰しました。
    /// false：復帰できませんでした。
    /// </returns>
    public bool ExitCurrentMode()
    {
        if (m_stateMachine == null)
        {
            return false;
        }

        if (m_stateMachine.TryPopMode())
        {
            return true;
        }

        Debug.LogWarning(
            $"復帰可能なゲームプレイモードがありません。"
            + $" Current: {m_stateMachine.CurrentMode}",
            this);

        return false;
    }

    /// <summary>
    /// ゲームプレイモードを終了します。
    /// </summary>
    /// <returns>
    /// true：終了しました。
    /// false：終了できませんでした。
    /// </returns>
    public bool DeactivateGameplay()
    {
        if (m_stateMachine == null)
        {
            return false;
        }

        return m_stateMachine.Reset();
    }

    /// <summary>
    /// ゲーム進行状態の変更を受け取ります。
    /// </summary>
    /// <param name="eventData">ゲーム進行状態変更情報。</param>
    private void OnGameFlowStateChanged(
        GameFlowStateChangedEventData eventData)
    {
        // PLAYING開始時に通常モードを開始
        if (eventData.CurrentState == GameFlowStateType.PLAYING)
        {
            ActivateGameplay();
            return;
        }

        // PLAYING終了時にモード管理を終了
        if (eventData.PreviousState == GameFlowStateType.PLAYING)
        {
            DeactivateGameplay();
        }
    }

    /// <summary>
    /// モード変更をゲーム全体へ通知します。
    /// </summary>
    /// <param name="eventData">モード変更情報。</param>
    private void OnModeChanged(
        GameplayModeChangedEventData eventData)
    {
        if (m_modeChangedEvent == null)
        {
            Debug.LogWarning(
                $"{nameof(GameplayModeChangedEvent)} が設定されていません。",
                this);

            return;
        }

        m_modeChangedEvent.Raise(eventData);
    }
}