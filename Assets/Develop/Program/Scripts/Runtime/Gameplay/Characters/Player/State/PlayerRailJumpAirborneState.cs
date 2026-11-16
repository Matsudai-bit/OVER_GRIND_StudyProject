/// <summary>レールジャンプの頂点以降の移動、空中攻撃、再搭乗を継続します。</summary>
public sealed class PlayerRailJumpAirborneState : StateBase<PlayerStateMachineComponent>
{
    private readonly PlayerRailJumpingState m_jump;
    /// <summary>レールジャンプの実行状態を引き継ぎます。</summary>
    public PlayerRailJumpAirborneState(PlayerRailJumpingState jump) { m_jump = jump; }
    /// <summary>開始アニメーションを再生せず処理を引き継ぎます。</summary>
    protected override void OnStartState() => m_jump.BeginAirborne();
    /// <summary>攻撃入力の解除を更新します。</summary>
    protected override void OnUpdate(float deltaTime) => m_jump.UpdateAirborne(deltaTime);
    /// <summary>移動、空中攻撃、着地を更新します。</summary>
    protected override void OnFixedUpdate() => m_jump.UpdateAirbornePhysics();
    /// <summary>引き継いだ攻撃とジャンプを終了します。</summary>
    protected override void OnExitState() => m_jump.FinishRailJump();
}
