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
    public BlackboardVariable<BossController> BossController;

    // S1P2行動選択パラメータ
    [SerializeReference]
    public BlackboardVariable<S1P2BossDecisionParameterAsset>
        DecisionParameterAsset;

    /// <summary>
    /// 尻尾叩きつけ攻撃を使用するか判定します。
    /// </summary>
    /// <returns>
    /// true：尻尾叩きつけ攻撃を選択します。
    /// false：尻尾叩きつけ攻撃を選択しません。
    /// </returns>
    public override bool IsTrue()
    {
        if (BossController?.Value == null ||
            DecisionParameterAsset?.Value == null)
        {
            return false;
        }

        if (!IsStateReady<S1P2BossTailSlamState>(
                BossController.Value))
        {
            return false;
        }

        if (!TryGetReferences(
                out BossPhaseReferences commonReferences,
                out S1P2BossReferences phaseReferences))
        {
            return false;
        }

        S1P2BossDecisionParameterAsset.TailSlamParameters parameters =
            DecisionParameterAsset.Value.TailSlam;

        float distance = GetHorizontalDistance(
            phaseReferences.TailTransform.position,
            commonReferences.PlayerTransform.position);

        float probability =
            distance <= parameters.Distance
                ? parameters.NearProbability
                : parameters.DefaultProbability;

        return CheckProbability(probability);
    }

    /// <summary>
    /// 尻尾判定に必要な参照を取得します。
    /// </summary>
    private bool TryGetReferences(
        out BossPhaseReferences commonReferences,
        out S1P2BossReferences phaseReferences)
    {
        commonReferences = null;
        phaseReferences = null;

        if (BossController?.Value == null ||
            BossController.Value.PhaseController == null)
        {
            return false;
        }

        if (!BossController.Value.PhaseController
                .TryGetCurrentPhaseComponent(
                    out commonReferences))
        {
            return false;
        }

        if (!BossController.Value.PhaseController
                .TryGetCurrentPhaseComponent(
                    out phaseReferences))
        {
            return false;
        }

        return commonReferences.PlayerTransform != null &&
               phaseReferences.TailTransform != null;
    }
}
