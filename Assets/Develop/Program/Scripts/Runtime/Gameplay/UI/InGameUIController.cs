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

    [SerializeField]
    private GameObject m_resultRoot;

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
        HideAll();
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
    /// <param name="isVisible">表示するか。</param>
    public void SetHudVisible(bool isVisible)
    {
        SetRootActive(
            m_hudRoot,
            isVisible);
    }

    public void SetResultVisible(bool isVisible)
    {
        SetRootActive(m_resultRoot, isVisible);
    }

    /// <summary>
    /// チュートリアルUIの表示状態を設定します。
    /// </summary>
    /// <param name="isVisible">表示可能にするか。</param>
    public void SetTutorialVisible(bool isVisible)
    {
        SetRootActive(
            m_tutorialRoot,
            isVisible);
    }

    /// <summary>
    /// 独り言・セリフUIの表示状態を設定します。
    /// </summary>
    /// <param name="isVisible">表示可能にするか。</param>
    public void SetDialogueVisible(bool isVisible)
    {
        SetRootActive(
            m_dialogueRoot,
            isVisible);
    }

    /// <summary>
    /// インゲームUIをすべて非表示にします。
    /// </summary>
    public void HideAll()
    {
        SetHudVisible(false);
        SetTutorialVisible(false);
        SetDialogueVisible(false);
        SetResultVisible(false);
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

        if (m_gameplayModeChangedEvent != null)
        {
            m_gameplayModeChangedEvent.RegisterListener(
                OnGameplayModeChanged);
        }

        if (m_pauseChangedEvent != null)
        {
            m_pauseChangedEvent.RegisterListener(
                OnPauseChanged);
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
    /// <param name="eventData">ゲーム進行状態変更情報。</param>
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
    /// <param name="eventData">ゲームプレイモード変更情報。</param>
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
    /// <param name="eventData">ポーズ状態変更情報。</param>
    private void OnPauseChanged(
        PauseChangedEventData eventData)
    {
        m_isPaused = eventData.IsPaused;

        ApplyVisibility();
    }

    /// <summary>
    /// 現在のゲーム状態に応じてUI表示状態を反映します。
    /// </summary>
    private void ApplyVisibility()
    {
        // ポーズ中はインゲームUIを非表示
        if (m_isPaused)
        {
            HideAll();
            return;
        }

        // RESULTではリザルトUIのみ表示
        if (m_currentGameFlowState ==
            GameFlowStateType.RESULT)
        {
            ApplyResultVisibility();
            return;
        }

        // PLAYING以外ではインゲームUIを非表示
        if (m_currentGameFlowState !=
            GameFlowStateType.PLAYING)
        {
            HideAll();
            return;
        }

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
                HideAll();
                break;
        }
    }

    /// <summary>
    /// 通常ゲームプレイ中のUI表示状態を設定します。
    /// </summary>
    private void ApplyNormalVisibility()
    {
        SetHudVisible(true);
        SetTutorialVisible(true);
        SetDialogueVisible(true);
        SetResultVisible(false);
    }

    /// <summary>
    /// カットシーン中のUI表示状態を設定します。
    /// </summary>
    private void ApplyCinematicVisibility()
    {
        SetHudVisible(false);
        SetTutorialVisible(false);

        // カットシーン中の字幕・セリフ表示は許可
        SetDialogueVisible(true);
        SetResultVisible(false);

    }

    /// <summary>
    /// QTE中のUI表示状態を設定します。
    /// </summary>
    private void ApplyQteVisibility()
    {
        SetHudVisible(false);
        SetTutorialVisible(false);

        // QTE中もセリフ表示は許可
        SetDialogueVisible(true);
        SetResultVisible(false);

    }

    /// <summary>
    /// 通常ゲームプレイ中のUI表示状態を設定します。
    /// </summary>
    private void ApplyResultVisibility()
    {
        HideAll();
        SetResultVisible(true);
    }

    /// <summary>
    /// GameObjectの表示状態を設定します。
    /// </summary>
    /// <param name="root">対象のGameObject。</param>
    /// <param name="isActive">表示するか。</param>
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