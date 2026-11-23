using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// キャラクター内に事前配置したエフェクトをID単位で一括再生します。
/// </summary>
[DisallowMultipleComponent]
public sealed class EffectLocalController : MonoBehaviour
{
    /// <summary>
    /// ローカルエフェクト1種類分の設定です。
    /// </summary>
    [Serializable]
    public sealed class LocalEntry
    {
        [SerializeField, Header("エフェクトID")]
        private EffectId m_id;

        [SerializeField, Header("キャラクター階層内のテンプレート（非アクティブ推奨）")]
        private EffectInstance m_template;

        [SerializeField, Header("再生方式")]
        private EffectPlaybackMode m_playbackMode = EffectPlaybackMode.ONE_SHOT;

        [SerializeField, Header("事前生成数"), Min(0)]
        private int m_initialInstanceCount = 1;

        [SerializeField, Header("このテンプレートの最大同時再生数"), Min(1)]
        private int m_maxConcurrentCount = 3;

        // クローンだけを再生し、テンプレートは保持します。
        private readonly List<EffectInstance> m_instances = new List<EffectInstance>();

        /// <summary>エフェクトIDを取得します。</summary>
        public EffectId Id => m_id;

        /// <summary>テンプレートを取得します。</summary>
        public EffectInstance Template => m_template;

        /// <summary>再生方式を取得します。</summary>
        public EffectPlaybackMode PlaybackMode => m_playbackMode;

        /// <summary>最大同時再生数を取得します。</summary>
        public int MaxConcurrentCount => Mathf.Max(1, m_maxConcurrentCount);

        /// <summary>事前生成数を取得します。</summary>
        public int InitialInstanceCount =>
            Mathf.Min(Mathf.Max(0, m_initialInstanceCount), MaxConcurrentCount);

        /// <summary>生成済みクローン一覧を取得します。</summary>
        public List<EffectInstance> Instances => m_instances;
    }

    [SerializeField, Header("追従先アンカー表")]
    private EffectAnchorRegistry m_anchorRegistry;

    [SerializeField, Header("事前配置エフェクト（同じIDを複数登録可能）")]
    private List<LocalEntry> m_entries = new List<LocalEntry>();

    [SerializeField, Header("ゲームプレイ中の追加生成を許可")]
    private bool m_allowRuntimeInstantiation = true;

    [SerializeField, Header("プール不足をログに出力する")]
    private bool m_logPoolExhaustion = false;

    // 1つのIDに複数のテンプレートを対応付けます。
    private readonly Dictionary<EffectId, List<LocalEntry>> m_map =
        new Dictionary<EffectId, List<LocalEntry>>();

    private bool m_isInitialized;
    private int m_poolExhaustionCount;

    /// <summary>
    /// プール不足回数を取得します。
    /// </summary>
    public int PoolExhaustionCount => m_poolExhaustionCount;

    /// <summary>
    /// プールの登録情報を初期化します。
    /// </summary>
    private void Awake()
    {
        EnsureInitialized();
    }

    /// <summary>
    /// 実行中の追加生成を許可または禁止します。
    /// </summary>
    public void SetRuntimeInstantiationAllowed(bool isAllowed)
    {
        m_allowRuntimeInstantiation = isAllowed;
    }

    /// <summary>
    /// プール不足の記録をクリアします。
    /// </summary>
    public void ResetPoolExhaustionCount()
    {
        m_poolExhaustionCount = 0;
    }

    /// <summary>
    /// 登録済みエフェクトを同期的に事前生成します。
    /// </summary>
    /// <returns>
    /// true：初期数を確保しました。
    /// false：生成に失敗しました。
    /// </returns>
    public bool Prewarm()
    {
        EnsureInitialized();

        foreach (LocalEntry entry in m_entries)
        {
            if (!IsEntryValid(entry))
            {
                return false;
            }

            while (entry.Instances.Count < entry.InitialInstanceCount)
            {
                if (!TryCreateInstance(entry))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// 事前生成を複数フレームへ分散します。
    /// </summary>
    /// <param name="instancesPerFrame">1フレーム当たりの生成数。</param>
    /// <param name="completed">完了通知。trueは成功、falseは失敗。</param>
    public IEnumerator PrewarmIncrementally(
        int instancesPerFrame,
        Action<bool> completed = null)
    {
        EnsureInitialized();
        int stepCount = 0;
        int limit = Mathf.Max(1, instancesPerFrame);

        foreach (LocalEntry entry in m_entries)
        {
            if (!IsEntryValid(entry))
            {
                completed?.Invoke(false);
                yield break;
            }

            while (entry.Instances.Count < entry.InitialInstanceCount)
            {
                if (!TryCreateInstance(entry))
                {
                    completed?.Invoke(false);
                    yield break;
                }

                stepCount++;
                if (stepCount >= limit)
                {
                    stepCount = 0;
                    yield return null;
                }
            }
        }

        completed?.Invoke(true);
    }

    /// <summary>
    /// 指定IDがローカル型に登録されているか取得します。
    /// </summary>
    public bool Contains(EffectId id)
    {
        EnsureInitialized();
        return m_map.ContainsKey(id);
    }

    /// <summary>
    /// 同じIDのすべてのテンプレートを同時に再生します。
    /// </summary>
    /// <param name="id">エフェクトID。</param>
    /// <returns>一括停止できるハンドル。失敗時は無効です。</returns>
    public EffectHandle Play(EffectId id)
    {
        EnsureInitialized();
        if (!m_map.TryGetValue(id, out List<LocalEntry> group))
        {
            Debug.LogWarning($"[EffectLocalController] IDがありません: {id}", this);
            return default;
        }

        // 単一テンプレートの場合はListを生成しません。
        if (group.Count == 1)
        {
            return PlayEntry(group[0]);
        }

        List<EffectHandle> handles = new List<EffectHandle>(group.Count);
        foreach (LocalEntry entry in group)
        {
            EffectHandle handle = PlayEntry(entry);
            if (!handle.IsValid)
            {
                foreach (EffectHandle started in handles)
                {
                    started.Stop(EffectStopMode.IMMEDIATE);
                }

                return default;
            }

            handles.Add(handle);
        }

        return EffectHandle.Combine(handles);
    }

    /// <summary>
    /// 1つのテンプレートから空きインスタンスを取得して再生します。
    /// </summary>
    private EffectHandle PlayEntry(LocalEntry entry)
    {
        EffectInstance instance = null;
        foreach (EffectInstance candidate in entry.Instances)
        {
            if (candidate != null && !candidate.IsPlaying)
            {
                instance = candidate;
                break;
            }
        }

        if (instance == null)
        {
            if (entry.Instances.Count >= entry.MaxConcurrentCount ||
                !m_allowRuntimeInstantiation || !TryCreateInstance(entry))
            {
                RecordPoolExhaustion(entry);
                return default;
            }

            instance = entry.Instances[entry.Instances.Count - 1];
        }

        EffectSpawnRequest request = new EffectSpawnRequest(
            Vector3.zero, Quaternion.identity, false, m_anchorRegistry);

        if (!instance.Play(request, entry.PlaybackMode))
        {
            instance.ResetForReuse();
            instance.gameObject.SetActive(false);
            return default;
        }

        return new EffectHandle(instance, instance.Generation);
    }

    /// <summary>
    /// モデル交換などの際にローカルエフェクトをすべて停止します。
    /// </summary>
    public void StopAll()
    {
        foreach (LocalEntry entry in m_entries)
        {
            if (entry == null)
            {
                continue;
            }

            foreach (EffectInstance instance in entry.Instances)
            {
                if (instance != null && instance.IsPlaying)
                {
                    instance.StopImmediately();
                }
            }
        }
    }

    /// <summary>
    /// コンポーネント無効化時にすべて停止します。
    /// </summary>
    private void OnDisable()
    {
        StopAll();
    }

    /// <summary>
    /// すべてのLocalEntryの検索マップを一度だけ生成します。
    /// </summary>
    private void EnsureInitialized()
    {
        if (m_isInitialized)
        {
            return;
        }

        m_map.Clear();
        foreach (LocalEntry entry in m_entries)
        {
            if (!IsEntryValid(entry))
            {
                Debug.LogWarning("[EffectLocalController] IDまたはテンプレートが未設定です。", this);
                continue;
            }

            if (!m_map.TryGetValue(entry.Id, out List<LocalEntry> group))
            {
                group = new List<LocalEntry>();
                m_map.Add(entry.Id, group);
            }

            group.Add(entry);
            entry.Template.gameObject.SetActive(false);
        }

        m_isInitialized = true;
    }

    /// <summary>
    /// LocalEntryの設定が正しいか確認します。
    /// </summary>
    private static bool IsEntryValid(LocalEntry entry)
    {
        return entry != null && entry.Id != EffectId.NONE && entry.Template != null;
    }

    /// <summary>
    /// テンプレートの複製と初期化を行います。
    /// </summary>
    private bool TryCreateInstance(LocalEntry entry)
    {
        if (!IsEntryValid(entry) || entry.Instances.Count >= entry.MaxConcurrentCount)
        {
            return false;
        }

        // プレイ用のクローンはテンプレートと同じ親に作成します。
        EffectInstance instance = Instantiate(entry.Template, entry.Template.transform.parent, false);
        instance.name = $"{entry.Template.name}_Runtime_{entry.Instances.Count}";
        instance.transform.localPosition = entry.Template.transform.localPosition;
        instance.transform.localRotation = entry.Template.transform.localRotation;
        instance.transform.localScale = entry.Template.transform.localScale;

        if (!instance.Prepare(OnEffectFinished))
        {
            Destroy(instance.gameObject);
            return false;
        }

        instance.gameObject.SetActive(false);
        entry.Instances.Add(instance);
        return true;
    }

    /// <summary>
    /// プール不足を記録します。
    /// </summary>
    private void RecordPoolExhaustion(LocalEntry entry)
    {
        m_poolExhaustionCount++;
        if (m_logPoolExhaustion)
        {
            Debug.LogWarning($"[EffectLocalController] 空きがありません: {entry.Template.name}", this);
        }
    }

    /// <summary>
    /// 終了したインスタンスを再利用待ちに戻します。
    /// </summary>
    private void OnEffectFinished(EffectInstance instance)
    {
        if (instance == null)
        {
            return;
        }

        instance.ResetForReuse();
        instance.gameObject.SetActive(false);
    }
}
