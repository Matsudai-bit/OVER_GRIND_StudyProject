using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ハイトマップデータから Marching Cubes 法によってメッシュを構築するクラス。
/// </summary>
public class MarchingCubesMeshBuilder
{
    // ---- 定数 ----
    private const float MIN_EDGE_T = 0.05f;               // 極細三角形による物理引っかかりを防ぐ最小補間値
    private const float DENSITY_EPSILON = 1e-6f;          // ゼロ除算防止用の微小値
    private const int LAYER_GROUND_OFFSET = 2;            // ローカルY=0に対応するY層インデックス

    // キューブの8頂点オフセット（Bourke形式）
    private static readonly Vector3Int[] CORNER_OFFSETS =
    {
        new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(1, 1, 0), new Vector3Int(0, 1, 0),
        new Vector3Int(0, 0, 1), new Vector3Int(1, 0, 1), new Vector3Int(1, 1, 1), new Vector3Int(0, 1, 1)
    };

    // キューブの12辺を構成する頂点インデックス対
    private static readonly int[,] EDGE_CORNERS =
    {
        {0, 1}, {1, 2}, {2, 3}, {3, 0},
        {4, 5}, {5, 6}, {6, 7}, {7, 4},
        {0, 4}, {1, 5}, {2, 6}, {3, 7}
    };

    // 辺の基準点オフセット
    private static readonly Vector3Int[] EDGE_BASE = new Vector3Int[12];
    // 辺が沿う軸（0:X, 1:Y, 2:Z）
    private static readonly int[] EDGE_AXIS = new int[12];

    // ---- メンバー変数 ----
    private readonly int m_gridSize;                      // XZ方向のサンプル点数
    private readonly int m_ny;                            // Y方向のサンプル層数
    private readonly float m_voxelSize;                   // サンプル点の間隔
    private readonly int[] m_edgeCache;                   // 共有頂点インデックスのキャッシュ
    private readonly List<Vector3> m_vertices = new List<Vector3>();
    private readonly List<Vector3> m_normals = new List<Vector3>();
    private readonly List<Vector2> m_uvs = new List<Vector2>();
    private readonly List<int> m_triangles = new List<int>();

    /// <summary>生成された三角形インデックスが存在するかどうか。</summary>
    public bool HasTriangles => m_triangles.Count > 0;

    static MarchingCubesMeshBuilder()
    {
        for (int e = 0; e < 12; e++)
        {
            Vector3Int a = CORNER_OFFSETS[EDGE_CORNERS[e, 0]];
            Vector3Int b = CORNER_OFFSETS[EDGE_CORNERS[e, 1]];
            EDGE_BASE[e] = Vector3Int.Min(a, b);
            EDGE_AXIS[e] = (a.x != b.x) ? 0 : (a.y != b.y) ? 1 : 2;
        }
    }

    /// <summary>
    /// MarchingCubesMeshBuilder を初期化します。
    /// </summary>
    /// <param name="gridSize">XZ方向のサンプル点数。</param>
    /// <param name="ny">Y方向のサンプル層数。</param>
    /// <param name="voxelSize">サンプル点の間隔。</param>
    public MarchingCubesMeshBuilder(int gridSize, int ny, float voxelSize)
    {
        m_gridSize = gridSize;
        m_ny = ny;
        m_voxelSize = voxelSize;
        m_edgeCache = new int[m_gridSize * m_ny * m_gridSize * 3];
    }

    /// <summary>
    /// ハイトマップデータからメッシュを再構築し、指定された描画用 Mesh へ適用します。
    /// </summary>
    /// <param name="heightField">参照するハイトマップデータ。</param>
    /// <param name="targetMesh">更新対象の Mesh。</param>
    public void RebuildMesh(SandHeightField heightField, Mesh targetMesh)
    {
        if (heightField == null || targetMesh == null)
        {
            return;
        }

        m_vertices.Clear();
        m_normals.Clear();
        m_uvs.Clear();
        m_triangles.Clear();
        Array.Fill(m_edgeCache, -1);

        int n = m_gridSize;
        float invVoxel = 1f / m_voxelSize;
        float sink = heightField.Sink;

        for (int x = 0; x < n - 1; x++)
        {
            for (int z = 0; z < n - 1; z++)
            {
                // 4列の表面高さ範囲から調査対象のY層を絞り込む
                float s00 = heightField.GetHeight(x, z) - sink;
                float s10 = heightField.GetHeight(x + 1, z) - sink;
                float s01 = heightField.GetHeight(x, z + 1) - sink;
                float s11 = heightField.GetHeight(x + 1, z + 1) - sink;

                float sMin = Mathf.Min(Mathf.Min(s00, s10), Mathf.Min(s01, s11));
                float sMax = Mathf.Max(Mathf.Max(s00, s10), Mathf.Max(s01, s11));

                int yLo = Mathf.Max(0, Mathf.FloorToInt(sMin * invVoxel) + LAYER_GROUND_OFFSET - 1);
                int yHi = Mathf.Min(m_ny - 2, Mathf.FloorToInt(sMax * invVoxel) + LAYER_GROUND_OFFSET + 1);

                for (int y = yLo; y <= yHi; y++)
                {
                    int cubeIndex = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        Vector3Int c = CORNER_OFFSETS[i];
                        if (EvaluateDensity(heightField, x + c.x, y + c.y, z + c.z) < 0f)
                        {
                            cubeIndex |= 1 << i;
                        }
                    }

                    sbyte[] triRow = MarchingCubesTables.TriTable[cubeIndex];
                    if (triRow.Length == 0)
                    {
                        continue;
                    }

                    for (int k = 0; k < triRow.Length; k += 3)
                    {
                        int i0 = GetOrCreateEdgeVertex(heightField, x, y, z, triRow[k]);
                        int i1 = GetOrCreateEdgeVertex(heightField, x, y, z, triRow[k + 1]);
                        int i2 = GetOrCreateEdgeVertex(heightField, x, y, z, triRow[k + 2]);

                        if (i0 == i1 || i1 == i2 || i0 == i2)
                        {
                            continue;
                        }

                        // 巻き方向を外向き法線と照合して揃える
                        Vector3 faceNormal = Vector3.Cross(
                            m_vertices[i1] - m_vertices[i0],
                            m_vertices[i2] - m_vertices[i0]
                        );
                        Vector3 avgNormal = m_normals[i0] + m_normals[i1] + m_normals[i2];
                        if (Vector3.Dot(faceNormal, avgNormal) < 0f)
                        {
                            int tmp = i1;
                            i1 = i2;
                            i2 = tmp;
                        }

                        m_triangles.Add(i0);
                        m_triangles.Add(i1);
                        m_triangles.Add(i2);
                    }
                }
            }
        }

        targetMesh.Clear();
        targetMesh.SetVertices(m_vertices);
        targetMesh.SetNormals(m_normals);
        targetMesh.SetUVs(0, m_uvs);
        targetMesh.SetTriangles(m_triangles, 0);
        targetMesh.RecalculateBounds();
    }

    /// <summary>
    /// 直近のメッシュ構築結果から、コライダーに必要な頂点座標と三角形インデックスのみをコピーします。
    /// </summary>
    /// <param name="targetColliderMesh">コピー先のコライダー専用 Mesh。</param>
    public void CopyCollisionGeometryTo(Mesh targetColliderMesh)
    {
        if (targetColliderMesh == null)
        {
            return;
        }

        targetColliderMesh.Clear();
        targetColliderMesh.SetVertices(m_vertices);
        targetColliderMesh.SetTriangles(m_triangles, 0);
        targetColliderMesh.RecalculateBounds();
    }

    /// <summary>
    /// 層インデックスをローカルY座標へ変換します。
    /// </summary>
    private float ConvertLayerToLocalY(int y)
    {
        return (y - LAYER_GROUND_OFFSET) * m_voxelSize;
    }

    /// <summary>
    /// 3次元グリッド座標をローカル座標へ変換します。
    /// </summary>
    private Vector3 ConvertGridToLocal(int x, int y, int z)
    {
        float offset = (m_gridSize - 1) * m_voxelSize * 0.5f;
        return new Vector3(x * m_voxelSize - offset, ConvertLayerToLocalY(y), z * m_voxelSize - offset);
    }

    /// <summary>
    /// 指定されたグリッド座標における外向き法線ベクトルを計算します。
    /// </summary>
    private Vector3 CalculateOutwardNormal(SandHeightField heightField, int x, int z)
    {
        float dhdx = (heightField.GetClampedHeight(x + 1, z) - heightField.GetClampedHeight(x - 1, z)) / (2f * m_voxelSize);
        float dhdz = (heightField.GetClampedHeight(x, z + 1) - heightField.GetClampedHeight(x, z - 1)) / (2f * m_voxelSize);
        return new Vector3(-dhdx, 1f, -dhdz);
    }

    /// <summary>
    /// 指定されたグリッド点における密度（表面高さ - Y座標）を評価します。
    /// </summary>
    private float EvaluateDensity(SandHeightField heightField, int x, int y, int z)
    {
        return (heightField.GetHeight(x, z) - heightField.Sink) - ConvertLayerToLocalY(y);
    }

    /// <summary>
    /// 辺上の頂点をキャッシュから取得、または新規生成してインデックスを返します。
    /// </summary>
    private int GetOrCreateEdgeVertex(SandHeightField heightField, int x, int y, int z, int edgeIndex)
    {
        Vector3Int bs = EDGE_BASE[edgeIndex];
        int ax = x + bs.x;
        int ay = y + bs.y;
        int az = z + bs.z;
        int axis = EDGE_AXIS[edgeIndex];

        int key = ((ax * m_ny + ay) * m_gridSize + az) * 3 + axis;
        int cachedIndex = m_edgeCache[key];
        if (cachedIndex >= 0)
        {
            return cachedIndex;
        }

        int bx = ax + (axis == 0 ? 1 : 0);
        int by = ay + (axis == 1 ? 1 : 0);
        int bz = az + (axis == 2 ? 1 : 0);

        float va = EvaluateDensity(heightField, ax, ay, az);
        float vb = EvaluateDensity(heightField, bx, by, bz);
        float t = Mathf.Abs(vb - va) < DENSITY_EPSILON ? 0.5f : -va / (vb - va);
        t = Mathf.Clamp(t, MIN_EDGE_T, 1f - MIN_EDGE_T);

        Vector3 pos = Vector3.Lerp(ConvertGridToLocal(ax, ay, az), ConvertGridToLocal(bx, by, bz), t);
        Vector3 normal = Vector3.Lerp(
            CalculateOutwardNormal(heightField, ax, az),
            CalculateOutwardNormal(heightField, bx, bz),
            t
        ).normalized;

        float totalSize = (m_gridSize - 1) * m_voxelSize;
        float offset = totalSize * 0.5f;
        float u = totalSize > 0f ? (pos.x + offset) / totalSize : 0.5f;
        float v = totalSize > 0f ? (pos.z + offset) / totalSize : 0.5f;

        int index = m_vertices.Count;
        m_vertices.Add(pos);
        m_normals.Add(normal);
        m_uvs.Add(new Vector2(u, v));
        m_edgeCache[key] = index;
        return index;
    }
}