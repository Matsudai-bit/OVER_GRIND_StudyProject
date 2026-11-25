using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 同一シーン内のウィンドウの表示履歴を管理します。
/// </summary>
[DisallowMultipleComponent]
public sealed class UIWindowManager : MonoBehaviour, IUIInputControllable
{
    /// <summary>
    /// ウィンドウ履歴情報です。
    /// </summary>
    private readonly struct WindowEntry
    {
        /// <summary>
        /// 対象ウィンドウを取得します。
        /// </summary>
        public UIWindow Window { get; }

        /// <summary>
        /// ウィンドウの表示方式を取得します。
        /// </summary>
        public UIWindowOpenMode OpenMode { get; }

        /// <summary>
        /// ウィンドウ履歴情報を生成します。
        /// </summary>
        /// <param name="window">対象ウィンドウ。</param>
        public WindowEntry(UIWindow window)
        {
            Window = window;
            OpenMode = window.OpenMode;
        }
    }

    // 初期ウィンドウ
    [SerializeField, Header("初期ウィンドウ")]
    private UIWindow m_initialWindow;

    // Start時に初期ウィンドウを自動で開くか
    [SerializeField]
    private bool m_openInitialWindowOnStart = true;

    // 管理するウィンドウ
    [SerializeField, Header("管理するウィンドウ")]
    private List<UIWindow> m_windows = new();

    // 開いているウィンドウの履歴
    private readonly Stack<WindowEntry> m_windowStack = new();

    // 外部からの入力許可状態
    private bool m_isInputEnabled = true;

    // ウィンドウ切り替え中か
    private bool m_isTransitioning = false;

    // 全ウィンドウを閉じた後に実行する処理
    private Action m_onAllWindowsClosed;

    /// <summary>
    /// ウィンドウ切り替え中か取得します。
    /// </summary>
    public bool IsTransitioning => m_isTransitioning;

    /// <summary>
    /// ウィンドウが1つ以上開いているか取得します。
    /// </summary>
    public bool HasOpenWindow => m_windowStack.Count > 0;

    private void Awake()
    {
        PrepareWindows();
    }

    private void Start()
    {
        if (!m_openInitialWindowOnStart)
        {
            return;
        }

        OpenInitialWindow();
    }

    /// <summary>
    /// 管理対象のウィンドウを初期状態へ設定します。
    /// </summary>
    private void PrepareWindows()
    {
        foreach (UIWindow window in m_windows)
        {
            if (window == null)
            {
                continue;
            }

            window.PrepareHidden();
        }

        if (m_initialWindow != null)
        {
            m_initialWindow.PrepareHidden();
        }
    }

    /// <summary>
    /// 初期ウィンドウを開きます。
    /// </summary>
    public void OpenInitialWindow()
    {
        if (m_initialWindow == null)
        {
            Debug.LogError(
                "初期ウィンドウを設定してください。",
                this);

            return;
        }

        OpenWindow(m_initialWindow);
    }

    /// <summary>
    /// 新しいウィンドウを開きます。
    /// </summary>
    /// <param name="window">開くウィンドウ。</param>
    public void OpenWindow(UIWindow window)
    {
        if (!m_isInputEnabled
            || m_isTransitioning
            || window == null)
        {
            return;
        }

        if (window != m_initialWindow
            && !m_windows.Contains(window))
        {
            Debug.LogWarning(
                "管理対象に登録されていないウィンドウです。",
                window);

            return;
        }

        // 同じWindowを重複してStackへ積まない
        foreach (WindowEntry entry in m_windowStack)
        {
            if (entry.Window == window)
            {
                return;
            }
        }

        m_isTransitioning = true;

        UIWindow currentWindow =
            GetCurrentWindow();

        if (currentWindow == null)
        {
            ShowNewWindow(window);
            return;
        }

        currentWindow.SetInputEnabled(false);

        switch (window.OpenMode)
        {
            case UIWindowOpenMode.REPLACE:
                currentWindow.Hide(
                    () => ShowNewWindow(window));
                break;

            case UIWindowOpenMode.OVERLAY:
                ShowNewWindow(window);
                break;

            default:
                ShowNewWindow(window);
                break;
        }
    }

    /// <summary>
    /// 一番上のウィンドウを閉じて前の画面へ戻ります。
    /// </summary>
    public void CloseTopWindow()
    {
        if (!m_isInputEnabled
            || m_isTransitioning
            || m_windowStack.Count <= 1)
        {
            return;
        }

        m_isTransitioning = true;

        WindowEntry closingEntry =
            m_windowStack.Peek();

        closingEntry.Window.SetInputEnabled(false);

        closingEntry.Window.Hide(() =>
        {
            m_windowStack.Pop();

            closingEntry.Window.NotifyClosed();

            UIWindow previousWindow =
                GetCurrentWindow();

            if (previousWindow == null)
            {
                FinishTransition(null);
                return;
            }

            if (closingEntry.OpenMode ==
                UIWindowOpenMode.REPLACE)
            {
                previousWindow.Show(
                    () => FinishTransition(previousWindow));

                return;
            }

            FinishTransition(previousWindow);
        });
    }

    /// <summary>
    /// 現在開いているウィンドウをすべて閉じます。
    /// </summary>
    /// <param name="onCompleted">
    /// すべて閉じた後に実行する処理。
    /// </param>
    public void CloseAllWindows(
        Action onCompleted = null)
    {
        if (m_isTransitioning)
        {
            return;
        }

        if (m_windowStack.Count == 0)
        {
            onCompleted?.Invoke();
            return;
        }

        m_isTransitioning = true;
        m_onAllWindowsClosed = onCompleted;

        CloseNextWindow();
    }

    /// <summary>
    /// Stack上のウィンドウを順番に閉じます。
    /// </summary>
    private void CloseNextWindow()
    {
        if (m_windowStack.Count == 0)
        {
            CompleteCloseAllWindows();
            return;
        }

        WindowEntry closingEntry =
            m_windowStack.Peek();

        UIWindow closingWindow =
            closingEntry.Window;

        if (closingWindow == null)
        {
            m_windowStack.Pop();
            CloseNextWindow();
            return;
        }

        closingWindow.SetInputEnabled(false);

        // REPLACEによって既に非表示になっているWindowは
        // Hide演出を再生せず履歴から削除する
        if (!closingWindow.gameObject.activeSelf)
        {
            m_windowStack.Pop();

            closingWindow.NotifyClosed();

            CloseNextWindow();
            return;
        }

        closingWindow.Hide(() =>
        {
            m_windowStack.Pop();

            closingWindow.NotifyClosed();

            CloseNextWindow();
        });
    }

    /// <summary>
    /// 全ウィンドウの終了処理を完了します。
    /// </summary>
    private void CompleteCloseAllWindows()
    {
        m_isTransitioning = false;

        Action callback =
            m_onAllWindowsClosed;

        m_onAllWindowsClosed = null;

        callback?.Invoke();
    }

    /// <summary>
    /// 最前面のウィンドウの入力状態を変更します。
    /// </summary>
    /// <param name="isEnabled">
    /// 入力を有効にするか。
    /// </param>
    public void SetInputEnabled(bool isEnabled)
    {
        m_isInputEnabled = isEnabled;

        UIWindow currentWindow =
            GetCurrentWindow();

        if (currentWindow == null)
        {
            return;
        }

        // 切り替え途中では入力を有効化しない
        currentWindow.SetInputEnabled(
            isEnabled && !m_isTransitioning);
    }

    /// <summary>
    /// 現在のウィンドウを取得します。
    /// </summary>
    /// <returns>
    /// 履歴の先頭のウィンドウ。
    /// 存在しない場合はnull。
    /// </returns>
    public UIWindow GetCurrentWindow()
    {
        if (m_windowStack.Count == 0)
        {
            return null;
        }

        return m_windowStack.Peek().Window;
    }

    /// <summary>
    /// 新しいウィンドウの表示を開始します。
    /// </summary>
    /// <param name="window">
    /// 表示するウィンドウ。
    /// </param>
    private void ShowNewWindow(UIWindow window)
    {
        m_windowStack.Push(
            new WindowEntry(window));

        window.NotifyOpened();

        window.Show(
            () => FinishTransition(window));
    }

    /// <summary>
    /// ウィンドウ切り替えを終了して入力を復帰させます。
    /// </summary>
    /// <param name="window">
    /// 最前面のウィンドウ。
    /// </param>
    private void FinishTransition(UIWindow window)
    {
        m_isTransitioning = false;

        if (window == null)
        {
            return;
        }

        window.SetInputEnabled(
            m_isInputEnabled);
    }
}