using UnityEngine;

/// <summary>
/// S1P2の尻尾叩きつけ攻撃を
/// 実行するか判定します。
/// </summary>
public static class S1P2TailSlamDecisionEvaluator
{
    /// <summary>
    /// 現在の状況から尻尾叩きつけ攻撃を
    /// 実行するか判定します。
    /// </summary>
    /// <param name="bossController">
    /// 判定対象のBoss。
    /// </param>
    /// <param name="decisionParameterAsset">
    /// S1P2行動選択パラメータ。
    /// </param>
    /// <returns>
    /// true：尻尾叩きつけ攻撃を実行します。
    /// false：実行しません。
    /// </returns>
    public static bool ShouldExecute(
        BossController bossController,
        S1P2BossDecisionParameterAsset decisionParameterAsset)
    {
        if (bossController == null ||
            decisionParameterAsset == null ||
            bossController.PhaseController == null)
        {
            return false;
        }

        if (!IsStateReady(
                bossController))
        {
            return false;
        }

        if (!TryGetReferences(
                bossController,
                out BossPhaseReferences commonReferences,
                out S1P2BossReferences phaseReferences))
        {
            return false;
        }

        S1P2BossDecisionParameterAsset.TailSlamParameters
            parameters =
                decisionParameterAsset.TailSlam;

        if (parameters == null)
        {
            return false;
        }

        float distance =
            GetHorizontalDistance(
                phaseReferences.TailTransform.position,
                commonReferences.PlayerTransform.position);

        float probability =
            distance <= parameters.Distance
                ? parameters.NearProbability
                : parameters.DefaultProbability;

        return CheckProbability(
            probability);
    }

    /// <summary>
    /// 尻尾叩きつけStateのクールタイムが
    /// 終了しているか確認します。
    /// </summary>
    private static bool IsStateReady(
        BossController bossController)
    {
        BossStateCoolTimeManager coolTimeManager =
            bossController.GetComponent<
                BossStateCoolTimeManager>();

        if (coolTimeManager == null)
        {
            return false;
        }

        return coolTimeManager
            .IsReady<S1P2BossTailSlamState>();
    }

    /// <summary>
    /// 判定に必要なフェーズ参照を取得します。
    /// </summary>
    private static bool TryGetReferences(
        BossController bossController,
        out BossPhaseReferences commonReferences,
        out S1P2BossReferences phaseReferences)
    {
        commonReferences =
            null;

        phaseReferences =
            null;

        if (!bossController.PhaseController
                .TryGetCurrentPhaseComponent(
                    out commonReferences))
        {
            return false;
        }

        if (!bossController.PhaseController
                .TryGetCurrentPhaseComponent(
                    out phaseReferences))
        {
            return false;
        }

        return commonReferences.PlayerTransform != null &&
               phaseReferences.TailTransform != null;
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

        difference.y =
            0.0f;

        return difference.magnitude;
    }

    /// <summary>
    /// 指定確率で抽選します。
    /// </summary>
    private static bool CheckProbability(
        float probability)
    {
        return Random.value <=
               Mathf.Clamp01(
                   probability);
    }
}