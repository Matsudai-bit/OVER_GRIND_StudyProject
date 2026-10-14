using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class TitleButtonManager : MonoBehaviour
{
    // ボタンセレクター
    [SerializeField]
    ButtonSelector m_buttonSelector;
    [SerializeField]
    UIOverlaySceneManager m_uIOverlaySceneManager;
    // カーソルの座標
    Vector3 m_cursorPosition = Vector3.zero;

    bool m_lock = false;

    private void Start()
    {
        // ボタンセレクターのロックを解除する
        EnableInput();
    }

    public void MoveCursor(
        UnityEngine.UI.Image cursor,
        Vector3 targetPosition)
    {
        if (m_cursorPosition == targetPosition)
        {
            cursor.DOFade(endValue: 1.0f, duration: 0.2f);
        }
        else if (m_cursorPosition != targetPosition)
        {
            cursor.DOFade(endValue: 0.0f, duration: 0.01f);

            m_cursorPosition = targetPosition;
        }

        cursor.transform.DOMove(targetPosition, 0.2f);

    }

    public void EnableInput()
    {
        m_buttonSelector.UnLockCursor();
        m_lock = false;
    }
    public void DisableInput()
    {
        m_buttonSelector.LockCursor();
        m_lock = true;
    }

    public void OnOpenMenu(InputAction.CallbackContext context)
    {
        if (!context.performed)
        {
            return;
        }

        Debug.Log("OpenMenu Input");

        if (m_lock)
        {
            return;
        }

        m_uIOverlaySceneManager.OpenOverlayScene();
    }
}