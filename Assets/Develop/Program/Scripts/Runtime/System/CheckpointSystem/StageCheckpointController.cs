using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ステージ内のチェックポイント記録と復元を管理します。
/// </summary>
[DisallowMultipleComponent]
public sealed class StageCheckpointController : MonoBehaviour
{
    // ボスフェーズ管理
    [SerializeField, Header("参照")]
    private BossPhaseController m_bossPhaseController;

    // ステージで使用するチェックポイント定義
    [SerializeField, Header("チェックポイント定義")]
    private List<CheckpointDefinitionAsset>
        m_checkpointDefinitions = new();

    // チェックポイントへ保存するコンポーネント
    [SerializeField, Header("保存対象")]
    private List<CheckpointParticipant>
        m_participants = new();

    // 現在保持しているチェックポイント
    [SerializeField, Header("Runtime確認用")]
    private StageCheckpointSnapshot
        m_currentSnapshot;

    // 復元処理中か
    private bool m_isRestoring = false;

    /// <summary>
    /// チェックポイントを保持しているか取得します。
    /// </summary>
    public bool HasCheckpoint =>
        m_currentSnapshot != null;

    /// <summary>
    /// チェックポイント復元中か取得します。
    /// </summary>
    public bool IsRestoring =>
        m_isRestoring;

    /// <summary>
    /// 現在のボスフェーズに対応するチェックポイントを記録します。
    /// </summary>
    /// <returns>
    /// true：記録しました。
    /// false：記録できませんでした。
    /// </returns>
    public bool CaptureCurrentPhaseCheckpoint()
    {
        if (m_bossPhaseController == null)
        {
            Debug.LogWarning(
                $"{nameof(BossPhaseController)} "
                + "が設定されていません。",
                this);

            return false;
        }

        BossPhaseID currentPhase =
            m_bossPhaseController.CurrentPhase;

        if (!TryGetCheckpointDefinition(
            currentPhase,
            out CheckpointDefinitionAsset definition))
        {
            Debug.LogWarning(
                $"{currentPhase} に対応する"
                + "チェックポイント定義がありません。",
                this);

            return false;
        }

        return CaptureCheckpoint(
            definition);
    }

    /// <summary>
    /// 指定されたチェックポイントを記録します。
    /// </summary>
    /// <param name="definition">
    /// 記録するチェックポイント定義。
    /// </param>
    /// <returns>
    /// true：記録しました。
    /// false：記録できませんでした。
    /// </returns>
    public bool CaptureCheckpoint(
        CheckpointDefinitionAsset definition)
    {
        if (definition == null
            || m_bossPhaseController == null)
        {
            return false;
        }

        if (m_isRestoring)
        {
            return false;
        }

        if (m_bossPhaseController.CurrentPhase !=
            definition.BossPhase)
        {
            Debug.LogWarning(
                $"現在フェーズ "
                + $"{m_bossPhaseController.CurrentPhase} と"
                + $"チェックポイントフェーズ "
                + $"{definition.BossPhase} が一致しません。",
                this);

            return false;
        }

        StageCheckpointSnapshot snapshot =
            new StageCheckpointSnapshot(
                definition.CheckpointId,
                definition.BossPhase);

        HashSet<string> capturedKeys =
            new HashSet<string>();

        foreach (
            CheckpointParticipant participant
            in m_participants)
        {
            if (participant == null)
            {
                continue;
            }

            string participantKey =
                participant.ParticipantKey;

            if (!capturedKeys.Add(
                participantKey))
            {
                Debug.LogWarning(
                    $"チェックポイント識別子 "
                    + $"'{participantKey}' が重複しています。",
                    participant);

                continue;
            }

            if (!participant.TryCaptureCheckpoint(
                out CheckpointParticipantSnapshot
                    participantSnapshot))
            {
                continue;
            }

            snapshot.AddParticipantSnapshot(
                participantSnapshot);
        }

        m_currentSnapshot =
            snapshot;

        return true;
    }

    /// <summary>
    /// 指定されたチェックポイント状態を復元します。
    /// </summary>
    /// <param name="snapshot">
    /// 復元するチェックポイント。
    /// </param>
    /// <returns>
    /// true：復元処理が完了しました。
    /// false：復元に失敗しました。
    /// </returns>
    public bool RestoreCheckpoint(
        StageCheckpointSnapshot snapshot)
    {
        if (snapshot == null
            || m_bossPhaseController == null)
        {
            return false;
        }

        m_isRestoring =
            true;

        bool isSuccess =
            true;

        // フェーズRootやフェーズ固有設定を先に復元
        if (!m_bossPhaseController.SetPhase(
            snapshot.BossPhase))
        {
            Debug.LogError(
                $"{snapshot.BossPhase} への"
                + "フェーズ復元に失敗しました。",
                this);

            m_isRestoring =
                false;

            return false;
        }

        Dictionary<string, CheckpointParticipant>
            participantMap =
                CreateParticipantMap();

        foreach (
            CheckpointParticipantSnapshot
                participantSnapshot
            in snapshot.ParticipantSnapshots)
        {
            if (participantSnapshot == null)
            {
                continue;
            }

            if (!participantMap.TryGetValue(
                participantSnapshot.ParticipantKey,
                out CheckpointParticipant participant))
            {
                Debug.LogWarning(
                    $"復元対象 "
                    + $"'{participantSnapshot.ParticipantKey}' "
                    + "がScene内に存在しません。",
                    this);

                isSuccess =
                    false;

                continue;
            }

            if (!participant.RestoreCheckpoint(
                participantSnapshot))
            {
                Debug.LogWarning(
                    $"'{participantSnapshot.ParticipantKey}' "
                    + "の復元に失敗しました。",
                    participant);

                isSuccess =
                    false;
            }
        }

        m_currentSnapshot =
            snapshot.CreateCopy();

        m_isRestoring =
            false;

        return isSuccess;
    }

    /// <summary>
    /// 現在のチェックポイントを取得します。
    /// </summary>
    /// <param name="snapshot">
    /// 取得したチェックポイント。
    /// </param>
    /// <returns>
    /// true：チェックポイントを取得しました。
    /// false：チェックポイントがありません。
    /// </returns>
    public bool TryGetCurrentSnapshot(
        out StageCheckpointSnapshot snapshot)
    {
        snapshot = null;

        if (m_currentSnapshot == null)
        {
            return false;
        }

        snapshot =
            m_currentSnapshot.CreateCopy();

        return true;
    }

    /// <summary>
    /// 現在のチェックポイントを破棄します。
    /// </summary>
    public void ClearCheckpoint()
    {
        m_currentSnapshot =
            null;
    }

    /// <summary>
    /// 指定フェーズに対応するチェックポイント定義を取得します。
    /// </summary>
    /// <param name="phase">検索するフェーズ。</param>
    /// <param name="definition">取得した定義。</param>
    /// <returns>
    /// true：見つかりました。
    /// false：見つかりませんでした。
    /// </returns>
    private bool TryGetCheckpointDefinition(
        BossPhaseID phase,
        out CheckpointDefinitionAsset definition)
    {
        foreach (
            CheckpointDefinitionAsset checkpointDefinition
            in m_checkpointDefinitions)
        {
            if (checkpointDefinition == null
                || checkpointDefinition.BossPhase != phase)
            {
                continue;
            }

            definition =
                checkpointDefinition;

            return true;
        }

        definition =
            null;

        return false;
    }

    /// <summary>
    /// 参加コンポーネントを識別子から取得できるMapへ変換します。
    /// </summary>
    /// <returns>参加コンポーネントMap。</returns>
    private Dictionary<string, CheckpointParticipant>
        CreateParticipantMap()
    {
        Dictionary<string, CheckpointParticipant>
            participantMap =
                new Dictionary<string, CheckpointParticipant>();

        foreach (
            CheckpointParticipant participant
            in m_participants)
        {
            if (participant == null
                || string.IsNullOrWhiteSpace(
                    participant.ParticipantKey))
            {
                continue;
            }

            if (participantMap.ContainsKey(
                participant.ParticipantKey))
            {
                Debug.LogWarning(
                    $"チェックポイント識別子 "
                    + $"'{participant.ParticipantKey}' "
                    + "が重複しています。",
                    participant);

                continue;
            }

            participantMap.Add(
                participant.ParticipantKey,
                participant);
        }

        return participantMap;
    }
}
