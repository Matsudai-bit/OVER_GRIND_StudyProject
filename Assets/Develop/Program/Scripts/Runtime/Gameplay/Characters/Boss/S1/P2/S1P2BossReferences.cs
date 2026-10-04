using UnityEngine;

/// <summary>
/// ステージ1フェーズ2固有の参照とパラメータ設定を保持します。
/// </summary>
[DisallowMultipleComponent]
public sealed class S1P2BossReferences :
    BossPhaseParameterProvider
{
    // 突進攻撃パラメータ
    [SerializeField, Header("攻撃パラメータ")]
    private S1BossChargeAttackParameterAsset
        m_chargeAttackParameterAsset;

    // State実行パラメータ
    [SerializeField, Header("状態のパラメータ")]
    private S1P2BossStateParameterAsset m_stateParameterAsset;

    // 行動選択パラメータ
    [SerializeField, Header("行動選択パラメータ")]
    private S1P2BossDecisionParameterAsset m_decisionParameterAsset;

    // ミサイル攻撃参照
    [SerializeField, Header("ミサイルリファレンス")]
    private S1BossMissileReferences m_missileReferences;

    // 尻尾叩きつけ参照
    [SerializeField, Header("尻尾叩きつけリファレンス")]
    private S1P2BossTailSlamReference m_tailSlamReference;

    // 尻尾周辺判定の基準位置
    [SerializeField, Header("尻尾地点")]
    private Transform m_tailTransform;

    // 移動参照
    [SerializeField, Header("移動")]
    private S1P2MoveStateReferences m_moveStateReferences;

    // ミサイル実行制御
    [SerializeField, Header("ミサイル実行制御")]
    private S1P2BossMissileExecutor m_missileExecutor;

    /// <summary>
    /// State実行パラメータを取得します。
    /// </summary>
    public S1P2BossStateParameterAsset StateParameterAsset =>
        m_stateParameterAsset;

    /// <summary>
    /// 行動選択パラメータを取得します。
    /// </summary>
    public S1P2BossDecisionParameterAsset DecisionParameterAsset =>
        m_decisionParameterAsset;

    /// <summary>
    /// ミサイル攻撃参照を取得します。
    /// </summary>
    public S1BossMissileReferences MissileReferences =>
        m_missileReferences;

    /// <summary>
    /// 尻尾叩きつけ参照を取得します。
    /// </summary>
    public S1P2BossTailSlamReference TailSlamReference =>
        m_tailSlamReference;

    /// <summary>
    /// 尻尾周辺判定の基準位置を取得します。
    /// </summary>
    public Transform TailTransform => m_tailTransform;

    /// <summary>
    /// 移動状態の参照を取得します。
    /// </summary>
    public S1P2MoveStateReferences MoveStateReferences =>
        m_moveStateReferences;

    /// <summary>
    /// ミサイル実行制御を取得します。
    /// </summary>
    public S1P2BossMissileExecutor MissileExecutor =>
        m_missileExecutor;

    /// <summary>
    /// フェーズで使用するパラメータを生成します。
    /// </summary>
    /// <returns>フェーズパラメータ。</returns>
    public override BossPhaseParameters CreatePhaseParameters()
    {
        if (m_chargeAttackParameterAsset == null)
        {
            Debug.LogError(
                $"[{nameof(S1P2BossReferences)}] " +
                $"{nameof(S1BossChargeAttackParameterAsset)}" +
                "が設定されていません。",
                this);

            return BossPhaseParameters.Empty;
        }

        S1BossChargeAttackParameters chargeParameters =
            m_chargeAttackParameterAsset.CreateParameters();

        return new BossPhaseParameters(
            chargeParameters);
    }
}
