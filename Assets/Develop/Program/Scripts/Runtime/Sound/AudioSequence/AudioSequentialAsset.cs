using UnityEngine;

/// <summary>
/// 開始音・ループ音・終了音を組み合わせた再生設定です。
/// 再生状態は保持せず、複数のキャラクターで共有できます。
/// </summary>
[CreateAssetMenu(fileName = "AudioSequentialAsset", menuName = "Game/Audio/Sequential Audio Asset")]
public sealed class AudioSequentialAsset : ScriptableObject
{
    [Header("開始音（任意）")]
    [SerializeField] private bool m_hasStartAudio = true;
    [SerializeField] private AudioID m_startAudioID;

    [Header("ループ音（必須）")]
    [SerializeField] private AudioID m_loopAudioID;

    [Header("終了音（任意）")]
    [SerializeField] private bool m_hasEndAudio = true;
    [SerializeField] private AudioID m_endAudioID;

    [Header("再生位置")]
    [SerializeField]
    private AudioSequentialSpatialMode m_spatialMode =
        AudioSequentialSpatialMode.THREE_DIMENSIONAL_FOLLOWING;

    [Header("無音対策 / 予約再生")]
    [SerializeField, Min(0.02f)] private float m_scheduleLeadSeconds = 0.05f;
    [SerializeField, Min(0.0f)] private float m_endCrossfadeSeconds = 0.06f;

    [Header("アニメーション連携")]
    [SerializeField] private bool m_stopOnAnimationEnd = true;
    [SerializeField] private bool m_preventDuplicatePlayback = true;

    /// <summary>開始音を使用するかを取得します。</summary>
    public bool HasStartAudio => m_hasStartAudio;

    /// <summary>開始音IDを取得します。</summary>
    public AudioID StartAudioID => m_startAudioID;

    /// <summary>ループ音IDを取得します。</summary>
    public AudioID LoopAudioID => m_loopAudioID;

    /// <summary>終了音を使用するかを取得します。</summary>
    public bool HasEndAudio => m_hasEndAudio;

    /// <summary>終了音IDを取得します。</summary>
    public AudioID EndAudioID => m_endAudioID;

    /// <summary>音源位置の方式を取得します。</summary>
    public AudioSequentialSpatialMode SpatialMode => m_spatialMode;

    /// <summary>DSP予約に必要な先読み時間（秒）です。</summary>
    public float ScheduleLeadSeconds => Mathf.Max(0.02f, m_scheduleLeadSeconds);

    /// <summary>終了音へのクロスフェード時間（秒）です。</summary>
    public float EndCrossfadeSeconds => Mathf.Max(0.0f, m_endCrossfadeSeconds);

    /// <summary>アニメーション終了時に停止するかを取得します。</summary>
    public bool StopOnAnimationEnd => m_stopOnAnimationEnd;

    /// <summary>同じ設定の多重再生を防ぐかを取得します。</summary>
    public bool PreventDuplicatePlayback => m_preventDuplicatePlayback;
}
