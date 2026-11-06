using UnityEngine;

/// <summary>
/// Animator上のAnimation Eventを受け取り、Timeline演出へ転送します。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public sealed class AnimationTimelineEventReceiver : MonoBehaviour
{
    // 親階層の汎用Timeline管理者
    [SerializeField, Header("Timeline制御")]
    private AnimationTimelineController m_timelineController;

    /// <summary>
    /// Animatorと同じオブジェクトからAnimation Eventを受け取ります。
    /// </summary>
    /// <param name="animationEvent">Cueを含むイベント情報。</param>
    public void OnAnimationTimelinePlay(AnimationEvent animationEvent)
    {
        if (!TryGetCue(animationEvent, out AnimationTimelineCue cue) ||
            !TryGetController())
        {
            return;
        }

        m_timelineController.TryPlay(cue);
    }

    /// <summary>
    /// Animation Eventから指定Cueの演出を停止します。
    /// </summary>
    /// <param name="animationEvent">Cueを含むイベント情報。</param>
    public void OnAnimationTimelineStop(AnimationEvent animationEvent)
    {
        if (!TryGetCue(animationEvent, out AnimationTimelineCue cue) ||
            !TryGetController())
        {
            return;
        }

        m_timelineController.StopCue(cue);
    }

    /// <summary>
    /// Event内のScriptableObject参照を取得します。
    /// </summary>
    private bool TryGetCue(AnimationEvent animationEvent, out AnimationTimelineCue cue)
    {
        cue = animationEvent?.objectReferenceParameter as AnimationTimelineCue;

        if (cue != null)
        {
            return true;
        }

        Debug.LogWarning(
            "[AnimationTimelineEventReceiver] AnimationEventのObjectにAnimationTimelineCueを指定してください。",
            this);
        return false;
    }

    /// <summary>
    /// 既存の参照か親階層からTimeline管理者を取得します。
    /// </summary>
    private bool TryGetController()
    {
        if (m_timelineController == null)
        {
            m_timelineController = GetComponentInParent<AnimationTimelineController>(true);
        }

        if (m_timelineController != null)
        {
            return true;
        }

        Debug.LogWarning(
            "[AnimationTimelineEventReceiver] AnimationTimelineControllerが見つかりません。",
            this);
        return false;
    }

    /// <summary>
    /// Inspector追加時に管理者を自動取得します。
    /// </summary>
    private void Reset()
    {
        m_timelineController = GetComponentInParent<AnimationTimelineController>(true);
    }
}
