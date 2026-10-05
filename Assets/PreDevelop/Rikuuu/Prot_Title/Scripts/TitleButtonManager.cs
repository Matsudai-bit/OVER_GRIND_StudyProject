using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class TitleButtonManager : MonoBehaviour
{
    // ボタンセレクター
    [SerializeField]
    ButtonSelector m_buttonSelector;
    // カーソルの座標
    Vector3 m_cursorPosition = Vector3.zero;

    private void Start()
    {
        // ボタンセレクターのロックを解除する
        m_buttonSelector.UnLockCursor();
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
}