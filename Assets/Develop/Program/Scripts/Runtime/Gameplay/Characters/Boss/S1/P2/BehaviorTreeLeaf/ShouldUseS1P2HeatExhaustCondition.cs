using System;
using Unity.Behavior;
using UnityEngine;

/// <summary>
/// S1P2の排熱攻撃を使用するか判定します。
/// </summary>
[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(
    name: "ShouldUseS1P2HeatExhaust",
    story: "[BossController] が [DecisionParameterAsset] の設定を使用してS1P2排熱攻撃を選択する",
    category: "Conditions",
    id: "6a79c75fad4a351aec0d667446d5c2bb")]
public partial class ShouldUseS1P2HeatExhaustCondition :
    BossActionDecisionCondition
{
    // ボスコントローラ
    [SerializeReference]
    public BlackboardVariable<BossController> BossController;

    // S1P2行動選択パラメータ
    [SerializeReference]
    public BlackboardVariable<S1P2BossDecisionParameterAsset>
        DecisionParameterAsset;

    // PlayerがBossの背中に乗っているか
    [SerializeReference]
    public BlackboardVariable<bool> IsPlayerOnBack;

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

        if (!IsStateReady<S1P2BossHeatVentState>(
                BossController.Value))
        {
            return false;
        }

        if (!TryGetCommonReferences(
                out BossPhaseReferences commonReferences))
        {
            return false;
        }

        S1P2BossDecisionParameterAsset.HeatExhaustParameters parameters =
            DecisionParameterAsset.Value.HeatExhaust;

        bool isPlayerOnBack =
            IsPlayerOnBack?.Value ?? false;

        if (isPlayerOnBack)
        {
            return CheckProbability(
                parameters.OnBackProbability);
        }

        float distance = GetHorizontalDistance(
            commonReferences.Origin.position,
            commonReferences.PlayerTransform.position);

        float probability =
            distance < parameters.Distance
                ? parameters.NearProbability
                : parameters.DefaultProbability;

        return CheckProbability(probability);
    }

    /// <summary>
    /// フェーズ共通参照を取得します。
    /// </summary>
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
