using System;
using Unity.Behavior;
using UnityEngine;

/// <summary>
/// S1P3の連続突進攻撃を使用するか判定します。
/// </summary>
[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(
    name: "ShouldUseS1P3Charge",
    story: "[BossController] が [DecisionParameterAsset] の設定を使用してS1P3連続突進攻撃を選択する",
    category: "Conditions",
    id: "7aadf0bd2116404dbcbd8e9aba4d6a61")]
public partial class ShouldUseS1P3ChargeCondition
    : BossActionDecisionCondition
{
    // ボスコントローラ
    [SerializeReference]
    public BlackboardVariable<BossController> BossController;

    // S1P3行動選択パラメータ
    [SerializeReference]
    public BlackboardVariable<S1P3BossDecisionParameterAsset>
        DecisionParameterAsset;

    /// <summary>
    /// 連続突進攻撃を使用するか判定します。
    /// </summary>
    /// <returns>
    /// true：連続突進攻撃を選択します。
    /// false：連続突進攻撃を選択しません。
    /// </returns>
    public override bool IsTrue()
    {
        if (BossController?.Value == null ||
            DecisionParameterAsset?.Value == null)
        {
            return false;
        }

        if (!IsStateReady())
        {
            return false;
        }

        if (!TryGetPhaseReferences(
                out S1P3BossReferences phaseReferences) ||
            phaseReferences.ActionSelectionTracker == null)
        {
            return false;
        }

        S1P3BossDecisionParameterAsset.ChargeParameters parameters =
            DecisionParameterAsset.Value.Charge;

        bool hasReachedRequiredCount =
            phaseReferences.ActionSelectionTracker
                .HasReachedRequiredCount(
                    parameters.RequiredOtherActionCount);

        float probability =
            hasReachedRequiredCount
                ? parameters.ReadyProbability
                : parameters.DefaultProbability;

        return CheckProbability(probability);
    }

    /// <summary>
    /// 突進Stateがクールタイム終了済みか確認します。
    /// </summary>
    /// <returns>
    /// true：使用可能です。
    /// false：クールタイム中、またはManagerがありません。
    /// </returns>
    private bool IsStateReady()
    {
        BossStateCoolTimeManager coolTimeManager =
            BossController.Value.GetComponent<
                BossStateCoolTimeManager>();

        if (coolTimeManager == null)
        {
            return false;
        }

        return coolTimeManager
            .IsReady<S1P3BossChargingAttackState>();
    }

    /// <summary>
    /// S1P3固有参照を取得します。
    /// </summary>
    private bool TryGetPhaseReferences(
        out S1P3BossReferences phaseReferences)
    {
        phaseReferences = null;

        if (BossController.Value.PhaseController == null)
        {
            return false;
        }

        return BossController.Value.PhaseController
            .TryGetCurrentPhaseComponent(
                out phaseReferences);
    }
}
