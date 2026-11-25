using System;
using UnityEngine;

/// <summary>
/// チェックポイント参加コンポーネント1つ分の記録です。
/// </summary>
[Serializable]
public sealed class CheckpointParticipantSnapshot
{
    // 参加コンポーネント識別子
    [SerializeField]
    private string m_participantKey;

    // 各参加コンポーネント固有のJSONデータ
    [SerializeField]
    private string m_json;

    /// <summary>
    /// 参加コンポーネント識別子を取得します。
    /// </summary>
    public string ParticipantKey =>
        m_participantKey;

    /// <summary>
    /// 保存されているJSONを取得します。
    /// </summary>
    public string Json =>
        m_json;

    /// <summary>
    /// スナップショットを生成します。
    /// </summary>
    /// <param name="participantKey">参加コンポーネント識別子。</param>
    /// <param name="json">保存データ。</param>
    public CheckpointParticipantSnapshot(
        string participantKey,
        string json)
    {
        m_participantKey =
            participantKey;

        m_json =
            json;
    }

    /// <summary>
    /// スナップショットを複製します。
    /// </summary>
    /// <returns>複製したスナップショット。</returns>
    public CheckpointParticipantSnapshot CreateCopy()
    {
        return new CheckpointParticipantSnapshot(
            m_participantKey,
            m_json);
    }
}
