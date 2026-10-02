using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// Timelineを使用してゲームプレイシーケンスを実行します。
/// </summary>
[DisallowMultipleComponent]
public sealed class CutSceneSequenceRunner : GameplaySequenceRunner
{
    // シーケンスで使用するPlayableDirector
    [SerializeField, Header("Timeline")]
    private PlayableDirector m_playableDirector;

    private void OnEnable()
    {
        if (m_playableDirector == null)
        {
            return;
        }

        m_playableDirector.stopped += OnPlayableDirectorStopped;
    }

    private void OnDisable()
    {
        if (m_playableDirector == null)
        {
            return;
        }

        m_playableDirector.stopped -= OnPlayableDirectorStopped;
    }

    /// <summary>
    /// Timelineを開始します。
    /// </summary>
    /// <returns>
    /// true：開始しました。
    /// false：開始できませんでした。
    /// </returns>
    protected override bool OnStartSequence()
    {
        if (m_playableDirector == null)
        {
            Debug.LogWarning(
                $"{nameof(PlayableDirector)} が設定されていません。",
                this);

            return false;
        }

        // Timelineを先頭から再生
        m_playableDirector.time = 0.0;
        m_playableDirector.Play();

        return true;
    }

    /// <summary>
    /// Timelineを停止します。
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
    /// Timeline終了時にシーケンス完了を通知します。
    /// </summary>
    /// <param name="director">終了したPlayableDirector。</param>
    private void OnPlayableDirectorStopped(PlayableDirector director)
    {
        if (!IsRunning)
        {
            return;
        }

        if (director != m_playableDirector)
        {
            return;
        }

        CompleteSequence();
    }
}