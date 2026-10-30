using System;
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
        [SerializeField, Header("このテンプレートの最大同時再生数"), Min(1)]
        private int m_maxConcurrentCount = 3;

        // クローンのみを再生し、テンプレートは保持します。
        private readonly List<EffectInstance> m_instances = new List<EffectInstance>();

        /// <summary>エフェクトIDを取得します。</summary>
        public EffectId Id => m_id;
        /// <summary>テンプレートを取得します。</summary>
        public EffectInstance Template => m_template;
        /// <summary>再生方式を取得します。</summary>
        public EffectPlaybackMode PlaybackMode => m_playbackMode;
        /// <summary>テンプレートごとの同時再生上限を取得します。</summary>
        public int MaxConcurrentCount => Mathf.Max(1, m_maxConcurrentCount);
        /// <summary>再利用するクローン一覧を取得します。</summary>
        public List<EffectInstance> Instances => m_instances;
    }

    [SerializeField, Header("追従先アンカー表")]
    private EffectAnchorRegistry m_anchorRegistry;
    [SerializeField, Header("事前配置エフェクト（同じIDを複数登録可能）")]
    private List<LocalEntry> m_entries = new List<LocalEntry>();

    // 1つのIDに複数のテンプレートを対応付けます。
    private readonly Dictionary<EffectId, List<LocalEntry>> m_map =
        new Dictionary<EffectId, List<LocalEntry>>();

    /// <summary>
    /// 登録情報を読み込み、テンプレートの自動再生を防ぎます。
    /// </summary>
    private void Awake()
    {
        m_map.Clear();
        foreach (LocalEntry entry in m_entries)
        {
            if (entry == null || entry.Id == EffectId.NONE || entry.Template == null)
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
    }

    /// <summary>
    /// 指定IDがローカル型に登録されているか取得します。
    /// </summary>
    public bool Contains(EffectId id)
    {
        return m_map.ContainsKey(id);
    }

    /// <summary>
    /// 同じIDのすべてのテンプレートを同時に再生します。
    /// </summary>
    /// <param name="id">エフェクトID。</param>
    /// <returns>一括停止できるハンドル。失敗時は無効です。</returns>
    public EffectHandle Play(EffectId id)
    {
        if (!m_map.TryGetValue(id, out List<LocalEntry> group))
        {
            Debug.LogWarning($"[EffectLocalController] IDがありません: {id}", this);
            return default;
        }

        List<EffectHandle> handles = new List<EffectHandle>(group.Count);
        foreach (LocalEntry entry in group)
        {
            EffectHandle handle = PlayEntry(entry);
            if (!handle.IsValid)
            {
                // グループが一部だけ再生された状態を残さないように戻します。
                foreach (EffectHandle started in handles)
                {
                    started.Stop(EffectStopMode.IMMEDIATE);
                }
                Debug.LogWarning(
                    $"[EffectLocalController] 同一IDグループの再生に失敗しました: {id} / {entry.Template.name}", this);
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
            if (entry.Instances.Count >= entry.MaxConcurrentCount)
            {
                return default;
            }

            // テンプレートは非アクティブのまま保持し、クローンだけを使用します。
            instance = Instantiate(entry.Template, entry.Template.transform.parent, false);
            instance.name = $"{entry.Template.name}_Runtime_{entry.Instances.Count}";
            instance.transform.localPosition = entry.Template.transform.localPosition;
            instance.transform.localRotation = entry.Template.transform.localRotation;
            instance.transform.localScale = entry.Template.transform.localScale;
            if (!instance.Prepare(OnEffectFinished))
            {
                Destroy(instance.gameObject);
                return default;
            }
            entry.Instances.Add(instance);
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
