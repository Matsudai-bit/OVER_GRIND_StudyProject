using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// Timeline上の時刻に、指定したAudioCueの再生・停止を通知します。
/// </summary>
[System.Serializable]
public sealed class AudioCueMarker : Marker, INotification, INotificationOptionProvider
{
    // Timelineから実行する音声演出。
    [SerializeField, Header("音声演出ID")]
    private AudioCueId m_cueId = AudioCueId.NONE;

    // 再生・通常停止・即時停止を選択します。
    [SerializeField, Header("実行する操作")]
    private AudioCueTimelineAction m_action = AudioCueTimelineAction.PLAY;

    /// <summary>実行する音声演出ID。</summary>
    public AudioCueId CueId => m_cueId;

    /// <summary>通知時に行う操作。</summary>
    public AudioCueTimelineAction Action => m_action;

    // この通知の種類を識別するキーです。CueIDは上のプロパティで渡します。
    PropertyName INotification.id => new PropertyName(nameof(AudioCueMarker));

    // プレビュー中は発火せず、Timelineのループ時は再び発火します。
    // 途中から再生した場合、過去のMarkerを遡って実行しません。
    NotificationFlags INotificationOptionProvider.flags => (NotificationFlags)0;
}
