using UnityEditor.Timeline;
using UnityEngine.Timeline;

/// <summary>
/// Timeline上のAudioCueMarkerにCueIDと操作内容のツールチップを表示します。
/// </summary>
[CustomTimelineEditor(typeof(AudioCueMarker))]
public sealed class AudioCueMarkerEditor : MarkerEditor
{
    /// <summary>
    /// マーカーの表示情報を取得します。
    /// </summary>
    /// <param name="marker">表示対象のマーカー。</param>
    /// <returns>ツールチップと警告情報。</returns>
    public override MarkerDrawOptions GetMarkerOptions(IMarker marker)
    {
        MarkerDrawOptions options = base.GetMarkerOptions(marker);
        if (marker is AudioCueMarker audioMarker)
        {
            options.tooltip = $"{audioMarker.Action} : {audioMarker.CueId}";
            if (audioMarker.CueId == AudioCueId.NONE)
            {
                options.errorText = "AudioCueIdがNONEです。";
            }
        }

        return options;
    }
}
