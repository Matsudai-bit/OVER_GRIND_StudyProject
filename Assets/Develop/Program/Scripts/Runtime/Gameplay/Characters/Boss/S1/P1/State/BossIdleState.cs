/// <summary>
/// ボスを一定時間待機させます。
/// </summary>
public sealed class BossIdleState : StateBase<BossController>
{
    // 待機時間
    private readonly float m_duration;

    // 経過時間
    private float m_elapsedTime;

    /// <summary>
    /// 待機ステートを生成します。
    /// </summary>
    /// <param name="duration">待機時間。</param>
    public BossIdleState(float duration)
    {
        m_duration = duration > 0.0f ? duration : 0.0f;
    }

    /// <summary>
    /// 待機を開始します。
    /// </summary>
    protected override void OnStartState()
    {
        m_elapsedTime = 0.0f;
        Owner.SetStateExecutionStatus(StateExecutionStatus.RUNNING);
    }

    /// <summary>
    /// 待機時間を更新します。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過時間。</param>
    protected override void OnUpdate(float deltaTime)
    {
        m_elapsedTime += deltaTime;

        if (m_elapsedTime < m_duration)
        {
            return;
        }

        Owner.SetStateExecutionStatus(StateExecutionStatus.SUCCEEDED);
    }

    /// <summary>
    /// 待機を終了します。
    /// </summary>
    protected override void OnExitState()
    {
        if (Owner.GetStateExecutionStatus() ==
            StateExecutionStatus.RUNNING)
        {
            Owner.SetStateExecutionStatus(
                StateExecutionStatus.FAILED);
        }
    }
}
