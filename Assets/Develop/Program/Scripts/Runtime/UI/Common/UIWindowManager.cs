using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 同一シーン内のウィンドウの表示履歴を管理します。
/// </summary>
[DisallowMultipleComponent]
public sealed class UIWindowManager : MonoBehaviour, IUIInputControllable
{
    private readonly struct WindowEntry
    {
        public UIWindow Window { get; }
        public UIWindowOpenMode OpenMode { get; }

        public WindowEntry(UIWindow window)
        {
            Window = window;
            OpenMode = window.OpenMode;
        }
    }

    [SerializeField, Header("初期ウィンドウ")]
    private UIWindow m_initialWindow;

    [SerializeField, Header("管理するウィンドウ")]
    private List<UIWindow> m_windows = new();

    // 開いているウィンドウの履歴です。
    private readonly Stack<WindowEntry> m_windowStack = new();

    // 外部からの入力許可状態です。
    private bool m_isInputEnabled = true;

    // ウィンドウ切り替え中かどうかです。
    private bool m_isTransitioning = false;

    private void Awake()
    {
        // 初期画面以外も起動時に非表示にします。
        foreach (UIWindow window in m_windows)
        {
            if (window != null)
            {
                window.PrepareHidden();
            }
        }

        if (m_initialWindow != null)
        {
            m_initialWindow.PrepareHidden();
        }
    }

    private void Start()
    {
        if (m_initialWindow == null)
        {
            Debug.LogError("初期ウィンドウを設定してください。", this);
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
        if (!m_isInputEnabled || m_isTransitioning || window == null)
        {
            return;
        }

        if (window != m_initialWindow && !m_windows.Contains(window))
        {
            Debug.LogWarning("管理対象に登録されていないウィンドウです。", window);
            return;
        }

        // 開いているウィンドウを重複して積まないようにします。
        foreach (WindowEntry entry in m_windowStack)
        {
            if (entry.Window == window)
            {
                return;
            }
        }

        m_isTransitioning = true;
        UIWindow currentWindow = GetCurrentWindow();

        if (currentWindow == null)
        {
            ShowNewWindow(window);
            return;
        }

        currentWindow.SetInputEnabled(false);

        if (window.OpenMode == UIWindowOpenMode.REPLACE)
        {
            currentWindow.Hide(() => ShowNewWindow(window));
        }
        else
        {
            ShowNewWindow(window);
        }
    }

    /// <summary>
    /// 一番上のウィンドウを閉じて前の画面へ戻ります。
    /// </summary>
    public void CloseTopWindow()
    {
        if (!m_isInputEnabled || m_isTransitioning || m_windowStack.Count <= 1)
        {
            return;
        }

        m_isTransitioning = true;
        WindowEntry closingEntry = m_windowStack.Peek();
        closingEntry.Window.SetInputEnabled(false);

        closingEntry.Window.Hide(() =>
        {
            m_windowStack.Pop();
            closingEntry.Window.NotifyClosed();

            UIWindow previousWindow = GetCurrentWindow();
            if (closingEntry.OpenMode == UIWindowOpenMode.REPLACE)
            {
                previousWindow.Show(() => FinishTransition(previousWindow));
            }
            else
            {
                FinishTransition(previousWindow);
            }
        });
    }

    /// <summary>
    /// 最前面のウィンドウの入力を切り替えます。
    /// </summary>
    /// <param name="isEnabled">入力を有効にするか。</param>
    public void SetInputEnabled(bool isEnabled)
    {
        m_isInputEnabled = isEnabled;
        UIWindow currentWindow = GetCurrentWindow();

        if (currentWindow != null)
        {
            // 切り替えの途中では入力を有効化しません。
            currentWindow.SetInputEnabled(isEnabled && !m_isTransitioning);
        }
    }

    /// <summary>
    /// 現在のウィンドウを取得します。
    /// </summary>
    /// <returns>履歴の先頭のウィンドウ。存在しない場合はnull。</returns>
    public UIWindow GetCurrentWindow()
    {
        return m_windowStack.Count > 0 ? m_windowStack.Peek().Window : null;
    }

    /// <summary>
    /// 新しいウィンドウの表示を開始します。
    /// </summary>
    private void ShowNewWindow(UIWindow window)
    {
        m_windowStack.Push(new WindowEntry(window));
        window.NotifyOpened();
        window.Show(() => FinishTransition(window));
    }

    /// <summary>
    /// 切り替えを終了して入力を復帰させます。
    /// </summary>
    private void FinishTransition(UIWindow window)
    {
        m_isTransitioning = false;
        if (window != null)
        {
            window.SetInputEnabled(m_isInputEnabled);
        }
    }
}
