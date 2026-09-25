using UnityEngine;

/// <summary>
/// S1P1ボスの各状態で使用する挙動パラメータを保持します。
/// </summary>
[CreateAssetMenu(
    fileName = "S1P1BossStateParameter",
    menuName = "Game/Parameters/Boss/S1/P1/State Parameter")]
public sealed class S1P1BossStateParameterAsset : ScriptableObject
{
    // 歩行状態のパラメータ
    [SerializeField, Header("歩行")]
    private S1P1BossWalkStateParameters m_walk = new();

    // 方向転換状態のパラメータ
    [SerializeField, Header("方向転換")]
    private S1P1BossTurnStateParameters m_turn = new();

    // 踏みつけ状態のパラメータ
    [SerializeField, Header("踏みつけ")]
    private S1P1BossStompStateParameters m_stomp = new();

    // ミサイル状態のパラメータ
    [SerializeField, Header("ミサイル")]
    private S1P1BossMissileStateParameters m_missile = new();

    // 排熱状態のパラメータ
    [SerializeField, Header("排熱")]
    private S1P1BossHeatVentStateParameters m_heatVent = new();

    // 脚崩壊状態のパラメータ
    [SerializeField, Header("脚崩壊")]
    private S1P1BossLegsCollapsingStateParameters m_legsCollapsing = new();

    /// <summary>
    /// 歩行状態のパラメータを取得します。
    /// </summary>
    public S1P1BossWalkStateParameters Walk =>
        m_walk;

    /// <summary>
    /// 方向転換状態のパラメータを取得します。
    /// </summary>
    public S1P1BossTurnStateParameters Turn =>
        m_turn;

    /// <summary>
    /// 踏みつけ状態のパラメータを取得します。
    /// </summary>
    public S1P1BossStompStateParameters Stomp =>
        m_stomp;

    /// <summary>
    /// ミサイル状態のパラメータを取得します。
    /// </summary>
    public S1P1BossMissileStateParameters Missile =>
        m_missile;

    /// <summary>
    /// 排熱状態のパラメータを取得します。
    /// </summary>
    public S1P1BossHeatVentStateParameters HeatVent =>
        m_heatVent;

    /// <summary>
    /// 脚崩壊状態のパラメータを取得します。
    /// </summary>
    public S1P1BossLegsCollapsingStateParameters LegsCollapsing =>
        m_legsCollapsing;

    /// <summary>
    /// 必要な状態パラメータが設定されているか確認します。
    /// </summary>
    /// <returns>
    /// true：必要なパラメータが設定されています。
    /// false：パラメータが不足しています。
    /// </returns>
    public bool HasRequiredParameters()
    {
        return m_walk != null &&
               m_turn != null &&
               m_stomp != null &&
               m_missile != null &&
               m_missile.HasRequiredParameters() &&
               m_heatVent != null &&
               m_legsCollapsing != null;
    }
}
