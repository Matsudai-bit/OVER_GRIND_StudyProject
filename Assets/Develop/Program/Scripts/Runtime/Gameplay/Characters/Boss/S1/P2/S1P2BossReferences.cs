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

    [SerializeField, Header("状態のパラメータ")]
    private S1P2BossStateParameterAsset m_stateParameterAsset;   
    [SerializeField, Header("ミサイルリファレンス")]
    private S1BossMissileReferences m_missileReferences;
    [SerializeField, Header("足攻撃状態のリファレンス")]
    private S1P2BossTailSlamReference m_tailSlamReference;
    [SerializeField, Header("移動")]
    private S1P2MoveStateReferences
    m_moveStateReferences;

    [SerializeField, Header("ミサイル実行制御")]
    private S1P2BossMissileExecutor m_missileExecutor;

    /// <summary>
    /// ミサイル実行制御を取得します。
    /// </summary>
    public S1P2BossMissileExecutor MissileExecutor =>
        m_missileExecutor;

    public S1P2BossStateParameterAsset StateParameterAsset => m_stateParameterAsset;
    public S1BossMissileReferences MissileReferences => m_missileReferences;
    public S1P2BossTailSlamReference TailSlamReference => m_tailSlamReference;

    /// <summary>
    /// 移動状態の参照を取得します。
    /// </summary>
    public S1P2MoveStateReferences MoveStateReferences =>
        m_moveStateReferences;

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