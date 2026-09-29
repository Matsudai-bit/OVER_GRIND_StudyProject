using System;
using Unity.Behavior;
using UnityEngine;

/// <summary>
/// S1P1の歩行を使用するか判定します。
/// </summary>
[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(
    name: "ShouldUseP1Walk",
    story: "[BossController] が [DecisionParameterAsset] の設定を使用してS1P1歩行を選択する",
    category: "Conditions",
    id: "f60947845252426784f37fbdb3a0e345")]
public partial class ShouldUseP1WalkCondition :
    BossActionDecisionCondition
{
    // ボスコントローラ
    [SerializeReference]
    public BlackboardVariable<BossController> BossController;

    // S1P1行動選択パラメータ
    [SerializeReference]
    public BlackboardVariable<S1P1BossDecisionParameterAsset>
        DecisionParameterAsset;

    /// <summary>
    /// 歩行を使用するか判定します。
    /// </summary>
    /// <returns>
    /// true：歩行を選択します。
    /// false：歩行を選択しません。
    /// </returns>
    public override bool IsTrue()
    {
        if (BossController?.Value == null ||
            DecisionParameterAsset?.Value == null)
        {
            return false;
        }

        if (!IsStateReady<S1P1BossWalkState>(
                BossController.Value))
        {
            return false;
        }

        float probability =
            DecisionParameterAsset.Value.Walk.Probability;

        return CheckProbability(probability);
    }
}
