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

    // プレイヤー攻撃機能
    private PlayerAttackController m_attackController;

    // スプライングラインド機能
    private SplineGrindController m_splineGrindController;

    // Vゲージ表示機能
    private VGaugeUI m_vGaugeUI;

    // 速度表示機能
    private VSpeedUI m_vSpeedUI;

    // 通常移動パラメータ
    private PlayerMovementParameterAsset
        m_movementParameterAsset;

    // Vブースト移動パラメータ
    private PlayerVBoostMovementParameterAsset
        m_vBoostMovementParameterAsset;

    // プレイヤー用ステートマシン
    private StateMachine<PlayerStateMachineComponent>
        m_stateMachine;

    // 初期化されているか
    private bool m_isInitialized;

    /// <summary>
    /// プレイヤー入力を取得します。
    /// </summary>
    public PlayerInputReader InputReader =>
        m_inputReader;

    /// <summary>
    /// プレイヤー監視機能を取得します。
    /// </summary>
    public PlayerMonitor Monitor =>
        m_monitor;

    /// <summary>
    /// プレイヤー移動機能を取得します。
    /// </summary>
    public PlayerMotor Motor =>
        m_motor;

    /// <summary>
    /// プレイヤー攻撃機能を取得します。
    /// </summary>
    public PlayerAttackController AttackController =>
        m_attackController;

    /// <summary>
    /// プレイヤーアニメーション表示機能を取得します。
    /// </summary>
    public PlayerAnimationPresenter AnimationPresenter =>
        m_animationPresenter;

    /// <summary>
    /// スプライングラインド機能を取得します。
    /// </summary>
    public SplineGrindController GrindController =>
        m_splineGrindController;

    /// <summary>
    /// Vゲージ表示機能を取得します。
    /// シーンに配置されていない場合はnullを返すことがあります。
    /// </summary>
    public VGaugeUI VGaugeUI =>
        m_vGaugeUI;

    /// <summary>
    /// 速度表示機能を取得します。
    /// シーンに配置されていない場合はnullを返すことがあります。
    /// </summary>
    public VSpeedUI VSpeedUI =>
        m_vSpeedUI;

    /// <summary>
    /// 通常移動パラメータを取得します。
    /// </summary>
    public PlayerMovementParameterAsset MovementParameterAsset =>
        m_movementParameterAsset;

    /// <summary>
    /// Vブースト移動パラメータを取得します。
    /// </summary>
    public PlayerVBoostMovementParameterAsset
        VBoostMovementParameterAsset =>
            m_vBoostMovementParameterAsset;

    /// <summary>
    /// 初期化されているかを取得します。
    /// </summary>
    public bool IsInitialized =>
        m_isInitialized;


    // チャージ解除時点でのゲージ量（0～1）
    private float m_carriedBoostGaugeRate;

    public float CarriedBoostGaugeRate
    {
        get => m_carriedBoostGaugeRate;
        set => m_carriedBoostGaugeRate = Mathf.Clamp01(value);
    }

    // Vブースト中断時点／進行中の残りゲージ量（0～1）
    // PlayerVRunningStateが開始・参照・更新し、
    // 中断中（Idling/Jumping中）もこの本体側で消費し続けます
    private float m_suspendedBoostGaugeRate;

    public float SuspendedBoostGaugeRate
    {
        get => m_suspendedBoostGaugeRate;
        set => m_suspendedBoostGaugeRate = Mathf.Clamp01(value);
    }

    // Vブーストが中断中か
    // （trueの間、移動入力・接地などの条件が揃えばVRunningへ復帰する）
    private bool m_isBoostSuspended;

    public bool IsBoostSuspended
    {
        get => m_isBoostSuspended;
        set => m_isBoostSuspended = value;
    }

    // ゲージを1秒あたりどれだけ消費するか
    // （VRunningState開始時に設定される）
    private float m_boostGaugeDepletionRatePerSecond;

    /// <summary>
    /// ゲージの1秒あたりの消費レートを設定します。
    /// PlayerVRunningStateがブースト開始時に呼び出します。
    /// </summary>
    public void SetBoostGaugeDepletionRate(float ratePerSecond)
    {
        m_boostGaugeDepletionRatePerSecond =
            Mathf.Max(ratePerSecond, 0.0f);
    }

    /// <summary>
    /// プレイヤー用ステートマシンを初期化します。
    /// </summary>
    public void Initialize(
        PlayerInputReader inputReader,
        PlayerMonitor monitor,
        PlayerMotor motor,
        PlayerAnimationPresenter animationPresenter,
        PlayerAttackController attackController,
        SplineGrindController splineGrindController,
        PlayerMovementParameterAsset movementParameterAsset,
        PlayerVBoostMovementParameterAsset
            vBoostMovementParameterAsset,
        VGaugeUI vGaugeUI,
        VSpeedUI vSpeedUI)
    {
        if (inputReader == null ||
            monitor == null ||
            motor == null ||
            animationPresenter == null ||
            attackController == null ||
            splineGrindController == null ||
            movementParameterAsset == null ||
            vBoostMovementParameterAsset == null)
        {
            Debug.LogError(
                $"[{nameof(PlayerStateMachineComponent)}] " +
                "初期化に必要な参照が不足しています。",
                this);

            m_isInitialized = false;
            return;
        }

        // VGaugeUI・VSpeedUIはUI側の演出用途のため、
        // 未設定でも初期化失敗とはしない
        if (vGaugeUI == null)
        {
            Debug.LogWarning(
                $"[{nameof(PlayerStateMachineComponent)}] " +
                $"{nameof(VGaugeUI)}が設定されていません。" +
                "ゲージ演出は行われません。",
                this);
        }

        if (vSpeedUI == null)
        {
            Debug.LogWarning(
                $"[{nameof(PlayerStateMachineComponent)}] " +
                $"{nameof(VSpeedUI)}が設定されていません。" +
                "速度表示は行われません。",
                this);
        }

        m_stateMachine?.Dispose();

        m_inputReader = inputReader;
        m_monitor = monitor;
        m_motor = motor;
        m_animationPresenter =
            animationPresenter;

        m_attackController =
            attackController;

        m_splineGrindController =
            splineGrindController;

        m_movementParameterAsset =
            movementParameterAsset;

        m_vBoostMovementParameterAsset =
            vBoostMovementParameterAsset;

        m_vGaugeUI = vGaugeUI;
        m_vSpeedUI = vSpeedUI;

        m_stateMachine =
            new StateMachine<PlayerStateMachineComponent>(
                this);

        m_isInitialized = true;

        m_stateMachine.ChangeState<PlayerIdlingState>();
    }

    /// <summary>
    /// 現在のステートが指定された型か確認します。
    /// </summary>
    public bool IsCurrentState<TState>()
        where TState :
            StateBase<PlayerStateMachineComponent>
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
    /// 物理状態とステートを更新します。
    /// </summary>
    private void FixedUpdate()
    {
        if (!m_isInitialized)
        {
            return;
        }

        m_monitor.Refresh();

        m_stateMachine.FixedUpdate();

        // 現在のStateに関係なく、常に実速度をUIへ反映する
        UpdateSpeedDisplay();

        // Vブースト中（中断中を含む）は、
        // 現在のStateに関係なく常にゲージを消費する
        UpdateSuspendableBoostGauge();
    }

    /// <summary>
    /// 現在の水平速度をVSpeedUIへ反映します。
    /// </summary>
    private void UpdateSpeedDisplay()
    {
        if (m_vSpeedUI == null)
        {
            return;
        }

        m_vSpeedUI.SetSpeed(m_motor.HorizontalSpeed);
    }

    /// <summary>
    /// Vブースト中（VRunningState中・中断中の両方）の
    /// ゲージ消費とUI反映を、Stateに関係なく行います。
    /// </summary>
    private void UpdateSuspendableBoostGauge()
    {
        bool isConsumingGauge =
            IsCurrentState<PlayerVRunningState>() ||
            m_isBoostSuspended;

        if (!isConsumingGauge)
        {
            return;
        }

        m_suspendedBoostGaugeRate -=
            m_boostGaugeDepletionRatePerSecond *
            (Time.fixedDeltaTime / 10.0f);

        if (m_suspendedBoostGaugeRate < 0.0f)
        {
            m_suspendedBoostGaugeRate = 0.0f;
        }

        if (m_vGaugeUI != null)
        {
            m_vGaugeUI.SetGaugeRate(m_suspendedBoostGaugeRate);
        }

        // 中断中にゲージを使い切った場合も、
        // 復帰しようがないため中断状態を解除しておく
        if (m_suspendedBoostGaugeRate <= 0.0f &&
            m_isBoostSuspended)
        {
            m_isBoostSuspended = false;

            if (m_vGaugeUI != null)
            {
                m_vGaugeUI.SetCharging(false);
            }
        }
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