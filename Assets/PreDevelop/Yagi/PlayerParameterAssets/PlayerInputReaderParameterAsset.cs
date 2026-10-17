using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>PlayerInputReaderの再利用可能な入力設定を保持します。</summary>
[CreateAssetMenu(fileName = "PlayerInputReaderParameter", menuName = "Game/Parameters/Player/Input Reader")]
public sealed class PlayerInputReaderParameterAsset : ScriptableObject
{
    [SerializeField] private InputActionReference m_moveActionReference;
    [SerializeField] private InputActionReference m_attackActionReference;
    [SerializeField] private InputActionReference m_jumpActionReference;
    [SerializeField] private InputActionReference m_vBoostActionReference;
    [SerializeField] private InputActionAsset m_inputActionsAsset;
    [SerializeField] private string m_vBoostActionName = "Player/Charge";
    [SerializeField, Min(0.0f)] private float m_moveInputSqrThreshold = 0.0001f;
    [SerializeField, Min(0.0f)] private float m_vBoostHoldTime = 0.2f;

    public InputActionReference MoveActionReference => m_moveActionReference;
    public InputActionReference AttackActionReference => m_attackActionReference;
    public InputActionReference JumpActionReference => m_jumpActionReference;
    public InputActionReference VBoostActionReference => m_vBoostActionReference;
    public InputActionAsset InputActionsAsset => m_inputActionsAsset;
    public string VBoostActionName => m_vBoostActionName;
    public float MoveInputSqrThreshold => Mathf.Max(0.0f, m_moveInputSqrThreshold);
    public float VBoostHoldTime => Mathf.Max(0.0f, m_vBoostHoldTime);
}
