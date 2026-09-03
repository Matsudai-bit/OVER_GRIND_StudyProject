using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ステージ1フェーズ2の弱点破壊条件を管理します。
/// </summary>
public sealed class S1P3CoreDestructionObjective : BossPhaseObjective
{
    // フェーズ2で破壊する弱点部位
    [SerializeField, Header("フェーズ2破壊対象")]
    private List<BossBreakablePart> m_weakPoints = new();

    /// <summary>
    /// 破壊通知を購読します。
    /// </summary>
    private void OnEnable()
    {
        foreach (BossBreakablePart weakPoint in m_weakPoints)
        {
            if (weakPoint == null)
            {
                continue;
            }

            weakPoint.Broken += HandleWeakPointBroken;
        }

        CheckObjectiveCompletion();
    }

    /// <summary>
    /// 破壊通知の購読を解除します。
    /// </summary>
    private void OnDisable()
    {
        foreach (BossBreakablePart weakPoint in m_weakPoints)
        {
            if (weakPoint == null)
            {
                continue;
            }

            weakPoint.Broken -= HandleWeakPointBroken;
        }
    }

    /// <summary>
    /// 弱点破壊通知を処理します。
    /// </summary>
    /// <param name="weakPoint">破壊された弱点。</param>
    private void HandleWeakPointBroken(BossBreakablePart weakPoint)
    {
        CheckObjectiveCompletion();
    }

    /// <summary>
    /// すべての弱点が破壊されたか確認します。
    /// </summary>
    private void CheckObjectiveCompletion()
    {
        if (m_weakPoints.Count == 0)
        {
            return;
        }

        foreach (BossBreakablePart weakPoint in m_weakPoints)
        {
            if (weakPoint == null || !weakPoint.IsBroken)
            {
                return;
            }
        }

        CompleteObjective();
    }
}
