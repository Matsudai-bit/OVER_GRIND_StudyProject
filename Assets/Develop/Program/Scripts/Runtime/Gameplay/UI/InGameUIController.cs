using UnityEngine;

/// <summary>
/// インゲームUIの表示・非表示を管理します。
/// </summary>
[DisallowMultipleComponent]
public sealed class InGameUIController : MonoBehaviour
{
    // HUD表示ルート
    [SerializeField, Header("UIルート")]
    private GameObject m_hudRoot;

    // チュートリアル表示ルート
    [SerializeField]
    private GameObject m_tutorialRoot;

    // 独り言・セリフ表示ルート
    [SerializeField]
    private GameObject m_dialogueRoot;

    // リザルト表示ルート
    [SerializeField]
    private GameObject m_resultRoot;

    // ゲームオーバー表示ルート
    [SerializeField]
    private GameObject m_gameOverRoot;

    // ポーズウィンドウ管理
    [SerializeField, Header("ポーズUI")]
    private UIWindowManager m_pauseWindowManager;

    // ゲーム進行状態変更イベント
    [SerializeField, Header("ゲーム状態イベント")]
    private GameFlowStateChangedEvent m_gameFlowStateChangedEvent;

    // ゲームプレイモード変更イベント
    [SerializeField]
    private GameplayModeChangedEvent m_gameplayModeChangedEvent;

    // ポーズ状態変更イベント
    [SerializeField]
    private PauseChangedEvent m_pauseChangedEvent;

    // 現在のゲーム進行状態
    private GameFlowStateType m_currentGameFlowState =
        GameFlowStateType.NONE;

    // 現在のゲームプレイモード
    private GameplayModeType m_currentGameplayMode =
        GameplayModeType.NONE;

    // 現在ポーズ中か
    private bool m_isPaused = false;

    private void Awake()
    {
        HideInGameUI();
    }

    private void OnEnable()
    {
        RegisterEvents();
    }

    private void OnDisable()
    {
        UnregisterEvents();
    }

    /// <summary>
    /// HUDの表示状態を設定します。
    /// </summary>
    /// <param name="isVisible">
    /// 表示するか。
    /// </param>
    public void SetHudVisible(bool isVisible)
    {
        SetRootActive(
            m_hudRoot,
            isVisible);
    }

    /// <summary>
    /// チュートリアルUIの表示状態を設定します。
    /// </summary>
    /// <param name="isVisible">
    /// 表示するか。
    /// </param>
    public void SetTutorialVisible(bool isVisible)
    {
        SetRootActive(
            m_tutorialRoot,
            isVisible);
    }

    /// <summary>
    /// 独り言・セリフUIの表示状態を設定します。
    /// </summary>
    /// <param name="isVisible">
    /// 表示するか。
    /// </param>
    public void SetDialogueVisible(bool isVisible)
    {
        SetRootActive(
            m_dialogueRoot,
            isVisible);
    }

    /// <summary>
    /// リザルトUIの表示状態を設定します。
    /// </summary>
    /// <param name="isVisible">
    /// 表示するか。
    /// </param>
    public void SetResultVisible(bool isVisible)
    {
        SetRootActive(
            m_resultRoot,
            isVisible);
    }

    /// <summary>
    /// ゲームオーバーUIの表示状態を設定します。
    /// </summary>
    /// <param name="isVisible">
    /// 表示するか。
    /// </param>
    public void SetGameOverVisible(bool isVisible)
    {
        SetRootActive(
            m_gameOverRoot,
            isVisible);
    }

    /// <summary>
    /// 通常のインゲームUIをすべて非表示にします。
    /// </summary>
    public void HideInGameUI()
    {
        SetHudVisible(false);
        SetTutorialVisible(false);
        SetDialogueVisible(false);
        SetResultVisible(false);
        SetGameOverVisible(false);
    }

    /// <summary>
    /// 状態変更イベントを登録します。
    /// </summary>
    private void RegisterEvents()
    {
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

        if (m_gameplayModeChangedEvent != null)
        {
            m_gameplayModeChangedEvent.RegisterListener(
                OnGameplayModeChanged);
        }
        else
        {
            Debug.LogWarning(
                $"{nameof(GameplayModeChangedEvent)} が設定されていません。",
                this);
        }

        if (m_pauseChangedEvent != null)
        {
            m_pauseChangedEvent.RegisterListener(
                OnPauseChanged);
        }
        else
        {
            Debug.LogWarning(
                $"{nameof(PauseChangedEvent)} が設定されていません。",
                this);
        }
    }

    /// <summary>
    /// 状態変更イベントを解除します。
    /// </summary>
    private void UnregisterEvents()
    {
        if (m_gameFlowStateChangedEvent != null)
        {
            m_gameFlowStateChangedEvent.UnregisterListener(
                OnGameFlowStateChanged);
        }

        if (m_gameplayModeChangedEvent != null)
        {
            m_gameplayModeChangedEvent.UnregisterListener(
                OnGameplayModeChanged);
        }

        if (m_pauseChangedEvent != null)
        {
            m_pauseChangedEvent.UnregisterListener(
                OnPauseChanged);
        }
    }

    /// <summary>
    /// ゲーム進行状態の変更を受け取ります。
    /// </summary>
    /// <param name="eventData">
    /// ゲーム進行状態変更情報。
    /// </param>
    private void OnGameFlowStateChanged(
        GameFlowStateChangedEventData eventData)
    {
        m_currentGameFlowState =
            eventData.CurrentState;

        ApplyVisibility();
    }

    /// <summary>
    /// ゲームプレイモードの変更を受け取ります。
    /// </summary>
    /// <param name="eventData">
    /// ゲームプレイモード変更情報。
    /// </param>
    private void OnGameplayModeChanged(
        GameplayModeChangedEventData eventData)
    {
        m_currentGameplayMode =
            eventData.CurrentMode;

        ApplyVisibility();
    }

    /// <summary>
    /// ポーズ状態の変更を受け取ります。
    /// </summary>
    /// <param name="eventData">
    /// ポーズ状態変更情報。
    /// </param>
    private void OnPauseChanged(
        PauseChangedEventData eventData)
    {
        m_isPaused = eventData.IsPaused;

        if (m_isPaused)
        {
            EnterPause();
            return;
        }

        ExitPause();
    }

    /// <summary>
    /// ポーズ状態へ移行します。
    /// </summary>
    private void EnterPause()
    {
        // 通常のインゲームUIは隠す
        HideInGameUI();

        if (m_pauseWindowManager == null)
        {
            Debug.LogWarning(
                $"{nameof(UIWindowManager)} が設定されていません。",
                this);

            return;
        }

        m_pauseWindowManager.OpenInitialWindow();
    }

    /// <summary>
    /// ポーズ状態を終了します。
    /// </summary>
    private void ExitPause()
    {
        if (m_pauseWindowManager == null)
        {
            // WindowManagerが無くても通常UIは復帰させる
            ApplyVisibility();
            return;
        }

        // ポーズWindowの終了演出が完了してから
        // 通常ゲームUIを復帰させる
        m_pauseWindowManager.CloseAllWindows(
            ApplyVisibility);
    }

    /// <summary>
    /// 現在のゲーム状態に応じてUI表示状態を反映します。
    /// </summary>
    private void ApplyVisibility()
    {
        // PauseWindowの表示はOnPauseChanged側で管理する
        if (m_isPaused)
        {
            HideInGameUI();
            return;
        }

        switch (m_currentGameFlowState)
        {
            case GameFlowStateType.PLAYING:
                ApplyPlayingVisibility();
                break;

            case GameFlowStateType.RESULT:
                ApplyResultVisibility();
                break;

            case GameFlowStateType.GAME_OVER:
                ApplyGameOverVisibility();
                break;

            case GameFlowStateType.INTRO:
            case GameFlowStateType.ENDING:
            case GameFlowStateType.NONE:
            default:
                HideInGameUI();
                break;
        }
    }

    /// <summary>
    /// ゲームプレイ中のUI表示状態を設定します。
    /// </summary>
    private void ApplyPlayingVisibility()
    {
        switch (m_currentGameplayMode)
        {
            case GameplayModeType.NORMAL:
                ApplyNormalVisibility();
                break;

            case GameplayModeType.CINEMATIC:
                ApplyCinematicVisibility();
                break;

            case GameplayModeType.QTE:
                ApplyQteVisibility();
                break;

            case GameplayModeType.NONE:
            default:
                HideInGameUI();
                break;
        }
    }

    /// <summary>
    /// 通常ゲームプレイ中のUI表示状態を設定します。
    /// </summary>
    private void ApplyNormalVisibility()
    {
        HideInGameUI();

        SetHudVisible(true);
        SetTutorialVisible(true);
        SetDialogueVisible(true);
    }

    /// <summary>
    /// カットシーン中のUI表示状態を設定します。
    /// </summary>
    private void ApplyCinematicVisibility()
    {
        HideInGameUI();

        // カットシーン中は字幕・セリフ表示を許可
        SetDialogueVisible(true);
    }

    /// <summary>
    /// QTE中のUI表示状態を設定します。
    /// </summary>
    private void ApplyQteVisibility()
    {
        HideInGameUI();

        // QTE中もセリフ表示を許可
        SetDialogueVisible(true);
    }

    /// <summary>
    /// リザルト中のUI表示状態を設定します。
    /// </summary>
    private void ApplyResultVisibility()
    {
        HideInGameUI();

        SetResultVisible(true);
    }

    /// <summary>
    /// ゲームオーバー中のUI表示状態を設定します。
    /// </summary>
    private void ApplyGameOverVisibility()
    {
        HideInGameUI();

        SetGameOverVisible(true);
    }

    /// <summary>
    /// GameObjectの表示状態を設定します。
    /// </summary>
    /// <param name="root">
    /// 対象のGameObject。
    /// </param>
    /// <param name="isActive">
    /// 表示するか。
    /// </param>
    private void SetRootActive(
        GameObject root,
        bool isActive)
    {
        if (root == null)
        {
            return;
        }

        if (root.activeSelf == isActive)
        {
            return;
        }

        root.SetActive(isActive);
    }
}