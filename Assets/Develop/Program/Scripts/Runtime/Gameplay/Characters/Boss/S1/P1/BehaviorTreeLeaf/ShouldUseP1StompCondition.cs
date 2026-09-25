using System;
using Unity.Behavior;
using UnityEngine;

/// <summary>
/// S1P1の踏みつけ攻撃を使用するか判定します。
/// </summary>
[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(
    name: "ShouldUseP1Stomp",
    story: "[BossController] が [DecisionParameterAsset] の設定を使用してS1P1踏みつけ攻撃を選択する",
    category: "Conditions",
    id: "e588ca320e4cc599c92887fe5e4ad388")]
public partial class ShouldUseP1StompCondition :
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
    /// 踏みつけ攻撃を使用するか判定します。
    /// </summary>
    /// <returns>
    /// true：踏みつけ攻撃を選択します。
    /// false：踏みつけ攻撃を選択しません。
    /// </returns>
    public override bool IsTrue()
    {
        if (BossController?.Value == null ||
            DecisionParameterAsset?.Value == null)
        {
            return false;
        }

        if (!IsStateReady<S1P1BossStompState>(
                BossController.Value))
        {
            return false;
        }

        if (!TryGetPhaseReferences(
                out S1P1BossReferences phaseReferences))
        {
            return false;
        }

        BossPlayerRangeStayTimer stayTimer =
            phaseReferences.PlayerRangeStayTimer;

        if (stayTimer == null)
        {
            return false;
        }

        S1P1BossDecisionParameterAsset.StompParameters parameters =
            DecisionParameterAsset.Value.Stomp;

        bool hasStayedLongEnough =
            stayTimer.StayTime >= parameters.RequiredStayDuration;

        float probability =
            hasStayedLongEnough
                ? parameters.NearProbability
                : parameters.DefaultProbability;

        return CheckProbability(probability);
    }

    /// <summary>
    /// S1P1固有参照を取得します。
    /// </summary>
    /// <param name="phaseReferences">S1P1固有参照。</param>
    /// <returns>
    /// true：参照を取得できました。
    /// false：取得できませんでした。
    /// </returns>
    private bool TryGetPhaseReferences(
        out S1P1BossReferences phaseReferences)
    {
        phaseReferences = null;

        if (BossController?.Value == null ||
            BossController.Value.PhaseController == null)
        {
            return false;
        }

        return BossController.Value.PhaseController
            .TryGetCurrentPhaseComponent(
                out phaseReferences);
    }
}
