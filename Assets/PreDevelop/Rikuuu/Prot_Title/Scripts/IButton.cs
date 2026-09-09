using System;
using UnityEngine;
using UnityEngine.UI;

public class IButton : MonoBehaviour
{
    // カーソルが合わさったときに実行する関数
    [SerializeField]
    private UnityEngine.Events.UnityEvent m_onCursorAction;
    // カーソルが離れたときに実行する関数
    [SerializeField]
    private UnityEngine.Events.UnityEvent m_onCursorExitAction;
    // クリックされたに実行する関数
    [SerializeField]
    private UnityEngine.Events.UnityEvent m_onClickAction;
    // クリックが離れたときに実行する関数
    [SerializeField]
    private UnityEngine.Events.UnityEvent m_onClickExitAction;

    // カーソルが乗ったら
    public void OnCursor()
    {
        Debug.Log("OnCursor");
        if(m_onCursorAction != null)
        {
            m_onCursorAction.Invoke();
        }
    }

    // カーソルが離れたら
    public void OnCursorExit()
    {
        Debug.Log("OnCursorExit");
        if(m_onCursorExitAction != null)
        {
            m_onCursorExitAction.Invoke();
        }
    }

    // クリックされたら
    public void OnClick()
    {
        Debug.Log("OnClick");
        if (m_onClickAction != null)
        {
            m_onClickAction.Invoke();
        }
    }

    // クリックが離れたら
    public void OnClickExit()
    {
        Debug.Log("OnClickExit");
        if(m_onClickExitAction != null)
        {
            m_onClickExitAction.Invoke();
        }
    }
}