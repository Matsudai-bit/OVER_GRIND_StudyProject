using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 音声演出IDから音声設定と発生位置を解決し、再生を要求します。
/// 音声プールやMixerの管理はAudioManagerに委ねます。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SequentialAudioPlayer))]
public sealed class AudioEmitter : MonoBehaviour
{
    /// <summary>再生中の連続音声と、追従先の識別情報です。</summary>
    private sealed class SequenceRecord
    {
        public AudioCueId CueId;
        public AudioAnchorId AnchorId;
        public bool FollowsAnchor;
        public AudioSequentialHandle Handle;
    }

    /// <summary>再生中の単発音声と、追従先の識別情報です。</summary>
    private sealed class OneShotRecord
    {
        public AudioCueId CueId;
        public AudioAnchorId AnchorId;
        public bool FollowsAnchor;
        public AudioPlayHandle Handle;
    }

    [SerializeField, Header("音声シーケンス再生コンポーネント")]
    private SequentialAudioPlayer m_sequentialPlayer;

    [SerializeField, Header("音声発生位置の対応表")]
    private AudioAnchorRegistry m_anchorRegistry;

    [SerializeField, Header("音声演出IDと音声Assetの対応表")]
    private List<AudioCueEntry> m_entries = new List<AudioCueEntry>();

    [SerializeField, Header("コンポーネント無効化時に単発SEも止めるか")]
    private bool m_stopOneShotsOnDisable = false;

    // 再生時の定義検索と、個別ハンドルの管理。
    private readonly Dictionary<AudioCueId, AudioCueEntry> m_entryMap =
        new Dictionary<AudioCueId, AudioCueEntry>();
    private readonly List<SequenceRecord> m_activeSequences = new List<SequenceRecord>();
    private readonly List<OneShotRecord> m_activeOneShots = new List<OneShotRecord>();
    private bool m_isInitialized;

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        if (m_anchorRegistry == null)
        {
            m_anchorRegistry = GetComponent<AudioAnchorRegistry>();
        }

        if (m_anchorRegistry != null)
        {
            m_anchorRegistry.BindingsChanged += OnAnchorBindingsChanged;
        }
    }

    /// <summary>
    /// 登録済みの音声演出IDが存在するか調べます。
    /// </summary>
    /// <param name="cueId">音声演出ID。</param>
    /// <returns>true：登録あり。false：登録なし。</returns>
    public bool Contains(AudioCueId cueId)
    {
        Initialize();
        return m_entryMap.ContainsKey(cueId);
    }

    /// <summary>
    /// 登録済みの種類に従って単発音声またはシーケンスを再生します。
    /// Animation EventやC#コードから使用できます。
    /// </summary>
    /// <param name="cueId">再生する音声演出ID。</param>
    /// <param name="sequenceHandle">連続音声の場合のハンドル。</param>
    /// <param name="oneShotHandle">単発音声の場合のハンドル。</param>
    /// <returns>true：再生成功。false：ID未登録や設定不正。</returns>
    public bool TryPlayCue(AudioCueId cueId, out AudioSequentialHandle sequenceHandle,
        out AudioPlayHandle oneShotHandle)
    {
        sequenceHandle = null;
        oneShotHandle = null;

        if (!TryGetEntry(cueId, out AudioCueEntry entry)) return false;

        if (entry.PlaybackType == AudioCuePlaybackType.ONE_SHOT)
        {
            oneShotHandle = PlayOneShot(cueId);
            return oneShotHandle != null;
        }

        sequenceHandle = PlaySequence(cueId);
        return sequenceHandle != null;
    }

    /// <summary>
    /// UnityEventやTimeline Signalから数値IDで音声演出を再生します。
    /// </summary>
    /// <param name="cueId">AudioCueIdに定義された固定数値。</param>
    public void PlayCue(int cueId)
    {
        if (TryConvertCueId(cueId, out AudioCueId id))
        {
            TryPlayCue(id, out _, out _);
        }
    }

    /// <summary>
    /// 登録済みの音声演出を、Assetに指定した位置方式で再生します。
    /// </summary>
    /// <param name="cueId">シーケンスの音声演出ID。</param>
    /// <returns>再生ハンドル。失敗時はnull。</returns>
    public AudioSequentialHandle PlaySequence(AudioCueId cueId)
    {
        if (!TryGetEntry(cueId, out AudioCueEntry entry) ||
            entry.PlaybackType != AudioCuePlaybackType.SEQUENTIAL || entry.SequentialAsset == null)
        {
            Debug.LogWarning($"[AudioEmitter] シーケンス設定がありません: {cueId}", this);
            return null;
        }

        // 多重再生防止は同じCueId単位で判定します。
        if (entry.SequentialAsset.PreventDuplicatePlayback && HasActiveSequence(cueId))
        {
            return null;
        }

        if (!TryGetPlayer(out SequentialAudioPlayer player)) return null;

        AudioSequentialHandle handle;
        bool followsAnchor = false;
        switch (entry.SequentialAsset.SpatialMode)
        {
            case AudioSequentialSpatialMode.TWO_DIMENSIONAL:
                handle = player.Play2D(entry.SequentialAsset);
                break;

            case AudioSequentialSpatialMode.THREE_DIMENSIONAL_AT_POSITION:
                if (!TryGetAnchor(entry.AnchorId, out Transform positionTarget)) return null;
                handle = player.PlayAt(entry.SequentialAsset, positionTarget.position);
                break;

            case AudioSequentialSpatialMode.THREE_DIMENSIONAL_FOLLOWING:
                if (!TryGetAnchor(entry.AnchorId, out Transform followTarget)) return null;
                handle = player.PlayFollowing(entry.SequentialAsset, followTarget);
                followsAnchor = true;
                break;

            default:
                Debug.LogWarning("[AudioEmitter] 未対応の空間再生方式です。", this);
                return null;
        }

        return RegisterSequence(cueId, entry.AnchorId, followsAnchor, handle);
    }

    /// <summary>
    /// 登録済みの単発音声を、Assetに指定した位置方式で再生します。
    /// </summary>
    /// <param name="cueId">単発音声の演出ID。</param>
    /// <returns>再生ハンドル。失敗時はnull。</returns>
    public AudioPlayHandle PlayOneShot(AudioCueId cueId)
    {
        if (!TryGetEntry(cueId, out AudioCueEntry entry) ||
            entry.PlaybackType != AudioCuePlaybackType.ONE_SHOT || entry.OneShotAsset == null)
        {
            Debug.LogWarning($"[AudioEmitter] 単発音声設定がありません: {cueId}", this);
            return null;
        }

        if (!TryGetPlayer(out SequentialAudioPlayer player)) return null;

        AudioPlayHandle handle;
        bool followsAnchor = false;
        switch (entry.OneShotAsset.SpatialMode)
        {
            case AudioSequentialSpatialMode.TWO_DIMENSIONAL:
                handle = player.PlayOneShot2D(entry.OneShotAsset);
                break;

            case AudioSequentialSpatialMode.THREE_DIMENSIONAL_AT_POSITION:
                if (!TryGetAnchor(entry.AnchorId, out Transform positionTarget)) return null;
                handle = player.PlayOneShotAt(entry.OneShotAsset, positionTarget.position);
                break;

            case AudioSequentialSpatialMode.THREE_DIMENSIONAL_FOLLOWING:
                if (!TryGetAnchor(entry.AnchorId, out Transform followTarget)) return null;
                handle = player.PlayOneShotFollowing(entry.OneShotAsset, followTarget);
                followsAnchor = true;
                break;

            default:
                Debug.LogWarning("[AudioEmitter] 未対応の空間再生方式です。", this);
                return null;
        }

        return RegisterOneShot(cueId, entry.AnchorId, followsAnchor, handle);
    }

    /// <summary>
    /// 連続音声を指定ワールド座標から再生します。
    /// </summary>
    /// <param name="asset">再生Asset。</param>
    /// <param name="position">固定するワールド座標。</param>
    /// <returns>再生ハンドル。失敗時はnull。</returns>
    public AudioSequentialHandle PlayAt(AudioSequentialAsset asset, Vector3 position)
    {
        return TryGetPlayer(out SequentialAudioPlayer player)
            ? RegisterSequence(AudioCueId.NONE, AudioAnchorId.ROOT, false, player.PlayAt(asset, position))
            : null;
    }

    /// <summary>
    /// 連続音声を指定Transformに追従させて再生します。
    /// </summary>
    /// <param name="asset">再生Asset。</param>
    /// <param name="target">追従するTransform。</param>
    /// <returns>再生ハンドル。失敗時はnull。</returns>
    public AudioSequentialHandle PlayFollowing(AudioSequentialAsset asset, Transform target)
    {
        return TryGetPlayer(out SequentialAudioPlayer player)
            ? RegisterSequence(AudioCueId.NONE, AudioAnchorId.ROOT, false, player.PlayFollowing(asset, target))
            : null;
    }

    /// <summary>
    /// 連続音声をアンカーのワールド座標で固定して再生します。
    /// </summary>
    /// <param name="asset">再生Asset。</param>
    /// <param name="anchorId">発生位置。</param>
    /// <returns>再生ハンドル。失敗時はnull。</returns>
    public AudioSequentialHandle PlayAtAnchor(AudioSequentialAsset asset, AudioAnchorId anchorId)
    {
        return TryGetAnchor(anchorId, out Transform target) ? PlayAt(asset, target.position) : null;
    }

    /// <summary>
    /// 連続音声を指定アンカーに追従させて再生します。
    /// </summary>
    /// <param name="asset">再生Asset。</param>
    /// <param name="anchorId">追従するアンカー。</param>
    /// <returns>再生ハンドル。失敗時はnull。</returns>
    public AudioSequentialHandle PlayFollowingAnchor(AudioSequentialAsset asset, AudioAnchorId anchorId)
    {
        if (!TryGetAnchor(anchorId, out Transform target) || !TryGetPlayer(out SequentialAudioPlayer player))
        {
            return null;
        }

        return RegisterSequence(AudioCueId.NONE, anchorId, true, player.PlayFollowing(asset, target));
    }

    /// <summary>
    /// 単発音声をアンカー位置に固定して再生します。
    /// </summary>
    public AudioPlayHandle PlayOneShotAtAnchor(AudioOneShotAsset asset, AudioAnchorId anchorId)
    {
        if (!TryGetAnchor(anchorId, out Transform target) || !TryGetPlayer(out SequentialAudioPlayer player))
        {
            return null;
        }

        return RegisterOneShot(AudioCueId.NONE, anchorId, false,
            player.PlayOneShotAt(asset, target.position));
    }

    /// <summary>
    /// 単発音声をアンカーに追従して再生します。
    /// </summary>
    public AudioPlayHandle PlayOneShotFollowingAnchor(AudioOneShotAsset asset, AudioAnchorId anchorId)
    {
        if (!TryGetAnchor(anchorId, out Transform target) || !TryGetPlayer(out SequentialAudioPlayer player))
        {
            return null;
        }

        return RegisterOneShot(AudioCueId.NONE, anchorId, true,
            player.PlayOneShotFollowing(asset, target));
    }

    /// <summary>
    /// 指定CueのすべてのシーケンスをENDへ移行させます。
    /// 単発音声には影響しません。
    /// </summary>
    public void StopSequence(AudioCueId cueId)
    {
        SequenceRecord[] records = m_activeSequences.ToArray();
        foreach (SequenceRecord record in records)
        {
            if (record.CueId == cueId && record.Handle != null && record.Handle.IsActive)
            {
                record.Handle.Stop();
            }
        }
    }

    /// <summary>
    /// 指定Cueで再生中の音声を停止します。
    /// シーケンスはENDへ移行し、単発音声は即時停止します。
    /// </summary>
    public void StopCue(AudioCueId cueId)
    {
        if (cueId == AudioCueId.NONE) return;

        StopSequence(cueId);
        OneShotRecord[] records = m_activeOneShots.ToArray();
        foreach (OneShotRecord record in records)
        {
            if (record.CueId == cueId)
            {
                record.Handle?.Stop();
            }
        }
    }

    /// <summary>
    /// UnityEventから数値IDを指定して音声演出を停止します。
    /// </summary>
    public void StopCue(int cueId)
    {
        if (TryConvertCueId(cueId, out AudioCueId id)) StopCue(id);
    }

    /// <summary>
    /// 指定Cueのすべての音声をENDなしで即停止します。
    /// </summary>
    public void StopCueImmediate(AudioCueId cueId)
    {
        if (cueId == AudioCueId.NONE) return;

        SequenceRecord[] sequences = m_activeSequences.ToArray();
        foreach (SequenceRecord record in sequences)
        {
            if (record.CueId == cueId) record.Handle?.StopImmediate();
        }

        OneShotRecord[] oneShots = m_activeOneShots.ToArray();
        foreach (OneShotRecord record in oneShots)
        {
            if (record.CueId == cueId) record.Handle?.Stop();
        }
    }

    /// <summary>
    /// UnityEventから数値IDを指定して音声を即停止します。
    /// </summary>
    public void StopCueImmediate(int cueId)
    {
        if (TryConvertCueId(cueId, out AudioCueId id)) StopCueImmediate(id);
    }

    /// <summary>
    /// このEmitterから開始したすべての音声を即時停止します。
    /// </summary>
    public void StopAllImmediate()
    {
        SequenceRecord[] sequences = m_activeSequences.ToArray();
        foreach (SequenceRecord record in sequences)
        {
            record.Handle?.StopImmediate();
        }

        OneShotRecord[] oneShots = m_activeOneShots.ToArray();
        foreach (OneShotRecord record in oneShots)
        {
            record.Handle?.Stop();
        }
    }

    /// <summary>一度だけ依存コンポーネントとCue検索表を初期化します。</summary>
    private void Initialize()
    {
        if (m_isInitialized) return;
        m_isInitialized = true;

        if (m_sequentialPlayer == null) m_sequentialPlayer = GetComponent<SequentialAudioPlayer>();
        if (m_anchorRegistry == null) m_anchorRegistry = GetComponent<AudioAnchorRegistry>();

        if (m_entries == null) return;
        foreach (AudioCueEntry entry in m_entries)
        {
            if (entry == null || entry.CueId == AudioCueId.NONE) continue;
            if (m_entryMap.ContainsKey(entry.CueId))
            {
                Debug.LogError($"[AudioEmitter] AudioCueIdが重複しています: {entry.CueId}", this);
                continue;
            }

            m_entryMap.Add(entry.CueId, entry);
        }
    }

    /// <summary>登録されている音声演出を取得します。</summary>
    private bool TryGetEntry(AudioCueId cueId, out AudioCueEntry entry)
    {
        Initialize();
        if (cueId != AudioCueId.NONE && m_entryMap.TryGetValue(cueId, out entry)) return true;

        entry = null;
        Debug.LogWarning($"[AudioEmitter] AudioCueIdが登録されていません: {cueId}", this);
        return false;
    }

    /// <summary>連続音声プレイヤーを取得します。</summary>
    private bool TryGetPlayer(out SequentialAudioPlayer player)
    {
        Initialize();
        player = m_sequentialPlayer;
        if (player != null) return true;

        Debug.LogWarning("[AudioEmitter] SequentialAudioPlayerが見つかりません。", this);
        return false;
    }

    /// <summary>指定IDから音声発生位置を解決します。</summary>
    private bool TryGetAnchor(AudioAnchorId anchorId, out Transform target)
    {
        Initialize();
        if (m_anchorRegistry != null && m_anchorRegistry.TryGetTarget(anchorId, out target))
        {
            return true;
        }

        // アンカー表が未配置の簡易構成はROOTのみ自身を利用します。
        if (m_anchorRegistry == null && anchorId == AudioAnchorId.ROOT)
        {
            target = transform;
            return true;
        }

        target = null;
        Debug.LogWarning($"[AudioEmitter] 発生アンカーを解決できません: {anchorId}", this);
        return false;
    }

    /// <summary>同じCueIdのシーケンスが再生中かを確認します。</summary>
    private bool HasActiveSequence(AudioCueId cueId)
    {
        foreach (SequenceRecord record in m_activeSequences)
        {
            if (record.CueId == cueId && record.Handle != null && record.Handle.IsActive)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>再生中のシーケンスと追従アンカー情報を保持します。</summary>
    private AudioSequentialHandle RegisterSequence(AudioCueId cueId, AudioAnchorId anchorId,
        bool followsAnchor, AudioSequentialHandle handle)
    {
        if (handle == null) return null;

        m_activeSequences.Add(new SequenceRecord
        {
            CueId = cueId,
            AnchorId = anchorId,
            FollowsAnchor = followsAnchor,
            Handle = handle
        });
        handle.Finished += OnSequenceFinished;
        return handle;
    }

    /// <summary>再生中の単発音声と追従アンカー情報を保持します。</summary>
    private AudioPlayHandle RegisterOneShot(AudioCueId cueId, AudioAnchorId anchorId,
        bool followsAnchor, AudioPlayHandle handle)
    {
        if (handle == null) return null;

        m_activeOneShots.Add(new OneShotRecord
        {
            CueId = cueId,
            AnchorId = anchorId,
            FollowsAnchor = followsAnchor,
            Handle = handle
        });
        handle.Finished += OnOneShotFinished;
        return handle;
    }

    /// <summary>終了したシーケンスを追跡一覧から削除します。</summary>
    private void OnSequenceFinished(AudioSequentialHandle handle)
    {
        handle.Finished -= OnSequenceFinished;
        for (int i = m_activeSequences.Count - 1; i >= 0; i--)
        {
            if (ReferenceEquals(m_activeSequences[i].Handle, handle))
            {
                m_activeSequences.RemoveAt(i);
            }
        }
    }

    /// <summary>終了した単発音声を追跡一覧から削除します。</summary>
    private void OnOneShotFinished(AudioPlayHandle handle, bool completedNaturally)
    {
        handle.Finished -= OnOneShotFinished;
        for (int i = m_activeOneShots.Count - 1; i >= 0; i--)
        {
            if (ReferenceEquals(m_activeOneShots[i].Handle, handle))
            {
                m_activeOneShots.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// モデル交換後、追従中のすべての音源を新しいボーンへ付け替えます。
    /// 未解決の音声は古いボーンを参照させず停止します。
    /// </summary>
    private void OnAnchorBindingsChanged()
    {
        SequenceRecord[] sequences = m_activeSequences.ToArray();
        foreach (SequenceRecord record in sequences)
        {
            if (!record.FollowsAnchor || record.Handle == null || !record.Handle.IsActive) continue;

            if (TryGetAnchor(record.AnchorId, out Transform target))
            {
                record.Handle.ChangeFollowTarget(target);
            }
            else
            {
                record.Handle.StopImmediate();
            }
        }

        OneShotRecord[] oneShots = m_activeOneShots.ToArray();
        foreach (OneShotRecord record in oneShots)
        {
            if (!record.FollowsAnchor || record.Handle == null || !record.Handle.IsActive) continue;

            if (TryGetAnchor(record.AnchorId, out Transform target))
            {
                record.Handle.ChangeFollowTarget(target);
            }
            else
            {
                record.Handle.Stop();
            }
        }
    }

    /// <summary>UnityEventのint引数を安全に音声演出IDへ変換します。</summary>
    private bool TryConvertCueId(int value, out AudioCueId cueId)
    {
        cueId = AudioCueId.NONE;
        if (!Enum.IsDefined(typeof(AudioCueId), value) || value == (int)AudioCueId.NONE)
        {
            Debug.LogWarning($"[AudioEmitter] 定義されていないAudioCueIdです: {value}", this);
            return false;
        }

        cueId = (AudioCueId)value;
        return true;
    }

    private void OnDisable()
    {
        if (m_anchorRegistry != null)
        {
            m_anchorRegistry.BindingsChanged -= OnAnchorBindingsChanged;
        }

        // 継続SEはオーナーと共に終了。単発SEは用途に応じて自然終了させます。
        SequenceRecord[] sequences = m_activeSequences.ToArray();
        foreach (SequenceRecord record in sequences)
        {
            record.Handle?.StopImmediate();
        }

        if (m_stopOneShotsOnDisable)
        {
            OneShotRecord[] oneShots = m_activeOneShots.ToArray();
            foreach (OneShotRecord record in oneShots)
            {
                record.Handle?.Stop();
            }
        }
        else
        {
            // コンポーネントが破棄されても再生ハンドル側に購読を残さないよう解除します。
            foreach (OneShotRecord record in m_activeOneShots)
            {
                if (record.Handle != null) record.Handle.Finished -= OnOneShotFinished;
            }
            m_activeOneShots.Clear();
        }
    }
}
