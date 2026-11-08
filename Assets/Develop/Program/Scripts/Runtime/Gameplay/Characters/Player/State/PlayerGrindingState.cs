using UnityEngine;

/// <summary>
/// グラインド移動と離脱先への遷移を物理更新に合わせて管理します。
/// </summary>
public sealed class PlayerGrindingState
    : StateBase<PlayerStateMachineComponent>
{
    // 搭乗中のレール
    private SplineRailInfo m_rail;

    // レールジャンプからの復帰か
    private readonly bool m_isResuming;

    // 復帰時のレール上位置
    private readonly float m_positionT;

    // 復帰時の進行方向
    private readonly int m_direction;

    // 復帰時の移動速度
    private readonly float m_speed;

    // グラインド開始済みか
    private bool m_hasStarted;

    /// <summary>
    /// 通常のレール検出からグラインドを開始します。
    /// </summary>
    public PlayerGrindingState()
    {
    }

    /// <summary>
    /// 搭乗するレールを指定してグラインドを開始します。
    /// </summary>
    /// <param name="rail">搭乗するレール。</param>
    public PlayerGrindingState(
        SplineRailInfo rail)
    {
        m_rail = rail;
    }

    /// <summary>
    /// レールジャンプの着地点・方向・速度を引き継いで
    /// グラインドを再開します。
    /// </summary>
    /// <param name="rail">搭乗するレール。</param>
    /// <param name="positionT">レール上の位置。</param>
    /// <param name="direction">進行方向。</param>
    /// <param name="speed">グラインド速度。</param>
    public PlayerGrindingState(
        SplineRailInfo rail,
        float positionT,
        int direction,
        float speed)
    {
        m_rail = rail;
        m_positionT = positionT;
        m_direction = direction;
        m_speed = speed;
        m_isResuming = true;
    }

    /// <summary>
    /// 搭乗先を確定し、グラインドアニメーションを開始します。
    /// </summary>
    protected override void OnStartState()
    {
        if (m_rail == null &&
            !m_isResuming)
        {
            m_rail =
                Owner.Monitor.HitRailInfo;
        }

        Owner.InputReader.ConsumeJumpPress();

        Owner.AnimationPresenter
            .PlayRailGrindAnimation();
    }

    /// <summary>
    /// 物理更新ごとにグラインド移動を更新し、
    /// 終了状態に応じて次のStateへ遷移します。
    /// </summary>
    protected override void OnFixedUpdate()
    {
        if (!m_hasStarted)
        {
            // Player Colliderは常に有効のまま維持する。
            // グラインド中の物理干渉は
            // PlayerMotor.BeginRailMotion() のKinematic制御で抑制する。
            if (m_isResuming)
            {
                Owner.GrindController.StartGrindAt(
                    m_rail,
                    m_positionT,
                    m_direction,
                    m_speed);
            }
            else
            {
                Owner.GrindController.StartGrind(
                    m_rail);
            }

            m_hasStarted = true;
        }

        Owner.GrindController.UpdateGrind(
            Time.fixedDeltaTime);

        if (Owner.GrindController.IsGrinding)
        {
            return;
        }

        // 障害物との衝突によるレール離脱
        if (Owner.GrindController.IsCollisionExiting)
        {
            Owner.RequestRailStateChange<
                PlayerRailExitingState>();

            return;
        }

        // 攻撃入力を維持した状態でレールを抜けた場合
        if (Owner.InputReader.IsAttackHeld)
        {
            Owner.RequestRailStateChange<
                PlayerRailExitingState>(
                true);

            return;
        }

        // 通常のレール終了
   // 通常のレール終了
Owner.RequestRailStateChange<
    PlayerRailExitingState>();
    }

    /// <summary>
    /// グラインドState終了時に
    /// グラインドアニメーションを停止します。
    /// </summary>
    protected override void OnExitState()
    {
        Owner.AnimationPresenter
            .StopRailGrindAnimation();
    }
}
