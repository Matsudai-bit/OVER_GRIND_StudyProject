using UnityEngine;

/// <summary>
/// プレイヤーのグラインド状態と離脱先への遷移を管理します。
/// </summary>
public sealed class PlayerGrindingState
    : StateBase<PlayerStateMachineComponent>
{
    private SplineRailInfo m_rail;

    /// <summary>通常の接触検出からグラインドを開始します。</summary>
    public PlayerGrindingState() { }

    /// <summary>ジャンプから戻るレールを明示してグラインドを開始します。</summary>
    /// <param name="rail">搭乗するレール。</param>
    public PlayerGrindingState(SplineRailInfo rail)
    {
        m_rail = rail;
    }

    /// <summary>
    /// 接触中のレールでグラインドを開始します。
    /// </summary>
    protected override void OnStartState()
    {
        if (m_rail == null) m_rail = Owner.Monitor.HitRailInfo;
        Owner.InputReader.ConsumeJumpPress();
        Owner.GrindController.StartGrind(m_rail);
    }

    /// <summary>
    /// 衝突による離脱、通常終了、ジャンプの遷移を処理します。
    /// </summary>
    protected override void OnUpdate(float deltaTime)
    {
        if (!Owner.GrindController.IsGrinding)
        {
            if (Owner.GrindController.IsCollisionExiting)
            {
                Machine.ChangeState<PlayerRailExitingState>();
            }
            else
            {
                Machine.ChangeState<PlayerIdlingState>();
            }
            return;
        }

        if (Owner.InputReader.ConsumeJumpPress())
        {
            Vector2 moveInput = Owner.InputReader.MoveInput;
            Owner.GrindController.StopGrind();
            Machine.ChangeState<PlayerRailJumpingState>(
                m_rail, moveInput, Owner.Monitor.CurrentVelocity);
        }
    }
    /// <summary>
    /// 一定間隔の更新処理を行います。
    /// </summary>
    protected override void OnFixedUpdate()
    {
      
    }

    /// <summary>
    /// グラインド状態の終了処理を行います。
    /// </summary>
    protected override void OnExitState()
    {
    }
}