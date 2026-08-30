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

    // Vブーストの長押し判定時間
    private const float V_BOOST_HOLD_TIME = 0.2f;

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

    // Vブースト入力が開始されたか
    private bool m_hasVBoostStarted;

    // Vブースト入力中か
    private bool m_isVBoostHeld;

    // Vブースト入力が長押し状態になったか（外部消費用フラグ）
    private bool m_hasVBoostHoldStarted;

    // 今回の押下中にすでに長押しをトリガー済みか
    // （Consumeされても戻らない、内部専用ガード）
    private bool m_vBoostHoldTriggeredThisPress;

    // Vブースト入力が離されたか
    private bool m_hasVBoostReleased;

    // Vブーストを押している時間
    private float m_vBoostHoldTime;

    // 入力が有効か
    private bool m_isInputEnabled;

    /// <summary>
    /// 現在の移動入力を取得します。
    /// </summary>
    public Vector2 MoveInput => m_moveInput;

    /// <summary>
    /// 移動入力があるかどうかを取得します。
    /// </summary>
    public bool HasMoveInput =>
        m_moveInput.sqrMagnitude >
        MOVE_INPUT_SQR_THRESHOLD;

    /// <summary>
    /// ジャンプ入力中かどうかを取得します。
    /// </summary>
    public bool HasJumpInput => m_hasJumpInput;

    /// <summary>
    /// 入力が有効かどうかを取得します。
    /// </summary>
    public bool IsInputEnabled => m_isInputEnabled;

    /// <summary>
    /// Vブースト入力が押され続けているか取得します。
    /// </summary>
    public bool IsVBoostHeld =>
        m_isVBoostHeld;

    /// <summary>
    /// 攻撃入力を取得して消費します。
    /// </summary>
    /// <returns>
    /// true：攻撃入力がある。
    /// false：攻撃入力がない。
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
    /// 既存のVブースト状態との互換性のために残します。
    /// </summary>
    /// <returns>
    /// true：Vブースト入力がある。
    /// false：Vブースト入力がない。
    /// </returns>
    public bool ConsumeVBoostInput()
    {
        if (!m_hasVBoostStarted)
        {
            return false;
        }

        m_hasVBoostStarted = false;
        return true;
    }

    /// <summary>
    /// Vブーストの長押し成立を取得して消費します。
    /// </summary>
    /// <returns>
    /// true：Vブーストの長押しが成立した。
    /// false：成立していない。
    /// </returns>
    public bool ConsumeVBoostHoldStarted()
    {
        if (!m_hasVBoostHoldStarted)
        {
            return false;
        }

        m_hasVBoostHoldStarted = false;
        return true;
    }

    /// <summary>
    /// Vブースト入力が開始されたか取得します。
    /// 取得すると開始入力を消費します。
    /// </summary>
    /// <returns>
    /// true：Vブースト入力が開始された。
    /// false：開始されていない。
    /// </returns>
    public bool ConsumeVBoostStarted()
    {
        if (!m_hasVBoostStarted)
        {
            return false;
        }

        m_hasVBoostStarted = false;
        return true;
    }

    /// <summary>
    /// Vブースト入力が押され続けているか取得します。
    /// </summary>
    public bool IsVBoostHeldInput =>
        m_isVBoostHeld;

    /// <summary>
    /// Vブースト入力が離されたか取得します。
    /// 取得すると離した入力を消費します。
    /// </summary>
    /// <returns>
    /// true：Vブースト入力が離された。
    /// false：離されていない。
    /// </returns>
    public bool ConsumeVBoostReleased()
    {
        if (!m_hasVBoostReleased)
        {
            return false;
        }

        m_hasVBoostReleased = false;
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
            vBoostAction.started +=
                HandleVBoostStarted;

            vBoostAction.canceled +=
                HandleVBoostCanceled;

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
            vBoostAction.started -=
                HandleVBoostStarted;

            vBoostAction.canceled -=
                HandleVBoostCanceled;

            vBoostAction.Disable();
        }

        ClearInput();

        m_isInputEnabled = false;
    }

    /// <summary>
    /// コンポーネント無効化時に入力を停止します。
    /// </summary>
    private void OnDisable()
    {
        DisableInput();
    }

    /// <summary>
    /// 毎フレーム入力状態を更新します。
    /// </summary>
    private void Update()
    {
        UpdateVBoostHold();
    }

    /// <summary>
    /// Vブーストの長押し状態を更新します。
    /// </summary>
    private void UpdateVBoostHold()
    {
        if (!m_isVBoostHeld)
        {
            m_vBoostHoldTime = 0.0f;
            return;
        }

        // すでにこの押下中に長押しをトリガー済みなら再判定しない
        // （ConsumeVBoostHoldStarted()で外部フラグが消費されても
        // 　この内部ガードは戻らないため、押しっぱなし中の
        // 　多重トリガーを防げる）
        if (m_vBoostHoldTriggeredThisPress)
        {
            return;
        }

        m_vBoostHoldTime += Time.deltaTime;

        if (m_vBoostHoldTime < V_BOOST_HOLD_TIME)
        {
            return;
        }

        m_hasVBoostHoldStarted = true;
        m_vBoostHoldTriggeredThisPress = true;
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
    /// 攻撃入力を取得します。
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
    /// Vブースト入力を開始します。
    /// </summary>
    /// <param name="context">入力情報。</param>
    private void HandleVBoostStarted(
        InputAction.CallbackContext context)
    {
        // 前回の入力状態をリセット
        m_hasVBoostReleased = false;
        m_hasVBoostHoldStarted = false;
        m_vBoostHoldTriggeredThisPress = false;
        m_vBoostHoldTime = 0.0f;

        // 今回のVブースト開始を記録
        m_hasVBoostStarted = true;

        // 現在Vブースト入力が押されている
        m_isVBoostHeld = true;
    }

    /// <summary>
    /// Vブースト入力を終了します。
    /// </summary>
    /// <param name="context">入力情報。</param>
    private void HandleVBoostCanceled(
        InputAction.CallbackContext context)
    {
        m_isVBoostHeld = false;
        m_hasVBoostReleased = true;

        // 長押し成立前に離した場合は
        // 長押し開始フラグを残さない
        if (m_vBoostHoldTime < V_BOOST_HOLD_TIME)
        {
            m_hasVBoostHoldStarted = false;
        }

        m_vBoostHoldTime = 0.0f;
    }

    /// <summary>
    /// 現在の入力状態をリセットします。
    /// </summary>
    private void ClearInput()
    {
        m_moveInput = Vector2.zero;
        m_hasAttackInput = false;
        m_hasJumpInput = false;
        m_hasVBoostStarted = false;
        m_isVBoostHeld = false;
        m_hasVBoostHoldStarted = false;
        m_vBoostHoldTriggeredThisPress = false;
        m_hasVBoostReleased = false;
        m_vBoostHoldTime = 0.0f;
    }

    /// <summary>
    /// InputActionを取得します。
    /// </summary>
    /// <param name="actionReference">入力アクション参照。</param>
    /// <param name="action">取得した入力アクション。</param>
    /// <returns>
    /// true：入力アクションを取得できた。
    /// false：入力アクションを取得できなかった。
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