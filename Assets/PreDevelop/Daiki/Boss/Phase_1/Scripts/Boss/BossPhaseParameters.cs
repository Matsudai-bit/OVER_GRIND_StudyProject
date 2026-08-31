/// <summary>
/// 現在のボスフェーズで使用するパラメータを保持します。
/// </summary>
public sealed class BossPhaseParameters
{
    // 突進攻撃パラメータ
    private readonly S1BossChargeAttackParameters m_chargeAttackParameters;

    /// <summary>
    /// 突進攻撃パラメータを取得します。
    /// </summary>
    public S1BossChargeAttackParameters ChargeAttack =>
        m_chargeAttackParameters;

    /// <summary>
    /// ボスフェーズパラメータを生成します。
    /// </summary>
    public BossPhaseParameters(
        S1BossChargeAttackParameters chargeAttackParameters)
    {
        m_chargeAttackParameters = chargeAttackParameters;
    }
}