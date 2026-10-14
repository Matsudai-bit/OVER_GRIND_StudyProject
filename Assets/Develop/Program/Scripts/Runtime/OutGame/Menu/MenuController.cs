using UnityEngine;
using UnityEngine.InputSystem;

public class MenuController : MonoBehaviour
{
    [SerializeField]
    UIOverlaySceneRoot m_uIOverlayScene;
    public void OnCloseMenu(InputAction.CallbackContext context)
    {
        Debug.Log("ƒƒjƒ…[‚ğ•Â‚¶‚Ü‚·");
        m_uIOverlayScene.RequestClose();
    }
}
