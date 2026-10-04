using System;
using Unity.Behavior;
using UnityEngine;

/// <summary>
/// S1P1のミサイル攻撃を使用するか判定します。
/// </summary>
[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(
    name: "ShouldUseP1Missile",
    story: "[BossController] が [DecisionParameterAsset] の設定を使用してS1P1ミサイル攻撃を選択する",
    category: "Conditions",
    id: "1df63c5f6cd64d2f84ea37f19323de7f")]
public partial class ShouldUseP1MissileCondition :
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
    /// ミサイル攻撃を使用するか判定します。
    /// </summary>
    /// <returns>
    /// true：ミサイル攻撃を選択します。
    /// false：ミサイル攻撃を選択しません。
    /// </returns>
    public override bool IsTrue()
    {
        if (BossController?.Value == null ||
            DecisionParameterAsset?.Value == null)
        {
            return false;
        }

        if (!IsStateReady<S1P1BossMissileState>(
                BossController.Value))
        {
            return false;
        }

        if (!TryGetCommonReferences(
                out BossPhaseReferences commonReferences))
        {
            return false;
        }

        S1P1BossDecisionParameterAsset.MissileParameters parameters =
            DecisionParameterAsset.Value.Missile;

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
