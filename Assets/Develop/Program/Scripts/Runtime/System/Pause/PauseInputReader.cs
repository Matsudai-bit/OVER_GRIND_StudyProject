using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Pasue使用する入力を取得します。
/// </summary>
[DisallowMultipleComponent]
public sealed class PauseInputReader : MonoBehaviour
{
    // ポーズ入力
    [SerializeField, Header("ポーズ入力")]
    private InputActionReference m_pauseActionReference;

    // ポーズ要求イベント
    [SerializeField, Header("ポーズ要求イベント")]
    private PauseRequestEvent m_pauseRequestEvent;

    private void OnEnable()
    {
        if (m_pauseActionReference == null)
        {
            Debug.LogWarning(
                "ポーズ入力アクションが設定されていません。",
                this);

            return;
        }

        m_pauseActionReference.action.performed += OnPausePerformed;
        m_pauseActionReference.action.Enable();
    }

    private void OnDisable()
    {
        if (m_pauseActionReference == null)
        {
            return;
        }

        m_pauseActionReference.action.performed -= OnPausePerformed;
        m_pauseActionReference.action.Disable();
    }

    /// <summary>
    /// ポーズ入力を処理します。
    /// </summary>
    /// <param name="context">入力情報。</param>
    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        if (m_pauseRequestEvent == null)
        {
            return;
        }

        m_pauseRequestEvent.RaiseToggle();
    }
}