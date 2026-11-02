using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// プレイヤー・ボス共通のAnimation Eventエフェクト再生と停止を管理します。
/// </summary>
[DisallowMultipleComponent]
public sealed class AnimationEffectController : MonoBehaviour
{
    /// <summary>
    /// Cueとチャンネルの組み合わせを識別します。
    /// </summary>
    private readonly struct EffectPlaybackKey : IEquatable<EffectPlaybackKey>
    {
        private readonly EffectAnimationCue m_cue;
        private readonly int m_channel;

        /// <summary>識別キーを生成します。</summary>
        public EffectPlaybackKey(EffectAnimationCue cue, int channel)
        {
            m_cue = cue;
            m_channel = channel;
        }

        /// <summary>キー同士が一致するか確認します。</summary>
        public bool Equals(EffectPlaybackKey other)
        {
            return ReferenceEquals(m_cue, other.m_cue) && m_channel == other.m_channel;
        }

        /// <summary>キー同士が一致するか確認します。</summary>
        public override bool Equals(object obj)
        {
            return obj is EffectPlaybackKey other && Equals(other);
        }

        /// <summary>ハッシュコードを取得します。</summary>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((m_cue != null ? m_cue.GetInstanceID() : 0) * 397) ^ m_channel;
            }
        }

        /// <summary>チャンネルが一致するか確認します。</summary>
        /// <summary>登録CueのエフェクトID。</summary>
        public EffectId EffectId => m_cue != null ? m_cue.EffectId : EffectId.NONE;

        /// <summary>登録Cueがアニメーション終了停止対象か。</summary>
        public bool StopOnAnimationEnd => m_cue != null && m_cue.StopOnAnimationEnd;

        /// <summary>登録Cueの停止方式。</summary>
        public EffectStopMode StopMode => m_cue != null ? m_cue.StopMode : EffectStopMode.GRACEFUL;

        public bool HasChannel(int channel)
        {
            return m_channel == channel;
        }
    }

    [SerializeField, Header("エフェクト発生元（通常は同じRoot上）")]
    private EffectEmitter m_effectEmitter;

    // 同じCue・チャンネルで複数発生したエフェクトをまとめて保持します。
    private readonly Dictionary<EffectPlaybackKey, List<EffectHandle>> m_activeHandles =
        new Dictionary<EffectPlaybackKey, List<EffectHandle>>();

    /// <summary>
    /// 発生元の参照を取得します。
    /// </summary>
    private void Awake()
    {
        if (m_effectEmitter == null)
        {
            m_effectEmitter = GetComponent<EffectEmitter>();
        }
    }

    /// <summary>
    /// 同一IDの再生が現在も有効か調べます。
    /// </summary>
    private bool HasActiveEffectId(EffectId id)
    {
        foreach (KeyValuePair<EffectPlaybackKey, List<EffectHandle>> pair in m_activeHandles)
        {
            if (pair.Key.EffectId != id)
            {
                continue;
            }

            foreach (EffectHandle handle in pair.Value)
            {
                if (handle.IsValid)
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// 指定Cueのエフェクトを再生し、停止用ハンドルを保持します。
    /// </summary>
    /// <param name="cue">再生するエフェクト設定。</param>
    /// <param name="channel">同じCueの再生グループを区別する番号。</param>
    /// <returns>再生できたエフェクトのハンドル。失敗時は無効です。</returns>
    public EffectHandle Play(EffectAnimationCue cue, int channel = 0)
    {
        if (cue == null || cue.EffectId == EffectId.NONE || m_effectEmitter == null)
        {
            Debug.LogWarning("[AnimationEffectController] Cue、ID、EffectEmitterを確認してください。", this);
            return default;
        }

        // 同一IDの重複禁止はCueやチャンネルが異なる場合にも適用します。
        if (cue.PreventDuplicatePlayback && HasActiveEffectId(cue.EffectId))
        {
            return default;
        }

        EffectHandle handle;
        switch (cue.SpawnMode)
        {
            case EffectAnimationSpawnMode.DEFAULT:
                // 追従型やローカル型も含め、既存のID管理を利用します。
                handle = m_effectEmitter.Play(cue.EffectId);
                break;

            case EffectAnimationSpawnMode.WORLD_AT_ANCHOR:
                handle = m_effectEmitter.PlayAtAnchor(cue.EffectId, cue.AnchorId);
                break;

            case EffectAnimationSpawnMode.WORLD_AT_EMITTER:
                Transform origin = m_effectEmitter.transform;
                handle = m_effectEmitter.PlayAt(cue.EffectId, origin.position, origin.rotation);
                break;

            default:
                Debug.LogWarning($"[AnimationEffectController] 不明な発生方式です: {cue.SpawnMode}", this);
                return default;
        }

        if (!handle.IsValid)
        {
            return default;
        }

        // 同じ設定で重複再生された場合も、停止イベントで一括停止できるように保持します。
        EffectPlaybackKey key = new EffectPlaybackKey(cue, channel);
        if (!m_activeHandles.TryGetValue(key, out List<EffectHandle> handles))
        {
            handles = new List<EffectHandle>();
            m_activeHandles.Add(key, handles);
        }
        RemoveCompletedHandles(handles);
        handles.Add(handle);
        return handle;
    }

    /// <summary>
    /// 同じCue・チャンネルで発生したエフェクトをすべて停止します。
    /// </summary>
    /// <param name="cue">再生時と同じCue。</param>
    /// <param name="channel">再生時と同じチャンネル。</param>
    /// <returns>true：1件以上の停止要求が成功。false：対象なし。</returns>
    public bool Stop(EffectAnimationCue cue, int channel = 0)
    {
        if (cue == null)
        {
            return false;
        }

        EffectPlaybackKey key = new EffectPlaybackKey(cue, channel);
        if (!m_activeHandles.TryGetValue(key, out List<EffectHandle> handles))
        {
            return false;
        }

        m_activeHandles.Remove(key);
        return StopHandles(handles, cue.StopMode);
    }

    /// <summary>
    /// State中断時などに、指定チャンネルの全エフェクトを停止します。
    /// </summary>
    /// <param name="channel">停止するチャンネル。</param>
    /// <param name="stopMode">停止方法。</param>
    public void StopChannel(int channel, EffectStopMode stopMode = EffectStopMode.GRACEFUL)
    {
        // Dictionaryを列挙しながら削除しないよう、削除対象のキーだけ先に集めます。
        List<EffectPlaybackKey> keys = new List<EffectPlaybackKey>();
        foreach (KeyValuePair<EffectPlaybackKey, List<EffectHandle>> pair in m_activeHandles)
        {
            if (pair.Key.HasChannel(channel))
            {
                StopHandles(pair.Value, stopMode);
                keys.Add(pair.Key);
            }
        }

        foreach (EffectPlaybackKey key in keys)
        {
            m_activeHandles.Remove(key);
        }
    }

    /// <summary>
    /// Animator Stateの終了・中断時に、終了停止が指定されたCueのみ停止します。
    /// </summary>
    /// <param name="channel">Animation Eventに登録したチャンネル。</param>
    public void StopOnAnimationEnd(int channel)
    {
        List<EffectPlaybackKey> keys = new List<EffectPlaybackKey>();
        foreach (KeyValuePair<EffectPlaybackKey, List<EffectHandle>> pair in m_activeHandles)
        {
            if (!pair.Key.HasChannel(channel) || !pair.Key.StopOnAnimationEnd)
            {
                continue;
            }

            // Cueに設定された停止方式を使用します。
            StopHandles(pair.Value, pair.Key.StopMode);
            keys.Add(pair.Key);
        }

        foreach (EffectPlaybackKey key in keys)
        {
            m_activeHandles.Remove(key);
        }
    }

    /// <summary>
    /// このキャラクターのAnimation Event由来のエフェクトをすべて停止します。
    /// </summary>
    /// <param name="stopMode">停止方法。</param>
    public void StopAll(EffectStopMode stopMode = EffectStopMode.IMMEDIATE)
    {
        foreach (List<EffectHandle> handles in m_activeHandles.Values)
        {
            StopHandles(handles, stopMode);
        }
        m_activeHandles.Clear();
    }

    /// <summary>
    /// コンポーネントが無効になったときに再生済みエフェクトを停止します。
    /// </summary>
    private void OnDisable()
    {
        StopAll(EffectStopMode.IMMEDIATE);
    }

    /// <summary>
    /// 一括再生のハンドルを停止します。
    /// </summary>
    private static bool StopHandles(List<EffectHandle> handles, EffectStopMode stopMode)
    {
        bool hasStopped = false;
        foreach (EffectHandle handle in handles)
        {
            if (handle.Stop(stopMode))
            {
                hasStopped = true;
            }
        }
        return hasStopped;
    }

    /// <summary>
    /// 自然終了したハンドルを一覧から取り除きます。
    /// </summary>
    private static void RemoveCompletedHandles(List<EffectHandle> handles)
    {
        for (int i = handles.Count - 1; i >= 0; i--)
        {
            if (!handles[i].IsValid)
            {
                handles.RemoveAt(i);
            }
        }
    }
}
