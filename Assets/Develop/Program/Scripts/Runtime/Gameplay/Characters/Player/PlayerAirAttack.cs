/// <summary>空中連続攻撃の判定、移動、命中時の滞空を管理します。</summary>
public sealed class PlayerAirAttack
{
    private readonly PlayerContinuousAttack m_attack;

    /// <summary>命中時の滞空処理が有効か取得します。</summary>
    public bool IsAirHitStopping => m_attack.IsAirHitStopping;

    /// <summary>空中攻撃を生成します。</summary>
    /// <param name="owner">攻撃するプレイヤー。</param>
    public PlayerAirAttack(PlayerStateMachineComponent owner)
    {
        m_attack = new PlayerContinuousAttack(owner, true);
    }

    /// <summary>空中攻撃を開始します。</summary>
    public void StartAttack() => m_attack.StartAttack();

    /// <summary>空中攻撃を更新します。</summary>
    /// <param name="deltaTime">物理更新時間。</param>
    /// <returns>trueは継続、falseは終了を示します。</returns>
    public bool UpdateAttack(float deltaTime) => m_attack.UpdateAttack(deltaTime);

    /// <summary>空中攻撃を終了します。</summary>
    public void StopAttack() => m_attack.StopAttack();
}
