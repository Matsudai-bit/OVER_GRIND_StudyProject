

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ステートマシン
/// <para>Push / Pop によるスタック構造で状態を管理する。</para>
/// </summary>
/// <typeparam name="TOwner">状態の所有者の型</typeparam>
public class StateMachine<TOwner>
{
    // ---------------------------------------------------------------
    // 定数
    // ---------------------------------------------------------------

    /// <summary>変更命令の初期値（何もしない）</summary>
    private static readonly Action INITIAL_CHANGE_FUNC = () => { };

    // ---------------------------------------------------------------
    // データメンバ
    // ---------------------------------------------------------------

    private readonly TOwner m_owner;                            ///< 所有者

    /// <summary>
    /// ステートのスタック（末尾 = 現在アクティブなステート）
    /// </summary>
    private readonly LinkedList<StateBase<TOwner>> m_stateList = new();

    /// <summary>次フレーム先頭で実行するステート変更命令</summary>
    private Action m_fnChangeState;

    // ---------------------------------------------------------------
    // コンストラクタ / デストラクタ相当
    // ---------------------------------------------------------------

    /// <param name="owner">ステートの所有者</param>
    public StateMachine(TOwner owner)
    {
        m_owner         = owner;
        m_fnChangeState = INITIAL_CHANGE_FUNC;
    }

    /// <summary>
    /// 破棄時に現在のステートの終了処理を呼ぶ
    /// （MonoBehaviour の OnDestroy などから明示的に呼んでください）
    /// </summary>
    public void Dispose()
    {
        GetNowState()?.CallExit();
        m_stateList.Clear();
    }

    // ---------------------------------------------------------------
    // 公開操作
    // ---------------------------------------------------------------

    /// <summary>
    /// 現在のステートを終了させて新しいステートに切り替える。
    /// 実際の切り替えは次回 Update の冒頭で行われる。
    /// </summary>
    /// <typeparam name="TState">切り替え先のステート型</typeparam>
    /// <param name="args">ステートのコンストラクタに渡す引数</param>
    public void ChangeState<TState>(params object[] args)
        where TState : StateBase<TOwner>
    {
        StateChanger<TState>(isPop: true, args);
    }

    /// <summary>
    /// 現在のステートを残したまま新しいステートをスタックに積む。
    /// 実際の追加は次回 Update の冒頭で行われる。
    /// </summary>
    /// <typeparam name="TState">追加するステート型</typeparam>
    /// <param name="args">ステートのコンストラクタに渡す引数</param>
    public void PushState<TState>(params object[] args)
        where TState : StateBase<TOwner>
    {
        StateChanger<TState>(isPop: false, args);
    }

    /// <summary>
    /// 現在のステートをスタックから取り除き、ひとつ前の状態に戻る。
    /// 実際のポップは次回 Update の冒頭で行われる。
    /// </summary>
    public void PopState()
    {
        m_fnChangeState = () =>
        {
            StateBase<TOwner> nowState = GetNowState();
            if (nowState == null) return;

            nowState.CallExit();
            m_stateList.RemoveLast();
        };
    }

    /// <summary>
    /// スタック上の全ステートを終了させてクリアする。
    /// 実際のクリアは次回 Update の冒頭で行われる。
    /// </summary>
    public void ClearState()
    {
        m_fnChangeState = () =>
        {
            StateBase<TOwner> nowState = GetNowState();
            if (nowState == null) return;

            nowState.CallExit();
            m_stateList.Clear();
        };
    }

    /// <summary>
    /// 一定間隔の混信処理。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過時間</param>
    public void FixedUpdate()
    {
        // 保留中の変更命令を実行してリセット
        GetNowState()?.CallFixedUpdate();
    }

    /// <summary>
    /// ステートマシンの更新処理。毎フレーム呼ぶこと。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過時間</param>
    public void Update(float deltaTime)
    {
        // 保留中の変更命令を実行してリセット
        m_fnChangeState();
        m_fnChangeState = INITIAL_CHANGE_FUNC;

        GetNowState()?.CallUpdate(deltaTime);
    }



    /// <summary>
    /// 現在アクティブなステートを取得する。
    /// スタックが空の場合は null を返す。
    /// </summary>
    public StateBase<TOwner> GetNowState()
    {
        return m_stateList.Count == 0 ? null : m_stateList.Last.Value;
    }

    /// <summary>
    /// 現在のステートが指定した型かどうかを返す。
    /// </summary>
    public bool IsCurrentState<TState>() where TState : StateBase<TOwner>
    {
        return GetNowState() is TState;
    }

    // ---------------------------------------------------------------
    // 内部実装
    // ---------------------------------------------------------------

    /// <summary>
    /// ステート変更命令をラムダに包んで予約する共通処理。
    /// </summary>
    /// <param name="isPop">true = 現在のステートをポップしてから積む（ChangeState）</param>
    /// <param name="args">新ステートのコンストラクタ引数</param>
    private void StateChanger<TState>(bool isPop, object[] args)
        where TState : StateBase<TOwner>
    {
        m_fnChangeState = () =>
        {
            if (m_owner == null) return;

            // 現在のステートを終了
            StateBase<TOwner> nowState = GetNowState();
            if (nowState != null)
            {
                nowState.CallExit();
                Debug.Log(nowState.GetType().Name + "の終了");
                if (isPop)
                {
                    m_stateList.RemoveLast();
                }
            }

            // 新しいステートを生成
            // 引数なしなら Activator.CreateInstance<TState>() の方が型安全
            StateBase<TOwner> newState = args.Length == 0
                ? Activator.CreateInstance<TState>()
                : (StateBase<TOwner>)Activator.CreateInstance(typeof(TState), args);

            if (newState == null)
            {
                Debug.LogError($"[StateMachine] ステートの生成に失敗しました: {typeof(TState).Name}");
                return;
            }

            // オーナーとマシンをステートに渡す
            newState.SetOwner(m_owner);
            newState.SetMachine(this);

            // 開始処理
            Debug.Log(newState.GetType() + "の開始");
            newState.CallStart();

            // スタックに積む
            m_stateList.AddLast(newState);
        };
    }
}
