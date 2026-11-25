/// <summary>地上連続攻撃の種類ごとの演出を定義します。</summary>
public abstract class PlayerGroundAttack
{
    private PlayerContinuousAttack m_attack;

    /// <summary>攻撃中に再生する音声IDを取得します。</summary>
    public abstract PlayerAudioActionID AudioActionID { get; }

    /// <summary>命中時のエフェクトIDを取得します。</summary>
    public abstract EffectId HitEffectId { get; }

    /// <summary>攻撃アニメーションを開始します。</summary>
    /// <param name="owner">攻撃するプレイヤー。</param>
    public abstract void PlayAnimation(PlayerStateMachineComponent owner);

    /// <summary>地上攻撃の判定を開始します。</summary>
    /// <param name="owner">攻撃するプレイヤー。</param>
    public void StartAttack(PlayerStateMachineComponent owner)
    {
        m_attack = new PlayerContinuousAttack(owner, false);
        PlayAnimation(owner);
        m_attack.StartAttack();
    }

    /// <summary>地上攻撃を更新します。</summary>
    /// <param name="deltaTime">物理更新時間。</param>
    /// <returns>trueは継続、falseは終了を示します。</returns>
    public bool UpdateAttack(float deltaTime) =>
        m_attack != null && m_attack.UpdateAttack(deltaTime);

    /// <summary>地上攻撃の判定を終了します。</summary>
    public void StopAttack()
    {
        m_attack?.StopAttack();
        m_attack = null;
    }
}
