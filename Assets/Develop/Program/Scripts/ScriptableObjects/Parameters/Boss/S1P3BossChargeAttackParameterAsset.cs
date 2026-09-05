using UnityEngine;

/// <summary>
/// ステージ1フェーズ3の連続突進攻撃パラメータを設定します。
/// </summary>
[CreateAssetMenu(
    fileName = "S1P3BossChargeAttackParameter",
    menuName = "Game/Parameters/Boss/S1/P3/Charge Attack Parameter")]
public sealed class S1P3BossChargeAttackParameterAsset :
    ScriptableObject
{
    // 1回目の突進パラメータ
    [SerializeField]
    private S1BossChargeAttackParameterAsset m_firstChargeParameterAsset;

    // 2回目の突進パラメータ
    [SerializeField]
    private S1BossChargeAttackParameterAsset m_secondChargeParameterAsset;

    // 3回目の突進パラメータ
    [SerializeField]
    private S1BossChargeAttackParameterAsset m_thirdChargeParameterAsset;

    /// <summary>
    /// 実行時に使用する連続突進パラメータを生成します。
    /// </summary>
    /// <returns>
    /// 連続突進パラメータ。
    /// 設定が不足している場合はnull。
    /// </returns>
    public S1P3BossSequentialChargeAttackParameters CreateParameters()
    {
        if (m_firstChargeParameterAsset == null ||
            m_secondChargeParameterAsset == null ||
            m_thirdChargeParameterAsset == null)
        {
            Debug.LogError(
                $"[{nameof(S1P3BossChargeAttackParameterAsset)}] " +
                "突進パラメータがすべて設定されていません。",
                this);

            return null;
        }

        return new S1P3BossSequentialChargeAttackParameters(
            m_firstChargeParameterAsset.CreateParameters(),
            m_secondChargeParameterAsset.CreateParameters(),
            m_thirdChargeParameterAsset.CreateParameters());
    }
}