using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>アンカーIDとモデルのTransformを管理し、モデル交換時に再解決します。</summary>
[DisallowMultipleComponent]
public sealed class EffectAnchorRegistry : MonoBehaviour
{
    /// <summary>1つの追従先を定義します。</summary>
    [Serializable]
    public sealed class AnchorEntry
    {
        // 論理的な発生位置のID。
        [SerializeField, Header("アンカーの識別")]
        private EffectAnchorId m_id;
        // 初期モデル上で指定するTransform。モデル変更後は実行時に更新します。
        [SerializeField, Header("初期Transform")]
        private Transform m_target;
        // モデル交換時の解決方式。
        [SerializeField, Header("モデル交換時の解決")]
        private EffectAnchorResolveMode m_mode = EffectAnchorResolveMode.RELATIVE_PATH;
        // モデルルートを基準としたパス（ContextMenuで収集可能）。
        [SerializeField]
        private string m_relativePath;
        // モデル内で重複しないボーン名。
        [SerializeField]
        private string m_uniqueName;
        // Humanoid専用のボーンID。
        [SerializeField]
        private HumanBodyBones m_humanoidBone = HumanBodyBones.Hips;

        /// <summary>アンカーID。</summary>
        public EffectAnchorId Id => m_id;
        /// <summary>現在の追従対象。</summary>
        public Transform Target => m_target;
        /// <summary>再解決方法。</summary>
        public EffectAnchorResolveMode Mode => m_mode;
        /// <summary>保存済みの相対パス。</summary>
        public string RelativePath => m_relativePath;
        /// <summary>保存済みのボーン名。</summary>
        public string UniqueName => m_uniqueName;
        /// <summary>HumanoidボーンID。</summary>
        public HumanBodyBones HumanoidBone => m_humanoidBone;

        /// <summary>初期Transformからモデル内の識別情報を取得します。</summary>
        /// <param name="modelRoot">現在のモデルルート。</param>
        public void Capture(Transform modelRoot)
        {
            if (m_target == null || modelRoot == null ||
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

        /// <summary>解決した追従対象を登録します。</summary>
        public void SetTarget(Transform target)
        {
            m_target = target;
        }
    }

    // 人型・モンスターのいずれも同じ表で扱います。
    [SerializeField, Header("モデルのルート")]
    private Transform m_modelRoot;
    // Humanoidモードのアンカーを解決するAnimator。
    [SerializeField, Header("Animator（任意）")]
    private Animator m_animator;
    // アンカーIDとTransformの設定一覧。
    [SerializeField, Header("アンカー対応表")]
    private List<AnchorEntry> m_entries = new List<AnchorEntry>();

    // 再生時の高速検索用辞書。
    private readonly Dictionary<EffectAnchorId, Transform> m_targets =
        new Dictionary<EffectAnchorId, Transform>();

    /// <summary>現在のモデルルート。</summary>
    public Transform ModelRoot => m_modelRoot;

    /// <summary>モデル変更時に追従対象を再設定するための通知です。</summary>
    public event Action BindingsChanged;

    /// <summary>現在のモデルから参照を構築します。</summary>
    private void Awake()
    {
        CaptureMissingPaths();
        Rebind();
    }

    /// <summary>Inspectorで登録済みのTransformから相対パスを保存します。</summary>
    [ContextMenu("Effects/登録Transformのパスを保存")]
    public void CapturePaths()
    {
        foreach (AnchorEntry entry in m_entries)
        {
            if (entry != null)
            {
                entry.Capture(m_modelRoot);
            }
        }
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }

    /// <summary>新しいモデルを設定し、全アンカーを再バインドします。</summary>
    /// <param name="modelRoot">交換後のモデルルート。</param>
    public void SetModelRoot(Transform modelRoot)
    {
        m_modelRoot = modelRoot;
        m_animator = m_modelRoot != null ? m_modelRoot.GetComponentInChildren<Animator>(true) : null;
        Rebind();
    }

    /// <summary>現在のモデルを使用して対応表を作り直します。</summary>
    public void Rebind()
    {
        m_targets.Clear();
        if (m_modelRoot == null)
        {
            Debug.LogWarning("[EffectAnchorRegistry] モデルルートが未設定です。", this);
            BindingsChanged?.Invoke();
            return;
        }
        if (m_animator == null)
        {
            m_animator = m_modelRoot.GetComponentInChildren<Animator>(true);
        }

        foreach (AnchorEntry entry in m_entries)
        {
            if (entry == null)
            {
                continue;
            }
            if (m_targets.ContainsKey(entry.Id))
            {
                Debug.LogError($"[EffectAnchorRegistry] IDが重複しています: {entry.Id}", this);
                continue;
            }

            Transform target = Resolve(entry);
            if (target == null)
            {
                Debug.LogWarning($"[EffectAnchorRegistry] 解決できません: {entry.Id}", this);
                continue;
            }
            entry.SetTarget(target);
            m_targets.Add(entry.Id, target);
        }
        BindingsChanged?.Invoke();
    }

    /// <summary>アンカーIDに対応するTransformを取得します。</summary>
    /// <param name="id">アンカーID。</param>
    /// <param name="target">対応するTransform。</param>
    /// <returns>true：取得成功。false：未登録または参照切れ。</returns>
    public bool TryGetTarget(EffectAnchorId id, out Transform target)
    {
        return m_targets.TryGetValue(id, out target) && target != null;
    }

    /// <summary>設定済みTransformのうち、未収集のパスを記録します。</summary>
    private void CaptureMissingPaths()
    {
        foreach (AnchorEntry entry in m_entries)
        {
            if (entry != null && entry.Mode == EffectAnchorResolveMode.RELATIVE_PATH &&
                string.IsNullOrEmpty(entry.UniqueName) && entry.Target != null)
            {
                entry.Capture(m_modelRoot);
            }
        }
    }

    /// <summary>定義ごとの方法でモデル内のTransformを検索します。</summary>
    private Transform Resolve(AnchorEntry entry)
    {
        switch (entry.Mode)
        {
            case EffectAnchorResolveMode.DIRECT:
                return entry.Target;

            case EffectAnchorResolveMode.RELATIVE_PATH:
                // ルート自身は空のパスとして扱います。
                Transform byPath = string.IsNullOrEmpty(entry.RelativePath)
                    ? (entry.Id == EffectAnchorId.ROOT ? m_modelRoot : null)
                    : m_modelRoot.Find(entry.RelativePath);
                return byPath != null ? byPath : FindUniqueName(entry.UniqueName);

            case EffectAnchorResolveMode.UNIQUE_NAME:
                return FindUniqueName(entry.UniqueName);

            case EffectAnchorResolveMode.HUMANOID_BONE:
                if (m_animator != null && m_animator.isHuman &&
                    m_animator.avatar != null && m_animator.avatar.isValid)
                {
                    return m_animator.GetBoneTransform(entry.HumanoidBone);
                }
                return null;

            default:
                return null;
        }
    }

    /// <summary>名前が一意に一致するボーンだけを返します。</summary>
    private Transform FindUniqueName(string boneName)
    {
        if (string.IsNullOrEmpty(boneName) || m_modelRoot == null)
        {
            return null;
        }
        Transform result = null;
        Transform[] transforms = m_modelRoot.GetComponentsInChildren<Transform>(true);
        foreach (Transform candidate in transforms)
        {
            if (candidate.name != boneName)
            {
                continue;
            }
            if (result != null)
            {
                // 名前が重複する場合は誤接続を避けるため失敗させます。
                return null;
            }
            result = candidate;
        }
        return result;
    }
}
