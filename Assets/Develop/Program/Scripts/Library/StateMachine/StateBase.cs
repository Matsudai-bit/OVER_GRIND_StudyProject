
/// <summary>
/// 状態の基底クラス（テンプレートメソッドパターン）
/// </summary>
/// <typeparam name="TOwner">この状態の所有者の型</typeparam>
public abstract class StateBase<TOwner>
{
    // ---------------------------------------------------------------
    // データメンバ
    // ---------------------------------------------------------------

    private TOwner m_owner;                         ///< 所有者
    private StateMachine<TOwner> m_machine;         ///< 管理元のステートマシン

    // ---------------------------------------------------------------
    // StateMachine からのみ呼び出す内部セッタ／呼び出し口
    // （C# には friend がないため internal で代用）
    // ---------------------------------------------------------------

    internal void SetOwner(TOwner owner)        => m_owner   = owner;
    internal void SetMachine(StateMachine<TOwner> machine) => m_machine = machine;

    internal void CallStart()               => OnStartState();
    internal void CallFixedUpdate() => OnFixedUpdate();
    internal void CallUpdate(float deltaTime) => OnUpdate(deltaTime);
    internal void CallExit()                => OnExitState();

    // ---------------------------------------------------------------
    // 派生クラスが override するテンプレートメソッド
    // ---------------------------------------------------------------

    /// <summary>状態開始時に呼ばれる</summary>
    protected virtual void OnStartState() { }

    /// <summary>一定間隔の更新処理</summary>
    protected virtual void OnFixedUpdate() { }
    /// <summary>毎フレームの更新処理</summary>
    /// <param name="deltaTime">前フレームからの経過時間</param>
    protected virtual void OnUpdate(float deltaTime) { }

    /// <summary>描画処理</summary>
    protected virtual void OnDraw() { }

    /// <summary>状態終了時に呼ばれる</summary>
    protected virtual void OnExitState() { }

    // ---------------------------------------------------------------
    // 派生クラス向け取得プロパティ
    // ---------------------------------------------------------------

    /// <summary>所有者を取得する</summary>
    protected TOwner Owner => m_owner;

    /// <summary>管理元のステートマシンを取得する</summary>
    protected StateMachine<TOwner> Machine => m_machine;
}
