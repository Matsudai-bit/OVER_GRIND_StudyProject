using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    // ---------------------------------------------------------------
    // パラメータ（Inspectorから変更可能）
    // ---------------------------------------------------------------
    
    [Header("移動パラメータ")]
    [Tooltip("移動速度（m/s）")]
    [SerializeField] private float m_moveSpeed = 5.0f;

    [Header("ジャンプパラメータ")]
    [Tooltip("ジャンプ力")]
    [SerializeField] private float m_jumpForce = 5.0f;

    // ---------------------------------------------------------------
    // 内部変数
    // ---------------------------------------------------------------

    // ステートマシーン
    private StateMachine<Player> m_stateMachine;

    // 物理ボディ
    private Rigidbody m_rigidbody;

    // 着地しているかどうか
    private bool m_isGround;

    // Input Systemから受け取る移動入力
    private Vector2 m_moveInput;
    // ジャンプ入力バッファ
    private bool m_jumpPressed;     // ジャンプボタンが押されたか
    private bool m_jumpHeld;        // ジャンプボタンが長押しされているか
    // 攻撃入力バッファ
    private bool m_attackPressed;   // 攻撃ボタンが押されたか

    // ---------------------------------------------------------------
    // Unity ライフサイクル
    // ---------------------------------------------------------------

    /// <summary>
    /// 読込時処理
    /// </summary>
    private void Awake()
    {
        // Rigitbodyコンポーネントを取得する
        m_rigidbody = GetComponent<Rigidbody>();
        // 物理演算による回転をしないようにする
        m_rigidbody.freezeRotation = true;
    }

    /// <summary>
    /// 初期化処理
    /// </summary>
    void Start()
    {
        // ステートマシーンの初期化
        m_stateMachine = new StateMachine<Player>(this);
        // 待機状態にする
        m_stateMachine.ChangeState<PlayerIdling>();

        // 地面についているかどうかの判定の初期化
        m_isGround = false;
    }

    /// <summary>
    /// 一定間隔の更新処理
    /// </summary>
    private void FixedUpdate()
    {
        // 状態の更新を行う
        m_stateMachine.FixedUpdate();
    }

    /// <summary>
    /// 毎フレームの更新処理
    /// </summary>
    void Update()
    {
        // 状態の更新を行う
        m_stateMachine.Update(Time.deltaTime);
    }

    // ---------------------------------------------------------------
    // Input System コールバック
    // ---------------------------------------------------------------

    /// <summary>
    /// Input System から送られてきた移動入力を受け取る
    /// </summary>
    public void OnMove(InputAction.CallbackContext context)
    {
        m_moveInput = context.ReadValue<Vector2>();
    }

    /// <summary>
    /// Input System から送られてきたジャンプ入力を受け取る
    /// </summary>
    public void OnJump(InputAction.CallbackContext context)
    {
        // ジャンプボタンが押されたら
        if(context.started)
        {
            m_jumpPressed = true;
        }

        // ジャンプボタンが長押しされている場合
        if(context.performed)
        {
            m_jumpHeld = true;
        }
        // ジャンプボタンを話したら
        if(context.canceled)
        {
            m_jumpHeld = false;
        }
    }

    /// <summary>
    /// Input System から送られてきた攻撃入力を受け取る
    /// </summary>
    public void OnAttack(InputAction.CallbackContext context)
    {
        // 攻撃ボタンが押されたら
        if (context.started)
        {
            m_attackPressed = true;
        }
    }

    // ---------------------------------------------------------------
    // ステートの遷移条件
    // ---------------------------------------------------------------

    /// <summary>
    /// 移動キーが押されているか
    /// </summary>
    public bool IsPressedMoveInput()
    {
        // 移動キーが押されている場合
        if(m_moveInput != Vector2.zero)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// ジャンプキーが押されているか
    /// </summary>
    public bool IsPressedJumpInput()
    {
        return (m_jumpPressed && m_isGround);
    }

    /// <summary>
    /// 攻撃キーが押されているか
    /// </summary>
    public bool IsPressedAttackInput()
    {
        return m_attackPressed;
    }

    // ---------------------------------------------------------------
    // 取得・設定
    // ---------------------------------------------------------------

    /// <summary>
    /// 移動方向を返す
    /// </summary>
    public Vector2 GetMoveInput()
    {
        return m_moveInput;
    }

    /// <summary>
    /// ジャンプキーが押されたかどうかの状態を設定する
    /// </summary>
    public void SetJumpPressed(bool jumpPressed)
    {
        m_jumpPressed = jumpPressed;
    }

    /// <summary>
    /// 攻撃キーが押されたかどうかの状態を設定する
    /// </summary>
    public void SetAttackPressed(bool attackPressed)
    {
        m_attackPressed = attackPressed;
    }

    /// <summary>
    /// 着地しているかどうかの判定を返す
    /// </summary>
    public bool IsGrounded()
    {
        return m_isGround;
    }
    /// <summary>
    /// 着地しているかどうかの判定を設定する
    /// </summary>
    public void SetGrounded(bool isGround)
    {
        m_isGround = isGround;
    }

    // ---------------------------------------------------------------
    // 衝突判定
    // ---------------------------------------------------------------

    /// <summary>
    /// オブジェクトと衝突したら行う処理
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Hit");
        if (collision.collider.CompareTag("Floor"))
        {
            Debug.Log("HitFloor");
            m_isGround = true;
        }
    }

    /// <summary>
    /// オブジェクトから離れたら行う処理
    /// </summary>
    private void OnCollisionExit(Collision collision)
    {
        Debug.Log("Exit");
        if (collision.collider.CompareTag("Floor"))
        {
            Debug.Log("ExitFloor");
            m_isGround = false;
        }
    }
}
