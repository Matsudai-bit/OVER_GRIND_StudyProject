using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public enum InputTriggerType
{ 
    NONE = 0,

    PRESS,      // 単押し
    HOLD,       // 長押し
    RELEASE,    // 放したとき
}

public class InputEventInvoker : MonoBehaviour
{
    // 反応するキー
    [SerializeField]
    private InputActionReference m_inputRef;
    // 反応の方法
    [SerializeField]
    private InputTriggerType m_triggerType = InputTriggerType.PRESS;

    // 実行する関数
    [SerializeField]
    private UnityEngine.Events.UnityEvent m_action;

    private void Awake()
    {
        // 入力を有効にする
        m_inputRef.action.Enable();
    }

    private void Update()
    {
        // キーが設定されていない場合
        if(m_inputRef == null)
        {
            return;
        }

        // キーが押されているかの判定を行う
        switch (m_triggerType)
        {
            case InputTriggerType.PRESS:
                if (m_inputRef.action.WasPressedThisFrame())
                {
                    m_action.Invoke();
                }
                break;

            case InputTriggerType.HOLD:
                if (m_inputRef.action.IsPressed())
                {
                    m_action.Invoke();
                }
                break;

            case InputTriggerType.RELEASE:
                if (m_inputRef.action.WasReleasedThisFrame())
                {
                    m_action.Invoke();
                }
                break;
        }
    }
}