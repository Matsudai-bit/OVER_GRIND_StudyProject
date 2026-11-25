using System;
using Unity.Behavior;
using UnityEngine;

/// <summary>
/// S1P2の尻尾叩きつけ攻撃を使用するか判定します。
/// </summary>
[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(
    name: "ShouldUseS1P2TailSlam",
    story: "[BossController] が [DecisionParameterAsset] の設定を使用してS1P2尻尾叩きつけ攻撃を選択する",
    category: "Conditions",
    id: "98e54eba07a280f9c55712e41ad1b319")]
public partial class ShouldUseS1P2TailSlamCondition :
    BossActionDecisionCondition
{
    // ボスコントローラ
    [SerializeReference]
    public BlackboardVariable<BossController>
        BossController;

    // S1P2行動選択パラメータ
    [SerializeReference]
    public BlackboardVariable<S1P2BossDecisionParameterAsset>
        DecisionParameterAsset;

    /// <summary>
    /// 尻尾叩きつけ攻撃を使用するか判定します。
    /// </summary>
    /// <returns>
    /// true：使用します。
    /// false：使用しません。
    /// </returns>
    public override bool IsTrue()
    {
        return S1P2TailSlamDecisionEvaluator
            .ShouldExecute(
                BossController?.Value,
                DecisionParameterAsset?.Value);
    }
}