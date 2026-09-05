using UnityEngine;

/// <summary>
/// ステージ1フェーズ2固有の参照とパラメータ設定を保持します。
/// </summary>
[DisallowMultipleComponent]
public sealed class S1P2BossReferences :
    BossPhaseParameterProvider
{
    // 突進攻撃パラメータ
    [SerializeField, Header("攻撃パラメータ")]
    private S1BossChargeAttackParameterAsset
        m_chargeAttackParameterAsset;

    /// <summary>
    /// フェーズで使用するパラメータを生成します。
    /// </summary>
    /// <returns>フェーズパラメータ。</returns>
    public override BossPhaseParameters CreatePhaseParameters()
    {
        if (m_chargeAttackParameterAsset == null)
        {
            Debug.LogError(
                $"[{nameof(S1P2BossReferences)}] " +
                $"{nameof(S1BossChargeAttackParameterAsset)}" +
                "が設定されていません。",
                this);

            return BossPhaseParameters.Empty;
        }

        S1BossChargeAttackParameters chargeParameters =
            m_chargeAttackParameterAsset.CreateParameters();

        return new BossPhaseParameters(
            chargeParameters);
    }
}