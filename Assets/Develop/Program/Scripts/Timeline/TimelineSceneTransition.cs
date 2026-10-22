using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

/// <summary>
/// Timelineの再生終了時に指定シーンへ遷移します。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayableDirector))]
public sealed class TimelineSceneTransition : MonoBehaviour
{
    [SerializeField, Header("再生対象のTimeline")]
    private PlayableDirector m_playableDirector;

    [SerializeField, Header("遷移先シーン名")]
    private string m_nextSceneName;

    // シーン遷移中かどうか
    private bool m_isTransitioning = false;

    private void Awake()
    {
        // 未設定なら同じオブジェクトから取得
        if (m_playableDirector == null)
        {
            m_playableDirector = GetComponent<PlayableDirector>();
        }
    }

    private void OnEnable()
    {
        if (m_playableDirector == null)
        {
            Debug.LogError(
                "PlayableDirectorが設定されていません。",
                this);

            return;
        }

        // Timeline停止イベントを登録
        m_playableDirector.stopped += OnTimelineStopped;
    }

    private void OnDisable()
    {
        if (m_playableDirector != null)
        {
            m_playableDirector.stopped -= OnTimelineStopped;
        }
    }

    /// <summary>
    /// Timelineの再生終了を受け取ります。
    /// </summary>
    private void OnTimelineStopped(PlayableDirector director)
    {
        if (director != m_playableDirector)
        {
            return;
        }

        // 多重遷移を防止
        if (m_isTransitioning)
        {
            return;
        }

        ChangeScene();
    }

    /// <summary>
    /// 指定したシーンへ遷移します。
    /// </summary>
    public void ChangeScene()
    {
        if (m_isTransitioning)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(m_nextSceneName))
        {
            Debug.LogError(
                "遷移先シーン名が設定されていません。",
                this);

            return;
        }

        m_isTransitioning = true;

        // 次のシーンを非同期で読み込み
        AsyncOperation operation = SceneManager.LoadSceneAsync(
            m_nextSceneName,
            LoadSceneMode.Single);

        if (operation == null)
        {
            Debug.LogError(
                $"シーンの読み込みに失敗しました: {m_nextSceneName}",
                this);

            m_isTransitioning = false;
        }
    }
}