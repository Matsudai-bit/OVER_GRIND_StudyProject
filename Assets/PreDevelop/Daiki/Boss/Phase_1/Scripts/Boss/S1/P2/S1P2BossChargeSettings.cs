using UnityEngine;

/// <summary>
/// ステージ1フェーズ2の突進設定を保持します。
/// </summary>
[DisallowMultipleComponent]
public sealed class S1P2BossChargeSettings : MonoBehaviour
{
    // フェーズ2の突進設定
    [SerializeField, Header("フェーズ2突進設定")]
    private StraightChargeSettings m_chargeSettings = new();

    /// <summary>
    /// フェーズ2の突進設定を取得します。
    /// </summary>
    public StraightChargeSettings ChargeSettings => m_chargeSettings;
}
