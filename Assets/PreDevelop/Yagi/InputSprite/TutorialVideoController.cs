using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Gameplay Cueの開始・終了に応じて、画面上の動画を切り替えます。
/// </summary>
[DisallowMultipleComponent]
public sealed class TutorialVideoController : MonoBehaviour
{
    private const int VIDEO_TEXTURE_WIDTH = 960;
    private const int VIDEO_TEXTURE_HEIGHT = 540;

    [SerializeField, Header("Cue変更イベント")]
    private GameplayCueChangedEvent m_cueChangedEvent;

    [SerializeField, Header("Cueと動画のデータ")]
    private List<TutorialVideoData> m_videoDataList = new();

    [SerializeField, Header("動画再生コンポーネント")]
    private VideoPlayer m_videoPlayer;

    [SerializeField, Header("動画表示UI")]
    private GameObject m_videoUiObject;

    [SerializeField, Header("動画表示先")]
    private RawImage m_videoImage;

    [SerializeField, Header("Cueが有効な間は動画をループ")]
    private bool m_loopVideo = true;

    [SerializeField, Min(0f), Header("動画の黒枠の幅")]
    private float m_borderWidth = 3f;

    private readonly List<GameplayCueAsset> m_activeCues = new();
    private UiTweenAnimator m_uiAnimator;
    private GameplayCueAsset m_currentCue;
    private VideoClip m_currentClip;
    private int m_transitionVersion;
    private bool m_isHiding;
    private Coroutine m_prepareCoroutine;
    private RenderTexture m_renderTexture;

    /// <summary>Cue変更イベントと動画準備完了イベントを購読します。</summary>
    private void OnEnable()
    {
        m_activeCues.Clear();
        m_currentCue = null;
        m_currentClip = null;
        m_isHiding = false;
        ++m_transitionVersion;

        if (m_videoPlayer == null || m_videoUiObject == null || m_videoImage == null)
        {
            Debug.LogWarning("動画再生用の参照が設定されていません。", this);
            return;
        }

        if (transform.IsChildOf(m_videoUiObject.transform) ||
            m_videoPlayer.transform.IsChildOf(m_videoUiObject.transform))
        {
            Debug.LogError("ControllerとVideoPlayerは動画表示UIの外に配置してください。", this);
            return;
        }

        m_uiAnimator = m_videoUiObject.GetComponent<UiTweenAnimator>();
        if (m_uiAnimator == null)
        {
            m_uiAnimator = m_videoUiObject.AddComponent<UiTweenAnimator>();
        }

        m_uiAnimator.SetAnimationType(UiTweenAnimator.AnimationType.HORIZONTAL_EXPAND);
        Outline border = m_videoImage.GetComponent<Outline>();
        if (border == null)
        {
            border = m_videoImage.gameObject.AddComponent<Outline>();
        }

        border.effectColor = Color.black;
        border.effectDistance = new Vector2(m_borderWidth, m_borderWidth);
        border.useGraphicAlpha = true;
        m_uiAnimator.HideImmediately();
        StopVideo();
        m_videoPlayer.playOnAwake = false;
        m_renderTexture = RenderTexture.GetTemporary(
            VIDEO_TEXTURE_WIDTH,
            VIDEO_TEXTURE_HEIGHT,
            0,
            RenderTextureFormat.ARGB32);
        ClearRenderTexture();
        m_videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        m_videoPlayer.targetTexture = m_renderTexture;
        m_videoPlayer.prepareCompleted += OnVideoPrepared;
        m_videoPlayer.errorReceived += OnVideoError;

        if (m_cueChangedEvent == null)
        {
            Debug.LogWarning($"{nameof(GameplayCueChangedEvent)}が設定されていません。", this);
            return;
        }

        m_cueChangedEvent.RegisterListener(OnCueChanged);
    }

    /// <summary>イベント購読を解除し、動画を停止します。</summary>
    private void OnDisable()
    {
        if (m_cueChangedEvent != null)
        {
            m_cueChangedEvent.UnregisterListener(OnCueChanged);
        }

        if (m_videoPlayer != null)
        {
            m_videoPlayer.prepareCompleted -= OnVideoPrepared;
            m_videoPlayer.errorReceived -= OnVideoError;
        }

        m_activeCues.Clear();
        m_isHiding = false;
        ++m_transitionVersion;
        if (m_uiAnimator != null)
        {
            m_uiAnimator.HideImmediately();
        }
        StopVideo();

        if (m_videoPlayer != null)
        {
            m_videoPlayer.targetTexture = null;
        }

        if (m_renderTexture != null)
        {
            RenderTexture.ReleaseTemporary(m_renderTexture);
            m_renderTexture = null;
        }
    }

    /// <summary>Cueの開始・終了に応じて再生対象を更新します。</summary>
    /// <param name="eventData">Cueの状態変更情報。</param>
    private void OnCueChanged(GameplayCueChangedEventData eventData)
    {
        if (eventData.Cue == null || FindVideoClip(eventData.Cue) == null)
        {
            return;
        }

        switch (eventData.EventType)
        {
            case GameplayCueEventType.STARTED:
                m_activeCues.Remove(eventData.Cue);
                m_activeCues.Add(eventData.Cue);
                RefreshVideo();
                break;

            case GameplayCueEventType.FINISHED:
                if (m_activeCues.Remove(eventData.Cue))
                {
                    RefreshVideo();
                }
                break;
        }
    }

    /// <summary>旧動画の退場完了後に、最後に開始された有効Cueへ切り替えます。</summary>
    private void RefreshVideo()
    {
        GameplayCueAsset nextCue = m_activeCues.Count == 0
            ? null
            : m_activeCues[m_activeCues.Count - 1];

        if (nextCue == m_currentCue && !m_isHiding)
        {
            return;
        }

        int transitionVersion = ++m_transitionVersion;
        if (m_videoUiObject.activeSelf)
        {
            m_isHiding = true;
            m_uiAnimator.PlayHide(() => CompleteHide(transitionVersion));
            return;
        }

        m_isHiding = false;
        StopVideo();
        StartLatestVideo(transitionVersion);
    }

    /// <summary>旧動画が完全に消えた後、最新のCueを再確認します。</summary>
    /// <param name="transitionVersion">切替要求の識別番号。</param>
    private void CompleteHide(int transitionVersion)
    {
        if (transitionVersion != m_transitionVersion)
        {
            return;
        }

        m_isHiding = false;
        StopVideo();
        StartLatestVideo(transitionVersion);
    }

    /// <summary>最新の有効Cueに対応する動画を準備します。</summary>
    /// <param name="transitionVersion">切替要求の識別番号。</param>
    private void StartLatestVideo(int transitionVersion)
    {
        if (transitionVersion != m_transitionVersion || m_activeCues.Count == 0)
        {
            return;
        }

        GameplayCueAsset nextCue = m_activeCues[m_activeCues.Count - 1];
        m_currentCue = nextCue;
        m_currentClip = FindVideoClip(nextCue);
        if (m_currentClip == null)
        {
            return;
        }

        // 旧動画の停止と新動画の準備を同じフレームに重ねません。
        m_prepareCoroutine = StartCoroutine(PrepareVideoAfterRelease(transitionVersion));
    }

    /// <summary>旧動画を解放した次のフレームで新しい動画を準備します。</summary>
    /// <param name="transitionVersion">切替要求の識別番号。</param>
    private IEnumerator PrepareVideoAfterRelease(int transitionVersion)
    {
        yield return null;
        m_prepareCoroutine = null;

        if (transitionVersion != m_transitionVersion ||
            m_currentCue == null ||
            m_currentClip == null)
        {
            yield break;
        }

        m_videoPlayer.source = VideoSource.VideoClip;
        m_videoPlayer.clip = m_currentClip;
        m_videoPlayer.isLooping = m_loopVideo;
        Debug.Log($"[TutorialVideoController] 動画を準備します: {m_currentClip.name}", this);
        m_videoPlayer.Prepare();
    }

    /// <summary>動画の準備が完了したらUIを表示して再生します。</summary>
    /// <param name="source">準備が完了したVideoPlayer。</param>
    private void OnVideoPrepared(VideoPlayer source)
    {
        if (m_isHiding ||
            m_currentCue == null ||
            m_activeCues.Count == 0 ||
            m_activeCues[m_activeCues.Count - 1] != m_currentCue ||
            source.clip != m_currentClip)
        {
            return;
        }

        m_videoImage.texture = m_renderTexture;
        Debug.Log($"[TutorialVideoController] 動画の準備が完了しました: {m_currentClip.name}", this);
        m_uiAnimator.PlayShow();
        source.Play();
    }

    /// <summary>動画の再生エラーを通知し、表示を消します。</summary>
    /// <param name="source">エラーが発生したVideoPlayer。</param>
    /// <param name="message">エラー内容。</param>
    private void OnVideoError(VideoPlayer source, string message)
    {
        Debug.LogWarning($"動画を再生できません。{message}", this);
        m_uiAnimator.HideImmediately();
        StopVideo();
    }

    /// <summary>再生を停止し、現在の動画参照をクリアします。</summary>
    private void StopVideo()
    {
        if (m_prepareCoroutine != null)
        {
            StopCoroutine(m_prepareCoroutine);
            m_prepareCoroutine = null;
        }

        // VideoPlayerを停止する前に、UIからネイティブ動画テクスチャへの参照を外します。
        if (m_videoImage != null)
        {
            m_videoImage.texture = null;
        }

        if (m_videoPlayer != null)
        {
            m_videoPlayer.Stop();
            m_videoPlayer.clip = null;
        }

        ClearRenderTexture();

        m_currentCue = null;
        m_currentClip = null;
    }

    /// <summary>次の動画が出る前に、共通の表示先を黒でクリアします。</summary>
    private void ClearRenderTexture()
    {
        if (m_renderTexture == null)
        {
            return;
        }

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = m_renderTexture;
        GL.Clear(true, true, Color.black);
        RenderTexture.active = previous;
    }

    /// <summary>指定Cueに対応する動画を取得します。</summary>
    /// <param name="cue">検索対象のCue。</param>
    /// <returns>対応する動画。未登録または未設定の場合はnull。</returns>
    private VideoClip FindVideoClip(GameplayCueAsset cue)
    {
        foreach (TutorialVideoData data in m_videoDataList)
        {
            if (data != null && data.Cue == cue)
            {
                return data.VideoClip;
            }
        }

        return null;
    }
}
