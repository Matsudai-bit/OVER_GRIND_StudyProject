using System;
using Unity.Behavior;
using UnityEngine;

/// <summary>
/// S1P2の突進攻撃を使用するか判定します。
/// </summary>
[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(
    name: "ShouldUseS1P2Charge",
    story: "[BossController] が [DecisionParameterAsset] の設定を使用してS1P2突進攻撃を選択する",
    category: "Conditions",
    id: "76967d8880333f7e080b19b7e2e7adaf")]
public partial class ShouldUseS1P2ChargeCondition :
    BossActionDecisionCondition
{
    // ボスコントローラ
    [SerializeReference]
    public BlackboardVariable<BossController> BossController;

    // S1P2行動選択パラメータ
    [SerializeReference]
    public BlackboardVariable<S1P2BossDecisionParameterAsset>
        DecisionParameterAsset;

    /// <summary>
    /// 突進攻撃を使用するか判定します。
    /// </summary>
    /// <returns>
    /// true：突進攻撃を選択します。
    /// false：突進攻撃を選択しません。
    /// </returns>
    public override bool IsTrue()
    {
        if (BossController?.Value == null ||
            DecisionParameterAsset?.Value == null)
        {
            return false;
        }

        if (!IsStateReady<S1P2BossChargingAttackState>(
                BossController.Value))
        {
            return false;
        }

        return CheckProbability(
            DecisionParameterAsset.Value.Charge.Probability);
    }
}
