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

    // Input Systemから受け取る移動入力
    private Vector2 m_moveInput;

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
    }

    /// <summary>
    /// 更新処理
    /// </summary>
    void Update()
    {
        // 状態の更新を行う
        m_stateMachine.Update(Time.deltaTime);
    }

    // =========================================================
    // Input System コールバック
    // =========================================================

    /// <summary>
    /// Input System から送られてきた移動入力を受け取り、m_moveInputに保存
    /// </summary>
    public void OnMove(InputAction.CallbackContext context)
    {
        m_moveInput = context.ReadValue<Vector2>();
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

    // ---------------------------------------------------------------
    // 取得・設定
    // ---------------------------------------------------------------

    /// <summary>
    /// Rigitbodyを設定
    /// </summary>
    public void SetRigidbody(Rigidbody rigidbody)
    {
        m_rigidbody = rigidbody;
    }
    /// <summary>
    /// Rigitbodyを返す
    /// </summary>
    public Rigidbody GetRigidbody()
    {
        return m_rigidbody;
    }

    /// <summary>
    /// 移動方向を返す
    /// </summary>
    public Vector2 GetMoveInput()
    {
        return m_moveInput;
    }
}
