using System;
using Unity.Behavior;
using UnityEngine;

/// <summary>
/// S1P1の方向転換を使用するか判定します。
/// </summary>
[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(
    name: "ShouldUseP1Turn",
    story: "[BossController] が [DecisionParameterAsset] の設定を使用してS1P1方向転換を選択する",
    category: "Conditions",
    id: "7ece6f0d690944aeb86aa7d0a3c9270d")]
public partial class ShouldUseP1TurnCondition :
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
    /// 方向転換を使用するか判定します。
    /// </summary>
    /// <returns>
    /// true：方向転換を選択します。
    /// false：方向転換を選択しません。
    /// </returns>
    public override bool IsTrue()
    {
        if (BossController?.Value == null ||
            DecisionParameterAsset?.Value == null)
        {
            return false;
        }

        // 方向転換後に必ず歩行するため、両Stateが使用可能な場合だけ選択する
        if (!IsStateReady<S1P1BossTurnState>(BossController.Value) ||
            !IsStateReady<S1P1BossWalkState>(BossController.Value))
        {
            return false;
        }

        if (!TryGetCommonReferences(
                out BossPhaseReferences commonReferences))
        {
            return false;
        }

        S1P1BossDecisionParameterAsset.TurnParameters parameters =
            DecisionParameterAsset.Value.Turn;

        float distance = GetHorizontalDistance(
            commonReferences.Origin.position,
            commonReferences.PlayerTransform.position);

        bool isPlayerFar = distance >= parameters.Distance;

        float probability =
            isPlayerFar
                ? parameters.FarProbability
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
