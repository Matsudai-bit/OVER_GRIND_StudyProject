using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ゲーム状態に応じてAction Mapの有効状態を管理します。
/// </summary>
[DisallowMultipleComponent]
public sealed class GameInputController : MonoBehaviour
{
    // デフォルトのAction Map名
    private const string DEFAULT_PLAYER_ACTION_MAP_NAME = "Player";
    private const string DEFAULT_SYSTEM_ACTION_MAP_NAME = "System";
    private const string DEFAULT_UI_ACTION_MAP_NAME = "UI";
    private const string DEFAULT_QTE_ACTION_MAP_NAME = "QTE";

    // Input Action Asset
    [SerializeField, Header("Input Action")]
    private InputActionAsset m_inputActionAsset;

    // Player用Action Map名
    [SerializeField, Header("Action Map名")]
    private string m_playerActionMapName = DEFAULT_PLAYER_ACTION_MAP_NAME;

    // System用Action Map名
    [SerializeField]
    private string m_systemActionMapName = DEFAULT_SYSTEM_ACTION_MAP_NAME;

    // UI用Action Map名
    [SerializeField]
    private string m_uiActionMapName = DEFAULT_UI_ACTION_MAP_NAME;

    // QTE用Action Map名
    [SerializeField]
    private string m_qteActionMapName = DEFAULT_QTE_ACTION_MAP_NAME;

    // ゲーム進行状態変更イベント
    [SerializeField, Header("ゲーム状態イベント")]
    private GameFlowStateChangedEvent m_gameFlowStateChangedEvent;

    // ゲームプレイモード変更イベント
    [SerializeField]
    private GameplayModeChangedEvent m_gameplayModeChangedEvent;

    // ポーズ状態変更イベント
    [SerializeField]
    private PauseChangedEvent m_pauseChangedEvent;

    // Player用Action Map
    private InputActionMap m_playerActionMap;

    // System用Action Map
    private InputActionMap m_systemActionMap;

    // UI用Action Map
    private InputActionMap m_uiActionMap;

    // QTE用Action Map
    private InputActionMap m_qteActionMap;

    // 現在のゲーム進行状態
    private GameFlowStateType m_currentGameFlowState = GameFlowStateType.NONE;

    // 現在のゲームプレイモード
    private GameplayModeType m_currentGameplayMode = GameplayModeType.NONE;

    // 現在ポーズ中か
    private bool m_isPaused = false;

    // Action Mapの初期化が完了しているか
    private bool m_isInitialized = false;

    private void Awake()
    {
        m_isInitialized = CacheActionMaps();
    }

    private void OnEnable()
    {
        RegisterEvents();
    }

    private void Start()
    {
        if (!m_isInitialized)
        {
            return;
        }

        ApplyInputState();
    }

    private void OnDisable()
    {
        UnregisterEvents();

        if (!m_isInitialized)
        {
            return;
        }

        DisableAllActionMaps();
    }

    /// <summary>
    /// 現在のゲーム状態からAction Mapの状態を再設定します。
    /// </summary>
    public void RefreshInputState()
    {
        if (!m_isInitialized)
        {
            return;
        }

        ApplyInputState();
    }

    /// <summary>
    /// 使用するAction Mapを取得します。
    /// </summary>
    /// <returns>
    /// true：すべて取得できました。
    /// false：取得に失敗しました。
    /// </returns>
    private bool CacheActionMaps()
    {
        if (m_inputActionAsset == null)
        {
            Debug.LogError(
                $"{nameof(InputActionAsset)} が設定されていません。",
                this);

            return false;
        }

        m_playerActionMap = FindActionMap(m_playerActionMapName);
        m_systemActionMap = FindActionMap(m_systemActionMapName);
        m_uiActionMap = FindActionMap(m_uiActionMapName);
        m_qteActionMap = FindActionMap(m_qteActionMapName);

        return m_playerActionMap != null
            && m_systemActionMap != null
            && m_uiActionMap != null
            && m_qteActionMap != null;
    }

    /// <summary>
    /// 指定した名前のAction Mapを取得します。
    /// </summary>
    /// <param name="actionMapName">Action Map名。</param>
    /// <returns>取得したAction Map。</returns>
    private InputActionMap FindActionMap(string actionMapName)
    {
        if (string.IsNullOrWhiteSpace(actionMapName))
        {
            Debug.LogError(
                "Action Map名が設定されていません。",
                this);

            return null;
        }

        InputActionMap actionMap =
            m_inputActionAsset.FindActionMap(
                actionMapName,
                false);

        if (actionMap == null)
        {
            Debug.LogError(
                $"Action Mapが見つかりません。Name: {actionMapName}",
                this);
        }

        return actionMap;
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
    /// ゲーム進行状態変更を処理します。
    /// </summary>
    /// <param name="eventData">ゲーム進行状態変更情報。</param>
    private void OnGameFlowStateChanged(
        GameFlowStateChangedEventData eventData)
    {
        m_currentGameFlowState = eventData.CurrentState;

        ApplyInputState();
    }

    /// <summary>
    /// ゲームプレイモード変更を処理します。
    /// </summary>
    /// <param name="eventData">ゲームプレイモード変更情報。</param>
    private void OnGameplayModeChanged(
        GameplayModeChangedEventData eventData)
    {
        m_currentGameplayMode = eventData.CurrentMode;

        ApplyInputState();
    }

    /// <summary>
    /// ポーズ状態変更を処理します。
    /// </summary>
    /// <param name="eventData">ポーズ状態変更情報。</param>
    private void OnPauseChanged(
        PauseChangedEventData eventData)
    {
        m_isPaused = eventData.IsPaused;

        ApplyInputState();
    }

    /// <summary>
    /// 現在のゲーム状態からAction Mapを設定します。
    /// </summary>
    private void ApplyInputState()
    {
        if (!m_isInitialized)
        {
            return;
        }

        bool enablePlayer = false;
        bool enableSystem = true;
        bool enableUI = false;
        bool enableQTE = false;

        // ポーズ状態を最優先
        if (m_isPaused)
        {
            enableUI = true;

            ApplyActionMaps(
                enablePlayer,
                enableSystem,
                enableUI,
                enableQTE);

            return;
        }

        switch (m_currentGameFlowState)
        {
            case GameFlowStateType.PLAYING:
                GetGameplayInputState(
                    out enablePlayer,
                    out enableQTE);
                break;

            case GameFlowStateType.RESULT:
                enableUI = true;
                break;

            case GameFlowStateType.INTRO:
            case GameFlowStateType.ENDING:
            case GameFlowStateType.NONE:
            default:
                break;
        }

        ApplyActionMaps(
            enablePlayer,
            enableSystem,
            enableUI,
            enableQTE);
    }

    /// <summary>
    /// GameplayModeからゲームプレイ中の入力状態を取得します。
    /// </summary>
    /// <param name="enablePlayer">Player Mapを有効にするか。</param>
    /// <param name="enableQTE">QTE Mapを有効にするか。</param>
    private void GetGameplayInputState(
        out bool enablePlayer,
        out bool enableQTE)
    {
        enablePlayer = false;
        enableQTE = false;

        switch (m_currentGameplayMode)
        {
            case GameplayModeType.NORMAL:
                enablePlayer = true;
                break;

            case GameplayModeType.QTE:
                enableQTE = true;
                break;

            case GameplayModeType.CINEMATIC:
            case GameplayModeType.NONE:
            default:
                break;
        }
    }

    /// <summary>
    /// 各Action Mapの有効状態を反映します。
    /// </summary>
    /// <param name="enablePlayer">Player Mapを有効にするか。</param>
    /// <param name="enableSystem">System Mapを有効にするか。</param>
    /// <param name="enableUI">UI Mapを有効にするか。</param>
    /// <param name="enableQTE">QTE Mapを有効にするか。</param>
    private void ApplyActionMaps(
        bool enablePlayer,
        bool enableSystem,
        bool enableUI,
        bool enableQTE)
    {
        SetActionMapEnabled(
            m_playerActionMap,
            enablePlayer);

        SetActionMapEnabled(
            m_systemActionMap,
            enableSystem);

        SetActionMapEnabled(
            m_uiActionMap,
            enableUI);

        SetActionMapEnabled(
            m_qteActionMap,
            enableQTE);
    }

    /// <summary>
    /// Action Mapの有効状態を設定します。
    /// </summary>
    /// <param name="actionMap">対象のAction Map。</param>
    /// <param name="isEnabled">有効にするか。</param>
    private void SetActionMapEnabled(
        InputActionMap actionMap,
        bool isEnabled)
    {
        if (actionMap == null)
        {
            return;
        }

        if (actionMap.enabled == isEnabled)
        {
            return;
        }

        if (isEnabled)
        {
            actionMap.Enable();
        }
        else
        {
            actionMap.Disable();
        }
    }

    /// <summary>
    /// 管理しているAction Mapをすべて無効化します。
    /// </summary>
    private void DisableAllActionMaps()
    {
        SetActionMapEnabled(m_playerActionMap, false);
        SetActionMapEnabled(m_systemActionMap, false);
        SetActionMapEnabled(m_uiActionMap, false);
        SetActionMapEnabled(m_qteActionMap, false);
    }
}