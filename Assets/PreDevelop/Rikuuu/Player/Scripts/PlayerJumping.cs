using Unity.VisualScripting;
using UnityEngine;

public class PlayerJumping : StateBase<Player>
{
    // 物理ボディ
    Rigidbody m_rigidbody;
    // ジャンプ開始時の進行方向
    Vector3 m_initialVelocity;

    // 入力受付時間定数
    [DebugParameterField] const float INPUT_DELAY_TIME = 0.25f;
    // 残り入力受付時間
    float m_inputDelayTimeLeft;

    // ---------------------------------------------------------------
    // ステート共通処理
    // ---------------------------------------------------------------

    /// <summary>
    /// 状態開始時に呼ばれる
    /// </summary>
    protected override void OnStartState()
    {
        // Rigitbodyコンポーネントを取得する
        m_rigidbody = Owner.GetComponent<Rigidbody>();
        // 進行方向の初期化
        m_initialVelocity = m_rigidbody.linearVelocity;

        // 接地状態を強制的に解除する（衝突判定の更新遅れ対策）
        Owner.SetGrounded(false);

        // 上方向ベクトルを加算する
        m_rigidbody.linearVelocity = new Vector3(m_rigidbody.linearVelocity.x, 0f, m_rigidbody.linearVelocity.z);
        m_rigidbody.AddForce(Vector3.up * 5.0f, ForceMode.Impulse);
    }

    /// <summary>
    /// 一定間隔の更新処理
    /// </summary>
    protected override void OnFixedUpdate()
    {
        // 一定時間経過したら
        if (m_inputDelayTimeLeft > INPUT_DELAY_TIME)
        {
            // 移動方向を取得する
            Vector2 moveInput = Owner.GetMoveInput();
            Vector3 input = new Vector3(moveInput.x, 0.0f, moveInput.y);

            // 2方向に入力されている場合
            if (input.magnitude > 1.0f)
            {
                // 正規化
                input.Normalize();
            }

            // 目標速度を計算
            Vector3 targetVelocity = input * Owner.GetMoveSpeed();
            if (IsOppositeDirection(m_initialVelocity.x, input.x))
            {
                targetVelocity.x *= Owner.GetJumpHorizontalSpeedModifier();
            }
            if (IsOppositeDirection(m_initialVelocity.z, input.z))
            {
                targetVelocity.z *= Owner.GetJumpHorizontalSpeedModifier();
            }

            // Y速度(重力・ジャンプ)はそのまま維持
            targetVelocity.y = 0.0f;
            // Rigitbodyに速度を設定
            m_rigidbody.linearVelocity += targetVelocity;
        }
    }

    /// <summary>
    /// 毎フレームの更新処理
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過時間</param>
    protected override void OnUpdate(float deltaTime)
    {
        // 着地した場合
        if (Owner.IsGrounded())
        {
            // 待機状態になる
            Machine.ChangeState<PlayerIdling>();
        }

        if(m_inputDelayTimeLeft < INPUT_DELAY_TIME)
        {
            m_inputDelayTimeLeft += deltaTime;
        }
    }

    /// <summary>
    /// 描画処理
    /// </summary>
    protected override void OnDraw()
    {
    }

    /// <summary>
    /// 状態終了時に呼ばれる
    /// </summary>
    protected override void OnExitState()
    {
        // ジャンプしていない状態にする
        Owner.SetJumpPressed(false);
    }

    private bool IsOppositeDirection(float nowVelocity, float inputVelocity)
    {
        if((nowVelocity < 0.0f && inputVelocity > 0.0f) ||
           (nowVelocity > 0.0f && inputVelocity < 0.0f))
        {
            return true;
        }
        return false;
    }
}
