using System;
using Unity.Behavior;
using UnityEngine;

/// <summary>
/// S1P2のミサイル攻撃を使用するか判定します。
/// </summary>
[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(
    name: "ShouldUseS1P2Missile",
    story: "[BossController] が [DecisionParameterAsset] の設定を使用してS1P2ミサイル攻撃を選択する",
    category: "Conditions",
    id: "41cbb013060fbf111406499a2e02d432")]
public partial class ShouldUseS1P2MissileCondition :
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

        if (!IsStateReady<S1P2BossMissileState>(
                BossController.Value))
        {
            return false;
        }

        if (!TryGetCommonReferences(
                out BossPhaseReferences commonReferences))
        {
            return false;
        }

        S1P2BossDecisionParameterAsset.MissileParameters parameters =
            DecisionParameterAsset.Value.Missile;

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
            distance >= parameters.Distance
                ? parameters.FarProbability
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
