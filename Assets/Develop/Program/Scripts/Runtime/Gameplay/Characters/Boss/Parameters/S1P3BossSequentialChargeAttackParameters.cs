using System.Collections.Generic;

/// <summary>
/// 複数回の連続突進で使用するパラメータを保持します。
/// </summary>
public sealed class S1P3BossSequentialChargeAttackParameters
{
    // 各突進のパラメータ
    private readonly S1BossChargeAttackParameters[] m_chargeParameters;

    /// <summary>
    /// 突進回数を取得します。
    /// </summary>
    public int Count => m_chargeParameters.Length;

    /// <summary>
    /// 各突進のパラメータを取得します。
    /// </summary>
    public IReadOnlyList<S1BossChargeAttackParameters> ChargeParameters =>
        m_chargeParameters;

    /// <summary>
    /// 連続突進パラメータを生成します。
    /// </summary>
    public S1P3BossSequentialChargeAttackParameters(
        S1BossChargeAttackParameters firstCharge,
        S1BossChargeAttackParameters secondCharge,
        S1BossChargeAttackParameters thirdCharge)
    {
        m_chargeParameters = new[]
        {
            firstCharge,
            secondCharge,
            thirdCharge
        };
    }

    /// <summary>
    /// 指定した回数の突進パラメータを取得します。
    /// </summary>
    /// <param name="index">0から始まる突進番号。</param>
    /// <param name="parameters">取得した突進パラメータ。</param>
    /// <returns>
    /// true：取得できました。
    /// false：取得できませんでした。
    /// </returns>
    public bool TryGetChargeParameters(
        int index,
        out S1BossChargeAttackParameters parameters)
    {
        parameters = null;

        if (index < 0 ||
            index >= m_chargeParameters.Length)
        {
            return false;
        }

        parameters = m_chargeParameters[index];

        return parameters != null;
    }
}