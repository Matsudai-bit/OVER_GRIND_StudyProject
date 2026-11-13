using System;
using Unity.Behavior;
using UnityEngine;

/// <summary>
/// S1P3のスタン状態へ遷移するか判定します。
/// </summary>
[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(
    name: "ShouldUseS1P3Stunned",
    story: "[BossController] のエネルギー砲終了によるスタン遷移要求を確認する",
    category: "Conditions",
    id: "f890358db73d47d5a682d1709de3ba42")]
public partial class ShouldUseS1P3StunnedCondition
    : BossActionDecisionCondition
{
    // ボスコントローラ
    [SerializeReference]
    public BlackboardVariable<BossController> BossController;

    /// <summary>
    /// スタン状態へ遷移するか判定します。
    /// </summary>
    /// <returns>
    /// true：スタン状態へ遷移します。
    /// false：スタン状態へ遷移しません。
    /// </returns>
    public override bool IsTrue()
    {
        if (BossController?.Value == null)
        {
            return false;
        }

         
            

        if (!BossController.Value.PhaseController.TryGetCurrentPhaseComponent(out S1P3BossStateTransitionRequest transitionRequest) || transitionRequest == null)
        {
            return false;
        }

        return transitionRequest.IsStunnedRequested;
    }
}
