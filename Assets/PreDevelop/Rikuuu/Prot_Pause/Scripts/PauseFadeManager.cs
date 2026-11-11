using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(UIWindow))]
public class PauseFadeManager : MonoBehaviour, IUIInputControllable
{
    // SetActive の切り替えに関する設定(Inspector でグループとして表示されます)
    [System.Serializable]
    private class ActiveSwitchSettings
    {
        [Tooltip("切り替える対象。このスクリプトが付いているオブジェクト自身は指定しないでください")]
        public GameObject[] m_targets;

        [Tooltip("フェードイン開始時に対象を有効にするか")]
        public bool m_activateOnFadeIn = true;

        [Tooltip("フェードアウト終了後に対象を無効にするか")]
        public bool m_deactivateOnFadeOut = true;

        [Tooltip("開始時に、表示状態(Start Shown)に合わせて対象の有効・無効を反映するか")]
        public bool m_applyOnStart = true;
    }

    [Header("切替キー")]
    [SerializeField]
    private InputActionReference m_inputRef;

    [Header("ButtonSelector")]
    [SerializeField]
    private ButtonSelector m_buttonSelector;

    // ポーズ要求イベント
    [Header("ポーズ要求イベント")]
    [SerializeField]
    private PauseRequestEvent m_pauseRequestEvent;
    [Header("メニューコンテキスト")]
    [SerializeField]
    private MenuOpenContext m_menuContext;
    // 管理対象ウィンドウ
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
    // ポーズ中(Time.timeScale = 0)でも再生するか
    [SerializeField]
    private bool m_useUnscaledTime = true;
    // 有効化時に自動でフェードインを再生するか
    [SerializeField]
    private bool m_playFadeInOnEnable = false;
    // 開始時点で表示された状態にするか(キー切り替えの初期状態)
    [SerializeField]
    private bool m_startShown = false;

    [Header("イベント")]
    // フェードアウト終了時に通知するイベント(UIWindow.NotifyHideCompleted を登録していても動作します)
    [SerializeField]
    private UnityEvent m_onFadeOutCompleted = new();

    // フェードイン再生中かどうか(終了検知用)
    private bool m_isFadingIn = false;
    // フェードアウト再生中かどうか(終了検知用)
    private bool m_isFadingOut = false;
    // Play() / StopAll() 内の Stop() で発生する stopped を無視するためのフラグ
    private bool m_ignoreStopped = false;
    // 表示状態かどうか(キー切り替え用)
    private bool m_isShown = false;
    // 初期状態を反映したかどうか
    private bool m_initialized = false;

    // キー入力が有効かどうか
    private bool m_isKeyInputEnabled = false;

    // 現在表示されているかどうか
    public bool IsShown => m_isShown;

    public void SetOptionMenu()
    {
        m_menuContext.SetOpenTarget(MenuOpenTargetType.OPTIONS);
    }

    public void SetManualMenu()
    {
        m_menuContext.SetOpenTarget(MenuOpenTargetType.MANUAL);
    }

    private void Awake()
    {
        SetupDirector(m_fadeInDirector);
        SetupDirector(m_fadeOutDirector);
    }

    // 呼び出される関数
    private void OnPause(InputAction.CallbackContext context)
    {
        if (context.performed && !m_buttonSelector.IsLockCursor)
        {
            m_pauseRequestEvent.RaiseResume();
        }
    }

    private void OnEnable()
    {
        // 初回だけ初期状態を反映する
        if (!m_initialized)
        {
            m_isShown = m_startShown;
            m_initialized = true;

            // 開始時の有効・無効を反映する
            if (m_activeSwitch.m_applyOnStart)
            {
                SetTargetsActive(m_startShown);
            }
        }

        // フェードイン・フェードアウト終了イベントを登録
        if (m_fadeInDirector != null)
        {
            m_fadeInDirector.stopped += OnFadeInStopped;
        }
        if (m_fadeOutDirector != null)
        {
            m_fadeOutDirector.stopped += OnFadeOutStopped;
        }

        // 切替キーの入力を有効にする
        if (m_inputRef != null)
        {
            m_inputRef.action.Enable();
        }

        if (m_playFadeInOnEnable)
        {
            PlayFadeIn();
        }
    }

    private void OnDisable()
    {
        // イベントを解除
        if (m_fadeInDirector != null)
        {
            m_fadeInDirector.stopped -= OnFadeInStopped;
        }
        if (m_fadeOutDirector != null)
        {
            m_fadeOutDirector.stopped -= OnFadeOutStopped;
        }

        if (m_inputRef != null)
        {
            m_inputRef.action.performed -= OnPause;
        }

        m_isFadingIn = false;
        m_isFadingOut = false;
    }

    private void Update()
    {
        if (m_inputRef == null || !m_isKeyInputEnabled)
        {
            return;
        }

        // キーが押された瞬間に切り替える
        if (m_inputRef.action.WasPressedThisFrame())
        {
            Toggle();
        }
    }

    // 背面UIとして入力を止める・戻す(UIOverlaySceneManager から呼ばれます)
    public void SetInputEnabled(bool isEnabled)
    {
        m_isKeyInputEnabled = isEnabled;

        if (m_buttonSelector == null)
        {
            return;
        }

        if (isEnabled)
        {
            // ポーズ画面が表示中のときだけカーソルを戻す
            if (m_isShown)
            {
                m_buttonSelector.UnLockCursor();
            }
        }
        else
        {
            m_buttonSelector.LockCursor();
        }
    }

    // 表示・非表示を切り替える(UnityEvent からも呼べます)
    public void Toggle()
    {
        // フェード再生中は切り替えない(連打対策)
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

    // フェードインを再生
    public void PlayFadeIn()
    {
        // フェードアウト中は開始しない
        if (m_isFadingOut)
        {
            return;
        }

        m_isShown = true;

        // 先に対象を有効にする(無効のままだと Timeline やカーソルが動かないため)
        if (m_activeSwitch.m_activateOnFadeIn)
        {
            SetTargetsActive(true);
        }

        if (m_buttonSelector != null)
        {
            // ボタンセレクターのリセット
            m_buttonSelector.ResetCursor();

            // ボタンセレクターのロック解除
            m_buttonSelector.UnLockCursor();
        }

        // 終了検知のフラグは、Play() 内の Stop() の影響を受けないよう Play() の前に立てる
        m_isFadingIn = true;
        Play(m_fadeInDirector, m_fadeOutDirector);
    }

    // フェードアウトを再生
    public void PlayFadeOut()
    {
        Debug.Log("PlayFadeOut が呼ばれました");

        m_isShown = false;
        m_isFadingIn = false;
        m_isFadingOut = true;

        // フェードアウト開始でポーズ入力を解除する
        if (m_inputRef != null)
        {
            m_inputRef.action.performed -= OnPause;
        }

        Play(m_fadeOutDirector, m_fadeInDirector);
    }

    // 再生中のフェードを止めて、最初の状態に戻す
    public void StopAll()
    {
        m_isFadingIn = false;
        m_isFadingOut = false;

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

    // フェードが再生中かどうか
    public bool IsPlaying
    {
        get
        {
            return m_isFadingIn || m_isFadingOut;
        }
    }

    // ----------------------------------------------------------------------

    // フェードインが終わったときに呼ばれる
    private void OnFadeInStopped(PlayableDirector director)
    {
        // 途中で中断された場合や、Play() 内の Stop() では何もしない
        if (m_ignoreStopped || !m_isFadingIn)
        {
            return;
        }
        m_isFadingIn = false;

        // ポーズ入力を有効にする(二重登録を避けるため先に解除する)
        if (m_inputRef != null)
        {
            m_inputRef.action.performed -= OnPause;
            m_inputRef.action.performed += OnPause;
        }

        // フェードイン終了を通知する(UIWindow 側の表示完了処理を呼ぶ)
        if (m_window != null)
        {
            m_window.NotifyShowCompleted();
        }

        Debug.Log("FadeIn 終了を検知しました");
    }

    // フェードアウトが終わったときに呼ばれる
    private void OnFadeOutStopped(PlayableDirector director)
    {
        // 途中で中断された場合や、Play() 内の Stop() では何もしない
        if (m_ignoreStopped || !m_isFadingOut)
        {
            return;
        }
        m_isFadingOut = false;

        // ボタンセレクターのロック(有効なうちに行う)
        if (m_buttonSelector != null)
        {
            m_buttonSelector.LockCursor();
        }

        // 最後に対象を無効にする
        if (m_activeSwitch.m_deactivateOnFadeOut)
        {
            SetTargetsActive(false);
        }

        m_onFadeOutCompleted?.Invoke();

        // フェードアウト終了を通知する(UIWindow 側の非表示完了処理を呼ぶ)
        if (m_window != null)
        {
            m_window.NotifyHideCompleted();
        }

        Debug.Log("FadeOut 終了を検知しました");
    }

    // 対象の有効・無効をまとめて切り替える
    private void SetTargetsActive(bool isActive)
    {
        Debug.Log("SetActive" + isActive);

        if (m_activeSwitch.m_targets == null)
        {
            return;
        }

        for (int i = 0; i < m_activeSwitch.m_targets.Length; i++)
        {
            GameObject target = m_activeSwitch.m_targets[i];
            if (target == null)
            {
                continue;
            }

            // 自分自身を無効にすると Update が止まって戻せなくなるため除外する
            if (target == gameObject)
            {
                Debug.LogWarning($"{gameObject.name}: 自分自身は切り替え対象に指定できません。", this);
                continue;
            }

            target.SetActive(isActive);
        }
    }

    private void Play(PlayableDirector target, PlayableDirector other)
    {
        if (target == null)
        {
            Debug.LogWarning($"{gameObject.name}: PlayableDirector が設定されていません。", this);
            return;
        }

        Debug.Log($"{target.name} 再生開始 duration={target.duration} updateMode={target.timeUpdateMode}");

        // 準備のための Stop() では stopped を無視する
        m_ignoreStopped = true;

        // 逆方向のフェードが再生中なら止める
        if (other != null)
        {
            other.Stop();
        }

        // 最初から再生する
        target.Stop();
        target.time = 0;
        target.Evaluate();

        m_ignoreStopped = false;

        target.Play();
    }

    // 設定の適用
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