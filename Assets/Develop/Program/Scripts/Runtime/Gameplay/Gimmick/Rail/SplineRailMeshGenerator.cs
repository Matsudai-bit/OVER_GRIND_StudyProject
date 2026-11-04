using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

/// <summary>
/// Unity Splineのノットから軽量な四角断面レールメッシュを生成します。
/// スプライン変更時だけ頂点を更新します。
/// </summary>
[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(SplineContainer))]
[RequireComponent(typeof(MeshFilter))]
public sealed class SplineRailMeshGenerator : MonoBehaviour
{
    private const int VERTICES_PER_KNOT = 4;
    private const int SIDE_COUNT = 4;
    private const int TRIANGLE_INDICES_PER_SIDE = 6;
    private const float TANGENT_VALID_SQR_LENGTH = 0.00000001f;
    private const float RIGHT_VALID_SQR_LENGTH = 0.000001f;

    [SerializeField, Header("断面サイズ")]
    private float m_halfWidth = 0.15f;

    [SerializeField]
    private float m_halfHeight = 0.15f;

    [SerializeField, Header("コライダー設定")]
    private MeshCollider m_meshCollider;

    [SerializeField]
    private bool m_updateCollider = true;

    [SerializeField, Min(1)]
    private int m_colliderUpdateIntervalFrames = 4;

    // Unity Splineと描画用Mesh。
    private SplineContainer m_splineContainer;
    private MeshFilter m_meshFilter;
    private Mesh m_mesh;

    // メッシュデータの再利用バッファ。
    private Vector3[] m_vertices;
    private Vector3[] m_normals;
    private int[] m_triangles;
    private Vector2[] m_uvs;

    // 更新制御用の情報。
    private int m_lastKnotCount = -1;
    private int m_lastColliderUpdateFrame = -1;
    private bool m_isMeshDirty = true;
    private bool m_isColliderDirty;

    private void Awake()
    {
        m_splineContainer = GetComponent<SplineContainer>();
        m_meshFilter = GetComponent<MeshFilter>();

        if (m_updateCollider && m_meshCollider == null)
        {
            TryGetComponent(out m_meshCollider);
        }

        m_mesh = new Mesh { name = "RailMesh (Runtime Generated)" };
        m_mesh.MarkDynamic();
        m_meshFilter.sharedMesh = m_mesh;
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        UnityEngine.Splines.Spline.Changed += HandleSplineChanged;
        m_isMeshDirty = true;
    }

    private void OnDisable()
    {
        UnityEngine.Splines.Spline.Changed -= HandleSplineChanged;
    }

    private void LateUpdate()
    {
        if (m_mesh == null || m_splineContainer == null)
        {
            return;
        }

        if (m_isMeshDirty)
        {
            m_isMeshDirty = false;
            UpdateRailMesh();
        }

        // 形状の変更が止まった後でも、未反映のコライダー更新を完了させる。
        UpdateColliderIfNeeded();
    }

    private void OnDestroy()
    {
        if (m_mesh == null)
        {
            return;
        }

        if (m_meshCollider != null && m_meshCollider.sharedMesh == m_mesh)
        {
            m_meshCollider.sharedMesh = null;
        }

        if (m_meshFilter != null && m_meshFilter.sharedMesh == m_mesh)
        {
            m_meshFilter.sharedMesh = null;
        }

        if (Application.isPlaying)
        {
            Destroy(m_mesh);
        }
        else
        {
            DestroyImmediate(m_mesh);
        }
    }

    /// <summary>
    /// 外部から頂点の再生成を要求します。
    /// </summary>
    public void RequestMeshUpdate()
    {
        m_isMeshDirty = true;
    }

    /// <summary>
    /// 対象Splineが変更されたときだけ再生成予約を行います。
    /// </summary>
    private void HandleSplineChanged(UnityEngine.Splines.Spline spline, int knotIndex, SplineModification modification)
    {
        if (m_splineContainer == null || !ReferenceEquals(spline, m_splineContainer.Spline))
        {
            return;
        }

        RequestMeshUpdate();
    }

    /// <summary>
    /// 頂点数が変わったときのみ構造情報を作り直します。
    /// </summary>
    private void UpdateRailMesh()
    {
        UnityEngine.Splines.Spline spline = m_splineContainer.Spline;
        if (spline == null || spline.Count < 2)
        {
            m_mesh.Clear();
            m_lastKnotCount = -1;
            m_isColliderDirty = true;
            return;
        }

        int knotCount = spline.Count;
        if (knotCount != m_lastKnotCount)
        {
            RebuildTopology(knotCount);
            m_lastKnotCount = knotCount;
        }

        UpdateVertexPositions(spline, knotCount);
        ApplyMeshVertices();
        m_isColliderDirty = m_updateCollider;
    }

    /// <summary>
    /// 頂点数・インデックス・UVを構築します。
    /// </summary>
    private void RebuildTopology(int knotCount)
    {
        int vertexCount = knotCount * VERTICES_PER_KNOT;
        m_vertices = new Vector3[vertexCount];
        m_normals = new Vector3[vertexCount];
        m_uvs = new Vector2[vertexCount];
        m_triangles = new int[(knotCount - 1) * SIDE_COUNT * TRIANGLE_INDICES_PER_SIDE];

        for (int i = 0; i < knotCount; i++)
        {
            float v = (float)i / (knotCount - 1);
            int baseIndex = i * VERTICES_PER_KNOT;
            m_uvs[baseIndex] = new Vector2(0.0f, v);
            m_uvs[baseIndex + 1] = new Vector2(0.33f, v);
            m_uvs[baseIndex + 2] = new Vector2(0.66f, v);
            m_uvs[baseIndex + 3] = new Vector2(1.0f, v);
        }

        int triangleIndex = 0;
        for (int i = 0; i < knotCount - 1; i++)
        {
            int frontBase = i * VERTICES_PER_KNOT;
            int backBase = (i + 1) * VERTICES_PER_KNOT;

            for (int side = 0; side < SIDE_COUNT; side++)
            {
                int nextSide = (side + 1) % SIDE_COUNT;
                int a = frontBase + side;
                int b = backBase + side;
                int c = frontBase + nextSide;
                int d = backBase + nextSide;

                m_triangles[triangleIndex++] = a;
                m_triangles[triangleIndex++] = b;
                m_triangles[triangleIndex++] = c;
                m_triangles[triangleIndex++] = c;
                m_triangles[triangleIndex++] = b;
                m_triangles[triangleIndex++] = d;
            }
        }

        m_mesh.Clear();
        m_mesh.vertices = m_vertices;
        m_mesh.uv = m_uvs;
        m_mesh.triangles = m_triangles;
    }

    /// <summary>
    /// スプラインのノット位置から頂点を更新します。
    /// </summary>
    private void UpdateVertexPositions(UnityEngine.Splines.Spline spline, int knotCount)
    {
        Vector3 referenceUp = transform.InverseTransformDirection(Vector3.up).normalized;
        if (referenceUp.sqrMagnitude < RIGHT_VALID_SQR_LENGTH)
        {
            referenceUp = Vector3.up;
        }

        for (int i = 0; i < knotCount; i++)
        {
            Vector3 knotPosition = spline[i].Position;
            Vector3 tangent = CalculateTangent(spline, i, knotCount, knotPosition);
            Vector3 right = Vector3.Cross(referenceUp, tangent);
            if (right.sqrMagnitude < RIGHT_VALID_SQR_LENGTH)
            {
                right = Vector3.Cross(Vector3.forward, tangent);
            }

            right.Normalize();
            Vector3 up = Vector3.Cross(tangent, right).normalized;
            WriteCrossSectionVertices(i, knotPosition, right, up);
        }
    }

    /// <summary>
    /// 隣接ノットから接線の方向を求めます。
    /// </summary>
    private Vector3 CalculateTangent(UnityEngine.Splines.Spline spline, int index, int knotCount, Vector3 position)
    {
        Vector3 tangent;
        if (index == 0)
        {
            tangent = (Vector3)spline[1].Position - position;
        }
        else if (index == knotCount - 1)
        {
            tangent = position - (Vector3)spline[index - 1].Position;
        }
        else
        {
            tangent = (Vector3)spline[index + 1].Position - (Vector3)spline[index - 1].Position;
        }

        return tangent.sqrMagnitude < TANGENT_VALID_SQR_LENGTH ? Vector3.forward : tangent.normalized;
    }

    /// <summary>
    /// レール1断面分の頂点と法線を書き込みます。
    /// </summary>
    private void WriteCrossSectionVertices(int knotIndex, Vector3 center, Vector3 right, Vector3 up)
    {
        int index = knotIndex * VERTICES_PER_KNOT;
        m_vertices[index] = center + right * m_halfWidth + up * m_halfHeight;
        m_vertices[index + 1] = center - right * m_halfWidth + up * m_halfHeight;
        m_vertices[index + 2] = center - right * m_halfWidth - up * m_halfHeight;
        m_vertices[index + 3] = center + right * m_halfWidth - up * m_halfHeight;

        m_normals[index] = up;
        m_normals[index + 1] = -right;
        m_normals[index + 2] = -up;
        m_normals[index + 3] = right;
    }

    private void ApplyMeshVertices()
    {
        m_mesh.SetVertices(m_vertices);
        m_mesh.SetNormals(m_normals);
        m_mesh.RecalculateBounds();
    }

    /// <summary>
    /// 変更された当たり判定だけを指定間隔で再ベイクします。
    /// </summary>
    private void UpdateColliderIfNeeded()
    {
        if (!m_isColliderDirty || !m_updateCollider || m_meshCollider == null)
        {
            return;
        }

        if (m_lastColliderUpdateFrame >= 0 &&
            Time.frameCount - m_lastColliderUpdateFrame < m_colliderUpdateIntervalFrames)
        {
            return;
        }

        m_isColliderDirty = false;
        m_lastColliderUpdateFrame = Time.frameCount;
        m_meshCollider.sharedMesh = null;

        if (m_mesh.vertexCount > 0)
        {
            m_meshCollider.sharedMesh = m_mesh;
        }
    }
}