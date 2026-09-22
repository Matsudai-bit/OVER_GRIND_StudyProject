/// <summary>
/// ステージ1フェーズ1の脚崩壊とフェーズ移行を実行します。
/// </summary>
public sealed class S1P1BossLegsCollapsingState :
    StateBase<BossController>
{
    // 脚管理
    private readonly S1P1BossLegsController m_legsController;

    // フェーズ移行待機時間
    private float m_transitionDuration;

    // 経過時間
    private float m_elapsedTime;

    /// <summary>
    /// 脚崩壊ステートを生成します。
    /// </summary>
    /// <param name="legsController">脚管理。</param>
    public S1P1BossLegsCollapsingState(
        S1P1BossLegsController legsController)
    {
        m_legsController =
            legsController;
    }

    /// <summary>
    /// 脚崩壊を開始します。
    /// </summary>
    protected override void OnStartState()
    {
        if (m_legsController == null ||
            Owner.PhaseController == null ||
            !TryGetTransitionDuration(
                out m_transitionDuration))
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);

            return;
        }

        m_elapsedTime = 0.0f;

        Owner.SetStateExecutionStatus(
            StateExecutionStatus.RUNNING);

        //m_legsController.CollapseLegs();
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

        if (m_elapsedTime <
            m_transitionDuration)
        {
            return;
        }


        if (!Owner.PhaseController.AdvancePhase())
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);
        }
        Owner.SetStateExecutionStatus(
            StateExecutionStatus.SUCCEEDED);
    }

    /// <summary>
    /// フェーズ移行待機時間を取得します。
    /// </summary>
    /// <param name="transitionDuration">取得した待機時間。</param>
    /// <returns>
    /// true：取得できました。
    /// false：取得できませんでした。
    /// </returns>
    private bool TryGetTransitionDuration(
        out float transitionDuration)
    {
        transitionDuration = 0.0f;

        if (!Owner.PhaseController.TryGetCurrentPhaseComponent(
                out S1P1BossReferences references) ||
            references.StateParameterAsset == null ||
            references.StateParameterAsset.LegsCollapsing == null)
        {
            return false;
        }

        transitionDuration =
            references.StateParameterAsset
                .LegsCollapsing
                .TransitionDuration;

        return true;
    }
}
