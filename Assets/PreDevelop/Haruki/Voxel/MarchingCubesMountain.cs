using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using System.Collections.Generic;

/// <summary>
/// 砂山（オーバーハング・空洞が絶対に出来ない版）
///
/// ・データは各 (x,z) の「砂の高さ」だけを持つハイトマップ。
///   → 1つの (x,z) に対して表面は1つだけなので、下が空洞になる形は構造上作れない。
/// ・削ると、球の“下側の面”より上の砂がその列ごと取り除かれる（反り返りなし）。
/// ・削った後は安息角（砂が自然に止まる角度）を超える斜面が崩れて、なだらかになる。
/// ・メッシュ生成は Marching Cubes（密度 = 高さ - y）。
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MarchingCubesMountain : MonoBehaviour
{
    [Header("グリッド")]
    public int gridSize = 72;          // XZ方向のサンプル点数
    public float voxelSize = 0.15f;    // サンプル点の間隔(m)
    [Tooltip("砂山の最大の高さ(m)")]
    public float mountainHeight = 3.0f;

    [Header("砂の性質")]
    [Tooltip("安息角(度)。これより急な斜面は崩れる")]
    [Range(10f, 60f)] public float reposeAngle = 33f;
    public bool settleAfterCarve = true;   // 削った後に崩す
    [Range(1, 20)] public int settleIterationsPerFrame = 3;

    [Header("削り")]
    [Range(0f, 2f)] public float carveSmoothness = 0.5f; // 削り跡の縁のなめらかさ(voxelSize単位)

    [Header("パフォーマンス")]
    [Tooltip("MeshColliderを作り直す最小間隔(秒)。見た目のメッシュは毎フレーム更新される")]
    public float colliderUpdateInterval = 0.1f;

    [Header("デバッグ入力（マウスで削る）")]
    public bool enableMouseCarve = false;
    public float mouseCarveRadius = 1.0f;
    public bool holdToCarve = false;

    // ---- データ ----
    private float[,] height;   // 砂の高さ(0以上)。0 = 砂なし
    private float[,] delta;    // 崩し処理用の作業バッファ
    private int ny;            // Y方向のサンプル層数
    private float sink;        // 高さ0の面を床より少し下に沈める量（床とのZファイト防止）
    private bool settling;
    private bool meshDirty;
    private bool colliderDirty;
    private float nextColliderTime;

    private Mesh mesh;
    private MeshCollider meshCollider;
    private readonly List<Vector3> vertices = new List<Vector3>();
    private readonly List<Vector3> normals = new List<Vector3>();
    private readonly List<int> triangles = new List<int>();
    private int[] edgeCache;

    // ---- キューブの定義（Bourke形式）----
    private static readonly Vector3Int[] Corner =
    {
        new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(1, 1, 0), new Vector3Int(0, 1, 0),
        new Vector3Int(0, 0, 1), new Vector3Int(1, 0, 1), new Vector3Int(1, 1, 1), new Vector3Int(0, 1, 1)
    };

    private static readonly int[,] EdgeCorners =
    {
        {0, 1}, {1, 2}, {2, 3}, {3, 0},
        {4, 5}, {5, 6}, {6, 7}, {7, 4},
        {0, 4}, {1, 5}, {2, 6}, {3, 7}
    };

    private static readonly Vector3Int[] EdgeBase = new Vector3Int[12];
    private static readonly int[] EdgeAxis = new int[12];

    static MarchingCubesMountain()
    {
        for (int e = 0; e < 12; e++)
        {
            Vector3Int a = Corner[EdgeCorners[e, 0]];
            Vector3Int b = Corner[EdgeCorners[e, 1]];
            EdgeBase[e] = Vector3Int.Min(a, b);
            EdgeAxis[e] = (a.x != b.x) ? 0 : (a.y != b.y) ? 1 : 2;
        }
    }

    // ------------------------------------------------------------

    void Start()
    {
        mesh = new Mesh { name = "MarchingCubesMountain" };
        mesh.indexFormat = IndexFormat.UInt32;
        GetComponent<MeshFilter>().sharedMesh = mesh;

        meshCollider = GetComponent<MeshCollider>();
        if (meshCollider == null) meshCollider = gameObject.AddComponent<MeshCollider>();

        sink = voxelSize * 0.5f;
        ny = Mathf.CeilToInt(mountainHeight / voxelSize) + 4;
        edgeCache = new int[gridSize * ny * gridSize * 3];

        InitMountain();
        RebuildMesh();
        UpdateCollider();
    }

    // ---- 座標変換 ----
    // 層 y のローカルY。y=2 が local y=0（床の高さ）に相当する
    float LayerY(int y) { return (y - 2) * voxelSize; }

    Vector3 GridToLocal(int x, int y, int z)
    {
        float offset = (gridSize - 1) * voxelSize * 0.5f;
        return new Vector3(x * voxelSize - offset, LayerY(y), z * voxelSize - offset);
    }

    // 初期形状：安息角ぴったりの円錐（先端は少し丸める）
    void InitMountain()
    {
        int n = gridSize;
        height = new float[n, n];
        delta = new float[n, n];

        float center = (n - 1) / 2.0f;
        float tanA = Mathf.Tan(reposeAngle * Mathf.Deg2Rad);

        // 裾野の半径(m)：高さ/tan(安息角)。グリッドに収まらなければ高さのほうを下げる
        float maxRadius = (center - 2.0f) * voxelSize;
        float baseRadius = Mathf.Min(mountainHeight / tanA, maxRadius);
        float peak = baseRadius * tanA + sink; // 頂上の表面が mountainHeight 以下になる

        for (int x = 0; x < n; x++)
        {
            for (int z = 0; z < n; z++)
            {
                float dx = (x - center) * voxelSize;
                float dz = (z - center) * voxelSize;
                float t = Mathf.Sqrt(dx * dx + dz * dz) / baseRadius;
                float t2 = Mathf.Sqrt(t * t + 0.0025f);              // 先端を丸める
                float profile = Mathf.Max(0f, (1f - t2) / (1f - 0.05f));
                height[x, z] = peak * profile;
            }
        }

        ClearBorder();
    }

    // グリッドの外周は砂なし（崩れた砂はここから外へ落ちていく扱い）
    void ClearBorder()
    {
        int n = gridSize;
        for (int i = 0; i < n; i++)
        {
            height[0, i] = 0f; height[n - 1, i] = 0f;
            height[i, 0] = 0f; height[i, n - 1] = 0f;
        }
    }

    // ------------------------------------------------------------
    // 削り
    // ------------------------------------------------------------

    static float SmoothMin(float a, float b, float k)
    {
        float h = Mathf.Max(k - Mathf.Abs(a - b), 0f) / k;
        return Mathf.Min(a, b) - h * h * k * 0.25f;
    }

    /// <summary>球の中心を worldPoint に置いて削る（深さ = 半径）</summary>
    public void Carve(Vector3 worldPoint, float worldRadius)
    {
        Carve(worldPoint, worldRadius, worldRadius);
    }

    /// <summary>
    /// worldPoint（砂の表面上の接触点）から depth だけ掘り下げる。
    /// 球の底が「接触点 - depth」に来るように球を配置するので、半径が大きくても浅く広く削れる。
    /// 多数回呼んでもメッシュは1フレームに1回しか作り直されない。
    /// </summary>
    public void Carve(Vector3 worldPoint, float worldRadius, float worldDepth)
    {
        float scale = Mathf.Abs(transform.lossyScale.x);
        float radius = worldRadius / scale;
        float depth = Mathf.Min(worldDepth / scale, radius);
        Vector3 localPos = transform.InverseTransformPoint(worldPoint) + Vector3.up * (radius - depth);
        float k = carveSmoothness * voxelSize;

        float offset = (gridSize - 1) * voxelSize * 0.5f;
        int cx = Mathf.RoundToInt((localPos.x + offset) / voxelSize);
        int cz = Mathf.RoundToInt((localPos.z + offset) / voxelSize);
        int r = Mathf.CeilToInt(radius / voxelSize) + 1;
        int n = gridSize;
        bool modified = false;

        for (int x = Mathf.Max(1, cx - r); x <= Mathf.Min(n - 2, cx + r); x++)
        {
            for (int z = Mathf.Max(1, cz - r); z <= Mathf.Min(n - 2, cz + r); z++)
            {
                float dx = (x * voxelSize - offset) - localPos.x;
                float dz = (z * voxelSize - offset) - localPos.z;
                float d2 = dx * dx + dz * dz;
                if (d2 >= radius * radius) continue;

                // この列における球の「下側の面」の高さ。それより上の砂は列ごと消える
                float bottom = localPos.y - Mathf.Sqrt(radius * radius - d2);
                float surface = height[x, z] - sink;
                float next = (k > 1e-5f) ? SmoothMin(surface, bottom, k) : Mathf.Min(surface, bottom);
                float newHeight = Mathf.Max(0f, next + sink);

                if (newHeight < height[x, z] - 1e-5f)
                {
                    height[x, z] = newHeight;
                    modified = true;
                }
            }
        }

        if (modified)
        {
            meshDirty = true; // 実際の再構築は LateUpdate で1回だけ
            if (settleAfterCarve) settling = true;
        }
    }

    // ------------------------------------------------------------
    // 崩れ（安息角）
    // ------------------------------------------------------------

    static readonly int[] NbX = { 1, -1, 0, 0, 1, 1, -1, -1 };
    static readonly int[] NbZ = { 0, 0, 1, -1, 1, -1, 1, -1 };

    // 1ステップ崩す。何か動いたら true
    bool SettleStep()
    {
        int n = gridSize;
        float tanA = Mathf.Tan(reposeAngle * Mathf.Deg2Rad);
        const float rate = 0.12f;
        bool moved = false;

        System.Array.Clear(delta, 0, delta.Length);

        for (int x = 1; x < n - 1; x++)
        {
            for (int z = 1; z < n - 1; z++)
            {
                float h = height[x, z];
                if (h <= 0f) continue;

                for (int i = 0; i < 8; i++)
                {
                    int nx = x + NbX[i], nz = z + NbZ[i];
                    float dist = voxelSize * (i < 4 ? 1f : 1.41421356f);
                    float excess = (h - height[nx, nz]) - tanA * dist;

                    if (excess > 1e-3f)
                    {
                        float t = excess * rate;
                        delta[x, z] -= t;
                        delta[nx, nz] += t;
                        moved = true;
                    }
                }
            }
        }

        if (!moved) return false;

        for (int x = 0; x < n; x++)
            for (int z = 0; z < n; z++)
                height[x, z] = Mathf.Max(0f, height[x, z] + delta[x, z]);

        ClearBorder();
        return true;
    }

    // ------------------------------------------------------------
    // Marching Cubes
    // ------------------------------------------------------------

    float H(int x, int z)
    {
        int n = gridSize - 1;
        return height[Mathf.Clamp(x, 0, n), Mathf.Clamp(z, 0, n)];
    }

    // 密度 = (高さ - sink) - y。外向き法線 = -勾配 = (-dh/dx, 1, -dh/dz)
    Vector3 OutwardNormal(int x, int z)
    {
        float dhdx = (H(x + 1, z) - H(x - 1, z)) / (2f * voxelSize);
        float dhdz = (H(x, z + 1) - H(x, z - 1)) / (2f * voxelSize);
        return new Vector3(-dhdx, 1f, -dhdz);
    }

    float Density(int x, int y, int z)
    {
        return (height[x, z] - sink) - LayerY(y);
    }

    int GetEdgeVertex(int x, int y, int z, int e)
    {
        int n = gridSize;
        Vector3Int bs = EdgeBase[e];
        int ax = x + bs.x, ay = y + bs.y, az = z + bs.z;
        int axis = EdgeAxis[e];

        int key = ((ax * ny + ay) * n + az) * 3 + axis;
        int cached = edgeCache[key];
        if (cached >= 0) return cached;

        int bx = ax + (axis == 0 ? 1 : 0);
        int by = ay + (axis == 1 ? 1 : 0);
        int bz = az + (axis == 2 ? 1 : 0);

        float va = Density(ax, ay, az);
        float vb = Density(bx, by, bz);
        float t = Mathf.Abs(vb - va) < 1e-6f ? 0.5f : -va / (vb - va);
        t = Mathf.Clamp01(t);

        Vector3 pos = Vector3.Lerp(GridToLocal(ax, ay, az), GridToLocal(bx, by, bz), t);
        Vector3 nrm = Vector3.Lerp(OutwardNormal(ax, az), OutwardNormal(bx, bz), t).normalized;

        int index = vertices.Count;
        vertices.Add(pos);
        normals.Add(nrm);
        edgeCache[key] = index;
        return index;
    }

    void RebuildMesh()
    {
        vertices.Clear();
        normals.Clear();
        triangles.Clear();
        System.Array.Fill(edgeCache, -1);

        int n = gridSize;
        float invV = 1f / voxelSize;

        for (int x = 0; x < n - 1; x++)
        {
            for (int z = 0; z < n - 1; z++)
            {
                // この4列の表面の高さの範囲から、調べるべき層だけに絞る（高速化）
                float s00 = height[x, z] - sink, s10 = height[x + 1, z] - sink;
                float s01 = height[x, z + 1] - sink, s11 = height[x + 1, z + 1] - sink;
                float sMin = Mathf.Min(Mathf.Min(s00, s10), Mathf.Min(s01, s11));
                float sMax = Mathf.Max(Mathf.Max(s00, s10), Mathf.Max(s01, s11));

                int yLo = Mathf.Max(0, Mathf.FloorToInt(sMin * invV) + 2 - 1);
                int yHi = Mathf.Min(ny - 2, Mathf.FloorToInt(sMax * invV) + 2 + 1);

                for (int y = yLo; y <= yHi; y++)
                {
                    int cubeIndex = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        Vector3Int c = Corner[i];
                        if (Density(x + c.x, y + c.y, z + c.z) < 0f) cubeIndex |= 1 << i;
                    }

                    sbyte[] row = MarchingCubesTables.TriTable[cubeIndex];
                    if (row.Length == 0) continue;

                    for (int k = 0; k < row.Length; k += 3)
                    {
                        int i0 = GetEdgeVertex(x, y, z, row[k]);
                        int i1 = GetEdgeVertex(x, y, z, row[k + 1]);
                        int i2 = GetEdgeVertex(x, y, z, row[k + 2]);
                        if (i0 == i1 || i1 == i2 || i0 == i2) continue;

                        // 巻き方向を外向き法線と照合して揃える
                        Vector3 faceN = Vector3.Cross(vertices[i1] - vertices[i0], vertices[i2] - vertices[i0]);
                        Vector3 avgN = normals[i0] + normals[i1] + normals[i2];
                        if (Vector3.Dot(faceN, avgN) < 0f)
                        {
                            int tmp = i1; i1 = i2; i2 = tmp;
                        }

                        triangles.Add(i0);
                        triangles.Add(i1);
                        triangles.Add(i2);
                    }
                }
            }
        }

        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
    }

    void UpdateCollider()
    {
        meshCollider.sharedMesh = null;
        if (triangles.Count > 0) meshCollider.sharedMesh = mesh;
    }

    // ------------------------------------------------------------

    void Update()
    {
        // 崩し処理：安定するまで毎フレーム少しずつ進める
        if (settling)
        {
            bool moved = false;
            for (int i = 0; i < settleIterationsPerFrame; i++)
            {
                if (SettleStep()) moved = true;
                else break;
            }

            if (moved) meshDirty = true;
            else settling = false;
        }

        if (enableMouseCarve) HandleMouse();
    }

    void LateUpdate()
    {
        // 見た目のメッシュ：変更があれば1フレームに1回だけ作り直す
        if (meshDirty)
        {
            RebuildMesh();
            meshDirty = false;
            colliderDirty = true;
        }

        // コライダー：作り直しは高コストなので間隔を空ける
        if (colliderDirty && Time.time >= nextColliderTime)
        {
            UpdateCollider();
            colliderDirty = false;
            nextColliderTime = Time.time + colliderUpdateInterval;
        }
    }

    void HandleMouse()
    {
        if (Mouse.current == null || Camera.main == null) return;

        bool pressed = holdToCarve
            ? Mouse.current.leftButton.isPressed
            : Mouse.current.leftButton.wasPressedThisFrame;
        if (!pressed) return;

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.gameObject == gameObject)
        {
            Carve(hit.point, mouseCarveRadius);
        }
    }
}