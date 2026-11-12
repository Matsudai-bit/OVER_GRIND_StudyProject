using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 音声発生位置のIDとモデル内Transformを対応付けます。
/// EffectAnchorRegistryとは独立して管理します。
/// </summary>
[DisallowMultipleComponent]
public sealed class AudioAnchorRegistry : MonoBehaviour
{
    /// <summary>
    /// 1つの音声発生位置の登録情報です。
    /// </summary>
    [Serializable]
    public sealed class AnchorEntry
    {
        [SerializeField, Header("アンカーID")]
        private AudioAnchorId m_id;

        [SerializeField, Header("初期Transform")]
        private Transform m_target;

        [SerializeField, Header("モデル交換時の解決方式")]
        private AudioAnchorResolveMode m_mode = AudioAnchorResolveMode.RELATIVE_PATH;

        // 相対パスや名前は初期モデルから収集できます。
        [SerializeField] private string m_relativePath;
        [SerializeField] private string m_uniqueName;
        [SerializeField] private HumanBodyBones m_humanoidBone = HumanBodyBones.Hips;

        /// <summary>アンカーID。</summary>
        public AudioAnchorId Id => m_id;

        /// <summary>現在のTransform。</summary>
        public Transform Target => m_target;

        /// <summary>登録の再解決方式。</summary>
        public AudioAnchorResolveMode ResolveMode => m_mode;

        /// <summary>保存済みのモデル相対パス。</summary>
        public string RelativePath => m_relativePath;

        /// <summary>保存済みのTransform名。</summary>
        public string UniqueName => m_uniqueName;

        /// <summary>Humanoidボーン。</summary>
        public HumanBodyBones HumanoidBone => m_humanoidBone;

        /// <summary>
        /// 登録済みTransformからモデル相対パスと名前を取得します。
        /// </summary>
        public void Capture(Transform modelRoot)
        {
            if (modelRoot == null || m_target == null ||
                (m_target != modelRoot && !m_target.IsChildOf(modelRoot)))
            {
                return;
            }

            List<string> names = new List<string>();
            Transform current = m_target;
            while (current != modelRoot)
            {
                names.Insert(0, current.name);
                current = current.parent;
            }

            m_relativePath = string.Join("/", names);
            m_uniqueName = m_target.name;
        }

        /// <summary>解決したTransformを保存します。</summary>
        public void SetTarget(Transform target)
        {
            m_target = target;
        }
    }

    [SerializeField, Header("モデルルート")]
    private Transform m_modelRoot;

    [SerializeField, Header("Animator（Humanoid時のみ必要）")]
    private Animator m_animator;

    [SerializeField, Header("音声発生位置の登録一覧")]
    private List<AnchorEntry> m_entries = new List<AnchorEntry>();

    // 再生時のTransform検索はDictionaryを使用します。
    private readonly Dictionary<AudioAnchorId, Transform> m_targets =
        new Dictionary<AudioAnchorId, Transform>();
    private bool m_isBound;

    /// <summary>現在のモデルルート。</summary>
    public Transform ModelRoot => m_modelRoot;

    /// <summary>モデルが交換されて対応表を作り直した際に発行します。</summary>
    public event Action BindingsChanged;

    private void Awake()
    {
        CaptureMissingPaths();
        Rebind();
    }

    /// <summary>
    /// Inspector上で登録Transformのモデル相対パスを収集します。
    /// </summary>
    [ContextMenu("Audio/登録Transformのパスを保存")]
    public void CapturePaths()
    {
        if (m_entries == null) return;

        foreach (AnchorEntry entry in m_entries)
        {
            entry?.Capture(m_modelRoot);
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }

    /// <summary>
    /// モデルルートを切り替え、全アンカーを再解決します。
    /// </summary>
    /// <param name="modelRoot">交換後のモデルルート。</param>
    public void SetModelRoot(Transform modelRoot)
    {
        m_modelRoot = modelRoot;
        m_animator = modelRoot != null ? modelRoot.GetComponentInChildren<Animator>(true) : null;
        Rebind();
    }

    /// <summary>
    /// 現在のモデルから音声発生位置の検索表を構築し直します。
    /// </summary>
    public void Rebind()
    {
        m_targets.Clear();
        m_isBound = true;

        if (m_modelRoot == null)
        {
            Debug.LogWarning("[AudioAnchorRegistry] モデルルートが未設定です。", this);
            BindingsChanged?.Invoke();
            return;
        }

        if (m_animator == null)
        {
            m_animator = m_modelRoot.GetComponentInChildren<Animator>(true);
        }

        if (m_entries != null)
        {
            foreach (AnchorEntry entry in m_entries)
            {
                if (entry == null) continue;

                if (m_targets.ContainsKey(entry.Id))
                {
                    Debug.LogError($"[AudioAnchorRegistry] AudioAnchorIdが重複しています: {entry.Id}", this);
                    continue;
                }

                Transform target = Resolve(entry);
                if (target == null)
                {
                    Debug.LogWarning($"[AudioAnchorRegistry] 解決できません: {entry.Id}", this);
                    entry.SetTarget(null);
                    continue;
                }

                entry.SetTarget(target);
                m_targets.Add(entry.Id, target);
            }
        }

        BindingsChanged?.Invoke();
    }

    /// <summary>
    /// IDに対応するTransformを取得します。
    /// </summary>
    /// <param name="id">音声発生位置のID。</param>
    /// <param name="target">解決されたTransform。</param>
    /// <returns>true：取得成功。false：未登録または参照切れ。</returns>
    public bool TryGetTarget(AudioAnchorId id, out Transform target)
    {
        if (!m_isBound) Rebind();

        // ROOTのみ、登録がなくてもモデルルートを使用できます。
        if (id == AudioAnchorId.ROOT && !m_targets.ContainsKey(id) && m_modelRoot != null)
        {
            target = m_modelRoot;
            return true;
        }

        return m_targets.TryGetValue(id, out target) && target != null;
    }

    /// <summary>未収集の相対パスを初期モデルから取得します。</summary>
    private void CaptureMissingPaths()
    {
        if (m_entries == null) return;

        foreach (AnchorEntry entry in m_entries)
        {
            if (entry != null && entry.ResolveMode == AudioAnchorResolveMode.RELATIVE_PATH &&
                string.IsNullOrEmpty(entry.RelativePath) && entry.Target != null)
            {
                entry.Capture(m_modelRoot);
            }
        }
    }

    /// <summary>登録情報に従ってTransformを解決します。</summary>
    private Transform Resolve(AnchorEntry entry)
    {
        switch (entry.ResolveMode)
        {
            case AudioAnchorResolveMode.DIRECT:
                return entry.Target;

            case AudioAnchorResolveMode.RELATIVE_PATH:
                if (string.IsNullOrEmpty(entry.RelativePath))
                {
                    // ルート以外は空パスによる誤接続を許しません。
                    return entry.Id == AudioAnchorId.ROOT ? m_modelRoot : FindUniqueName(entry.UniqueName);
                }

                Transform byPath = m_modelRoot.Find(entry.RelativePath);
                return byPath != null ? byPath : FindUniqueName(entry.UniqueName);

            case AudioAnchorResolveMode.UNIQUE_NAME:
                return FindUniqueName(entry.UniqueName);

            case AudioAnchorResolveMode.HUMANOID_BONE:
                return m_animator != null && m_animator.isHuman && m_animator.avatar != null &&
                    m_animator.avatar.isValid
                    ? m_animator.GetBoneTransform(entry.HumanoidBone)
                    : null;

            default:
                return null;
        }
    }

    /// <summary>同名のTransformが一つだけ存在する場合に返します。</summary>
    private Transform FindUniqueName(string boneName)
    {
        if (string.IsNullOrEmpty(boneName) || m_modelRoot == null) return null;

        Transform result = null;
        Transform[] transforms = m_modelRoot.GetComponentsInChildren<Transform>(true);
        foreach (Transform candidate in transforms)
        {
            if (candidate.name != boneName) continue;

            if (result != null)
            {
                // 同名が複数ある場合、意図しない音声位置を防ぎます。
                return null;
            }

            result = candidate;
        }

        return result;
    }
}
