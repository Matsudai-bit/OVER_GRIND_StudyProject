using UnityEngine;

/// <summary>
/// S1P2ボスの各状態で使用する挙動パラメータを保持します。
/// </summary>
[CreateAssetMenu(
    fileName = "S1P2BossStateParameter",
    menuName = "Game/Parameters/Boss/S1/P2/State Parameter")]
public sealed class S1P2BossStateParameterAsset : ScriptableObject
{
    // ミサイル状態のパラメータ
    [SerializeField, Header("ミサイル")]
    private S1P2BossMissileStateParameters m_missile = new();

    // 排熱状態のパラメータ
    [SerializeField, Header("排熱")]
    private S1P2BossHeatVentStateParameters m_heatVent = new();

    /// <summary>
    /// ミサイル状態のパラメータを取得します。
    /// </summary>
    public S1P2BossMissileStateParameters Missile =>
        m_missile;

    /// <summary>
    /// 排熱状態のパラメータを取得します。
    /// </summary>
    public S1P2BossHeatVentStateParameters HeatVent =>
        m_heatVent;

    /// <summary>
    /// 必要な状態パラメータが設定されているか確認します。
    /// </summary>
    /// <returns>
    /// true：必要なパラメータが設定されています。
    /// false：パラメータが不足しています。
    /// </returns>
    public bool HasRequiredParameters()
    {
        return m_missile != null &&
               m_missile.HasRequiredParameters() &&
               m_heatVent != null;
    }
}
