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

    // 現在の移動入力
    private Vector2 m_moveInput;
    // 攻撃入力をしたかどうか
    private bool m_hasAttackInput = false;
    private bool m_hasJumpInput = false;

    // 入力が有効か
    private bool m_isInputEnabled;

    /// <summary>
    /// 現在の移動入力を取得します。
    /// </summary>
    public Vector2 MoveInput => m_moveInput;

    /// <summary>
    /// 移動入力があるかを取得します。
    /// </summary>
    /// <returns>
    /// true：移動入力があります。
    /// false：移動入力がありません。
    /// </returns>
    public bool HasMoveInput =>
        m_moveInput.sqrMagnitude > MOVE_INPUT_SQR_THRESHOLD;

    /// <summary>
    /// 入力が有効かを取得します。
    /// </summary>
    /// <returns>
    /// true：入力が有効です。
    /// false：入力が無効です。
    /// </returns>
    public bool IsInputEnabled => m_isInputEnabled;

    public bool HasJumpInput => m_hasJumpInput;

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
    /// プレイヤー入力を有効化します。
    /// </summary>
    public void EnableInput()
    {
        if (m_isInputEnabled)
        {
            return;
        }

        if (TryGetAction(m_moveActionReference, out InputAction moveAction))
        {
            // 入力イベントを登録
            moveAction.performed += HandleMovePerformed;
            moveAction.canceled += HandleMoveCanceled;
            moveAction.Enable();

            // 現在入力されている値を取得
            m_moveInput = moveAction.ReadValue<Vector2>();
        }
        if (TryGetAction(m_attackActionReference, out InputAction attackAction))
        {
            // 入力イベントを登録
            attackAction.performed += HandleAttackPerformed;
            attackAction.canceled += HandleAttackCanceled;
            attackAction.Enable();

        }
        if (TryGetAction(m_jumpActionReference, out InputAction jumpAction))
        {
            // 入力イベントを登録
            jumpAction.performed += HandleJumpPerformed;
            jumpAction.canceled += HandleJumpCanceled;
            jumpAction.Enable();
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
            m_moveInput = Vector2.zero;
            return;
        }

        if (TryGetAction(m_moveActionReference, out InputAction moveAction))
        {
            // 入力イベントを解除
            moveAction.performed -= HandleMovePerformed;
            moveAction.canceled -= HandleMoveCanceled;
            moveAction.Disable();
        }

        if (TryGetAction(m_attackActionReference, out InputAction attackAction))
        {
            // 入力イベントを解除
            attackAction.performed -= HandleAttackPerformed;
            attackAction.canceled -= HandleAttackCanceled;
            attackAction.Disable();
        }
        if (TryGetAction(m_jumpActionReference, out InputAction jumpAction))
        {
            // 入力イベントを解除
            jumpAction.performed -= HandleJumpPerformed;
            jumpAction.canceled -= HandleJumpCanceled;
            jumpAction.Disable();
        }

        m_moveInput = Vector2.zero;
        m_hasAttackInput = false;
        m_isInputEnabled = false;
        m_hasJumpInput = false;
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
    private void HandleMovePerformed(InputAction.CallbackContext context)
    {
        m_moveInput = context.ReadValue<Vector2>();
    }

    /// <summary>
    /// 移動入力をリセットします。
    /// </summary>
    /// <param name="context">入力情報。</param>
    private void HandleMoveCanceled(InputAction.CallbackContext context)
    {
        m_moveInput = Vector2.zero;
    }

    /// <summary>
    /// 攻撃入力を更新します。
    /// </summary>
    /// <param name="context">入力情報。</param>
    private void HandleAttackPerformed(InputAction.CallbackContext context)
    {
        m_hasAttackInput = true;
    }

    /// <summary>
    /// 攻撃入力をリセットします。
    /// </summary>
    /// <param name="context">入力情報。</param>
    private void HandleAttackCanceled(InputAction.CallbackContext context)
    {
        m_hasAttackInput = false;
    }

    /// <summary>
    /// ジャンプ入力を更新します。
    /// </summary>
    /// <param name="context">入力情報。</param>
    private void HandleJumpPerformed(InputAction.CallbackContext context)
    {
         m_hasJumpInput = true;
        Debug.Log("ジャンプ入力");
    }

    /// <summary>
    /// ジャンプ入力をリセットします。
    /// </summary>
    /// <param name="context">入力情報。</param>
    private void HandleJumpCanceled(InputAction.CallbackContext context)
    {
        m_hasJumpInput = false;
    }




    /// <summary>
    /// アクションを取得します。
    /// </summary>
    /// <param name="moveAction">取得した移動アクション。</param>
    /// <returns>
    /// true：移動アクションを取得しました。
    /// false：移動アクションを取得できませんでした。
    /// </returns>
    private bool TryGetAction(InputActionReference actionReference, out InputAction action)
    {
        action = null;

        if (actionReference == null)
        {
            Debug.LogError(
                $"[{nameof(PlayerInputReader)}] Move Action Referenceが設定されていません。",
                this);

            return false;
        }

        action = actionReference.action;

        if (action == null)
        {
            Debug.LogError(
                $"[{nameof(PlayerInputReader)}] Move Actionを取得できませんでした。",
                this);

            return false;
        }

        return true;
    }
}