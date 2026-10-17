using System;
using UnityEngine;

/// <summary>
/// ステージ1フェーズ1の破壊進行と
/// フェーズ終了条件を管理します。
/// </summary>
public sealed class S1P1LegDestructionObjective :
    BossPhaseObjective
{
    // 最初に破壊する左右脚
    [SerializeField, Header("脚破壊グループ")]
    private BossBreakablePartGroup m_legGroup =
        new();

    // 脚破壊後に出現する最終弱点
    [SerializeField, Header("最終弱点グループ")]
    private BossBreakablePartGroup m_finalWeakPointGroup =
        new();

    /// <summary>
    /// 両脚の破壊が完了したときに通知されます。
    /// </summary>
    public event Action<S1P1LegDestructionObjective>
        LegsDestroyed;

    /// <summary>
    /// 破壊監視を開始します。
    /// </summary>
    private void OnEnable()
    {
        m_legGroup.AllPartsBroken +=
            HandleAllLegsBroken;

        m_finalWeakPointGroup.AllPartsBroken +=
            HandleFinalWeakPointBroken;

        m_legGroup.StartMonitoring();

        m_finalWeakPointGroup.StartMonitoring();

        CheckPhaseCompletion();

       
    }

    /// <summary>
    /// 破壊監視を終了します。
    /// </summary>
    private void OnDisable()
    {
        m_legGroup.AllPartsBroken -=
            HandleAllLegsBroken;

        m_finalWeakPointGroup.AllPartsBroken -=
            HandleFinalWeakPointBroken;

        m_legGroup.StopMonitoring();

        m_finalWeakPointGroup.StopMonitoring();
    }

    /// <summary>
    /// 両脚の全破壊通知を処理します。
    /// </summary>
    /// <param name="group">破壊完了したグループ。</param>
    private void HandleAllLegsBroken(
        BossBreakablePartGroup group)
    {
        /*
         * Phase終了ではない。
         * 次の弱点を出現させる側へ通知する。
         */
        LegsDestroyed?.Invoke(
            this);

        CheckPhaseCompletion();
    }

    /// <summary>
    /// 最終弱点の破壊通知を処理します。
    /// </summary>
    /// <param name="group">破壊完了したグループ。</param>
    private void HandleFinalWeakPointBroken(
        BossBreakablePartGroup group)
    {
        CheckPhaseCompletion();
    }

    /// <summary>
    /// Phase1の終了条件を確認します。
    /// </summary>
    private void CheckPhaseCompletion()
    {
        /*
         * 最終弱点だけ先に破壊された場合でも
         * フェーズ終了しないよう両条件を確認する。
         */
        if (!m_legGroup.AreAllPartsBroken ||
            !m_finalWeakPointGroup.AreAllPartsBroken)
        {
            return;
        }

        CompleteObjective();
    }

    /// <summary>
    /// フェーズ終了条件をリセットします。
    /// </summary>
    protected override void OnResetObjective()
    {
        m_legGroup.Reset();

        m_finalWeakPointGroup.Reset();
    }
}