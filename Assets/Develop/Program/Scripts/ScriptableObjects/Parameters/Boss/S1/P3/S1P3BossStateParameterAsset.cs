using UnityEngine;

/// <summary>
/// S1P3ボスの各状態で使用する挙動パラメータを保持します。
/// </summary>
[CreateAssetMenu(
    fileName = "S1P3BossStateParameter",
    menuName = "Game/Parameters/Boss/S1/P3/State Parameter")]
public sealed class S1P3BossStateParameterAsset : ScriptableObject
{
    [SerializeField, Header("エネルギー砲")]
    private S1P3BossEnergyCannonStateParameters m_energyCannon = new();

    [SerializeField, Header("突進")]
    private S1P3BossChargeAttackParameterAsset m_chargeParameterAsset;

    /// <summary>
    /// エネルギー砲状態のパラメータを取得します。
    /// </summary>
    public S1P3BossEnergyCannonStateParameters EnergyCannon =>
        m_energyCannon;

    /// <summary>
    /// 連続突進状態のパラメータアセットを取得します。
    /// </summary>
    public S1P3BossChargeAttackParameterAsset ChargeParameterAsset =>
        m_chargeParameterAsset;

    /// <summary>
    /// 必要な状態パラメータが設定されているか確認します。
    /// </summary>
    /// <returns>
    /// true：必要なパラメータが設定されています。
    /// false：パラメータが不足しています。
    /// </returns>
    public bool HasRequiredParameters()
    {
        return m_energyCannon != null &&
               m_chargeParameterAsset != null;
    }
}
