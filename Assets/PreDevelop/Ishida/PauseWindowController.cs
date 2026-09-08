using UnityEngine;
using UnityEngine.InputSystem;

public class PauseWindowController : MonoBehaviour
{
    public GameObject m_pauseScreen;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

 
    public void OnInputTriggered(InputAction.CallbackContext context)
    {
        string mapName = context.action.actionMap.name;
        if(mapName == "UI")
        {
            Debug.Log("UI‚Ìƒ}ƒbƒv");
        }

        string actionName = context.action.name;
        if(actionName == "Navigate")
        {
            Debug.Log("Navigate‚ªŒÄ‚Î‚ê‚½");
        }
    }
}
