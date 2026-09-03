using UnityEngine;

public class PlayerAttacking : StateBase<Player>
{
    // 入力受付時間定数
    [DebugParameterField] const float INPUT_ACCEPT_TIME = 1.0f;
    // 残り入力受付時間
    float m_inputAcceptTimeLeft;

    // 生成した攻撃判定コライダー
    GameObject m_attackCollider;

    // ---------------------------------------------------------------
    // ステート共通処理
    // ---------------------------------------------------------------

    /// <summary>
    /// 状態開始時に呼ばれる
    /// </summary>
    protected override void OnStartState()
    {
        // 残り入力受付時間の初期化
        m_inputAcceptTimeLeft = INPUT_ACCEPT_TIME;

        // 攻撃判定コライダーの生成
        generateAttackCollider();
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
        // 時間経過計算
        m_inputAcceptTimeLeft -= deltaTime;

        // 入力受付中に攻撃ボタンが押されなかったら
        if (m_inputAcceptTimeLeft < 0.0f)
        {
            // 待機状態になる
            Machine.ChangeState<PlayerIdling>();
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
        // 攻撃キーを放された状態にする
        Owner.SetAttackPressed(false);
        // キー判定のリセット
        Owner.ResetKeyPressed();

        // 攻撃コライダーを削除
        if (m_attackCollider != null)
        {
            GameObject.Destroy(m_attackCollider);
        }
    }

    /// <summary>
    /// 攻撃判定コライダーを生成する
    /// </summary>
    private void generateAttackCollider()
    {
        // 攻撃判定コライダーを生成する
        m_attackCollider = GameObject.CreatePrimitive(PrimitiveType.Cube);

        // 座標を決める
        Vector3 initialPosition = Owner.transform.position + Owner.transform.forward;
        m_attackCollider.transform.position = initialPosition;
        // 回転情報を決める
        m_attackCollider.transform.rotation = Owner.transform.rotation;

        // レイヤーを設定する
        m_attackCollider.layer = LayerMask.NameToLayer("Attack");
    }
}
