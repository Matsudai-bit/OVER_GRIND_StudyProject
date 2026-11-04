using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// ポーズウィンドウ固有の表示演出とカーソル制御を行います。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(UIWindow))]
public sealed class PauseWindowController : MonoBehaviour
{
    // 管理対象ウィンドウ
    [SerializeField, Header("ウィンドウ")]
    private UIWindow m_window;

    // 表示Timeline
    [SerializeField, Header("表示演出")]
    private PlayableDirector m_fadeInDirector;

    // 非表示Timeline
    [SerializeField]
    private PlayableDirector m_fadeOutDirector;

    // 詳細テキスト
    [SerializeField, Header("UI")]
    private GameObject m_detailText;

    // UIコンテナ
    [SerializeField]
    private GameObject m_container;

    // 背景
    [SerializeField]
    private GameObject m_background;

    // 選択ボタン
    [SerializeField]
    private GameObject m_selectButton;

    // メカパネル
    [SerializeField]
    private GameObject m_mechPanel;

    // ボタン選択管理
    [SerializeField, Header("Button Selector")]
    private ButtonSelector m_buttonSelector;

    // カーソル
    [SerializeField, Header("Cursor")]
    private GameObject m_cursor;

    // カーソル初期位置
    [SerializeField]
    private Vector2 m_cursorInitialPosition;

    private void Awake()
    {
        InitializeReferences();
        InitializeDirectors();
        InitializeUI();
    }

    /// <summary>
    /// UIWindowが開かれたときに呼び出します。
    /// </summary>
    public void OnWindowOpened()
    {
        PrepareCursor();

        SetUIActive(true);
    }

    /// <summary>
    /// UIWindowの表示開始時に呼び出します。
    /// </summary>
    public void OnWindowShow()
    {
        StopFadeOut();

        PrepareCursor();

        PlayFadeIn();
    }

    /// <summary>
    /// FadeIn Timelineの終了時に呼び出します。
    /// </summary>
    public void OnFadeInFinished()
    {
        PreserveCursorPosition();

        if (m_fadeInDirector != null)
        {
            m_fadeInDirector.Pause();
        }

        if (m_buttonSelector != null)
        {
            m_buttonSelector.UnLockCursorAnimation();
        }

        // UIWindowへ表示演出の完了を通知
        if (m_window != null)
        {
            m_window.NotifyShowCompleted();
        }
    }

    /// <summary>
    /// UIWindowの入力が有効になったときに呼び出します。
    /// </summary>
    public void OnWindowInputEnabled()
    {
        if (m_buttonSelector != null)
        {
            m_buttonSelector.UnLockCursor();
        }

        SetCursorInteraction(true);
    }

    /// <summary>
    /// UIWindowの入力が無効になったときに呼び出します。
    /// </summary>
    public void OnWindowInputDisabled()
    {
        if (m_buttonSelector != null)
        {
            m_buttonSelector.LockCursor();
        }

        SetCursorInteraction(false);
    }

    /// <summary>
    /// UIWindowの非表示開始時に呼び出します。
    /// </summary>
    public void OnWindowHide()
    {
        StopFadeIn();

        if (m_buttonSelector != null)
        {
            m_buttonSelector.LockCursorAnimation();
        }

        PlayFadeOut();
    }

    /// <summary>
    /// FadeOut Timelineの終了時に呼び出します。
    /// </summary>
    public void OnFadeOutFinished()
    {
        SetCursorInteraction(false);

        if (m_buttonSelector != null)
        {
            m_buttonSelector.LockCursor();
        }

        // UIWindowへ非表示演出の完了を通知
        if (m_window != null)
        {
            m_window.NotifyHideCompleted();
        }
    }

    /// <summary>
    /// UIWindowが完全に閉じたときに呼び出します。
    /// </summary>
    public void OnWindowClosed()
    {
        SetUIActive(false);

        if (m_buttonSelector != null)
        {
            m_buttonSelector.LockCursor();
            m_buttonSelector.LockCursorAnimation();
        }
    }

    /// <summary>
    /// 必要な参照を初期化します。
    /// </summary>
    private void InitializeReferences()
    {
        if (m_window == null)
        {
            m_window = GetComponent<UIWindow>();
        }
    }

    /// <summary>
    /// Timelineの初期状態を設定します。
    /// </summary>
    private void InitializeDirectors()
    {
        InitializeDirector(m_fadeInDirector);
        InitializeDirector(m_fadeOutDirector);
    }

    /// <summary>
    /// PlayableDirectorを初期化します。
    /// </summary>
    /// <param name="director">初期化するDirector。</param>
    private void InitializeDirector(
        PlayableDirector director)
    {
        if (director == null)
        {
            return;
        }

        director.playOnAwake = false;

        director.timeUpdateMode =
            DirectorUpdateMode.UnscaledGameTime;

        director.extrapolationMode =
            DirectorWrapMode.Hold;

        director.Stop();
        director.time = 0.0;
        director.Evaluate();
    }

    /// <summary>
    /// UIの初期状態を設定します。
    /// </summary>
    private void InitializeUI()
    {
        SetUIActive(false);

        if (m_buttonSelector != null)
        {
            m_buttonSelector.LockCursor();
            m_buttonSelector.LockCursorAnimation();
        }

        SetCursorInteraction(false);
    }

    /// <summary>
    /// FadeIn演出を開始します。
    /// </summary>
    private void PlayFadeIn()
    {
        if (m_fadeInDirector == null)
        {
            if (m_window != null)
            {
                m_window.NotifyShowCompleted();
            }

            return;
        }

        m_fadeInDirector.Stop();
        m_fadeInDirector.time = 0.0;
        m_fadeInDirector.Evaluate();
        m_fadeInDirector.Play();
    }

    /// <summary>
    /// FadeOut演出を開始します。
    /// </summary>
    private void PlayFadeOut()
    {
        if (m_fadeOutDirector == null)
        {
            if (m_window != null)
            {
                m_window.NotifyHideCompleted();
            }

            return;
        }

        m_fadeOutDirector.Stop();
        m_fadeOutDirector.time = 0.0;
        m_fadeOutDirector.Evaluate();
        m_fadeOutDirector.Play();
    }

    /// <summary>
    /// FadeIn演出を停止します。
    /// </summary>
    private void StopFadeIn()
    {
        if (m_fadeInDirector == null)
        {
            return;
        }

        m_fadeInDirector.Stop();
        m_fadeInDirector.time = 0.0;
    }

    /// <summary>
    /// FadeOut演出を停止します。
    /// </summary>
    private void StopFadeOut()
    {
        if (m_fadeOutDirector == null)
        {
            return;
        }

        m_fadeOutDirector.Stop();
        m_fadeOutDirector.time = 0.0;
    }

    /// <summary>
    /// カーソルを表示開始時の状態へ設定します。
    /// </summary>
    private void PrepareCursor()
    {
        if (m_buttonSelector != null)
        {
            m_buttonSelector.LockCursorAnimation();
            m_buttonSelector.ResetCursor();
        }

        if (m_cursor == null)
        {
            return;
        }

        m_cursor.SetActive(true);

        RectTransform cursorRect =
            m_cursor.GetComponent<RectTransform>();

        if (cursorRect != null)
        {
            cursorRect.anchoredPosition =
                m_cursorInitialPosition;
        }
    }

    /// <summary>
    /// Timeline終了時のカーソル位置を保持します。
    /// </summary>
    private void PreserveCursorPosition()
    {
        if (m_cursor == null)
        {
            return;
        }

        Vector3 cursorPosition =
            m_cursor.transform.position;

        if (m_fadeInDirector != null)
        {
            m_fadeInDirector.Pause();
        }

        m_cursor.transform.position =
            cursorPosition;
    }

    /// <summary>
    /// カーソルの操作可能状態を設定します。
    /// </summary>
    /// <param name="isEnabled">操作可能にするか。</param>
    private void SetCursorInteraction(bool isEnabled)
    {
        if (m_cursor == null)
        {
            return;
        }

        CanvasGroup canvasGroup =
            m_cursor.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha =
            isEnabled ? 1.0f : 0.0f;

        canvasGroup.interactable =
            isEnabled;

        canvasGroup.blocksRaycasts =
            isEnabled;

        if (isEnabled)
        {
            m_cursor.transform.SetAsLastSibling();
        }
    }

    /// <summary>
    /// ポーズ画面固有UIの有効状態を設定します。
    /// </summary>
    /// <param name="isActive">有効にするか。</param>
    private void SetUIActive(bool isActive)
    {
        SetActive(
            m_container,
            isActive);

        SetActive(
            m_detailText,
            isActive);

        SetActive(
            m_background,
            isActive);

        SetActive(
            m_selectButton,
            isActive);

        SetActive(
            m_mechPanel,
            isActive);
    }

    /// <summary>
    /// GameObjectの有効状態を設定します。
    /// </summary>
    /// <param name="target">対象。</param>
    /// <param name="isActive">有効にするか。</param>
    private void SetActive(
        GameObject target,
        bool isActive)
    {
        if (target == null)
        {
            return;
        }

        target.SetActive(isActive);
    }
}