using UnityEngine;

/// <summary>
/// 二等辺三角形を奥行き方向に押し出した三角柱Meshを生成します。
/// </summary>
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class TriangularPrismMesh : MonoBehaviour
{
    private const float MIN_SIZE = 0.01f;

    [Header("形状設定")]

    // 三角形の底辺の幅。
    [SerializeField]
    private float m_width = 4.0f;

    // 三角形の高さ。
    [SerializeField]
    private float m_height = 2.0f;

    // 三角柱の奥行き。
    [SerializeField]
    private float m_depth = 2.0f;

    // MeshFilterコンポーネント。
    private MeshFilter m_meshFilter;

    // MeshColliderコンポーネント。
    private MeshCollider m_meshCollider;

    /// <summary>
    /// コンポーネントの初期化を行います。
    /// </summary>
    private void Awake()
    {
        InitializeComponents();
        GenerateMesh();
    }

#if UNITY_EDITOR

    /// <summary>
    /// Inspectorの値が変更されたときにMeshを再生成します。
    /// </summary>
    private void OnValidate()
    {
        InitializeComponents();
        GenerateMesh();
    }

#endif

    /// <summary>
    /// 必要なコンポーネントを取得します。
    /// </summary>
    private void InitializeComponents()
    {
        if (m_meshFilter == null)
        {
            m_meshFilter = GetComponent<MeshFilter>();
        }

        if (m_meshCollider == null)
        {
            m_meshCollider = GetComponent<MeshCollider>();
        }
    }

    /// <summary>
    /// 三角柱Meshを生成します。
    /// </summary>
    private void GenerateMesh()
    {
        if (m_meshFilter == null)
        {
            return;
        }

        float width = Mathf.Max(MIN_SIZE, m_width);
        float height = Mathf.Max(MIN_SIZE, m_height);
        float depth = Mathf.Max(MIN_SIZE, m_depth);

        // 生成するMeshを作成します。
        Mesh mesh = new Mesh
        {
            name = "TriangularPrismMesh"
        };

        // 三角形の前面と背面の頂点を作成します。
        Vector3[] vertices =
        {
            // 前面。
            new Vector3(-width * 0.5f, 0.0f, -depth * 0.5f),
            new Vector3(width * 0.5f, 0.0f, -depth * 0.5f),
            new Vector3(0.0f, height, -depth * 0.5f),

            // 背面。
            new Vector3(-width * 0.5f, 0.0f, depth * 0.5f),
            new Vector3(width * 0.5f, 0.0f, depth * 0.5f),
            new Vector3(0.0f, height, depth * 0.5f)
        };

        // 三角形の面を作成します。
        int[] triangles =
        {
            // 前面。
            0, 2, 1,

            // 背面。
            3, 4, 5,

            // 左側面。
            0, 3, 5,
            0, 5, 2,

            // 右側面。
            1, 2, 5,
            1, 5, 4,

            // 底面。
            0, 1, 4,
            0, 4, 3
        };

        // Meshに頂点と面を設定します。
        mesh.vertices = vertices;
        mesh.triangles = triangles;

        // 法線とBoundsを再計算します。
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // MeshFilterにMeshを設定します。
        m_meshFilter.sharedMesh = mesh;

        // MeshColliderを更新します。
        UpdateMeshCollider(mesh);
    }

    /// <summary>
    /// MeshColliderに生成したMeshを設定します。
    /// </summary>
    /// <param name="mesh">設定するMesh。</param>
    private void UpdateMeshCollider(Mesh mesh)
    {
        if (m_meshCollider == null)
        {
            return;
        }

        m_meshCollider.sharedMesh = null;
        m_meshCollider.sharedMesh = mesh;
    }
}