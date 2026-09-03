using UnityEngine;

/// <summary>
/// ボス全体の状態実行を管理します。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BossPhaseController))]
[RequireComponent(typeof(BossAnimationController))]
[RequireComponent(typeof(BossMotor))]
[RequireComponent(typeof(AttackHitboxRegistry))]
[RequireComponent(typeof(BossNavigation))]
public sealed class BossController : MonoBehaviour, IStateStatusProvider
{
    // フェーズ管理
    [SerializeField, Header("ボス制御")]
    private BossPhaseController m_phaseController;

    // アニメーション管理
    [SerializeField]
    private BossAnimationController m_animationController;

    // 移動管理
    [SerializeField]
    private BossMotor m_motor;

    // 攻撃判定管理
    [SerializeField]
    private AttackHitboxRegistry m_attackHitboxRegistry;

    // 移動ナビゲーション
    [SerializeField]
    private BossNavigation m_bossNavigation;

    // 現在のステート実行状態
    [SerializeField, Header("デバッグ")]
    private StateExecutionStatus m_currentStatus = StateExecutionStatus.SUCCEEDED;

    // ボス全体で使用するステートマシン
    private StateMachine<BossController> m_stateMachine;


    /// <summary>
    /// ステートマシンを取得します。
    /// </summary>
    public StateMachine<BossController> StateMachine => m_stateMachine;

    /// <summary>
    /// フェーズ管理を取得します。
    /// </summary>
    public BossPhaseController PhaseController => m_phaseController;

    /// <summary>
    /// アニメーション管理を取得します。
    /// </summary>
    public BossAnimationController AnimationController => m_animationController;

    /// <summary>
    /// 移動管理を取得します。
    /// </summary>
    public BossMotor Motor => m_motor;

    /// <summary>
    /// 攻撃判定管理を取得します。
    /// </summary>
    public AttackHitboxRegistry AttackHitboxRegistry => m_attackHitboxRegistry;

    /// <summary>
    /// ボスナビゲーションを取得する
    /// </summary>
    public BossNavigation Navigation => m_bossNavigation;

    /// <summary>
    /// 初期化します。
    /// </summary>
    private void Awake()
    {
        CacheComponents();
        m_stateMachine = new StateMachine<BossController>(this);
    }

    /// <summary>
    /// ステートを更新します。
    /// </summary>
    private void Update()
    {
        m_stateMachine?.Update(Time.deltaTime);
    }

    /// <summary>
    /// 物理ステートを更新します。
    /// </summary>
    private void FixedUpdate()
    {
        m_stateMachine?.FixedUpdate();
    }

    /// <summary>
    /// 現在のステート実行状態を取得します。
    /// </summary>
    /// <returns>現在のステート実行状態。</returns>
    public StateExecutionStatus GetStateExecutionStatus()
    {
        return m_currentStatus;
    }

    /// <summary>
    /// ステート実行状態を設定します。
    /// </summary>
    /// <param name="status">設定する実行状態。</param>
    public void SetStateExecutionStatus(StateExecutionStatus status)
    {
        m_currentStatus = status;
    }

    /// <summary>
    /// 必要なコンポーネントを取得します。
    /// </summary>
    private void CacheComponents()
    {
        if (m_phaseController == null)
        {
            m_phaseController = GetComponent<BossPhaseController>();
        }

        if (m_animationController == null)
        {
            m_animationController = GetComponent<BossAnimationController>();
        }

        if (m_motor == null)
        {
            m_motor = GetComponent<BossMotor>();
        }

        if (m_attackHitboxRegistry == null)
        {
            m_attackHitboxRegistry = GetComponent<AttackHitboxRegistry>();
        }

        if (m_bossNavigation == null)
        {
            m_bossNavigation = GetComponent<BossNavigation>();
        }
    }

   
}
