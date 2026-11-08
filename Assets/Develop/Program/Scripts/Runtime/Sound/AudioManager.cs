using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 音声の再生およびオーディオプールを管理するシングルトンクラスです。
/// </summary>
public class AudioManager : MonoBehaviour
{
    private const int DEFAULT_POOL_COUNT = 10;
    private const float MIN_VOLUME_THRESHOLD = 0.001f;
    private const float MIN_VOLUME_DB = -80.0f;
    private const float DECIBEL_MULTIPLIER = 20.0f;

    public static AudioManager Instance { get; private set; }

    [SerializeField] private SoundDatabase m_database;
    [SerializeField] private AudioSourceNode m_nodePrefab;

    [Header("Audio Mixer Groups")]
    [SerializeField] private AudioMixer m_audioMixer;
    [SerializeField] private AudioMixerGroup m_bgmGroup;
    [SerializeField] private AudioMixerGroup m_seInGameGroup;
    [SerializeField] private AudioMixerGroup m_seSystemGroup;
    [SerializeField] private AudioMixerGroup m_voiceGroup;

    private List<AudioSourceNode> m_pool = new List<AudioSourceNode>();
    private List<AudioSourceNode> m_activeNodes = new List<AudioSourceNode>();

    /// <summary>
    /// Transform ごとに追従再生中のハンドルを管理するディクショナリです。
    /// </summary>
    private Dictionary<Transform, List<AudioPlayHandle>> m_followingHandles = new Dictionary<Transform, List<AudioPlayHandle>>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePool();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializePool()
    {
        if (m_nodePrefab == null)
        {
            Debug.LogError("Node Prefab が設定されていません。");
            return;
        }

        for (int i = 0; i < DEFAULT_POOL_COUNT; i++)
        {
            CreateNewNode();
        }
    }

    private AudioSourceNode CreateNewNode()
    {
        var node = Instantiate(m_nodePrefab, transform);
        node.gameObject.SetActive(false);
        m_pool.Add(node);
        return node;
    }

    private AudioSourceNode GetAvailableNode()
    {
        foreach (var node in m_pool)
        {
            if (node != null && !node.gameObject.activeInHierarchy)
            {
                node.gameObject.SetActive(true);
                m_activeNodes.Add(node);
                return node;
            }
        }

        var newNode = CreateNewNode();
        newNode.gameObject.SetActive(true);
        m_activeNodes.Add(newNode);
        return newNode;
    }

    private void ReturnNodeToPool(AudioSourceNode node)
    {
        if (m_activeNodes.Contains(node))
        {
            m_activeNodes.Remove(node);
        }
    }

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

    public AudioPlayHandle Play2D(AudioID id)
    {
        return PlayInternal(id, node => node.Play2D());
    }

    public AudioPlayHandle Play3DAtPosition(AudioID id, Vector3 pos)
    {
        return PlayInternal(id, node => node.Play3DAtPosition(pos));
    }

    /// <summary>
    /// 指定された Transform を追従しながら 3D 音声を再生し、内部でハンドルを追尾・管理します。
    /// </summary>
    public AudioPlayHandle Play3DFollowing(AudioID id, Transform target)
    {
        var handle = PlayInternal(id, node => node.Play3DFollowing(target));
        if (handle != null && target != null)
        {
            if (!m_followingHandles.TryGetValue(target, out var handles))
            {
                handles = new List<AudioPlayHandle>();
                m_followingHandles[target] = handles;
            }
            handles.Add(handle);
        }
        return handle;
    }

    /// <summary>
    /// 指定された Transform に追従して再生されているすべての音声を停止します。
    /// </summary>
    /// <param name="target">追従対象の Transform</param>
    public void Stop3DFollowing(Transform target)
    {
        if (target == null) return;

        if (m_followingHandles.TryGetValue(target, out var handles))
        {
            for (int i = 0; i < handles.Count; i++)
            {
                if (handles[i] != null && handles[i].IsPlaying)
                {
                    handles[i].Stop();
                }
            }
            m_followingHandles.Remove(target);
        }
    }

    private AudioPlayHandle PlayInternal(AudioID id, Action<AudioSourceNode> playAction)
    {
        if (m_database == null)
        {
            Debug.LogError("SoundDatabase が設定されていません。");
            return null;
        }

        var data = m_database.GetSoundData(id);
        if (data == null)
        {
            Debug.LogWarning($"SoundID:{id} not found.");
            return null;
        }

        var node = GetAvailableNode();
        node.Setup(data, GetMixerGroup(data.m_category), ReturnNodeToPool);
        playAction(node);
        return new AudioPlayHandle(node);
    }

    public void StopAllInGameSounds()
    {
        for (int i = m_activeNodes.Count - 1; i >= 0; i--)
        {
            if (m_activeNodes[i] != null && m_activeNodes[i].Category == SoundCategory.SE_InGame)
            {
                m_activeNodes[i].StopAndReturnToPool();
            }
        }
        m_followingHandles.Clear();
    }

    public void SetVolume(SoundCategory category, float volume0to1)
    {
        if (m_audioMixer == null)
        {
            Debug.LogWarning("AudioMixer が設定されていません。");
            return;
        }

        float db = volume0to1 > MIN_VOLUME_THRESHOLD ? DECIBEL_MULTIPLIER * Mathf.Log10(volume0to1) : MIN_VOLUME_DB;
        m_audioMixer.SetFloat(category.ToString() + "Volume", db);
    }
}