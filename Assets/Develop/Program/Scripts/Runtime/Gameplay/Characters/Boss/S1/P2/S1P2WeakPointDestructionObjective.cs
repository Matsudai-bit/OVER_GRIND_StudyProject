using UnityEngine;

/// <summary>
/// ステージ1フェーズ2の弱点破壊による
/// フェーズ終了条件を管理します。
/// </summary>
public sealed class S1P2WeakPointDestructionObjective :
    BossPhaseObjective
{
    // フェーズ2で破壊対象となる弱点
    [SerializeField, Header("破壊対象")]
    private BossBreakablePart m_weakPoint;

    /// <summary>
    /// 弱点の破壊監視を開始します。
    /// </summary>
    private void OnEnable()
    {
        if (m_weakPoint == null)
        {
            Debug.LogError(
                $"[{nameof(S1P2WeakPointDestructionObjective)}] " +
                $"{nameof(BossBreakablePart)}が設定されていません。",
                this);

            return;
        }

        m_weakPoint.Broken +=
            HandleWeakPointBroken;

        /*
         * 有効化時点ですでに破壊済みの場合にも対応します。
         */
        CheckPhaseCompletion();
    }

    /// <summary>
    /// 弱点の破壊監視を終了します。
    /// </summary>
    private void OnDisable()
    {
        if (m_weakPoint == null)
        {
            return;
        }

        m_weakPoint.Broken -=
            HandleWeakPointBroken;
    }

    /// <summary>
    /// 弱点破壊通知を処理します。
    /// </summary>
    /// <param name="weakPoint">
    /// 破壊された弱点。
    /// </param>
    private void HandleWeakPointBroken(
        BossBreakablePart weakPoint)
    {
        if (weakPoint !=
            m_weakPoint)
        {
            return;
        }

        CheckPhaseCompletion();
    }

    /// <summary>
    /// フェーズ2の終了条件を確認します。
    /// </summary>
    private void CheckPhaseCompletion()
    {
        if (m_weakPoint == null ||
            !m_weakPoint.IsBroken)
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
        /*
         * BossBreakablePart自体の復元は
         * Objectiveの責務ではないため何もしません。
         */
    }
}