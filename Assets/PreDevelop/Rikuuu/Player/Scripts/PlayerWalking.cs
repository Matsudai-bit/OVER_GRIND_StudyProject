using UnityEngine;

public class PlayerWalking : StateBase<Player>
{
    // 物理ボディ
    Rigidbody m_rigidbody;

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
    }

    /// <summary>
    /// 一定間隔の更新処理
    /// </summary>
    protected override void OnFixedUpdate() 
    {
        // 移動キーが放されたら
        if (!Owner.IsPressedMoveInput())
        {
            // 待機状態になる
            Machine.ChangeState<PlayerIdling>();
        }
        // ジャンプキーが押されたら
        if (Owner.IsPressedJumpInput())
        {
            // ジャンプ状態になる
            Machine.ChangeState<PlayerJumping>();
            return;
        }
        // 攻撃キーが押されたら
        if (Owner.IsPressedAttackInput())
        {
            // 攻撃状態になる
            Machine.ChangeState<PlayerAttacking>();
            return;
        }

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
        // Y速度(重力・ジャンプ)はそのまま維持
        targetVelocity.y = m_rigidbody.linearVelocity.y; 
        // Rigitbodyに速度を設定
        m_rigidbody.linearVelocity = targetVelocity;
    }

    /// <summary>
    /// 毎フレームの更新処理
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過時間</param>
    protected override void OnUpdate(float deltaTime) 
    {
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
    }
}
