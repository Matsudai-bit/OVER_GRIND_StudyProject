using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    // ステートマシーン
    private StateMachine<Player> m_stateMachine;

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

    // ---------------------------------------------------------------
    // 取得・設定
    // ---------------------------------------------------------------

    /// <summary>
    /// ステートマシーンを返す
    /// </summary>
    public StateMachine<Player> GetStateMachine()
    {
        return m_stateMachine;
    }

}
