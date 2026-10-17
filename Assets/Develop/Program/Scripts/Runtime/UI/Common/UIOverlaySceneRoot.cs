using System;
using UnityEngine;

/// <summary>
/// Additiveシーン内のウィンドウと閉じる要求を仲介します。
/// </summary>
[DisallowMultipleComponent]
public sealed class UIOverlaySceneRoot : MonoBehaviour, IUIScreen
{
    [SerializeField, Header("シーン内のウィンドウ")]
    private UIWindow m_window;

    // 呼び出し元への閉じる要求です。
    private Action m_onCloseRequested;

    /// <summary>
    /// ウィンドウが設定されているかを取得します。
    /// </summary>
    public bool HasWindow => m_window != null;

    private void Awake()
    {
        // シーン読み込み直後にUIが一瞬映ることを防ぎます。
        if (m_window != null)
        {
            m_window.PrepareHidden();
        }
    }

    /// <summary>
    /// 閉じる要求先を設定します。
    /// </summary>
    /// <param name="onCloseRequested">閉じるときの処理。</param>
    public void Initialize(Action onCloseRequested)
    {
        m_onCloseRequested = onCloseRequested;
    }

    /// <summary>
    /// シーン内のウィンドウを表示します。
    /// </summary>
    /// <param name="onCompleted">表示完了時の処理。</param>
    public void Show(Action onCompleted = null)
    {
        m_window.NotifyOpened();
        m_window.Show(onCompleted);
    }

    /// <summary>
    /// シーン内のウィンドウを非表示にします。
    /// </summary>
    /// <param name="onCompleted">非表示完了時の処理。</param>
    public void Hide(Action onCompleted = null)
    {
        m_window.Hide(() =>
        {
            m_window.NotifyClosed();
            onCompleted?.Invoke();
        });
    }

    /// <summary>
    /// シーン内のUI入力を切り替えます。
    /// </summary>
    /// <param name="isEnabled">入力を有効にするか。</param>
    public void SetInputEnabled(bool isEnabled)
    {
        if (m_window != null)
        {
            m_window.SetInputEnabled(isEnabled);
        }
    }

    /// <summary>
    /// 閉じるボタンなどから終了を要求します。
    /// </summary>
    public void RequestClose()
    {
        m_onCloseRequested?.Invoke();
    }
}
