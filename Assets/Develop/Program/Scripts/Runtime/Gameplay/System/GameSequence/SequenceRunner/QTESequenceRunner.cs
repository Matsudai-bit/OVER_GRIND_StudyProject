using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// QTE用のGameplaySequenceを実行します。
/// </summary>
[DisallowMultipleComponent]
public sealed class QTESequenceRunner :
    GameplaySequenceRunner
{
    // QTEを再生するPlayableDirector
    [SerializeField, Header("QTE Timeline")]
    private PlayableDirector m_playableDirector;

    /// <summary>
    /// PlayableDirectorのイベントを購読します。
    /// </summary>
    private void OnEnable()
    {
        if (m_playableDirector == null)
        {
            return;
        }

        m_playableDirector.stopped +=
            HandlePlayableDirectorStopped;
    }

    /// <summary>
    /// PlayableDirectorのイベント購読を解除します。
    /// </summary>
    private void OnDisable()
    {
        if (m_playableDirector == null)
        {
            return;
        }

        m_playableDirector.stopped -=
            HandlePlayableDirectorStopped;
    }

    /// <summary>
    /// QTE Timelineを開始します。
    /// </summary>
    /// <returns>
    /// true：開始しました。
    /// false：開始できませんでした。
    /// </returns>
    protected override bool OnStartSequence()
    {
        if (m_playableDirector == null)
        {
            Debug.LogError(
                $"[{nameof(QTESequenceRunner)}] " +
                $"{nameof(PlayableDirector)}が設定されていません。",
                this);

            return false;
        }

        // 常にTimeline先頭から開始する
        m_playableDirector.time =
            0.0;

        m_playableDirector.Play();

        return true;
    }

    /// <summary>
    /// QTE Timelineを停止します。
    /// </summary>
    protected override void OnStopSequence()
    {
        if (m_playableDirector == null)
        {
            return;
        }

        m_playableDirector.Stop();
    }

    /// <summary>
    /// PlayableDirectorの終了通知を処理します。
    /// </summary>
    /// <param name="director">
    /// 終了したPlayableDirector。
    /// </param>
    private void HandlePlayableDirectorStopped(
        PlayableDirector director)
    {
        if (!IsRunning ||
            director != m_playableDirector)
        {
            return;
        }

        CompleteSequence();
    }
}