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

    /// <summary>
    /// AudioManager の静的インスタンスを取得します。
    /// </summary>
    public static AudioManager Instance { get; private set; }

    /// <summary>
    /// サウンドデータベースのアセット参照です。
    /// </summary>
    [SerializeField] private SoundDatabase m_database;

    /// <summary>
    /// 音源再生ノードのプレハブ参照です。
    /// </summary>
    [SerializeField] private AudioSourceNode m_nodePrefab;

    /// <summary>
    /// 全体制御用の Audio Mixer です。
    /// </summary>
    [Header("Audio Mixer Groups")]
    [SerializeField] private AudioMixer m_audioMixer;

    /// <summary>
    /// BGM 用 MixerGroup です。
    /// </summary>
    [SerializeField] private AudioMixerGroup m_bgmGroup;

    /// <summary>
    /// インゲーム SE 用 MixerGroup です。
    /// </summary>
    [SerializeField] private AudioMixerGroup m_seInGameGroup;

    /// <summary>
    /// システム SE 用 MixerGroup です。
    /// </summary>
    [SerializeField] private AudioMixerGroup m_seSystemGroup;

    /// <summary>
    /// ボイス用 MixerGroup です。
    /// </summary>
    [SerializeField] private AudioMixerGroup m_voiceGroup;

    /// <summary>
    /// プールされているすべての音源ノードのリストです。
    /// </summary>
    private List<AudioSourceNode> m_pool = new List<AudioSourceNode>();

    /// <summary>
    /// 現在再生中の音源ノードのリストです。
    /// </summary>
    private List<AudioSourceNode> m_activeNodes = new List<AudioSourceNode>();

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

    /// <summary>
    /// 初期プールを生成します。
    /// </summary>
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

    /// <summary>
    /// 新しい音源ノードを生成してプールに追加します。
    /// </summary>
    /// <returns>生成された音源ノード。</returns>
    private AudioSourceNode CreateNewNode()
    {
        var node = Instantiate(m_nodePrefab, transform);
        node.gameObject.SetActive(false);
        m_pool.Add(node);
        return node;
    }

    /// <summary>
    /// 使用可能な音源ノードをプールから取得します。
    /// </summary>
    /// <returns>使用可能な音源ノード。</returns>
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

    /// <summary>
    /// ノードを非アクティブプールへ戻します。
    /// </summary>
    /// <param name="node">返却する音源ノード。</param>
    private void ReturnNodeToPool(AudioSourceNode node)
    {
        if (m_activeNodes.Contains(node))
        {
            m_activeNodes.Remove(node);
        }
    }

    /// <summary>
    /// サウンドカテゴリに対応する AudioMixerGroup を取得します。
    /// </summary>
    /// <param name="category">サウンドカテゴリ。</param>
    /// <returns>対応する AudioMixerGroup。</returns>
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

    /// <summary>
    /// 2D音声を再生します。
    /// </summary>
    /// <param name="id">再生するサウンドID。</param>
    /// <returns>再生コントロール用ハンドル。</returns>
    public AudioPlayHandle Play2D(AudioID id)
    {
        return PlayInternal(id, node => node.Play2D());
    }

    /// <summary>
    /// 指定された 3D 座標で音声を再生します。
    /// </summary>
    /// <param name="id">再生するサウンドID。</param>
    /// <param name="pos">再生位置のワールド座標。</param>
    /// <returns>再生コントロール用ハンドル。</returns>
    public AudioPlayHandle Play3DAtPosition(AudioID id, Vector3 pos)
    {
        return PlayInternal(id, node => node.Play3DAtPosition(pos));
    }

    /// <summary>
    /// 指定された Transform を追従しながら 3D 音声を再生します。
    /// </summary>
    /// <param name="id">再生するサウンドID。</param>
    /// <param name="target">追従対象の Transform。</param>
    /// <returns>再生コントロール用ハンドル。</returns>
    public AudioPlayHandle Play3DFollowing(AudioID id, Transform target)
    {
        return PlayInternal(id, node => node.Play3DFollowing(target));
    }

    /// <summary>
    /// 内部的な音声再生の共通処理を行います。
    /// </summary>
    /// <param name="id">再生するサウンドID。</param>
    /// <param name="playAction">実行する再生アクション。</param>
    /// <returns>再生コントロール用ハンドル。</returns>
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

    /// <summary>
    /// インゲーム中に再生されているすべての SE を停止してプールに戻します。
    /// </summary>
    public void StopAllInGameSounds()
    {
        for (int i = m_activeNodes.Count - 1; i >= 0; i--)
        {
            if (m_activeNodes[i] != null && m_activeNodes[i].Category == SoundCategory.SE_InGame)
            {
                m_activeNodes[i].StopAndReturnToPool();
            }
        }
    }

    /// <summary>
    /// 指定したカテゴリの音量を変更します。
    /// </summary>
    /// <param name="category">対象のサウンドカテゴリ。</param>
    /// <param name="volume0to1">0～1の音量値。</param>
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