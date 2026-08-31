using UnityEngine;

/// <summary>
/// ステージ1フェーズ2固有の参照とパラメータ設定を保持します。
/// </summary>
[DisallowMultipleComponent]
public sealed class S1P2BossReferences : MonoBehaviour
{
    // 突進攻撃パラメータ
    [SerializeField, Header("パラメータ")]
    private S1BossChargeAttackParameterAsset m_chargeAttackParameterAsset;

    /// <summary>
    /// このフェーズで使用する実行時パラメータを生成します。
    /// </summary>
    /// <returns>フェーズで使用するパラメータ。</returns>
    public BossPhaseParameters CreatePhaseParameters()
    {
        if (m_chargeAttackParameterAsset == null)
        {
            Debug.LogError(
                $"[{nameof(S1P2BossReferences)}] " +
                $"{nameof(S1BossChargeAttackParameterAsset)}が設定されていません。",
                this);

            return null;
        }

        S1BossChargeAttackParameters chargeAttackParameters =
            m_chargeAttackParameterAsset.CreateParameters();

        return new BossPhaseParameters(
            chargeAttackParameters);
    }
}