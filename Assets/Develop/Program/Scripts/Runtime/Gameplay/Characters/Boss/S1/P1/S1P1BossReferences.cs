using UnityEngine;

/// <summary>
/// ステージ1フェーズ1固有の参照を保持します。
/// </summary>
[DisallowMultipleComponent]
public sealed class S1P1BossReferences : BossPhaseParameterProvider
{
    // 左足の座標
    [SerializeField, Header("左足の座標")]
    private Transform m_leftLegTransform;

    // 右足の座標
    [SerializeField, Header("右足の座標")]
    private Transform m_rightLegTransform;

    // ミサイル攻撃で使用する参照
    [SerializeField, Header("ミサイルリファレンス")]
    private S1P1BossMissileReferences m_missileReferences;

    // S1P1行動選択パラメータ
    [SerializeField, Header("行動選択パラメータ")]
    private S1P1BossDecisionParameterAsset m_decisionParameterAsset;

    // S1P1状態挙動パラメータ
    [SerializeField, Header("状態挙動パラメータ")]
    private S1P1BossStateParameterAsset m_stateParameterAsset;

    // Playerの足元滞在時間計測
    [SerializeField, Header("足元滞在時間計測")]
    private BossPlayerRangeStayTimer m_playerRangeStayTimer;

    /// <summary>
    /// 左足の座標を取得します。
    /// </summary>
    public Transform LeftLegTransform =>
        m_leftLegTransform;

    /// <summary>
    /// 右足の座標を取得します。
    /// </summary>
    public Transform RightLegTransform =>
        m_rightLegTransform;

    /// <summary>
    /// ミサイル攻撃で使用する参照を取得します。
    /// </summary>
    public S1P1BossMissileReferences MissileReferences =>
        m_missileReferences;

    /// <summary>
    /// 行動選択パラメータを取得します。
    /// </summary>
    public S1P1BossDecisionParameterAsset DecisionParameterAsset =>
        m_decisionParameterAsset;

    /// <summary>
    /// 状態挙動パラメータを取得します。
    /// </summary>
    public S1P1BossStateParameterAsset StateParameterAsset =>
        m_stateParameterAsset;

    /// <summary>
    /// Playerの足元滞在時間計測を取得します。
    /// </summary>
    public BossPlayerRangeStayTimer PlayerRangeStayTimer =>
        m_playerRangeStayTimer;

    /// <summary>
    /// フェーズパラメータを生成します。
    /// </summary>
    /// <returns>フェーズパラメータ。</returns>
    public override BossPhaseParameters CreatePhaseParameters()
    {
        return BossPhaseParameters.Empty;
    }
}
