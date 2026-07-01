using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerIdling : StateBase<Player>
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
        // 移動キーが押されたら
        if (Owner.IsPressedMoveInput())
        {
            // 移動状態になる
            Machine.ChangeState<PlayerWalking>();
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
    }
}
