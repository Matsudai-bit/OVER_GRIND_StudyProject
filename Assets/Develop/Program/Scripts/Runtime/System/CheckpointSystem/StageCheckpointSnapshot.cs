using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// チェックポイント時点のステージ状態を保持します。
/// </summary>
[Serializable]
public sealed class StageCheckpointSnapshot
{
    // チェックポイント識別子
    [SerializeField]
    private string m_checkpointId;

    // チェックポイント時点のボスフェーズ
    [SerializeField]
    private BossPhaseID m_bossPhase;

    // 各システムのスナップショット
    [SerializeField]
    private List<CheckpointParticipantSnapshot>
        m_participantSnapshots = new();

    /// <summary>
    /// チェックポイント識別子を取得します。
    /// </summary>
    public string CheckpointId =>
        m_checkpointId;

    /// <summary>
    /// チェックポイント時点のボスフェーズを取得します。
    /// </summary>
    public BossPhaseID BossPhase =>
        m_bossPhase;

    /// <summary>
    /// 各システムのスナップショットを取得します。
    /// </summary>
    public IReadOnlyList<CheckpointParticipantSnapshot>
        ParticipantSnapshots =>
            m_participantSnapshots;

    /// <summary>
    /// スナップショットを生成します。
    /// </summary>
    /// <param name="checkpointId">チェックポイント識別子。</param>
    /// <param name="bossPhase">ボスフェーズ。</param>
    public StageCheckpointSnapshot(
        string checkpointId,
        BossPhaseID bossPhase)
    {
        m_checkpointId =
            checkpointId;

        m_bossPhase =
            bossPhase;

        m_participantSnapshots =
            new List<CheckpointParticipantSnapshot>();
    }

    /// <summary>
    /// 参加コンポーネントのスナップショットを追加します。
    /// </summary>
    /// <param name="snapshot">追加するスナップショット。</param>
    public void AddParticipantSnapshot(
        CheckpointParticipantSnapshot snapshot)
    {
        if (snapshot == null)
        {
            return;
        }

        m_participantSnapshots.Add(
            snapshot);
    }

    /// <summary>
    /// スナップショットを複製します。
    /// </summary>
    /// <returns>複製したスナップショット。</returns>
    public StageCheckpointSnapshot CreateCopy()
    {
        StageCheckpointSnapshot copy =
            new StageCheckpointSnapshot(
                m_checkpointId,
                m_bossPhase);

        foreach (
            CheckpointParticipantSnapshot snapshot
            in m_participantSnapshots)
        {
            if (snapshot == null)
            {
                continue;
            }

            copy.AddParticipantSnapshot(
                snapshot.CreateCopy());
        }

        return copy;
    }
}
