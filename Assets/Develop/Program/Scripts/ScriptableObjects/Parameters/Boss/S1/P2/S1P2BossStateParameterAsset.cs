using UnityEngine;

/// <summary>
/// S1P2ボスの各状態で使用する挙動パラメータを保持します。
/// </summary>
[CreateAssetMenu(
    fileName = "S1P2BossStateParameter",
    menuName = "Game/Parameters/Boss/S1/P2/State Parameter")]
public sealed class S1P2BossStateParameterAsset : ScriptableObject
{
    // 移動状態のパラメータ
    [SerializeField, Header("移動")]
    private S1P2BossMoveStateParameters m_move = new();

    // ミサイル状態のパラメータ
    [SerializeField, Header("ミサイル")]
    private S1P2BossMissileStateParameters m_missile = new();

    // 移動ミサイル状態のパラメータ設定
    [SerializeField, Header("移動ミサイル")]
    private S1P2BossMoveMissileStateParameterSettings m_moveMissile = new();

    // 排熱状態のパラメータ
    [SerializeField, Header("排熱")]
    private S1P2BossHeatVentStateParameters m_heatVent = new();

    /// <summary>
    /// 移動状態のパラメータを取得します。
    /// </summary>
    public S1P2BossMoveStateParameters Move =>
        m_move;

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
        if (m_move == null ||
            m_missile == null ||
            !m_missile.HasRequiredParameters() ||
            m_moveMissile == null ||
            m_heatVent == null)
        {
            return false;
        }

        return m_moveMissile.TryCreateParameters(
            m_move,
            m_missile,
            out _);
    }

    /// <summary>
    /// 移動ミサイル状態のパラメータを取得します。
    /// </summary>
    /// <param name="parameters">取得したパラメータ。</param>
    /// <returns>
    /// true：必要なパラメータを取得できました。
    /// false：パラメータが不足しています。
    /// </returns>
    public bool TryGetMoveMissileParameters(
        out S1P2BossMoveMissileStateParameters parameters)
    {
        if (m_moveMissile == null)
        {
            parameters = default;
            return false;
        }

        return m_moveMissile.TryCreateParameters(
            m_move,
            m_missile,
            out parameters);
    }
}
