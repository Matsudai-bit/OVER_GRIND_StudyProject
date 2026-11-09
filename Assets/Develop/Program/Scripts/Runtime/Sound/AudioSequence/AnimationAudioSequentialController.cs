using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Animation Eventで開始した連続音声を、Assetとチャンネルごとに管理します。
/// </summary>
[DisallowMultipleComponent]
public sealed class AnimationAudioSequentialController : MonoBehaviour
{
    /// <summary>
    /// 再生設定とチャンネルの組を識別します。
    /// </summary>
    private readonly struct PlaybackKey : IEquatable<PlaybackKey>
    {
        private readonly AudioSequentialAsset m_asset;
        private readonly int m_channel;

        public PlaybackKey(AudioSequentialAsset asset, int channel)
        {
            m_asset = asset;
            m_channel = channel;
        }

        public AudioSequentialAsset Asset => m_asset;
        public bool HasChannel(int channel) => m_channel == channel;

        public bool Equals(PlaybackKey other)
        {
            return ReferenceEquals(m_asset, other.m_asset) && m_channel == other.m_channel;
        }

        public override bool Equals(object obj)
        {
            return obj is PlaybackKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((m_asset != null ? m_asset.GetInstanceID() : 0) * 397) ^ m_channel;
            }
        }
    }

    [SerializeField, Header("音声セット再生コンポーネント")]
    private SequentialAudioPlayer m_audioPlayer;

    [SerializeField, Header("アニメーション音声の発生位置（未指定時はPlayer既定値）")]
    private Transform m_emissionPoint;

    // 同一Assetとチャンネルで発生した複数の再生を管理します。
    private readonly Dictionary<PlaybackKey, List<AudioSequentialHandle>> m_activeHandles =
        new Dictionary<PlaybackKey, List<AudioSequentialHandle>>();

    private void Awake()
    {
        if (m_audioPlayer == null)
        {
            m_audioPlayer = GetComponent<SequentialAudioPlayer>();
        }
    }

    /// <summary>
    /// 音声セットを再生し、停止イベント用にハンドルを保持します。
    /// </summary>
    /// <param name="asset">再生する音声セット。</param>
    /// <param name="channel">再生グループを識別する番号。</param>
    /// <returns>有効な再生ハンドル。失敗した場合はnull。</returns>
    public AudioSequentialHandle Play(AudioSequentialAsset asset, int channel = 0)
    {
        if (asset == null || m_audioPlayer == null)
        {
            Debug.LogWarning("[AnimationAudioSequentialController] AssetまたはSequentialAudioPlayerがありません。", this);
            return null;
        }

        if (asset.PreventDuplicatePlayback && HasActiveAsset(asset))
        {
            return null;
        }

        AudioSequentialHandle handle = m_audioPlayer.Play(asset, m_emissionPoint);
        if (handle == null)
        {
            return null;
        }

        PlaybackKey key = new PlaybackKey(asset, channel);
        if (!m_activeHandles.TryGetValue(key, out List<AudioSequentialHandle> handles))
        {
            handles = new List<AudioSequentialHandle>();
            m_activeHandles.Add(key, handles);
        }

        handle.Finished += OnPlaybackFinished;
        handles.Add(handle);
        return handle;
    }

    /// <summary>
    /// 再生時と同じAssetとチャンネルの音声セットを終了音へ遷移させます。
    /// </summary>
    /// <returns>true：停止対象がありました。false：対象なし。</returns>
    public bool Stop(AudioSequentialAsset asset, int channel = 0)
    {
        if (asset == null)
        {
            return false;
        }

        PlaybackKey key = new PlaybackKey(asset, channel);
        if (!m_activeHandles.TryGetValue(key, out List<AudioSequentialHandle> handles))
        {
            return false;
        }

        m_activeHandles.Remove(key);
        return StopHandles(handles, false);
    }

    /// <summary>
    /// 指定チャンネルの自動停止対象を終了音へ遷移させます。
    /// </summary>
    public void StopOnAnimationEnd(int channel)
    {
        List<PlaybackKey> keys = new List<PlaybackKey>();
        foreach (KeyValuePair<PlaybackKey, List<AudioSequentialHandle>> pair in m_activeHandles)
        {
            if (pair.Key.HasChannel(channel) &&
                pair.Key.Asset != null &&
                pair.Key.Asset.StopOnAnimationEnd)
            {
                keys.Add(pair.Key);
            }
        }

        StopKeys(keys, false);
    }

    /// <summary>
    /// 指定チャンネルの音声セットを終了します。
    /// </summary>
    /// <param name="channel">停止するチャンネル。</param>
    /// <param name="immediate">true：終了音を再生せず停止。</param>
    public void StopChannel(int channel, bool immediate = false)
    {
        List<PlaybackKey> keys = new List<PlaybackKey>();
        foreach (KeyValuePair<PlaybackKey, List<AudioSequentialHandle>> pair in m_activeHandles)
        {
            if (pair.Key.HasChannel(channel))
            {
                keys.Add(pair.Key);
            }
        }

        StopKeys(keys, immediate);
    }

    /// <summary>
    /// 管理しているすべての音声セットを即座に停止します。
    /// </summary>
    public void StopAllImmediate()
    {
        List<PlaybackKey> keys = new List<PlaybackKey>(m_activeHandles.Keys);
        StopKeys(keys, true);
    }

    /// <summary>
    /// 同じAssetの音声セットがすでに動作しているか調べます。
    /// </summary>
    private bool HasActiveAsset(AudioSequentialAsset asset)
    {
        foreach (KeyValuePair<PlaybackKey, List<AudioSequentialHandle>> pair in m_activeHandles)
        {
            if (!ReferenceEquals(pair.Key.Asset, asset))
            {
                continue;
            }

            foreach (AudioSequentialHandle handle in pair.Value)
            {
                if (handle != null && handle.IsActive)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 列挙中に辞書を変更しないよう、対象を取り出してから停止します。
    /// </summary>
    private void StopKeys(List<PlaybackKey> keys, bool immediate)
    {
        foreach (PlaybackKey key in keys)
        {
            if (!m_activeHandles.TryGetValue(key, out List<AudioSequentialHandle> handles))
            {
                continue;
            }

            m_activeHandles.Remove(key);
            StopHandles(handles, immediate);
        }
    }

    /// <summary>
    /// 管理対象から解除して音声を停止します。
    /// </summary>
    /// <returns>true：停止対象がありました。false：対象なし。</returns>
    private bool StopHandles(List<AudioSequentialHandle> handles, bool immediate)
    {
        bool hasStopped = false;
        foreach (AudioSequentialHandle handle in handles)
        {
            if (handle == null)
            {
                continue;
            }

            handle.Finished -= OnPlaybackFinished;
            if (!handle.IsActive)
            {
                continue;
            }

            hasStopped = true;
            if (immediate)
            {
                handle.StopImmediate();
            }
            else
            {
                handle.Stop();
            }
        }

        return hasStopped;
    }

    /// <summary>
    /// 自然終了したハンドルを管理対象から取り除きます。
    /// </summary>
    private void OnPlaybackFinished(AudioSequentialHandle handle)
    {
        handle.Finished -= OnPlaybackFinished;

        List<PlaybackKey> emptyKeys = new List<PlaybackKey>();
        foreach (KeyValuePair<PlaybackKey, List<AudioSequentialHandle>> pair in m_activeHandles)
        {
            pair.Value.Remove(handle);
            if (pair.Value.Count == 0)
            {
                emptyKeys.Add(pair.Key);
            }
        }

        foreach (PlaybackKey key in emptyKeys)
        {
            m_activeHandles.Remove(key);
        }
    }

    private void OnDisable()
    {
        StopAllImmediate();
    }
}
