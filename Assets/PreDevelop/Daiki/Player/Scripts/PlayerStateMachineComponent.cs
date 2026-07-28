using UnityEngine;

/// <summary>
/// プレイヤーのステートマシンを管理します。
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerStateMachineComponent : MonoBehaviour
{
    // プレイヤー入力
    private PlayerInputReader m_inputReader;

    // プレイヤー監視機能
    private PlayerMonitor m_monitor;

    // プレイヤー移動機能
    private PlayerMotor m_motor;

    // プレイヤーアニメーション表示機能
    private PlayerAnimationPresenter m_animationPresenter;

    // プレイヤーアタックコントローラ
    private PlayerAttackController m_attackController;

    // プレイヤー用ステートマシン
    private StateMachine<PlayerStateMachineComponent> m_stateMachine;

    // 初期化されているか
    private bool m_isInitialized;

    /// <summary>
    /// プレイヤー入力を取得します。
    /// </summary>
    public PlayerInputReader InputReader => m_inputReader;

    /// <summary>
    /// プレイヤー監視機能を取得します。
    /// </summary>
    public PlayerMonitor Monitor => m_monitor;

    /// <summary>
    /// プレイヤー移動機能を取得します。
    /// </summary>
    public PlayerMotor Motor => m_motor;
    /// <summary>
    /// プレイヤーの攻撃コントローラ
    /// </summary>
    public PlayerAttackController AttackController => m_attackController;

    /// <summary>
    /// プレイヤーアニメーション表示機能を取得します。
    /// </summary>
    public PlayerAnimationPresenter AnimationPresenter =>
        m_animationPresenter;

    /// <summary>
    /// 初期化されているかを取得します。
    /// </summary>
    /// <returns>
    /// true：初期化されています。
    /// false：初期化されていません。
    /// </returns>
    public bool IsInitialized => m_isInitialized;

    /// <summary>
    /// プレイヤー用ステートマシンを初期化します。
    /// </summary>
    /// <param name="inputReader">プレイヤー入力。</param>
    /// <param name="monitor">プレイヤー監視機能。</param>
    /// <param name="motor">プレイヤー移動機能。</param>
    /// <param name="animationPresenter">
    /// プレイヤーアニメーション表示機能。
    /// </param>
    public void Initialize(
        PlayerInputReader inputReader,
        PlayerMonitor monitor,
        PlayerMotor motor,
        PlayerAnimationPresenter animationPresenter,
        PlayerAttackController playerAttackController)
    {
        if (inputReader == null ||
            monitor == null ||
            motor == null ||
            animationPresenter == null ||
            playerAttackController == null)
        {
            Debug.LogError(
                $"[{nameof(PlayerStateMachineComponent)}] " +
                "初期化に必要な参照が不足しています。",
                this);

            m_isInitialized = false;
            return;
        }

        // 再初期化時は既存のステートを破棄
        m_stateMachine?.Dispose();

        m_inputReader = inputReader;
        m_monitor = monitor;
        m_motor = motor;
        m_animationPresenter = animationPresenter;
        m_attackController = playerAttackController;

        m_stateMachine =
            new StateMachine<PlayerStateMachineComponent>(this);

        m_isInitialized = true;

        // 初期状態として待機状態を予約
        m_stateMachine.ChangeState<PlayerIdlingState>();
    }

    /// <summary>
    /// 現在のステートが指定された型か確認します。
    /// </summary>
    /// <typeparam name="TState">確認するステート型。</typeparam>
    /// <returns>
    /// true：指定されたステートです。
    /// false：指定されたステートではありません。
    /// </returns>
    public bool IsCurrentState<TState>()
        where TState : StateBase<PlayerStateMachineComponent>
    {
        if (!m_isInitialized)
        {
            return false;
        }

        return m_stateMachine.IsCurrentState<TState>();
    }

    /// <summary>
    /// ステートマシンを更新します。
    /// </summary>
    private void Update()
    {
        if (!m_isInitialized)
        {
            return;
        }

        m_stateMachine.Update(Time.deltaTime);
    }

    /// <summary>
    /// 物理状態とステートを一定間隔で更新します。
    /// </summary>
    private void FixedUpdate()
    {
        if (!m_isInitialized)
        {
            return;
        }

        // Stateから参照する前に監視情報を更新
        m_monitor.Refresh();

        m_stateMachine.FixedUpdate();
    }

    /// <summary>
    /// ステートマシンを破棄します。
    /// </summary>
    private void OnDestroy()
    {
        m_stateMachine?.Dispose();

        m_stateMachine = null;
        m_isInitialized = false;
    }
}