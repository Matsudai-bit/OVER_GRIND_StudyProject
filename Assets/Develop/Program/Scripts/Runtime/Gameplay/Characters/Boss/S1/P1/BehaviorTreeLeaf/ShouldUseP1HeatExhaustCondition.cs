using System;
using Unity.Behavior;
using UnityEngine;

/// <summary>
/// S1P1の排熱攻撃を使用するか判定します。
/// </summary>
[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(
    name: "ShouldUseP1HeatExhaust",
    story: "[BossController] が [DecisionParameterAsset] の設定を使用してS1P1排熱攻撃を選択する",
    category: "Conditions",
    id: "a6736ebae1154991b557680eb0729746")]
public partial class ShouldUseP1HeatExhaustCondition :
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
    /// 排熱攻撃を使用するか判定します。
    /// </summary>
    /// <returns>
    /// true：排熱攻撃を選択します。
    /// false：排熱攻撃を選択しません。
    /// </returns>
    public override bool IsTrue()
    {
        if (BossController?.Value == null ||
            DecisionParameterAsset?.Value == null)
        {
            return false;
        }

        if (!IsStateReady<S1P1BossHeatVentState>(
                BossController.Value))
        {
            return false;
        }

        if (!TryGetCommonReferences(
                out BossPhaseReferences commonReferences))
        {
            return false;
        }

        S1P1BossDecisionParameterAsset.HeatExhaustParameters parameters =
            DecisionParameterAsset.Value.HeatExhaust;

        float distance = GetHorizontalDistance(
            commonReferences.Origin.position,
            commonReferences.PlayerTransform.position);

        bool isPlayerNear = distance <= parameters.Distance;

        float probability =
            isPlayerNear
                ? parameters.NearProbability
                : parameters.DefaultProbability;

        return CheckProbability(probability);
    }

    /// <summary>
    /// フェーズ共通参照を取得します。
    /// </summary>
    /// <param name="commonReferences">フェーズ共通参照。</param>
    /// <returns>
    /// true：参照を取得できました。
    /// false：取得できませんでした。
    /// </returns>
    private bool TryGetCommonReferences(
        out BossPhaseReferences commonReferences)
    {
        commonReferences = null;

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

        return commonReferences.Origin != null &&
               commonReferences.PlayerTransform != null;
    }
}
