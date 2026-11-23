using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

using System.Collections;

/// <summary>
/// ポーズ画面の表示演出と入力の有効状態を管理します。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(UIWindow))]
public class PauseFadeManager : MonoBehaviour, IUIInputControllable
{
    /// <summary>
    /// 表示状態に応じて切り替えるオブジェクトを設定します。
    /// </summary>
    [System.Serializable]
    private class ActiveSwitchSettings
    {
        [Tooltip("切り替え対象（このスクリプトのオブジェクト自身は指定しない）")]
        public GameObject[] m_targets;

        [Tooltip("フェードイン開始時に対象を有効にする")]
        public bool m_activateOnFadeIn = true;

        [Tooltip("フェードアウト終了後に対象を無効にする")]
        public bool m_deactivateOnFadeOut = true;

        [Tooltip("初回有効化時に初期表示状態を反映する")]
        public bool m_applyOnStart = true;
    }

    [Header("切替キー")]
    [SerializeField]
    private InputActionReference m_inputRef;

    [Header("ButtonSelector")]
    [SerializeField]
    private ButtonSelector m_buttonSelector;

    [Header("ポーズ要求イベント")]
    [SerializeField]
    private PauseRequestEvent m_pauseRequestEvent;

    [Header("メニューコンテキスト")]
    [SerializeField]
    private MenuOpenContext m_menuContext;

    [Header("ウィンドウ")]
    [SerializeField]
    private UIWindow m_window;

    [Header("Timeline")]
    [SerializeField]
    private PlayableDirector m_fadeInDirector;
    [SerializeField]
    private PlayableDirector m_fadeOutDirector;

    [Header("SetActive の切り替え")]
    [SerializeField]
    private ActiveSwitchSettings m_activeSwitch = new ActiveSwitchSettings();

    [Header("設定")]
    [SerializeField]
    private bool m_useUnscaledTime = true;
    [SerializeField]
    private bool m_playFadeInOnEnable = false;
    [SerializeField]
    private bool m_startShown = false;

    [Header("イベント")]
    [SerializeField]
    private UnityEvent m_onFadeOutCompleted = new UnityEvent();

    [Header("ポーズ状態の同期")]
    [SerializeField]
    private PauseChangedEvent m_pauseChangedEvent;
    // InGameUIController と同じものを指定する
    [SerializeField]
    private UIWindowManager m_pauseWindowManager;

    // フェードの進行状態
    private bool m_isFadingIn;
    private bool m_isFadingOut;
    private bool m_ignoreStopped;

    // 表示状態と初期化状態
    private bool m_isShown;
    private bool m_isDisplayCompleted;
    private bool m_initialized;

    // UIOverlaySceneManager によって許可された入力状態
    private bool m_isKeyInputEnabled;

    // 最後に届いた要求が「開く」か
    private bool m_wantOpen;
    // 開き直しの待機中か（コルーチンは UIWindowManager 側で動かす）
    private bool m_isReopenPending;

    /// <summary>
    /// 表示中、または表示開始済みかを取得します。
    /// </summary>
    public bool IsShown => m_isShown;

    /// <summary>
    /// フェード演出中かを取得します。
    /// </summary>
    public bool IsPlaying => m_isFadingIn || m_isFadingOut;

    private void Awake()
    {
        SetupDirector(m_fadeInDirector);
        SetupDirector(m_fadeOutDirector);

        m_buttonSelector.LockCursor();
        m_buttonSelector.ResetCursor();
    }

    private void OnEnable()
    {
        if (!m_initialized)
        {
            m_isShown = m_startShown;
            m_isDisplayCompleted = m_startShown;
            m_initialized = true;

            if (m_activeSwitch.m_applyOnStart)
            {
                SetTargetsActive(m_startShown);
            }
        }

        // 演出完了通知と入力イベントは有効期間中のみ登録する
        if (m_fadeInDirector != null)
        {
            m_fadeInDirector.stopped += OnFadeInStopped;
        }

        if (m_fadeOutDirector != null)
        {
            m_fadeOutDirector.stopped += OnFadeOutStopped;
        }

        if (m_inputRef != null && m_inputRef.action != null)
        {
            m_inputRef.action.performed += OnPause;
            m_inputRef.action.Enable();
        }

        // ポーズ状態の変更通知を受け取る
        if (m_pauseChangedEvent != null)
        {
            m_pauseChangedEvent.RegisterListener(OnPauseChanged);
        }

        UpdateUIInputState();

        if (m_playFadeInOnEnable)
        {
            PlayFadeIn();
        }
    }

    private void OnDisable()
    {
        if (m_fadeInDirector != null)
        {
            m_fadeInDirector.stopped -= OnFadeInStopped;
        }

        if (m_fadeOutDirector != null)
        {
            m_fadeOutDirector.stopped -= OnFadeOutStopped;
        }

        if (m_inputRef != null && m_inputRef.action != null)
        {
            m_inputRef.action.performed -= OnPause;
            // InputAction は他のコンポーネントと共有される可能性があるため無効化しない
        }

        // ポーズ状態の変更通知を解除する（追加）
        // 開き直しの待機は UIWindowManager 側で動くため、ここでは止めない
        if (m_pauseChangedEvent != null)
        {
            m_pauseChangedEvent.UnregisterListener(OnPauseChanged);
        }

        // 非アクティブ中に選択を受け付けない
        m_isFadingIn = false;
        m_isFadingOut = false;
        UpdateUIInputState();
    }

    /// <summary>
    /// オプション画面をメニューの遷移先に設定します。
    /// </summary>
    public void SetOptionMenu()
    {
        if (m_menuContext != null)
        {
            m_menuContext.SetOpenTarget(MenuOpenTargetType.OPTIONS);
        }
    }

    /// <summary>
    /// マニュアル画面をメニューの遷移先に設定します。
    /// </summary>
    public void SetManualMenu()
    {
        if (m_menuContext != null)
        {
            m_menuContext.SetOpenTarget(MenuOpenTargetType.MANUAL);
        }
    }

    /// <summary>
    /// ウィンドウが開いた際にゲーム時間を停止します。
    /// </summary>
    public void OnWindowOpened()
    {
        Time.timeScale = 0.0f;
    }

    /// <summary>
    /// ウィンドウが閉じた際にゲーム時間を戻します。
    /// </summary>
    public void OnWindowClosed()
    {
        Time.timeScale = 1.0f;
    }

    /// <summary>
    /// 背面 UI の入力を許可、または禁止します。
    /// </summary>
    /// <param name="isEnabled">入力を許可するか。</param>
    public void SetInputEnabled(bool isEnabled)
    {
        m_isKeyInputEnabled = isEnabled;
        UpdateUIInputState();
    }

    /// <summary>
    /// 表示状態に応じて UI 入力を切り替えます。
    /// </summary>
    private void UpdateUIInputState()
    {
        if (m_buttonSelector == null)
        {
            return;
        }

        // UI はフェードインが完了するまで操作不可にする
        bool canInteract = isActiveAndEnabled && m_isKeyInputEnabled &&
                           m_isShown && m_isDisplayCompleted && !IsPlaying;

        if (canInteract)
        {
            if (m_buttonSelector.IsLockCursor)
            {
                m_buttonSelector.UnLockCursor();
            }
        }
        else
        {
            m_buttonSelector.LockCursor();
        }
    }

    /// <summary>
    /// ポーズ状態の変更通知を受け取ります。
    /// </summary>
    private void OnPauseChanged(PauseChangedEventData eventData)
    {
        // 最後に届いた要求を覚えておく
        m_wantOpen = eventData.IsPaused;

        // フェードアウト中に「開く」要求が来たら、
        if (eventData.IsPaused && m_isFadingOut)
        {
            // フェードアウトを最後まで進めて、開き直す
            ForceFinishFadeOut();

            if (!m_isReopenPending && m_pauseWindowManager != null)
            {
                m_isReopenPending = true;
                m_pauseWindowManager.StartCoroutine(ReopenNextFrame());
            }
        }
    }

    /// <summary>
    /// フェードアウトを終了位置まで進めて、完了処理を実行します。
    /// </summary>
    private void ForceFinishFadeOut()
    {
        if (!m_isFadingOut)
        {
            return;
        }

        m_ignoreStopped = true;

        if (m_fadeOutDirector != null)
        {
            // 最終状態を反映してから止める
            m_fadeOutDirector.time = m_fadeOutDirector.duration;
            m_fadeOutDirector.Evaluate();
            m_fadeOutDirector.Stop();
        }

        m_ignoreStopped = false;

        // 通常の完了処理（NotifyHideCompleted まで流す）
        CompleteFadeOut();
    }

    /// <summary>
    /// 閉じ処理が落ち着いた次のフレームで、ウィンドウを開き直します。
    /// </summary>
    private IEnumerator ReopenNextFrame()
    {
        // timeScale が 0 でもフレームは進む
        yield return null;
        m_isReopenPending = false;

        // 途中でまた閉じる要求が来ていたら何もしない
        // すでに別の経路（InGameUIController）で開いていたら何もしない
        if (!m_wantOpen || m_isShown || IsPlaying)
        {
            yield break;
        }

        if (m_pauseWindowManager != null)
        {
            m_pauseWindowManager.OpenInitialWindow();
        }
    }

    /// <summary>
    /// キー入力時にウィンドウの開閉を要求します。
    /// </summary>
    private void OnPause(InputAction.CallbackContext context)
    {
        if (!context.performed || !m_isKeyInputEnabled || IsPlaying)
        {
            return;
        }

        if (!m_isShown)
        {
            PlayFadeIn();
            return;
        }

        // 表示が完了し、選択操作が解放されている場合のみ閉じる
        if (!m_isDisplayCompleted ||
            (m_buttonSelector != null && m_buttonSelector.IsLockCursor))
        {
            return;
        }

        // イベント側でウィンドウを閉じる場合もあるため、重複実行を防ぐ
        if (m_pauseRequestEvent != null)
        {
            m_pauseRequestEvent.RaiseResume();
        }

        if (isActiveAndEnabled && m_isShown && !IsPlaying)
        {
            PlayFadeOut();
        }
    }

    /// <summary>
    /// 表示と非表示を切り替えます。
    /// </summary>
    public void Toggle()
    {
        if (IsPlaying)
        {
            return;
        }

        if (m_isShown)
        {
            PlayFadeOut();
        }
        else
        {
            PlayFadeIn();
        }
    }

    /// <summary>
    /// フェードイン演出を開始します。
    /// </summary>
    public void PlayFadeIn()
    {
        if (m_isFadingIn || m_isFadingOut)
        {
            return;
        }

        Debug.Log($"[Pause] FadeIn START t={Time.unscaledTime:F2} duration={(m_fadeInDirector != null ? m_fadeInDirector.duration : -1):F2}");

        m_isShown = true;
        m_isDisplayCompleted = false;
        m_isFadingIn = true;

        // 表示対象を先に有効にし、選択カーソルをロックしたまま初期化する
        UpdateUIInputState();

        if (m_activeSwitch.m_activateOnFadeIn)
        {
            SetTargetsActive(true);
        }

        if (m_buttonSelector != null)
        {
            m_buttonSelector.ResetCursor();
        }

        // ResetCursor がロック状態を変更しても、演出中は必ず入力を禁止する
        UpdateUIInputState();

        if (m_fadeInDirector == null)
        {
            CompleteFadeIn();
            return;
        }

        PlayDirector(m_fadeInDirector, m_fadeOutDirector);
    }

    /// <summary>
    /// フェードアウト演出を開始します。
    /// </summary>
    public void PlayFadeOut()
    {
        if (m_isFadingOut || (!m_isShown && !m_isFadingIn))
        {
            return;
        }

        m_isShown = false;
        m_isDisplayCompleted = false;
        m_isFadingIn = false;
        m_isFadingOut = true;

        // 非表示演出が始まる時点で UI 入力を止める
        UpdateUIInputState();

        if (m_fadeOutDirector == null)
        {
            CompleteFadeOut();
            return;
        }

        PlayDirector(m_fadeOutDirector, m_fadeInDirector);
    }

    /// <summary>
    /// 実行中のフェード演出を中断します。
    /// </summary>
    public void StopAll()
    {
        m_isFadingIn = false;
        m_isFadingOut = false;
        m_isDisplayCompleted = false;
        UpdateUIInputState();

        m_ignoreStopped = true;

        if (m_fadeInDirector != null)
        {
            m_fadeInDirector.Stop();
        }

        if (m_fadeOutDirector != null)
        {
            m_fadeOutDirector.Stop();
        }

        m_ignoreStopped = false;
    }

    /// <summary>
    /// フェードインが正常終了した場合の後処理を行います。
    /// </summary>
    private void OnFadeInStopped(PlayableDirector director)
    {
        if (m_ignoreStopped || !m_isFadingIn)
        {
            return;
        }

        CompleteFadeIn();
    }

    /// <summary>
    /// フェードアウトが正常終了した場合の後処理を行います。
    /// </summary>
    private void OnFadeOutStopped(PlayableDirector director)
    {
        if (m_ignoreStopped || !m_isFadingOut)
        {
            return;
        }

        CompleteFadeOut();
    }

    /// <summary>
    /// 表示完了を通知してから UI 入力を許可します。
    /// </summary>
    private void CompleteFadeIn()
    {
        Debug.Log($"[Pause] FadeIn COMPLETE t={Time.unscaledTime:F2}");

        m_isFadingIn = false;
        m_isDisplayCompleted = true;

        if (m_window != null)
        {
            m_window.NotifyShowCompleted();
        }

        //UpdateUIInputState();
    }

    /// <summary>
    /// 非表示完了を通知し、対象オブジェクトを無効にします。
    /// </summary>
    private void CompleteFadeOut()
    {
        m_isFadingOut = false;
        UpdateUIInputState();

        if (m_activeSwitch.m_deactivateOnFadeOut)
        {
            SetTargetsActive(false);
        }

        m_onFadeOutCompleted?.Invoke();

        if (m_window != null)
        {
            m_window.NotifyHideCompleted();
        }
    }

    /// <summary>
    /// 指定されたオブジェクトの有効状態を切り替えます。
    /// </summary>
    private void SetTargetsActive(bool isActive)
    {
        if (m_activeSwitch == null || m_activeSwitch.m_targets == null)
        {
            return;
        }

        foreach (GameObject target in m_activeSwitch.m_targets)
        {
            if (target == null)
            {
                continue;
            }

            if (target == gameObject)
            {
                Debug.LogWarning($"{name}: 自分自身を切り替え対象に指定できません。", this);
                continue;
            }

            target.SetActive(isActive);
        }
    }

    /// <summary>
    /// 反対側の演出を止め、指定された Timeline を先頭から再生します。
    /// </summary>
    private void PlayDirector(PlayableDirector target, PlayableDirector other)
    {
        m_ignoreStopped = true;

        if (other != null)
        {
            other.Stop();
        }

        target.time = 0.0;
        target.Evaluate();
        m_ignoreStopped = false;

        target.Play();
    }

    /// <summary>
    /// Timeline の更新方法と終了動作を設定します。
    /// </summary>
    private void SetupDirector(PlayableDirector director)
    {
        if (director == null)
        {
            return;
        }

        director.playOnAwake = false;
        director.extrapolationMode = DirectorWrapMode.None;
        director.timeUpdateMode = m_useUnscaledTime
            ? DirectorUpdateMode.UnscaledGameTime
            : DirectorUpdateMode.GameTime;
    }
}