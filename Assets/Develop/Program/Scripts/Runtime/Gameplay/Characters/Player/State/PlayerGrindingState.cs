using UnityEngine;

/// <summary>グラインド移動と離脱先への遷移を物理更新に合わせて管理します。</summary>
public sealed class PlayerGrindingState : StateBase<PlayerStateMachineComponent>
{
    private SplineRailInfo m_rail;
    private readonly bool m_isResuming;
    private readonly float m_positionT;
    private readonly int m_direction;
    private readonly float m_speed;
    private bool m_hasStarted;

    /// <summary>通常の接触検出からグラインドを開始します。</summary>
    public PlayerGrindingState() { }

    /// <summary>搭乗先のレールを保持します。</summary>
    /// <param name="rail">搭乗するレール。</param>
    public PlayerGrindingState(SplineRailInfo rail)
    {
        m_rail = rail;
    }

    /// <summary>無入力ジャンプの着地点・方向・速度を引き継ぎます。</summary>
    public PlayerGrindingState(SplineRailInfo rail, float positionT, int direction, float speed)
    {
        m_rail = rail;
        m_positionT = positionT;
        m_direction = direction;
        m_speed = speed;
        m_isResuming = true;
    }

    /// <summary>搭乗先を確定し、実際の物理操作はFixedUpdateまで待ちます。</summary>
    protected override void OnStartState()
    {
        if (m_rail == null && !m_isResuming) m_rail = Owner.Monitor.HitRailInfo;
        Owner.InputReader.ConsumeJumpPress();
    }

    /// <summary>物理更新ごとに一度だけ滑走し、終了・衝突を判定します。</summary>
    protected override void OnFixedUpdate()
    {
        if (!m_hasStarted)
        {
            if (m_isResuming)
                Owner.GrindController.StartGrindAt(m_rail, m_positionT, m_direction, m_speed);
            else
                Owner.GrindController.StartGrind(m_rail);
            m_hasStarted = true;
        }

        Owner.GrindController.UpdateGrind(Time.fixedDeltaTime);
        if (Owner.GrindController.IsGrinding) return;
        if (Owner.GrindController.IsCollisionExiting)
            Owner.RequestRailStateChange<PlayerRailExitingState>();
        else
            Owner.RequestRailStateChange<PlayerIdlingState>();
    }
}
