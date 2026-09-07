/// <summary>
/// ボスの現在フェーズで使用するパラメータを保持します。
/// </summary>
public sealed class BossPhaseParameters
{
    // パラメータを持たないフェーズで使用する空データ
    private static readonly BossPhaseParameters EMPTY =
        new BossPhaseParameters();

    // 突進攻撃パラメータ
    private readonly S1BossChargeAttackParameters
        m_chargeAttackParameters;

    // 連続突進攻撃パラメータ
    private readonly S1P3BossSequentialChargeAttackParameters
        m_sequentialChargeAttackParameters;

    /// <summary>
    /// 空のフェーズパラメータを取得します。
    /// </summary>
    public static BossPhaseParameters Empty => EMPTY;

    /// <summary>
    /// 突進攻撃パラメータを取得します。
    /// </summary>
    public S1BossChargeAttackParameters ChargeAttack =>
        m_chargeAttackParameters;

    /// <summary>
    /// 連続突進攻撃パラメータを取得します。
    /// </summary>
    public S1P3BossSequentialChargeAttackParameters SequentialChargeAttack =>
        m_sequentialChargeAttackParameters;

    /// <summary>
    /// 空のフェーズパラメータを生成します。
    /// </summary>
    public BossPhaseParameters()
    {
    }

    public BossPhaseParameters(
    S1P3BossSequentialChargeAttackParameters sequentialChargeAttackParameters)
    {
        m_sequentialChargeAttackParameters =
            sequentialChargeAttackParameters;
    }

    /// <summary>
    /// フェーズパラメータを生成します。
    /// </summary>
    /// <param name="chargeAttackParameters">
    /// 突進攻撃パラメータ。
    /// </param>
    public BossPhaseParameters(
        S1BossChargeAttackParameters chargeAttackParameters)
    {
        m_chargeAttackParameters =
            chargeAttackParameters;
    }
}