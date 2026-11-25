using UnityEngine;

/// <summary>
/// チェックポイントへ状態を保存・復元するコンポーネントの基底クラスです。
/// </summary>
public abstract class CheckpointParticipant : MonoBehaviour
{
    // チェックポイント内で使用する一意な識別子
    [SerializeField, Header("チェックポイント")]
    private string m_participantKey = "Participant";

    /// <summary>
    /// 参加コンポーネント識別子を取得します。
    /// </summary>
    public string ParticipantKey =>
        m_participantKey;

    /// <summary>
    /// 現在の状態をスナップショットへ変換します。
    /// </summary>
    /// <param name="snapshot">生成したスナップショット。</param>
    /// <returns>
    /// true：状態を取得しました。
    /// false：状態を取得できませんでした。
    /// </returns>
    public bool TryCaptureCheckpoint(
        out CheckpointParticipantSnapshot snapshot)
    {
        snapshot = null;

        if (string.IsNullOrWhiteSpace(
            m_participantKey))
        {
            Debug.LogWarning(
                $"{nameof(CheckpointParticipant)} の"
                + "識別子が設定されていません。",
                this);

            return false;
        }

        string json =
            CaptureStateJson();

        if (json == null)
        {
            return false;
        }

        snapshot =
            new CheckpointParticipantSnapshot(
                m_participantKey,
                json);

        return true;
    }

    /// <summary>
    /// スナップショットから状態を復元します。
    /// </summary>
    /// <param name="snapshot">復元するスナップショット。</param>
    /// <returns>
    /// true：復元しました。
    /// false：復元できませんでした。
    /// </returns>
    public bool RestoreCheckpoint(
        CheckpointParticipantSnapshot snapshot)
    {
        if (snapshot == null)
        {
            return false;
        }

        if (snapshot.ParticipantKey !=
            m_participantKey)
        {
            return false;
        }

        return RestoreStateJson(
            snapshot.Json);
    }

    /// <summary>
    /// 現在状態をJSONへ変換します。
    /// </summary>
    /// <returns>
    /// 状態を表すJSON。
    /// 取得できない場合はnull。
    /// </returns>
    protected abstract string CaptureStateJson();

    /// <summary>
    /// JSONから状態を復元します。
    /// </summary>
    /// <param name="json">復元するJSON。</param>
    /// <returns>
    /// true：復元しました。
    /// false：復元できませんでした。
    /// </returns>
    protected abstract bool RestoreStateJson(
        string json);
}
