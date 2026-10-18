using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class PauseButtonManager : MonoBehaviour
{
    // ボタンセレクター
    [SerializeField]
    ButtonSelector m_buttonSelector;

    private void Start()
    {
        // ボタンセレクターのロックを解除する
        m_buttonSelector.UnLockCursor();
    }

}
