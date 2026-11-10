using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 全音声の再生およびAudioSourceプールを管理します。
/// </summary>
public class AudioManager : MonoBehaviour
{
    private const int DEFAULT_POOL_COUNT = 10;
    private const float MIN_VOLUME_THRESHOLD = 0.001f;
    private const float MIN_VOLUME_DB = -80.0f;
    private const float DECIBEL_MULTIPLIER = 20.0f;

    /// <summary>
    /// 共通の音声管理者を取得します。
    /// </summary>
    public static AudioManager Instance { get; private set; }

    [Header("Audio Resources")]
    [SerializeField] private SoundDatabase m_database;
    [SerializeField] private AudioSourceNode m_nodePrefab;

    [Header("Audio Mixer Groups")]
    [SerializeField] private AudioMixer m_audioMixer;
    [SerializeField] private AudioMixerGroup m_bgmGroup;
    [SerializeField] private AudioMixerGroup m_seInGameGroup;
    [SerializeField] private AudioMixerGroup m_seSystemGroup;
    [SerializeField] private AudioMixerGroup m_voiceGroup;

    // AudioSourceの再利用と現在再生中のノード
    private readonly List<AudioSourceNode> m_pool = new List<AudioSourceNode>();
    private readonly List<AudioSourceNode> m_activeNodes = new List<AudioSourceNode>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializePool();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// データベースの設定で2D音声を再生します。
    /// </summary>
    /// <param name="id">音声ID。</param>
    /// <returns>再生ハンドル。失敗した場合はnull。</returns>
    public AudioPlayHandle Play2D(AudioID id)
    {
        return Play2D(id, null);
    }

    /// <summary>
    /// ループ指定を上書きして2D音声を再生します。
    /// </summary>
    /// <param name="id">音声ID。</param>
    /// <param name="loopOverride">再生中のみ適用するループ設定。</param>
    /// <returns>再生ハンドル。失敗した場合はnull。</returns>
    public AudioPlayHandle Play2D(AudioID id, bool? loopOverride)
    {
        return PlayInternal(id, node => node.Play2D(), loopOverride);
    }

    /// <summary>
    /// 指定倍率で音量を初期化してから2D音声を再生します。
    /// </summary>
    /// <param name="id">音声ID。</param>
    /// <param name="loopOverride">再生中のみ適用するループ設定。</param>
    /// <param name="initialVolumeMultiplier">再生開始時の音量倍率（0〜1）。</param>
    /// <returns>再生ハンドル。失敗した場合はnull。</returns>
    public AudioPlayHandle Play2D(AudioID id, bool? loopOverride, float initialVolumeMultiplier)
    {
        return PlayInternal(id, node => node.Play2D(), loopOverride, initialVolumeMultiplier);
    }

    /// <summary>
    /// データベースの設定で指定地点から3D音声を再生します。
    /// </summary>
    /// <param name="id">音声ID。</param>
    /// <param name="position">再生位置。</param>
    /// <returns>再生ハンドル。失敗した場合はnull。</returns>
    public AudioPlayHandle Play3DAtPosition(AudioID id, Vector3 position)
    {
        return Play3DAtPosition(id, position, null);
    }

    /// <summary>
    /// ループ指定を上書きして指定地点から3D音声を再生します。
    /// </summary>
    /// <param name="id">音声ID。</param>
    /// <param name="position">再生位置。</param>
    /// <param name="loopOverride">再生中のみ適用するループ設定。</param>
    /// <returns>再生ハンドル。失敗した場合はnull。</returns>
    public AudioPlayHandle Play3DAtPosition(AudioID id, Vector3 position, bool? loopOverride)
    {
        return PlayInternal(id, node => node.Play3DAtPosition(position), loopOverride);
    }

    /// <summary>
    /// データベースの設定で指定Transformに追従して3D音声を再生します。
    /// </summary>
    /// <param name="id">音声ID。</param>
    /// <param name="target">追従先。</param>
    /// <returns>再生ハンドル。失敗した場合はnull。</returns>
    public AudioPlayHandle Play3DFollowing(AudioID id, Transform target)
    {
        return Play3DFollowing(id, target, null);
    }

    /// <summary>
    /// ループ指定を上書きして指定Transformに追従して3D音声を再生します。
    /// </summary>
    /// <param name="id">音声ID。</param>
    /// <param name="target">追従先。</param>
    /// <param name="loopOverride">再生中のみ適用するループ設定。</param>
    /// <returns>再生ハンドル。失敗した場合はnull。</returns>
    public AudioPlayHandle Play3DFollowing(AudioID id, Transform target, bool? loopOverride)
    {
        if (target == null)
        {
            Debug.LogWarning("AudioManager: 追従先Transformが設定されていません。");
            return null;
        }

        return PlayInternal(id, node => node.Play3DFollowing(target), loopOverride);
    }

    /// <summary>
    /// 指定したTransformに追従するすべての音声を停止します。
    /// </summary>
    /// <param name="target">追従先。</param>
    public void Stop3DFollowing(Transform target)
    {
        if (target == null) return;

        // 追従先変更後も正しく停止できるよう、現在のノード状態で判定します。
        for (int i = m_activeNodes.Count - 1; i >= 0; i--)
        {
            AudioSourceNode node = m_activeNodes[i];
            if (node != null && node.FollowTarget == target)
            {
                node.StopAndReturnToPool();
            }
        }
    }

    /// <summary>
    /// インゲームSEをすべて停止します。
    /// </summary>
    public void StopAllInGameSounds()
    {
        for (int i = m_activeNodes.Count - 1; i >= 0; i--)
        {
            AudioSourceNode node = m_activeNodes[i];
            if (node != null && node.Category == SoundCategory.SE_InGame)
            {
                node.StopAndReturnToPool();
            }
        }
    }

    /// <summary>
    /// カテゴリごとの音量を変更します。
    /// </summary>
    /// <param name="category">音声カテゴリ。</param>
    /// <param name="volume0to1">0〜1の音量。</param>
    public void SetVolume(SoundCategory category, float volume0to1)
    {
        if (m_audioMixer == null)
        {
            Debug.LogWarning("AudioManager: AudioMixerが設定されていません。");
            return;
        }

        float volume = Mathf.Clamp01(volume0to1);
        float db = volume > MIN_VOLUME_THRESHOLD
            ? DECIBEL_MULTIPLIER * Mathf.Log10(volume)
            : MIN_VOLUME_DB;
        m_audioMixer.SetFloat(category.ToString() + "Volume", db);
    }

    /// <summary>
    /// UnityEventの整数パラメータから2D音声を再生します。
    /// </summary>
    /// <param name="audioId">再生するAudioIDの数値。</param>
    public void Play2DFromEvent(int audioId)
    {
        // 数値とAudioIDの対応が有効か確認します。
        if (!Enum.IsDefined(typeof(AudioID), audioId))
        {
            Debug.LogWarning($"[AudioManager] 存在しないAudioIDです: {audioId}", this);
            return;
        }

        Play2D((AudioID)audioId);
    }

    /// <summary>
    /// UnityEventからBGMカテゴリの音量を変更します。
    /// </summary>
    /// <param name="volume">音量（0〜1）。</param>
    public void SetBGMVolume(float volume)
    {
        SetVolume(SoundCategory.BGM, volume);
    }

    /// <summary>
    /// UnityEventからインゲームSEカテゴリの音量を変更します。
    /// </summary>
    /// <param name="volume">音量（0〜1）。</param>
    public void SetInGameSEVolume(float volume)
    {
        SetVolume(SoundCategory.SE_InGame, volume);
    }

    /// <summary>
    /// UnityEventからボイスカテゴリの音量を変更します。
    /// </summary>
    /// <param name="volume">音量（0〜1）。</param>
    public void SetVoiceVolume(float volume)
    {
        SetVolume(SoundCategory.Voice, volume);
    }

    /// <summary>
    /// 音声IDを検索し、個別音源を取得して再生します。
    /// </summary>
    private AudioPlayHandle PlayInternal(
        AudioID id,
        Action<AudioSourceNode> playAction,
        bool? loopOverride,
        float initialVolumeMultiplier = 1.0f)
    {
        if (m_database == null || m_nodePrefab == null)
        {
            Debug.LogError("AudioManager: SoundDatabaseまたはNode Prefabが未設定です。");
            return null;
        }

        SoundDatabase.SoundData data = m_database.GetSoundData(id);
        if (data == null || data.m_clip == null)
        {
            Debug.LogWarning($"AudioManager: 音声ID {id} が見つからないか、AudioClipがありません。");
            return null;
        }

        AudioSourceNode node = GetAvailableNode();
        if (node == null) return null;

        node.Setup(data, GetMixerGroup(data.m_category), ReturnNodeToPool);
        if (loopOverride.HasValue) node.SetLoop(loopOverride.Value);
        node.SetVolumeMultiplier(initialVolumeMultiplier);

        // 再生終了を見逃さないように、Playより前にハンドルを登録します。
        AudioPlayHandle handle = new AudioPlayHandle(node);
        node.SetPlaybackFinishedCallback(handle.NotifyFinished);
        playAction(node);
        return handle;
    }

    /// <summary>
    /// プールを初期化します。
    /// </summary>
    private void InitializePool()
    {
        if (m_nodePrefab == null)
        {
            Debug.LogError("AudioManager: Node Prefabが設定されていません。");
            return;
        }

        for (int i = 0; i < DEFAULT_POOL_COUNT; i++) CreateNewNode();
    }

    /// <summary>
    /// 新しい音源ノードを生成します。
    /// </summary>
    private AudioSourceNode CreateNewNode()
    {
        AudioSourceNode node = Instantiate(m_nodePrefab, transform);
        node.gameObject.SetActive(false);
        m_pool.Add(node);
        return node;
    }

    /// <summary>
    /// プールから未使用の音源ノードを取得します。
    /// </summary>
    private AudioSourceNode GetAvailableNode()
    {
        foreach (AudioSourceNode node in m_pool)
        {
            if (node != null && !node.gameObject.activeInHierarchy)
            {
                node.gameObject.SetActive(true);
                m_activeNodes.Add(node);
                return node;
            }
        }

        AudioSourceNode createdNode = CreateNewNode();
        createdNode.gameObject.SetActive(true);
        m_activeNodes.Add(createdNode);
        return createdNode;
    }

    /// <summary>
    /// 再生を終えた音源ノードを使用中リストから除外します。
    /// </summary>
    private void ReturnNodeToPool(AudioSourceNode node)
    {
        m_activeNodes.Remove(node);
    }

    /// <summary>
    /// 音声カテゴリのMixerGroupを取得します。
    /// </summary>
    private AudioMixerGroup GetMixerGroup(SoundCategory category)
    {
        return category switch
        {
            SoundCategory.BGM => m_bgmGroup,
            SoundCategory.SE_InGame => m_seInGameGroup,
            SoundCategory.Voice => m_voiceGroup,
            _ => m_seSystemGroup,
        };
    }
}
