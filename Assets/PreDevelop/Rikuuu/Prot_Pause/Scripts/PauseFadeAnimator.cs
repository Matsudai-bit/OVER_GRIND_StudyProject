using UnityEngine;
using UnityEngine.Playables;

public class PauseFadeAnimator : MonoBehaviour
{
    [Header("Timeline")]
    [SerializeField] private PlayableDirector m_fadeInDirector;
    [SerializeField] private PlayableDirector m_fadeOutDirector;

    [Header("設定")]
    // ポーズ中(Time.timeScale = 0)でも再生するか
    [SerializeField] private bool m_useUnscaledTime = true;
    // 有効化時に自動でフェードインを再生するか
    [SerializeField] private bool m_playFadeInOnEnable = false;

    private void Awake()
    {
        SetupDirector(m_fadeInDirector);
    }

    private void OnEnable()
    {
        if (m_playFadeInOnEnable)
        {
            PlayFadeIn();
        }
    }

    // フェードインを再生
    public void PlayFadeIn()
    {
        Play(m_fadeInDirector, m_fadeOutDirector);
    }

    // フェードアウトを再生
    public void PlayFadeOut()
    {
        Play(m_fadeOutDirector, m_fadeInDirector);
    }

    // 再生中のフェードを止めて、最初の状態に戻す
    public void StopAll()
    {
        if (m_fadeInDirector != null)
        {
            m_fadeInDirector.Stop();
        }
    }

    // フェードが再生中かどうか
    public bool IsPlaying
    {
        get
        {
            return (m_fadeInDirector != null && m_fadeInDirector.state == PlayState.Playing) ||
                   (m_fadeOutDirector != null && m_fadeOutDirector.state == PlayState.Playing);
        }
    }

    // ----------------------------------------------------------------------

    private void Play(PlayableDirector target, PlayableDirector other)
    {
        if (target == null)
        {
            Debug.LogWarning($"{gameObject.name}: PlayableDirector が設定されていません。", this);
            return;
        }

        // 逆方向のフェードが再生中なら止める
        if (other != null)
        {
            other.Stop();
        }

        // 最初から再生する
        target.time = 0;
        target.Play();
    }

    // 設定の適用
    private void SetupDirector(PlayableDirector director)
    {
        if (director == null)
        {
            return;
        }

        director.playOnAwake = false;
        director.extrapolationMode = DirectorWrapMode.None;
        director.timeUpdateMode = m_useUnscaledTime
            ? DirectorUpdateMode.UnscaledGameTime
            : DirectorUpdateMode.GameTime;
    }
}
