using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// ウィンドウの表示と入力を制御します。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public sealed class UIWindow : MonoBehaviour, IUIScreen
{
    private enum TransitionState
    {
        NONE,
        SHOWING,
        HIDING
    }

    [SerializeField, Header("ウィンドウの設定")]
    private UIWindowOpenMode m_openMode = UIWindowOpenMode.REPLACE;

    [SerializeField, Header("入力開始時に選択するUI")]
    private Selectable m_firstSelectable;

    [SerializeField, Header("演出完了通知を待つ")]
    private bool m_waitForShowCompletion = false;

    [SerializeField]
    private bool m_waitForHideCompletion = false;

    [SerializeField, Header("表示・非表示イベント")]
    private UnityEvent m_onShow = new();

    [SerializeField]
    private UnityEvent m_onHide = new();

    [SerializeField, Header("画面の開始・終了イベント")]
    private UnityEvent m_onOpened = new();

    [SerializeField]
    private UnityEvent m_onClosed = new();

    [SerializeField, Header("入力状態イベント")]
    private UnityEvent m_onInputEnabled = new();

    [SerializeField]
    private UnityEvent m_onInputDisabled = new();

    // UIの入力制御に使用します。
    private CanvasGroup m_canvasGroup;

    // 表示・非表示完了後に実行する処理です。
    private Action m_onTransitionCompleted;

    // 表示・非表示の進行状態です。
    private TransitionState m_transitionState = TransitionState.NONE;

    // UI入力の有効状態です。
    private bool m_isInputEnabled = false;

    // 前回選択していたUIです。
    private GameObject m_lastSelectedObject;

    /// <summary>
    /// ウィンドウの表示方式を取得します。
    /// </summary>
    public UIWindowOpenMode OpenMode => m_openMode;

    private void Awake()
    {
        m_canvasGroup = GetComponent<CanvasGroup>();
    }

    /// <summary>
    /// ウィンドウを非表示の初期状態に戻します。
    /// </summary>
    public void PrepareHidden()
    {
        EnsureCanvasGroup();

        m_onTransitionCompleted = null;
        m_transitionState = TransitionState.NONE;
        m_isInputEnabled = false;
        m_lastSelectedObject = null;

        m_canvasGroup.interactable = false;
        m_canvasGroup.blocksRaycasts = false;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// ウィンドウの表示を開始します。
    /// </summary>
    /// <param name="onCompleted">表示完了時の処理。</param>
    public void Show(Action onCompleted = null)
    {
        if (m_transitionState != TransitionState.NONE)
        {
            Debug.LogWarning("ウィンドウの演出中は表示を開始できません。", this);
            return;
        }

        EnsureCanvasGroup();
        gameObject.SetActive(true);
        SetInputEnabled(false);
        transform.SetAsLastSibling();

        m_transitionState = TransitionState.SHOWING;
        m_onTransitionCompleted = onCompleted;
        m_onShow?.Invoke();

        if (!m_waitForShowCompletion)
        {
            NotifyShowCompleted();
        }
    }

    /// <summary>
    /// ウィンドウの非表示を開始します。
    /// </summary>
    /// <param name="onCompleted">非表示完了時の処理。</param>
    public void Hide(Action onCompleted = null)
    {
        if (m_transitionState != TransitionState.NONE)
        {
            Debug.LogWarning("ウィンドウの演出中は非表示を開始できません。", this);
            return;
        }

        SetInputEnabled(false);

        m_transitionState = TransitionState.HIDING;
        m_onTransitionCompleted = onCompleted;
        m_onHide?.Invoke();

        if (!m_waitForHideCompletion)
        {
            NotifyHideCompleted();
        }
    }

    /// <summary>
    /// 表示演出の完了を通知します。
    /// </summary>
    public void NotifyShowCompleted()
    {
        if (m_transitionState != TransitionState.SHOWING)
        {
            return;
        }

        CompleteTransition();
    }

    /// <summary>
    /// 非表示演出の完了を通知します。
    /// </summary>
    public void NotifyHideCompleted()
    {
        if (m_transitionState != TransitionState.HIDING)
        {
            return;
        }

        // 非表示を完了してから呼び出し元へ通知します。
        gameObject.SetActive(false);
        CompleteTransition();
    }

    /// <summary>
    /// ウィンドウの入力状態を変更します。
    /// </summary>
    /// <param name="isEnabled">入力を有効にするか。</param>
    public void SetInputEnabled(bool isEnabled)
    {
        EnsureCanvasGroup();
        m_canvasGroup.interactable = isEnabled;
        m_canvasGroup.blocksRaycasts = isEnabled;

        if (m_isInputEnabled == isEnabled)
        {
            return;
        }

        m_isInputEnabled = isEnabled;
        UpdateSelection(isEnabled);

        if (isEnabled)
        {
            m_onInputEnabled?.Invoke();
        }
        else
        {
            m_onInputDisabled?.Invoke();
        }
    }

    /// <summary>
    /// ウィンドウを新しく開いたことを通知します。
    /// </summary>
    public void NotifyOpened()
    {
        m_onOpened?.Invoke();
    }

    /// <summary>
    /// ウィンドウを閉じたことを通知します。
    /// </summary>
    public void NotifyClosed()
    {
        m_onClosed?.Invoke();
    }

    /// <summary>
    /// CanvasGroupの参照を取得します。
    /// </summary>
    private void EnsureCanvasGroup()
    {
        if (m_canvasGroup == null)
        {
            m_canvasGroup = GetComponent<CanvasGroup>();
        }
    }

    /// <summary>
    /// 表示・非表示の完了処理を実行します。
    /// </summary>
    private void CompleteTransition()
    {
        m_transitionState = TransitionState.NONE;

        Action callback = m_onTransitionCompleted;
        m_onTransitionCompleted = null;
        callback?.Invoke();
    }

    /// <summary>
    /// EventSystemの選択状態を更新します。
    /// </summary>
    private void UpdateSelection(bool isEnabled)
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            return;
        }

        if (!isEnabled)
        {
            GameObject selectedObject = eventSystem.currentSelectedGameObject;
            if (selectedObject != null && selectedObject.transform.IsChildOf(transform))
            {
                m_lastSelectedObject = selectedObject;
                eventSystem.SetSelectedGameObject(null);
            }

            return;
        }

        Selectable selectable = null;
        if (m_lastSelectedObject != null)
        {
            selectable = m_lastSelectedObject.GetComponent<Selectable>();
        }

        if (selectable == null || !selectable.IsActive() || !selectable.IsInteractable())
        {
            selectable = m_firstSelectable;
        }

        if (selectable != null && selectable.IsActive() && selectable.IsInteractable())
        {
            eventSystem.SetSelectedGameObject(selectable.gameObject);
        }
    }
}
