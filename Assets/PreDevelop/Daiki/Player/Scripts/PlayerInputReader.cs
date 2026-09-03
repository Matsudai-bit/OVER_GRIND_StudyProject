using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// プレイヤー入力を取得して保持します。
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerInputReader : MonoBehaviour
{
    // 移動入力の有効判定に使用する閾値
    private const float MOVE_INPUT_SQR_THRESHOLD = 0.0001f;

    // 移動入力アクション
    [SerializeField, Header("移動入力アクション")]
    private InputActionReference m_moveActionReference;

    // 攻撃入力アクション
    [SerializeField, Header("攻撃入力アクション")]
    private InputActionReference m_attackActionReference;

    // ジャンプ入力アクション
    [SerializeField, Header("ジャンプ入力アクション")]
    private InputActionReference m_jumpActionReference;

    // Vブースト入力アクション
    [SerializeField, Header("Vブースト入力アクション")]
    private InputActionReference m_vBoostActionReference;

    // 現在の移動入力
    private Vector2 m_moveInput;

    // 攻撃入力があるか
    private bool m_hasAttackInput;

    // ジャンプ入力中か
    private bool m_hasJumpInput;

    // Vブースト入力があるか
    private bool m_hasVBoostInput;

    // 入力が有効か
    private bool m_isInputEnabled;

    /// <summary>
    /// 現在の移動入力を取得します。
    /// </summary>
    public Vector2 MoveInput => m_moveInput;

    /// <summary>
    /// 移動入力があるかを取得します。
    /// </summary>
    public bool HasMoveInput =>
        m_moveInput.sqrMagnitude >
        MOVE_INPUT_SQR_THRESHOLD;

    /// <summary>
    /// ジャンプ入力中かを取得します。
    /// </summary>
    public bool HasJumpInput => m_hasJumpInput;

    /// <summary>
    /// 入力が有効かを取得します。
    /// </summary>
    public bool IsInputEnabled => m_isInputEnabled;

    /// <summary>
    /// 攻撃入力を取得して消費します。
    /// </summary>
    /// <returns>
    /// true：攻撃入力がありました。
    /// false：攻撃入力がありません。
    /// </returns>
    public bool ConsumeAttackInput()
    {
        if (!m_hasAttackInput)
        {
            return false;
        }

        m_hasAttackInput = false;
        return true;
    }

    /// <summary>
    /// Vブースト入力を取得して消費します。
    /// </summary>
    /// <returns>
    /// true：Vブースト入力がありました。
    /// false：Vブースト入力がありません。
    /// </returns>
    public bool ConsumeVBoostInput()
    {
        if (!m_hasVBoostInput)
        {
            return false;
        }

        m_hasVBoostInput = false;
        return true;
    }

    /// <summary>
    /// プレイヤー入力を有効化します。
    /// </summary>
    public void EnableInput()
    {
        if (m_isInputEnabled)
        {
            return;
        }

        if (TryGetAction(
            m_moveActionReference,
            out InputAction moveAction))
        {
            moveAction.performed +=
                HandleMovePerformed;

            moveAction.canceled +=
                HandleMoveCanceled;

            moveAction.Enable();

            m_moveInput =
                moveAction.ReadValue<Vector2>();
        }

        if (TryGetAction(
            m_attackActionReference,
            out InputAction attackAction))
        {
            attackAction.performed +=
                HandleAttackPerformed;

            attackAction.Enable();
        }

        if (TryGetAction(
            m_jumpActionReference,
            out InputAction jumpAction))
        {
            jumpAction.performed +=
                HandleJumpPerformed;

            jumpAction.canceled +=
                HandleJumpCanceled;

            jumpAction.Enable();
        }

        if (TryGetAction(
            m_vBoostActionReference,
            out InputAction vBoostAction))
        {
            vBoostAction.performed +=
                HandleVBoostPerformed;

            vBoostAction.Enable();
        }

        m_isInputEnabled = true;
    }

    /// <summary>
    /// プレイヤー入力を無効化します。
    /// </summary>
    public void DisableInput()
    {
        if (!m_isInputEnabled)
        {
            ClearInput();
            return;
        }

        if (TryGetAction(
            m_moveActionReference,
            out InputAction moveAction))
        {
            moveAction.performed -=
                HandleMovePerformed;

            moveAction.canceled -=
                HandleMoveCanceled;

            moveAction.Disable();
        }

        if (TryGetAction(
            m_attackActionReference,
            out InputAction attackAction))
        {
            attackAction.performed -=
                HandleAttackPerformed;

            attackAction.Disable();
        }

        if (TryGetAction(
            m_jumpActionReference,
            out InputAction jumpAction))
        {
            jumpAction.performed -=
                HandleJumpPerformed;

            jumpAction.canceled -=
                HandleJumpCanceled;

            jumpAction.Disable();
        }

        if (TryGetAction(
            m_vBoostActionReference,
            out InputAction vBoostAction))
        {
            vBoostAction.performed -=
                HandleVBoostPerformed;

            vBoostAction.Disable();
        }

        ClearInput();

        m_isInputEnabled = false;
    }

    /// <summary>
    /// コンポーネント無効化時に入力を解除します。
    /// </summary>
    private void OnDisable()
    {
        DisableInput();
    }

    /// <summary>
    /// 移動入力を更新します。
    /// </summary>
    /// <param name="context">入力情報。</param>
    private void HandleMovePerformed(
        InputAction.CallbackContext context)
    {
        m_moveInput =
            context.ReadValue<Vector2>();
    }

    /// <summary>
    /// 移動入力をリセットします。
    /// </summary>
    /// <param name="context">入力情報。</param>
    private void HandleMoveCanceled(
        InputAction.CallbackContext context)
    {
        m_moveInput = Vector2.zero;
    }

    /// <summary>
    /// 攻撃入力を保存します。
    /// </summary>
    /// <param name="context">入力情報。</param>
    private void HandleAttackPerformed(
        InputAction.CallbackContext context)
    {
        m_hasAttackInput = true;
    }

    /// <summary>
    /// ジャンプ入力を開始します。
    /// </summary>
    /// <param name="context">入力情報。</param>
    private void HandleJumpPerformed(
        InputAction.CallbackContext context)
    {
        m_hasJumpInput = true;
    }

    /// <summary>
    /// ジャンプ入力を終了します。
    /// </summary>
    /// <param name="context">入力情報。</param>
    private void HandleJumpCanceled(
        InputAction.CallbackContext context)
    {
        m_hasJumpInput = false;
    }

    /// <summary>
    /// Vブースト入力を保存します。
    /// </summary>
    /// <param name="context">入力情報。</param>
    private void HandleVBoostPerformed(
        InputAction.CallbackContext context)
    {
        m_hasVBoostInput = true;
    }

    /// <summary>
    /// 現在の入力状態をリセットします。
    /// </summary>
    private void ClearInput()
    {
        m_moveInput = Vector2.zero;
        m_hasAttackInput = false;
        m_hasJumpInput = false;
        m_hasVBoostInput = false;
    }

    /// <summary>
    /// InputActionを取得します。
    /// </summary>
    /// <param name="actionReference">入力アクション参照。</param>
    /// <param name="action">取得した入力アクション。</param>
    /// <returns>
    /// true：入力アクションを取得しました。
    /// false：入力アクションを取得できませんでした。
    /// </returns>
    private bool TryGetAction(
        InputActionReference actionReference,
        out InputAction action)
    {
        action = null;

        if (actionReference == null)
        {
            Debug.LogError(
                $"[{nameof(PlayerInputReader)}] " +
                "InputActionReferenceが設定されていません。",
                this);

            return false;
        }

        action = actionReference.action;

        if (action == null)
        {
            Debug.LogError(
                $"[{nameof(PlayerInputReader)}] " +
                "InputActionを取得できませんでした。",
                this);

            return false;
        }

        return true;
    }
}