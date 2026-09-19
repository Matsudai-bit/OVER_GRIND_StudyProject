using System;
using Unity.Behavior;
using UnityEngine;

/// <summary>
/// S1P1の踏みつけ攻撃を使用するか判定します。
/// </summary>
[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(
    name: "ShouldUseP1Stomp",
    story: "[BossController] がS1P1踏みつけ攻撃を [StompDistance]m以内なら [NearProbability]、それ以外なら [DefaultProbability] の確率で選択する",
    category: "Conditions",
    id: "e588ca320e4cc599c92887fe5e4ad388")]
public partial class ShouldUseP1StompCondition
    : BossActionDecisionCondition
{
    // ボスコントローラ
    [SerializeReference]
    public BlackboardVariable<BossController> BossController;

    // 足元と判定する距離
    [SerializeReference]
    public BlackboardVariable<float> StompDistance;

    // Playerが足元にいる場合の選択確率
    [SerializeReference]
    public BlackboardVariable<float> NearProbability;

    // 通常時の選択確率
    [SerializeReference]
    public BlackboardVariable<float> DefaultProbability;

    /// <summary>
    /// 踏みつけ攻撃を使用するか判定します。
    /// </summary>
    /// <returns>
    /// true：踏みつけ攻撃を選択します。
    /// false：踏みつけ攻撃を選択しません。
    /// </returns>
    public override bool IsTrue()
    {
        if (!TryGetReferences(
                out BossPhaseReferences commonReferences,
                out S1P1BossReferences phaseReferences))
        {
            return false;
        }

        float distance = GetHorizontalDistance(
            commonReferences.Origin.position,
            phaseReferences.PlayerTransform.position);

        bool isPlayerNearFeet =
            distance <= StompDistance.Value;

        float probability =
            isPlayerNearFeet
                ? NearProbability.Value
                : DefaultProbability.Value;

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