using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>キャラクターPrefab内のエフェクトを複数同時再生します。</summary>
[DisallowMultipleComponent]
public sealed class EffectLocalController : MonoBehaviour
{
    /// <summary>ローカルエフェクトごとの登録情報です。</summary>
    [Serializable]
    public sealed class LocalEntry
    {
        [SerializeField, Header("エフェクトID")]
        private EffectId m_id;
        [SerializeField, Header("キャラクター階層内のテンプレート（非アクティブ推奨）")]
        private EffectInstance m_template;
        [SerializeField, Header("再生方式")]
        private EffectPlaybackMode m_playbackMode = EffectPlaybackMode.ONE_SHOT;
        [SerializeField, Header("最大同時再生数"), Min(1)]
        private int m_maxConcurrentCount = 3;

        // クローンのみ再生し、テンプレートは変更しません。
        private readonly List<EffectInstance> m_instances = new List<EffectInstance>();

        /// <summary>ID。</summary>
        public EffectId Id => m_id;
        /// <summary>原本。</summary>
        public EffectInstance Template => m_template;
        /// <summary>再生方式。</summary>
        public EffectPlaybackMode PlaybackMode => m_playbackMode;
        /// <summary>同時再生の上限。</summary>
        public int MaxConcurrentCount => Mathf.Max(1, m_maxConcurrentCount);
        /// <summary>生成済みのクローン。</summary>
        public List<EffectInstance> Instances => m_instances;
    }

    [SerializeField, Header("追従先アンカー表")]
    private EffectAnchorRegistry m_anchorRegistry;
    [SerializeField, Header("事前配置エフェクト")]
    private List<LocalEntry> m_entries = new List<LocalEntry>();
    private readonly Dictionary<EffectId, LocalEntry> m_map =
        new Dictionary<EffectId, LocalEntry>();

    /// <summary>設定を読み込み、テンプレートの自動再生を防ぎます。</summary>
    private void Awake()
    {
        foreach (LocalEntry entry in m_entries)
        {
            if (entry == null || entry.Id == EffectId.NONE || entry.Template == null)
            {
                Debug.LogWarning("[EffectLocalController] IDまたはテンプレートが未設定です。", this);
                continue;
            }
            if (!m_map.TryAdd(entry.Id, entry))
            {
                Debug.LogError($"[EffectLocalController] IDが重複しています: {entry.Id}", this);
                continue;
            }
            // 原本は常に保持し、再生用インスタンスだけを作成します。
            entry.Template.gameObject.SetActive(false);
        }
    }

    /// <summary>エフェクトIDがローカル側に登録されているか。</summary>
    public bool Contains(EffectId id)
    {
        return m_map.ContainsKey(id);
    }

    /// <summary>登録済みエフェクトを空きインスタンスで再生します。</summary>
    /// <param name="id">エフェクトID。</param>
    /// <returns>再生ハンドル。失敗時は無効です。</returns>
    public EffectHandle Play(EffectId id)
    {
        if (!m_map.TryGetValue(id, out LocalEntry entry))
        {
            Debug.LogWarning($"[EffectLocalController] IDがありません: {id}", this);
            return default;
        }

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
                Debug.LogWarning($"[EffectLocalController] 同時再生上限: {id}", this);
                return default;
            }
            // オリジナルをテンプレートとして複製します。
            instance = Instantiate(entry.Template, entry.Template.transform.parent, false);
            instance.name = $"{entry.Template.name}_Runtime_{entry.Instances.Count}";
            instance.transform.localPosition = entry.Template.transform.localPosition;
            instance.transform.localRotation = entry.Template.transform.localRotation;
            instance.transform.localScale = entry.Template.transform.localScale;
            EffectInstance captured = instance;
            if (!instance.Prepare(OnEffectFinished))
            {
                Destroy(captured.gameObject);
                return default;
            }
            entry.Instances.Add(captured);
        }

        // ローカル型は原本のローカルTransformを使います。
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

    /// <summary>モデル交換前に再生中エフェクトを停止します。</summary>
    public void StopAll()
    {
        foreach (LocalEntry entry in m_map.Values)
        {
            foreach (EffectInstance instance in entry.Instances)
            {
                if (instance != null && instance.IsPlaying)
                {
                    instance.StopImmediately();
                }
            }
        }
    }

    /// <summary>内部の全エフェクトを停止します。</summary>
    private void OnDisable()
    {
        StopAll();
    }

    /// <summary>終了したエフェクトを再利用待ちに戻します。</summary>
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
