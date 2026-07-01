using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerWalking : StateBase<Player>
{
    // ---------------------------------------------------------------
    // ステート共通処理
    // ---------------------------------------------------------------

    /// <summary>
    /// 状態開始時に呼ばれる
    /// </summary>
    protected override void OnStartState()
    { 
    }

    /// <summary>
    /// 一定間隔の更新処理
    /// </summary>
    protected override void OnFixedUpdate() 
    { 
    }

    /// <summary>
    /// 毎フレームの更新処理
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過時間</param>
    protected override void OnUpdate(float deltaTime) 
    {
        // 移動キーが放されたら
        if (!Owner.IsPressedMoveInput())
        {
            // 移動状態になる
            Machine.ChangeState<PlayerIdling>();
        }

        // 移動方向を取得する
        Vector2 moveInput = Owner.GetMoveInput();
        Vector3 input = new Vector3(moveInput.x, 0.0f, moveInput.y);

        // 2方向に入力されている場合
        if(input.magnitude > 1.0f)
        {
            // 正規化
            input.Normalize();
        }

        // 目標速度を計算
        Vector3 targetVelocity = input * (5.0f * deltaTime);
        // Rigitbodyに速度を設定
        Rigidbody rigidbody = Owner.GetComponent<Rigidbody>();
        rigidbody.linearVelocity = input;
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
