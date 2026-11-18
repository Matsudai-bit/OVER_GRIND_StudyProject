/// <summary>レール移動状態内の空中攻撃を管理するサブ状態です。</summary>
public sealed class PlayerAirAttackingState
{
    private readonly PlayerStateMachineComponent m_owner;
    private PlayerAirAttack m_attack;
    private PlayerAudioActionID m_audioActionID = PlayerAudioActionID.NONE;

    /// <summary>空中攻撃中か取得します。</summary>
    public bool IsActive => m_attack != null;

    /// <summary>命中時の滞空処理が有効か取得します。</summary>
    public bool IsAirHitStopping => m_attack != null && m_attack.IsAirHitStopping;

    /// <summary>サブ状態を生成します。</summary>
    /// <param name="owner">攻撃するプレイヤー。</param>
    public PlayerAirAttackingState(PlayerStateMachineComponent owner)
    {
        m_owner = owner;
    }

    /// <summary>空中攻撃を開始します。</summary>
    public void StartAttack()
    {
        if (IsActive) return;
        bool isBoostAttack = m_owner.IsBoostSuspended &&
            m_owner.SuspendedBoostGaugeRate > 0.0f;
        m_audioActionID = PlayerAudioActionUtility.GetAttackActionID(isBoostAttack);
        PlayerAudioActionUtility.PlaySequential(m_owner, m_audioActionID);
        m_attack = new PlayerAirAttack(m_owner);
        m_attack.StartAttack();
    }

    /// <summary>空中攻撃を更新します。</summary>
    /// <param name="deltaTime">物理更新時間。</param>
    /// <returns>trueは継続、falseは終了または未開始を示します。</returns>
    public bool UpdateAttack(float deltaTime) =>
        m_attack != null && m_attack.UpdateAttack(deltaTime);

    /// <summary>空中攻撃を終了します。</summary>
    public void StopAttack()
    {
        if (m_attack == null) return;
        m_attack.StopAttack();
        m_attack = null;
        if (m_audioActionID == PlayerAudioActionID.NONE) return;
        PlayerAudioActionUtility.StopSequential(m_owner, m_audioActionID);
        m_audioActionID = PlayerAudioActionID.NONE;
    }
}
