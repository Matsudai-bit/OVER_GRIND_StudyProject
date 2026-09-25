using UnityEngine;

/// <summary>
/// S1P1ボスのミサイル攻撃で使用するパラメータを保持します。
/// </summary>
[CreateAssetMenu(
    fileName = "S1P1BossMissileParameter",
    menuName = "Game/Parameters/Boss/S1/P1/Missile Parameter")]
public sealed class S1P1BossMissileParameterAsset : ScriptableObject
{
    // ミサイル本体の挙動パラメータ
    [SerializeField, Header("ミサイル本体")]
    private MissileParameterAsset m_missileParameterAsset;

    // ボスのミサイル攻撃状態パラメータ
    [SerializeField, Header("ミサイル攻撃状態")]
    private S1P1BossMissileStateParameters m_stateParameters = new();

    /// <summary>
    /// ミサイル本体のパラメータを取得します。
    /// </summary>
    public MissileParameterAsset MissileParameterAsset =>
        m_missileParameterAsset;

    /// <summary>
    /// ミサイル攻撃状態のパラメータを取得します。
    /// </summary>
    public S1P1BossMissileStateParameters StateParameters =>
        m_stateParameters;

    /// <summary>
    /// 必要なパラメータが設定されているか確認します。
    /// </summary>
    /// <returns>
    /// true：必要なパラメータが設定されています。
    /// false：パラメータが不足しています。
    /// </returns>
    public bool HasRequiredParameters()
    {
        return m_missileParameterAsset != null &&
               m_stateParameters != null;
    }
}
