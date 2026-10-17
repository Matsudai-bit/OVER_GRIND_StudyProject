using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ステージ1フェーズ3固有の参照を保持します。
/// </summary>
[DisallowMultipleComponent]
public sealed class S1P3BossReferences :
    BossPhaseParameterProvider
{
    // 3回目の突進で使用する目的地候補
    [SerializeField, Header("突進目的地")]
    private List<Transform> m_chargeDestinationPoints = new();

    // 突進攻撃パラメータ
    [SerializeField, Header("突進攻撃パラメータ")]
    private S1P3BossChargeAttackParameterAsset
        m_chargeAttackParameterAsset;

    // エネルギー砲状態
    [SerializeField, Header("エネルギー砲")]
    private S1P3EnergyCannonStateReferences
        m_energyCannonStateReferences;

    // 状態パラメータ
    [SerializeField, Header("状態のパラメータ")]
    private S1P3BossStateParameterAsset
        m_stateParameterAsset;

    // 行動選択パラメータ
    [SerializeField, Header("行動選択パラメータ")]
    private S1P3BossDecisionParameterAsset
        m_decisionParameterAsset;

    // 行動選択回数管理
    [SerializeField, Header("行動選択回数管理")]
    private S1P3BossActionSelectionTracker
        m_actionSelectionTracker;

    // フェーズ3で使用する攻撃設定Provider
    [SerializeField, Header("共通参照")]
    private S1BossPhaseAttackSettingsProvider
        m_attackSettingsProvider;

    // ドレッド攻撃状態
    [SerializeField, Header("ドレッド攻撃")]
    private DreadAttackStateReferences
        m_dreadAttackStateReferences;

    /// <summary>
    /// 状態パラメータを取得します。
    /// </summary>
    public S1P3BossStateParameterAsset StateParameterAsset =>
        m_stateParameterAsset;

    /// <summary>
    /// 行動選択パラメータを取得します。
    /// </summary>
    public S1P3BossDecisionParameterAsset DecisionParameterAsset =>
        m_decisionParameterAsset;

    /// <summary>
    /// 行動選択回数管理を取得します。
    /// </summary>
    public S1P3BossActionSelectionTracker ActionSelectionTracker =>
        m_actionSelectionTracker;

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
    /// ドレッド攻撃状態の参照を取得します。
    /// </summary>
    public DreadAttackStateReferences DreadAttackStateReferences =>
        m_dreadAttackStateReferences;

    /// <summary>
    /// フェーズで使用するパラメータを生成します。
    /// </summary>
    /// <returns>フェーズパラメータ。</returns>
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
