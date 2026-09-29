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
    // エネルギー砲状態
    [SerializeField, Header("エネルギー砲")]
    private S1P3EnergyCannonStateReferences m_energyCannonStateReferences;
    [SerializeField, Header("状態のパラメータ")]
    private S1P3BossStateParameterAsset m_stateParameterAsset;

    // フェーズ3で使用する攻撃設定Provider
    [SerializeField, Header("共通参照")]
    private S1BossPhaseAttackSettingsProvider
        m_attackSettingsProvider;

    // ドレット攻撃状態
    [SerializeField, Header("ドレット攻撃")]
    private DreadAttackStateReferences
        m_dreadAttackStateReferences;

    public S1P3BossStateParameterAsset StateParameterAsset => m_stateParameterAsset;
    /// <summary>
    /// 突進目的地候補を取得します。
    /// </summary>
    public IReadOnlyList<Transform> ChargeDestinationPoints =>
        m_chargeDestinationPoints;

    /// <summary>
    /// エネルギー砲状態の参照を取得します。
    /// </summary>
    public S1P3EnergyCannonStateReferences EnergyCannonStateReferences =>
            m_energyCannonStateReferences;

    /// <summary>
    /// 攻撃設定Providerを取得します。
    /// </summary>
    public S1BossPhaseAttackSettingsProvider AttackSettingsProvider =>
        m_attackSettingsProvider;

    /// <summary>
    /// ドレット攻撃状態の参照を取得します。
    /// </summary>
    public DreadAttackStateReferences DreadAttackStateReferences =>
        m_dreadAttackStateReferences;

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