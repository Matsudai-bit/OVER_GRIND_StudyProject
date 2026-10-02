using System;
using Unity.Behavior;
using UnityEngine;

/// <summary>
/// S1P2の移動を使用するか判定します。
/// </summary>
[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(
    name: "ShouldUseS1P2Move",
    story: "[BossController] が [DecisionParameterAsset] の設定を使用してS1P2移動を選択する",
    category: "Conditions",
    id: "b4a3396a29fc631e91336bbf49bafdc5")]
public partial class ShouldUseS1P2MoveCondition :
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
    /// 移動を使用するか判定します。
    /// </summary>
    /// <returns>
    /// true：移動を選択します。
    /// false：移動を選択しません。
    /// </returns>
    public override bool IsTrue()
    {
        if (BossController?.Value == null ||
            DecisionParameterAsset?.Value == null)
        {
            return false;
        }

        return CheckProbability(
            DecisionParameterAsset.Value.Move.Probability);
    }
}
