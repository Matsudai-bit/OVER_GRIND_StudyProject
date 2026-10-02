using System;
using Unity.Behavior;
using UnityEngine;

/// <summary>
/// S1P3のドレッド攻撃を使用するか判定します。
/// </summary>
[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(
    name: "ShouldUseS1P3DreadAttack",
    story: "[BossController] が [DecisionParameterAsset] の設定を使用してS1P3ドレッド攻撃を選択する",
    category: "Conditions",
    id: "4ff9ddef866c4a79928c0d68f385c7e1")]
public partial class ShouldUseS1P3DreadAttackCondition
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
    /// ドレッド攻撃を使用するか判定します。
    /// </summary>
    /// <returns>
    /// true：ドレッド攻撃を選択します。
    /// false：ドレッド攻撃を選択しません。
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

        if (!TryGetCommonReferences(
                out BossPhaseReferences commonReferences))
        {
            return false;
        }

        S1P3BossDecisionParameterAsset.DreadAttackParameters parameters =
            DecisionParameterAsset.Value.DreadAttack;

        float distance =
            GetHorizontalDistance(
                commonReferences.Origin.position,
                commonReferences.PlayerTransform.position);

        bool isNear =
            distance < parameters.Distance;

        float probability =
            isNear
                ? parameters.NearProbability
                : parameters.DefaultProbability;

        return CheckProbability(probability);
    }

    /// <summary>
    /// ドレッド攻撃Stateがクールタイム終了済みか確認します。
    /// </summary>
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
            .IsReady<S1P3BossDreadAttackState>();
    }

    /// <summary>
    /// 共通フェーズ参照を取得します。
    /// </summary>
    private bool TryGetCommonReferences(
        out BossPhaseReferences commonReferences)
    {
        commonReferences = null;

        if (BossController.Value.PhaseController == null)
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

    /// <summary>
    /// XZ平面上の距離を取得します。
    /// </summary>
    private static float GetHorizontalDistance(
        Vector3 from,
        Vector3 to)
    {
        Vector3 difference =
            to - from;

        difference.y = 0.0f;

        return difference.magnitude;
    }
}
