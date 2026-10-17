using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// モデル内で使用するRenderer参照を管理します。
/// </summary>
[DisallowMultipleComponent]
public sealed class MeshReferenceRegistry : MonoBehaviour
{
    /// <summary>
    /// Rendererの参照情報です。
    /// </summary>
    [Serializable]
    public sealed class MeshReference
    {
        // 参照用ID
        [SerializeField]
        private string m_id;

        // Model Rootからの相対パス
        [SerializeField]
        private string m_relativePath;

        // Renderer参照
        [SerializeField]
        private Renderer m_renderer;

        /// <summary>
        /// 参照用IDを取得します。
        /// </summary>
        public string Id => m_id;

        /// <summary>
        /// 相対パスを取得します。
        /// </summary>
        public string RelativePath => m_relativePath;

        /// <summary>
        /// Rendererを取得します。
        /// </summary>
        public Renderer Renderer => m_renderer;

        /// <summary>
        /// 相対パスを設定します。
        /// </summary>
        /// <param name="relativePath">設定する相対パス。</param>
        public void SetRelativePath(string relativePath)
        {
            m_relativePath = relativePath;
        }

        /// <summary>
        /// Renderer参照を設定します。
        /// </summary>
        /// <param name="renderer">設定するRenderer。</param>
        public void SetRenderer(Renderer renderer)
        {
            m_renderer = renderer;
        }
    }

    [Header("モデル設定")]

    // Rendererを検索する基準となるRoot
    [SerializeField]
    private Transform m_modelRoot;

    [Header("メッシュ参照")]

    // 使用するRenderer一覧
    [SerializeField]
    private List<MeshReference> m_references = new();

    // ID検索用
    private readonly Dictionary<string, Renderer> m_rendererMap = new();

    /// <summary>
    /// 初期化します。
    /// </summary>
    private void Awake()
    {
        BuildReferenceMap();
    }

    /// <summary>
    /// IDからRendererを取得します。
    /// </summary>
    /// <param name="id">RendererのID。</param>
    /// <returns>
    /// Rendererが存在する場合はRenderer。
    /// 存在しない場合はnull。
    /// </returns>
    public Renderer GetRenderer(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        if (m_rendererMap.TryGetValue(id, out Renderer renderer))
        {
            return renderer;
        }

        Debug.LogWarning(
            $"[{nameof(MeshReferenceRegistry)}] " +
            $"ID「{id}」のRendererが見つかりません。",
            this);

        return null;
    }

    /// <summary>
    /// IDからRendererの取得を試みます。
    /// </summary>
    /// <param name="id">RendererのID。</param>
    /// <param name="renderer">取得したRenderer。</param>
    /// <returns>
    /// true：取得しました。
    /// false：取得できませんでした。
    /// </returns>
    public bool TryGetRenderer(
        string id,
        out Renderer renderer)
    {
        if (string.IsNullOrEmpty(id))
        {
            renderer = null;
            return false;
        }

        return m_rendererMap.TryGetValue(
            id,
            out renderer);
    }

    /// <summary>
    /// GameObject名からRendererを取得します。
    /// </summary>
    /// <param name="objectName">GameObject名。</param>
    /// <returns>
    /// Rendererが存在する場合はRenderer。
    /// 存在しない場合はnull。
    /// </returns>
    public Renderer GetRendererByName(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
        {
            return null;
        }

        foreach (MeshReference reference in m_references)
        {
            if (reference.Renderer == null)
            {
                continue;
            }

            if (reference.Renderer.gameObject.name == objectName)
            {
                return reference.Renderer;
            }
        }

        return null;
    }

    /// <summary>
    /// Renderer参照を再バインドします。
    /// </summary>
    public void Rebind()
    {
        if (m_modelRoot == null)
        {
            Debug.LogError(
                $"[{nameof(MeshReferenceRegistry)}] " +
                "Model Rootが設定されていません。",
                this);

            return;
        }

        foreach (MeshReference reference in m_references)
        {
            if (string.IsNullOrEmpty(reference.RelativePath))
            {
                Debug.LogWarning(
                    $"[{nameof(MeshReferenceRegistry)}] " +
                    $"ID「{reference.Id}」のパスが設定されていません。",
                    this);

                continue;
            }

            Transform targetTransform =
                m_modelRoot.Find(reference.RelativePath);

            if (targetTransform == null)
            {
                Debug.LogWarning(
                    $"[{nameof(MeshReferenceRegistry)}] " +
                    $"ID「{reference.Id}」のオブジェクトが見つかりません。" +
                    $"\nPath: {reference.RelativePath}",
                    this);

                reference.SetRenderer(null);
                continue;
            }

            Renderer renderer =
                targetTransform.GetComponent<Renderer>();

            if (renderer == null)
            {
                Debug.LogWarning(
                    $"[{nameof(MeshReferenceRegistry)}] " +
                    $"ID「{reference.Id}」にRendererがありません。" +
                    $"\nPath: {reference.RelativePath}",
                    this);

                reference.SetRenderer(null);
                continue;
            }

            reference.SetRenderer(renderer);
        }

        BuildReferenceMap();
    }

    /// <summary>
    /// 現在のRenderer参照から相対パスを保存します。
    /// </summary>
    public void CapturePaths()
    {
        if (m_modelRoot == null)
        {
            Debug.LogError(
                $"[{nameof(MeshReferenceRegistry)}] " +
                "Model Rootが設定されていません。",
                this);

            return;
        }

        foreach (MeshReference reference in m_references)
        {
            Renderer renderer = reference.Renderer;

            if (renderer == null)
            {
                continue;
            }

            string path =
                GetRelativePath(
                    m_modelRoot,
                    renderer.transform);

            reference.SetRelativePath(path);
        }
    }

    /// <summary>
    /// ID検索用Dictionaryを構築します。
    /// </summary>
    private void BuildReferenceMap()
    {
        m_rendererMap.Clear();

        foreach (MeshReference reference in m_references)
        {
            if (string.IsNullOrEmpty(reference.Id) ||
                reference.Renderer == null)
            {
                continue;
            }

            if (!m_rendererMap.TryAdd(
                    reference.Id,
                    reference.Renderer))
            {
                Debug.LogWarning(
                    $"[{nameof(MeshReferenceRegistry)}] " +
                    $"ID「{reference.Id}」が重複しています。",
                    this);
            }
        }
    }

    /// <summary>
    /// RootからTargetまでの相対パスを取得します。
    /// </summary>
    private static string GetRelativePath(
        Transform root,
        Transform target)
    {
        if (root == target)
        {
            return string.Empty;
        }

        List<string> pathElements = new();

        Transform current = target;

        while (current != null &&
               current != root)
        {
            pathElements.Add(current.name);
            current = current.parent;
        }

        if (current != root)
        {
            return string.Empty;
        }

        pathElements.Reverse();

        return string.Join("/", pathElements);
    }
}