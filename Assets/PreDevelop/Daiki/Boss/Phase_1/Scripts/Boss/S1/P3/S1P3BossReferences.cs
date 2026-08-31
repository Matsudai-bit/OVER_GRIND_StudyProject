using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ステージ1フェーズ3固有の参照を保持します。
/// </summary>
[DisallowMultipleComponent]
public sealed class S1P3BossReferences : BossPhaseParameterProvider

{
    // 3回目の突進で使用する目的地候補
    [SerializeField, Header("突進目的地")]
    private List<Transform> m_chargeDestinationPoints = new();

    [SerializeField, Header("突進攻撃パラメータ")]
    S1P3BossChargeAttackParameterAsset m_chargeAttackParameterAsset;

    /// <summary>
    /// 突進目的地候補を取得します。
    /// </summary>
    public IReadOnlyList<Transform> ChargeDestinationPoints =>
        m_chargeDestinationPoints;

    public override BossPhaseParameters CreatePhaseParameters()
    {
        if (m_chargeAttackParameterAsset == null)
        {
            Debug.LogError(
                $"[{nameof(S1P3BossReferences)}] " +
                $"{nameof(S1P3BossChargeAttackParameterAsset)}" +
                "が設定されていません。",
                this);

            return BossPhaseParameters.Empty;
        }

        S1P3BossSequentialChargeAttackParameters
            chargeAttackParameters =
                m_chargeAttackParameterAsset.CreateParameters();

        if (chargeAttackParameters == null)
        {
            return BossPhaseParameters.Empty;
        }

        return new BossPhaseParameters(
            chargeAttackParameters);
    }
}