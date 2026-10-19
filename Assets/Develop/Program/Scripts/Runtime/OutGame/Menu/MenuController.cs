using UnityEngine;
using UnityEngine.InputSystem;

public class MenuController : MonoBehaviour
{
    private void Awake()
    {
        InputSystem.onActionChange += (obj, change) => {
            if (change == InputActionChange.ActionDisabled)
            {
                // action または actionMap が無効化されたタイミング
                if (obj is InputAction action)
                {
                    Debug.Log($"Action disabled: {action.name}");
                }
            }
        };
    }

    [SerializeField]
    UIOverlaySceneRoot m_uIOverlayScene;
    public void OnCloseMenu(InputAction.CallbackContext context)
    {
        Debug.Log("メニューを閉じます");
        m_uIOverlayScene.RequestClose();
    }
}
