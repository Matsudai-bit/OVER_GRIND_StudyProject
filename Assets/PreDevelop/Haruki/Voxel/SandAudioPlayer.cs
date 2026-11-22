using UnityEngine;

/// <summary>
/// 砂の掘削サウンドを AudioSource プールを用いて再生するクラス。
/// </summary>
public class SandAudioPlayer
{
    // ---- 定数 ----
    private const int AUDIO_POOL_SIZE = 4;     // 同時再生用の AudioSource プール数
    private const float MIN_DISTANCE = 2f;     // 3Dサウンドの最小減衰距離(m)
    private const float MAX_DISTANCE = 30f;    // 3Dサウンドの最大減衰距離(m)

    // ---- メンバー変数 ----
    private AudioSource[] m_audioPool;         // 生成された AudioSource の配列
    private int m_audioIndex;                  // 次に使用するプールのインデックス
    private float m_nextSoundTime;             // 次に音を鳴らせる時刻

    /// <summary>
    /// AudioSource プールを生成して初期化します。
    /// </summary>
    /// <param name="parent">生成した AudioSource の親 Transform。</param>
    /// <param name="clips">使用する AudioClip 配列。</param>
    public void Initialize(Transform parent, AudioClip[] clips)
    {
        if (parent == null || clips == null || clips.Length == 0)
        {
            m_audioPool = null;
            return;
        }

        m_audioPool = new AudioSource[AUDIO_POOL_SIZE];
        for (int i = 0; i < AUDIO_POOL_SIZE; i++)
        {
            GameObject go = new GameObject("SandAudio" + i);
            go.transform.SetParent(parent, false);

            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.minDistance = MIN_DISTANCE;
            source.maxDistance = MAX_DISTANCE;
            m_audioPool[i] = source;
        }
    }

    /// <summary>
    /// 指定された位置でランダムな砂削り音を再生します。
    /// </summary>
    /// <param name="position">再生するワールド座標。</param>
    /// <param name="volume">削った砂の体積(m³)。</param>
    /// <param name="clips">再生候補のクリップ配列。</param>
    /// <param name="baseVolume">基準音量(0〜1)。</param>
    /// <param name="pitchRange">ピッチのランダム範囲。</param>
    /// <param name="minInterval">連続再生の最小間隔(秒)。</param>
    public void PlaySound(
        Vector3 position,
        float volume,
        AudioClip[] clips,
        float baseVolume,
        Vector2 pitchRange,
        float minInterval)
    {
        if (m_audioPool == null || clips == null || clips.Length == 0 || Time.time < m_nextSoundTime)
        {
            return;
        }

        m_nextSoundTime = Time.time + minInterval;

        AudioSource source = m_audioPool[m_audioIndex];
        m_audioIndex = (m_audioIndex + 1) % m_audioPool.Length;

        if (source == null)
        {
            return;
        }

        source.transform.position = position;
        source.clip = clips[Random.Range(0, clips.Length)];
        source.pitch = Random.Range(pitchRange.x, pitchRange.y);
        source.volume = baseVolume * Mathf.Clamp(0.3f + volume * 10f, 0.3f, 1f);
        source.Play();
    }
}