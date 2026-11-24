using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

// ボタンと同じオブジェクト（または任意のオブジェクト）に付ける
public class TimelineClickSequence : MonoBehaviour
{
    [SerializeField] private PlayableDirector m_director;
    // Timeline再生が終了したときに実行する関数
    [SerializeField] private UnityEvent m_onTimelineEnd;

    private bool m_isPlaying;

    private void OnEnable()
    {
        m_director.stopped += OnDirectorStopped;
    }

    private void OnDisable()
    {
        m_director.stopped -= OnDirectorStopped;
    }

    // SelectButtonのOnClickに登録する関数
    public void Play()
    {
        if (m_isPlaying) return; // 連打防止
        m_isPlaying = true;

        m_director.time = 0;
        m_director.Play();
    }

    private void OnDirectorStopped(PlayableDirector director)
    {
        if (!m_isPlaying) return;
        m_isPlaying = false;
        m_onTimelineEnd?.Invoke();
    }
}