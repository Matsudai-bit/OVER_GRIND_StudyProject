using System;
using Unity.Behavior;
using UnityEngine;

/// <summary>
/// S1P3の噛み潰し攻撃を使用するか判定します。
/// </summary>
[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(
    name: "ShouldUseS1P3Bite",
    story: "[BossController] が [IsPlayerInMouth] と [DecisionParameterAsset] を使用してS1P3噛み潰し攻撃を選択する",
    category: "Conditions",
    id: "c11b975c7867481dbcb995da2e7f385f")]
public partial class ShouldUseS1P3BiteCondition
    : BossActionDecisionCondition
{
    // ボスコントローラ
    [SerializeReference]
    public BlackboardVariable<BossController> BossController;

    // Playerが口内にいるか
    [SerializeReference]
    public BlackboardVariable<bool> IsPlayerInMouth;

    // S1P3行動選択パラメータ
    [SerializeReference]
    public BlackboardVariable<S1P3BossDecisionParameterAsset>
        DecisionParameterAsset;

    /// <summary>
    /// 噛み潰し攻撃を使用するか判定します。
    /// </summary>
    /// <returns>
    /// true：噛み潰し攻撃を選択します。
    /// false：噛み潰し攻撃を選択しません。
    /// </returns>
    public override bool IsTrue()
    {
        if (BossController?.Value == null ||
            DecisionParameterAsset?.Value == null)
        {
            return false;
        }

        S1P3BossDecisionParameterAsset.BiteParameters parameters =
            DecisionParameterAsset.Value.Bite;

        bool isPlayerInMouth =
            IsPlayerInMouth?.Value ?? false;

        float probability =
            isPlayerInMouth
                ? parameters.InMouthProbability
                : parameters.DefaultProbability;

        return CheckProbability(probability);
    }
}
