using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class MenuController : MonoBehaviour
{
    [SerializeField]
    private MenuOpenContext m_menuOpenContext;
    [SerializeField]
    private UserSupportManager userSupportManager;
    [SerializeField]
    private SaveDataController m_saveDataController;
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

    private void Start()
    {

    }

    public void StartMenu()
    {
        userSupportManager.Initialize(m_menuOpenContext.ConsumeOpenTarget());
    }

    [SerializeField]
    UIOverlaySceneRoot m_uIOverlayScene;
    public void OnCloseMenu(InputAction.CallbackContext context)
    {
        Debug.Log("メニューを閉じます");
        m_uIOverlayScene.RequestClose();
    }

    public void Save()
    {
        m_saveDataController.SaveIfDirty();
    }
}
