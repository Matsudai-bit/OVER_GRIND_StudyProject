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
public partial class ShouldUseP1StompCondition
    : BossActionDecisionCondition
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
        if (DecisionParameterAsset?.Value == null)
        {
            return false;
        }

        if (!TryGetReferences(
                out BossPhaseReferences commonReferences,
                out S1P1BossReferences phaseReferences))
        {
            return false;
        }

        S1P1BossDecisionParameterAsset.StompParameters parameters =
            DecisionParameterAsset.Value.Stomp;

        float distance = GetHorizontalDistance(
            commonReferences.Origin.position,
            phaseReferences.PlayerTransform.position);

        bool isPlayerNearFeet =
            distance <= parameters.Distance;

        float probability =
            isPlayerNearFeet
                ? parameters.NearProbability
                : parameters.DefaultProbability;

        return CheckProbability(probability);
    }

    /// <summary>
    /// 現在のフェーズから必要な参照情報を取得します。
    /// </summary>
    /// <param name="commonReferences">ボス共通参照情報。</param>
    /// <param name="phaseReferences">S1P1固有参照情報。</param>
    /// <returns>
    /// true：必要な参照情報を取得できました。
    /// false：取得できませんでした。
    /// </returns>
    private bool TryGetReferences(
        out BossPhaseReferences commonReferences,
        out S1P1BossReferences phaseReferences)
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

        return commonReferences.Origin != null &&
               phaseReferences.PlayerTransform != null;
    }

    /// <summary>
    /// XZ平面上の距離を取得します。
    /// </summary>
    /// <param name="from">開始位置。</param>
    /// <param name="to">終了位置。</param>
    /// <returns>水平距離。</returns>
    private static float GetHorizontalDistance(
        Vector3 from,
        Vector3 to)
    {
        Vector3 difference = to - from;
        difference.y = 0.0f;

        return difference.magnitude;
    }
}