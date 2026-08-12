/// <summary>
/// ステージ1フェーズ1の脚崩壊とフェーズ移行を実行します。
/// </summary>
public sealed class S1P1BossLegsCollapsingState : StateBase<BossController>
{
    // 脚管理
    private readonly S1P1BossLegsController m_legsController;

    // フェーズ移行までの待機時間
    private readonly float m_transitionDuration;

    // 経過時間
    private float m_elapsedTime;

    /// <summary>
    /// 脚崩壊ステートを生成します。
    /// </summary>
    /// <param name="legsController">脚管理。</param>
    /// <param name="transitionDuration">フェーズ移行までの待機時間。</param>
    public S1P1BossLegsCollapsingState(
        S1P1BossLegsController legsController,
        float transitionDuration)
    {
        m_legsController = legsController;
        m_transitionDuration =
            transitionDuration > 0.0f ?
            transitionDuration : 0.0f;
    }

    /// <summary>
    /// 脚崩壊を開始します。
    /// </summary>
    protected override void OnStartState()
    {
        if (m_legsController == null ||
            Owner.PhaseController == null)
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);
            return;
        }

        m_elapsedTime = 0.0f;

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.RUNNING);

        m_legsController.CollapseLegs();
    }

    /// <summary>
    /// フェーズ移行待機時間を更新します。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過時間。</param>
    protected override void OnUpdate(float deltaTime)
    {
        if (Owner.GetStateExecutionStatus() !=
            StateExecutionStatus.RUNNING)
        {
            return;
        }

        m_elapsedTime += deltaTime;

        if (m_elapsedTime < m_transitionDuration)
        {
            return;
        }

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.SUCCEEDED);

        if (!Owner.PhaseController.AdvancePhase())
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);
        }
    }
}
