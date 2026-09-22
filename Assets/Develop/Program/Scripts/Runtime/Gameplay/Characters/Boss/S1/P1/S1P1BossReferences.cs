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

    // ボスのNavMesh占有範囲
    [SerializeField, Header("NavMesh占有範囲")]
    private BossNavMeshFootprint m_bossNavMeshFootprint;

    // ミサイル攻撃で使用する参照
    [SerializeField, Header("ミサイルリファレンス")]
    private S1P1BossMissileReferences m_missileReferences;

    // S1P1行動選択パラメータ
    [SerializeField, Header("行動選択パラメータ")]
    private S1P1BossDecisionParameterAsset m_decisionParameterAsset;

    // Playerの足元滞在時間計測
    [SerializeField, Header("足元滞在時間計測")]
    private BossPlayerRangeStayTimer m_playerRangeStayTimer;

    /// <summary>
    /// 左足の座標を取得します。
    /// </summary>
    public Transform LeftLegTransform => m_leftLegTransform;

    /// <summary>
    /// 右足の座標を取得します。
    /// </summary>
    public Transform RightLegTransform => m_rightLegTransform;

    /// <summary>
    /// ボスのNavMesh占有範囲を取得します。
    /// </summary>
    public BossNavMeshFootprint BossNavMeshFootprint =>
        m_bossNavMeshFootprint;

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
