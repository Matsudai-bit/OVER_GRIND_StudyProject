using UnityEngine;
using UnityEngine.Timeline;

/// <summary>
/// AudioCueMarkerを配置し、通知先のReceiverをバインドするTimelineトラックです。
/// </summary>
[TrackColor(0.24f, 0.68f, 0.83f)]
[TrackBindingType(typeof(AudioCueTimelineReceiver))]
public sealed class AudioCueTimelineTrack : TrackAsset
{
    // Markerの通知はTimeline標準のPlayableGraph経由で配信されます。
    // 独自のPlayableBehaviourやUpdateは不要です。
}
