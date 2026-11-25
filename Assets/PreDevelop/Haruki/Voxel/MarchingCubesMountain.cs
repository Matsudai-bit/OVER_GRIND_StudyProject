using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// オーバーハングや空洞ができないハイトマップベースの砂山コンポーネント。
/// XZスケールとボクセル密度による直感的なサイズ指定、チャンク最適化、シーンビュープレビューに対応します。
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MarchingCubesMountain : MonoBehaviour
{
    // ---- チャンク管理用クラス ----
    private class SandChunk
    {
        public GameObject m_gameObject;
        public MeshFilter m_meshFilter;
        public MeshRenderer m_meshRenderer;
        public MeshCollider m_meshCollider;
        public Mesh m_visualMesh;
        public Mesh[] m_colliderMeshes;
        public int m_activeColliderIndex;
        public int m_startX;
        public int m_endX;
        public int m_startZ;
        public int m_endZ;
        public bool m_isMeshDirty;
        public bool m_isColliderDirty;
        public bool m_colliderDeferredOnce;
        public float m_nextColliderTime;
    }

    // ---- 計算済みグリッドパラメータ構造体 ----
    private struct GridParameters
    {
        public float m_scaleX;
        public float m_scaleZ;
        public float m_mountainHeight;
        public float m_voxelDensity;
        public int m_cellCountX;
        public int m_cellCountZ;
        public int m_gridSizeX;
        public int m_gridSizeZ;
        public float m_voxelSizeX;
        public float m_voxelSizeY;
        public float m_voxelSizeZ;
        public float m_sink;
        public int m_ny;
    }

    // ---- 定数 ----
    private const float MIN_SCALE_XZ = 1.0f;                  // XZ軸スケールの最小値(m)
    private const float MIN_VOXEL_DENSITY = 1.0f;             // ボクセル密度の最小値(1mあたりの分割数)
    private const float MAX_VOXEL_DENSITY = 20.0f;            // ボクセル密度の最大値(1mあたりの分割数)
    private const int MIN_CELLS = 2;                          // 1軸あたりの最小セル数
    private const int MAX_CELLS_PER_AXIS = 256;               // 1軸あたりの安全上限セル数
    private const float MIN_MOUNTAIN_HEIGHT = 0.1f;           // 最小の高さ(m)
    private const float MIN_SCALE = 1e-5f;                    // スケールゼロ除算防止用の最小値
    private const float SINK_VOXEL_RATIO = 0.5f;              // ボクセルサイズに対する沈め量の割合
    private const int EXTRA_Y_LAYERS = 4;                     // Y方向の余裕層数
    private const int CHUNK_CELL_SIZE = 16;                   // 1チャンクあたりのXZセル数
    private const int COLLIDER_MESH_COUNT = 2;                // コライダーダブルバッファ用のメッシュ数
    private const float UNSELECTED_GIZMO_ALPHA = 0.3f;        // 非選択時ギズモの不透明度
    private const float SELECTED_GIZMO_ALPHA = 0.9f;          // 選択時ギズモの不透明度

    // ---- 静的メンバー ----
    private static readonly List<MarchingCubesMountain> m_instances = new List<MarchingCubesMountain>();

    /// <summary>シーン内の有効な砂山の一覧。</summary>
    public static IReadOnlyList<MarchingCubesMountain> Instances => m_instances;

    // ---- Inspector 設定項目 ----
    [Header("サイズ・解像度")]
    [Tooltip("砂山のXZ軸のサイズ(m)")]
    [SerializeField] private Vector2 m_scaleXZ = new Vector2(10f, 10f);
    [Tooltip("1mあたりのボクセル分割数。値が大きいほど高精細になります")]
    [Range(1f, 20f)]
    [SerializeField] private float m_voxelDensity = 6.5f;
    [Tooltip("砂山の最大の高さ(m)")]
    [SerializeField] private float m_mountainHeight = 3.0f;

    [Header("初期形状のカスタム設定")]
    [Tooltip("初期形状の生成方式")]
    [SerializeField] private SandHeightField.SandShapeType m_initShapeType = SandHeightField.SandShapeType.CONE;
    [Tooltip("HEIGHTMAP_TEXTUREモード時の入力画像（白＝最高地点、黒＝砂なし）")]
    [SerializeField] private Texture2D m_initialHeightmap;

    [Header("砂の性質")]
    [Tooltip("安息角(度)。これより急な斜面は崩れる")]
    [Range(10f, 60f)]
    [SerializeField] private float m_reposeAngle = 33f;
    [Tooltip("削った後に安息角まで崩すかどうか")]
    [SerializeField] private bool m_settleAfterCarve = true;
    [Tooltip("1フレームあたりに実行する崩し反復回数")]
    [Range(1, 20)]
    [SerializeField] private int m_settleIterationsPerFrame = 3;

    [Header("削り")]
    [Tooltip("削り跡の縁のなめらかさ(ボクセル間隔単位)")]
    [Range(0f, 2f)]
    [SerializeField] private float m_carveSmoothness = 0.5f;

    [Header("パフォーマンス・コライダー更新")]
    [Tooltip("チャンクのMeshColliderを作り直す最小間隔(秒)")]
    [Min(0.05f)]
    [SerializeField] private float m_colliderUpdateInterval = 0.2f;
    [Tooltip("1フレームあたりに再ベイクするコライダーチャンク数の上限（スパイク防止）")]
    [Range(1, 16)]
    [SerializeField] private int m_maxColliderUpdatesPerFrame = 2;
    [Tooltip("ONにすると、崩れが完全に落ち着くまでコライダーの再構築を遅らせ、歩行中のかくつきを防ぐ")]
    [SerializeField] private bool m_deferColliderDuringSettle = false;
    [Tooltip("崩れ中(settling)のコライダー更新間隔(秒)。m_deferColliderDuringSettle が OFF の場合に使用")]
    [Min(0.1f)]
    [SerializeField] private float m_settleColliderUpdateInterval = 0.5f;

    [Header("シーンビュー表示（エディタ用）")]
    [Tooltip("ONにすると、編集中のシーンビューに実際の砂山形状をプレビュー表示する")]
    [SerializeField] private bool m_showEditorPreview = true;
    [Tooltip("ONにすると、シーンビューに砂山の全体サイズ枠（ギズモ）を表示する")]
    [SerializeField] private bool m_showBoundsGizmo = true;
    [Tooltip("シーンビューに表示するサイズ枠の色")]
    [SerializeField] private Color m_boundsGizmoColor = new Color(0.95f, 0.78f, 0.35f, 1f);

    // ---- 内部データ ----
    private SandHeightField m_heightField;          // ハイトマップ・シミュレーション担当
    private MarchingCubesMeshBuilder m_meshBuilder; // チャンクメッシュ構築担当
    private SandChunk[,] m_chunks;                  // チャンクの2次元配列
    private int m_chunkCountX;                      // X方向のチャンク数
    private int m_chunkCountZ;                      // Z方向のチャンク数
    private GridParameters m_gridParams;            // 実行時の確定グリッドパラメータ
    private MeshRenderer m_parentMeshRenderer;      // 親メッシュレンダラー（設定継承元）
    private bool m_isSettling;                      // 崩れ処理中フラグ
    private bool m_hasDirtyChunks;                  // 更新待ちのチャンクがあるか
    private Bounds m_localBounds;                   // 砂山全体のローカルバウンディングボックス
    private readonly List<Vector3> m_tempVertices = new List<Vector3>(); // コライダーコピー用の一時バッファ
    private readonly List<int> m_tempTriangles = new List<int>();        // コライダーコピー用の一時バッファ

#if UNITY_EDITOR
    private Mesh m_previewMesh;                     // エディタ上のシーンビュー表示用メッシュ
    private bool m_previewUpdateQueued;             // エディタプレビュー更新予約フラグ
#endif

    // ---- イベント ----
    /// <summary>
    /// 砂が削られたときに呼ばれます。
    /// 引数: 削った中心位置(ワールド) / 表面法線 / 削り跡の半径(m) / 取り除いた砂の体積(m³)
    /// </summary>
    public event Action<Vector3, Vector3, float, float> Carved;

    /// <summary>
    /// 崩れ(安息角)で砂が斜面を滑り落ちたときに呼ばれます。
    /// 引数: 位置(ワールド) / 滑り落ちる向き(ワールド・水平) / 動いた砂の体積(m³)
    /// </summary>
    public event Action<Vector3, Vector3, float> SandSlid;

    // ---- 公開プロパティ ----
    /// <summary>砂山のワールド空間でのバウンディングボックス。</summary>
    public Bounds WorldBounds => CalculateWorldBounds();

    /// <summary>サンプル点の間隔（ワールド単位の最小間隔）。</summary>
    public float WorldVoxelSize
    {
        get
        {
            float baseSize = m_gridParams.m_voxelSizeY > 0f
                ? Mathf.Min(m_gridParams.m_voxelSizeX, m_gridParams.m_voxelSizeZ)
                : 1f / Mathf.Clamp(m_voxelDensity, MIN_VOXEL_DENSITY, MAX_VOXEL_DENSITY);
            return baseSize * Mathf.Abs(transform.lossyScale.x);
        }
    }

    // ---- Unity ライフサイクル ----
    private void Awake()
    {
        InitializeMountain();
    }

    private void OnEnable()
    {
        if (!m_instances.Contains(this))
        {
            m_instances.Add(this);
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            ScheduleEditorPreviewUpdate();
        }
#endif
    }

    private void OnDisable()
    {
        m_instances.Remove(this);

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            ClearEditorPreview();
        }
#endif
    }

    private void Update()
    {
        if (!m_isSettling)
        {
            return;
        }

        SimulateSettle();
    }

    private void LateUpdate()
    {
        if (!m_hasDirtyChunks || m_chunks == null)
        {
            return;
        }

        bool anyRemainingDirty = false;
        int colliderUpdatesThisFrame = 0;
        float currentTime = Time.time;

        for (int cx = 0; cx < m_chunkCountX; cx++)
        {
            for (int cz = 0; cz < m_chunkCountZ; cz++)
            {
                SandChunk chunk = m_chunks[cx, cz];
                bool rebuiltMeshThisFrame = false;

                if (chunk.m_isMeshDirty)
                {
                    m_meshBuilder.RebuildChunkMesh(
                        m_heightField,
                        chunk.m_startX,
                        chunk.m_endX,
                        chunk.m_startZ,
                        chunk.m_endZ,
                        chunk.m_visualMesh
                    );
                    chunk.m_isMeshDirty = false;
                    chunk.m_isColliderDirty = true;
                    rebuiltMeshThisFrame = true;
                }

                if (chunk.m_isColliderDirty)
                {
                    bool canUpdate = !(m_deferColliderDuringSettle && m_isSettling)
                                  && currentTime >= chunk.m_nextColliderTime
                                  && colliderUpdatesThisFrame < m_maxColliderUpdatesPerFrame;

                    if (canUpdate)
                    {
                        if (rebuiltMeshThisFrame && !chunk.m_colliderDeferredOnce)
                        {
                            chunk.m_colliderDeferredOnce = true;
                        }
                        else
                        {
                            UpdateChunkCollider(chunk);
                            chunk.m_isColliderDirty = false;
                            chunk.m_colliderDeferredOnce = false;
                            float interval = m_isSettling ? m_settleColliderUpdateInterval : m_colliderUpdateInterval;
                            chunk.m_nextColliderTime = currentTime + interval;
                            colliderUpdatesThisFrame++;
                        }
                    }

                    if (chunk.m_isColliderDirty)
                    {
                        anyRemainingDirty = true;
                    }
                }
            }
        }

        m_hasDirtyChunks = anyRemainingDirty;
    }

    private void OnDestroy()
    {
        DestroyChunks();

#if UNITY_EDITOR
        ClearEditorPreview();
#endif
    }

    // ---- 公開メソッド ----
    /// <summary>
    /// 砂山を指定されたパラメータで初期化・再生成します。
    /// </summary>
    public void InitializeMountain()
    {
#if UNITY_EDITOR
        ClearEditorPreview();
#endif

        DestroyChunks();

        m_parentMeshRenderer = GetComponent<MeshRenderer>();
        if (m_parentMeshRenderer != null)
        {
            m_parentMeshRenderer.enabled = false;
        }

        MeshFilter parentFilter = GetComponent<MeshFilter>();
        if (parentFilter != null)
        {
            parentFilter.sharedMesh = null;
        }

        MeshCollider parentCollider = GetComponent<MeshCollider>();
        if (parentCollider != null)
        {
            parentCollider.enabled = false;
        }

        m_gridParams = ComputeGridParameters();
        m_localBounds = CalculateLocalBounds(m_gridParams);

        m_heightField = new SandHeightField(
            m_gridParams.m_gridSizeX,
            m_gridParams.m_gridSizeZ,
            m_gridParams.m_voxelSizeX,
            m_gridParams.m_voxelSizeZ,
            m_gridParams.m_mountainHeight,
            m_gridParams.m_sink
        );
        m_heightField.InitializeShape(m_initShapeType, m_initialHeightmap, m_reposeAngle);

        m_meshBuilder = new MarchingCubesMeshBuilder(
            m_gridParams.m_gridSizeX,
            m_gridParams.m_gridSizeZ,
            m_gridParams.m_ny,
            m_gridParams.m_voxelSizeX,
            m_gridParams.m_voxelSizeY,
            m_gridParams.m_voxelSizeZ,
            CHUNK_CELL_SIZE,
            CHUNK_CELL_SIZE
        );
        CreateChunks();

        m_isSettling = false;
        m_hasDirtyChunks = false;
    }

    /// <summary>
    /// 指定されたワールド座標のXZ位置にある砂の表面の点と法線を返します。
    /// </summary>
    /// <returns>
    /// true：グリッド範囲内で表面をサンプリングできました。
    /// false：グリッド範囲外、または初期化前です。
    /// </returns>
    public bool TrySampleSurface(Vector3 worldPos, out Vector3 surfacePoint, out Vector3 surfaceNormal)
    {
        surfacePoint = worldPos;
        surfaceNormal = Vector3.up;

        if (m_heightField == null)
        {
            return false;
        }

        Vector3 localPos = transform.InverseTransformPoint(worldPos);
        if (!m_heightField.TrySampleSurface(localPos, out float surfaceY, out Vector3 localNormal))
        {
            return false;
        }

        surfacePoint = transform.TransformPoint(new Vector3(localPos.x, surfaceY, localPos.z));
        surfaceNormal = transform.TransformDirection(localNormal).normalized;
        return true;
    }

    /// <summary>
    /// ローカル座標のXZ位置における表面のローカルY座標のみを高速に取得します。
    /// </summary>
    /// <returns>
    /// true：グリッド範囲内で取得できました。
    /// false：グリッド範囲外、または初期化前です。
    /// </returns>
    public bool TrySampleLocalHeight(float localX, float localZ, out float localSurfaceY)
    {
        if (m_heightField == null)
        {
            localSurfaceY = 0f;
            return false;
        }

        return m_heightField.TrySampleHeight(localX, localZ, out localSurfaceY);
    }

    /// <summary>
    /// ローカル座標のXZ位置における表面のローカル法線を計算します。
    /// </summary>
    public Vector3 CalculateLocalNormal(float localX, float localZ)
    {
        if (m_heightField == null)
        {
            return Vector3.up;
        }

        return m_heightField.CalculateLocalNormal(localX, localZ);
    }

    /// <summary>
    /// 球の中心をワールド座標に置いて砂を削ります（深さ = 半径）。
    /// </summary>
    public void Carve(Vector3 worldPoint, float worldRadius)
    {
        Carve(worldPoint, worldRadius, worldRadius);
    }

    /// <summary>
    /// 接触点から指定された深さだけ砂を掘り下げます。
    /// </summary>
    public void Carve(Vector3 worldPoint, float worldRadius, float worldDepth)
    {
        if (m_heightField == null)
        {
            return;
        }

        float scale = Mathf.Max(MIN_SCALE, Mathf.Abs(transform.lossyScale.x));
        float radius = worldRadius / scale;
        float depth = Mathf.Min(worldDepth / scale, radius);
        if (radius <= 0f || depth <= 0f)
        {
            return;
        }

        Vector3 surfaceNormal = Vector3.up;
        if (Carved != null)
        {
            TrySampleSurface(worldPoint, out _, out surfaceNormal);
        }

        Vector3 localPos = transform.InverseTransformPoint(worldPoint) + Vector3.up * (radius - depth);
        float smoothK = m_carveSmoothness * m_gridParams.m_voxelSizeY;

        if (m_heightField.Carve(
            localPos,
            radius,
            smoothK,
            out float removedHeight,
            out int modMinX,
            out int modMaxX,
            out int modMinZ,
            out int modMaxZ))
        {
            MarkChunksDirtyInGridRect(modMinX, modMaxX, modMinZ, modMaxZ);

            if (m_settleAfterCarve)
            {
                m_isSettling = true;
            }

            if (Carved != null)
            {
                float volume = removedHeight * m_gridParams.m_voxelSizeX * m_gridParams.m_voxelSizeZ * scale * scale * scale;
                float footprint = Mathf.Sqrt(Mathf.Max(0f, depth * (2f * radius - depth))) * scale;
                Carved.Invoke(worldPoint, surfaceNormal, footprint, volume);
            }
        }
    }

    // ---- 内部処理 ----
    /// <summary>
    /// XZスケールとボクセル密度から、グリッド解像度と各軸のボクセルサイズを算出します。
    /// </summary>
    private GridParameters ComputeGridParameters()
    {
        float scaleX = Mathf.Max(MIN_SCALE_XZ, m_scaleXZ.x);
        float scaleZ = Mathf.Max(MIN_SCALE_XZ, m_scaleXZ.y);
        float density = Mathf.Clamp(m_voxelDensity, MIN_VOXEL_DENSITY, MAX_VOXEL_DENSITY);
        float mountainHeight = Mathf.Max(MIN_MOUNTAIN_HEIGHT, m_mountainHeight);

        int cellCountX = Mathf.Clamp(Mathf.RoundToInt(scaleX * density), MIN_CELLS, MAX_CELLS_PER_AXIS);
        int cellCountZ = Mathf.Clamp(Mathf.RoundToInt(scaleZ * density), MIN_CELLS, MAX_CELLS_PER_AXIS);

        float voxelSizeX = scaleX / cellCountX;
        float voxelSizeZ = scaleZ / cellCountZ;
        float voxelSizeY = 1f / density;
        float sink = voxelSizeY * SINK_VOXEL_RATIO;
        int ny = Mathf.CeilToInt(mountainHeight / voxelSizeY) + EXTRA_Y_LAYERS;

        return new GridParameters
        {
            m_scaleX = scaleX,
            m_scaleZ = scaleZ,
            m_mountainHeight = mountainHeight,
            m_voxelDensity = density,
            m_cellCountX = cellCountX,
            m_cellCountZ = cellCountZ,
            m_gridSizeX = cellCountX + 1,
            m_gridSizeZ = cellCountZ + 1,
            m_voxelSizeX = voxelSizeX,
            m_voxelSizeY = voxelSizeY,
            m_voxelSizeZ = voxelSizeZ,
            m_sink = sink,
            m_ny = ny
        };
    }

    /// <summary>
    /// 砂山全体のローカルバウンディングボックスを計算します。
    /// </summary>
    private static Bounds CalculateLocalBounds(GridParameters gridParams)
    {
        return new Bounds(
            new Vector3(0f, (gridParams.m_mountainHeight - gridParams.m_sink) * 0.5f, 0f),
            new Vector3(gridParams.m_scaleX, gridParams.m_mountainHeight + gridParams.m_sink, gridParams.m_scaleZ)
        );
    }

    /// <summary>
    /// チャンクオブジェクトと初期メッシュ・コライダーを生成します。
    /// </summary>
    private void CreateChunks()
    {
        int cellCountX = m_gridParams.m_cellCountX;
        int cellCountZ = m_gridParams.m_cellCountZ;
        m_chunkCountX = Mathf.CeilToInt(cellCountX / (float)CHUNK_CELL_SIZE);
        m_chunkCountZ = Mathf.CeilToInt(cellCountZ / (float)CHUNK_CELL_SIZE);
        m_chunks = new SandChunk[m_chunkCountX, m_chunkCountZ];

        Material[] materials = m_parentMeshRenderer != null ? m_parentMeshRenderer.sharedMaterials : null;
        ShadowCastingMode shadowMode = m_parentMeshRenderer != null ? m_parentMeshRenderer.shadowCastingMode : ShadowCastingMode.On;
        bool receiveShadows = m_parentMeshRenderer == null || m_parentMeshRenderer.receiveShadows;
        LightProbeUsage lightProbes = m_parentMeshRenderer != null ? m_parentMeshRenderer.lightProbeUsage : LightProbeUsage.BlendProbes;
        ReflectionProbeUsage reflectionProbes = m_parentMeshRenderer != null ? m_parentMeshRenderer.reflectionProbeUsage : ReflectionProbeUsage.BlendProbes;

        for (int cx = 0; cx < m_chunkCountX; cx++)
        {
            int startX = cx * CHUNK_CELL_SIZE;
            int endX = Mathf.Min(startX + CHUNK_CELL_SIZE, cellCountX);

            for (int cz = 0; cz < m_chunkCountZ; cz++)
            {
                int startZ = cz * CHUNK_CELL_SIZE;
                int endZ = Mathf.Min(startZ + CHUNK_CELL_SIZE, cellCountZ);

                GameObject go = new GameObject($"SandChunk_{cx}_{cz}");
                go.layer = gameObject.layer;
                go.transform.SetParent(transform, false);

                MeshFilter mf = go.AddComponent<MeshFilter>();
                MeshRenderer mr = go.AddComponent<MeshRenderer>();
                MeshCollider mc = go.AddComponent<MeshCollider>();

                if (materials != null)
                {
                    mr.sharedMaterials = materials;
                }
                mr.shadowCastingMode = shadowMode;
                mr.receiveShadows = receiveShadows;
                mr.lightProbeUsage = lightProbes;
                mr.reflectionProbeUsage = reflectionProbes;

                Mesh visualMesh = new Mesh { name = $"SandChunk_Visual_{cx}_{cz}" };
                visualMesh.indexFormat = IndexFormat.UInt32;
                mf.sharedMesh = visualMesh;

                Mesh[] colliderMeshes = new Mesh[COLLIDER_MESH_COUNT];
                for (int i = 0; i < COLLIDER_MESH_COUNT; i++)
                {
                    colliderMeshes[i] = new Mesh { name = $"SandChunk_Col_{cx}_{cz}_{i}" };
                    colliderMeshes[i].indexFormat = IndexFormat.UInt32;
                }

                SandChunk chunk = new SandChunk
                {
                    m_gameObject = go,
                    m_meshFilter = mf,
                    m_meshRenderer = mr,
                    m_meshCollider = mc,
                    m_visualMesh = visualMesh,
                    m_colliderMeshes = colliderMeshes,
                    m_startX = startX,
                    m_endX = endX,
                    m_startZ = startZ,
                    m_endZ = endZ,
                    m_nextColliderTime = Time.time
                };

                m_meshBuilder.RebuildChunkMesh(m_heightField, startX, endX, startZ, endZ, visualMesh);
                UpdateChunkCollider(chunk);

                m_chunks[cx, cz] = chunk;
            }
        }
    }

    /// <summary>
    /// 生成済みのチャンクと動的メッシュをすべて破棄します。
    /// </summary>
    private void DestroyChunks()
    {
        if (m_chunks == null)
        {
            return;
        }

        for (int cx = 0; cx < m_chunks.GetLength(0); cx++)
        {
            for (int cz = 0; cz < m_chunks.GetLength(1); cz++)
            {
                SandChunk chunk = m_chunks[cx, cz];
                if (chunk == null)
                {
                    continue;
                }

                if (chunk.m_visualMesh != null)
                {
                    Destroy(chunk.m_visualMesh);
                }

                if (chunk.m_colliderMeshes != null)
                {
                    for (int i = 0; i < chunk.m_colliderMeshes.Length; i++)
                    {
                        if (chunk.m_colliderMeshes[i] != null)
                        {
                            Destroy(chunk.m_colliderMeshes[i]);
                        }
                    }
                }

                if (chunk.m_gameObject != null)
                {
                    Destroy(chunk.m_gameObject);
                }
            }
        }

        m_chunks = null;
    }

    /// <summary>
    /// グリッド点の変化範囲（法線影響範囲を含む）に対応するチャンクへダーティフラグを立てます。
    /// </summary>
    private void MarkChunksDirtyInGridRect(int gridMinX, int gridMaxX, int gridMinZ, int gridMaxZ)
    {
        if (m_chunks == null || gridMinX > gridMaxX || gridMinZ > gridMaxZ)
        {
            return;
        }

        int maxCellX = m_gridParams.m_cellCountX - 1;
        int maxCellZ = m_gridParams.m_cellCountZ - 1;
        int cellMinX = Mathf.Clamp(gridMinX - 2, 0, maxCellX);
        int cellMaxX = Mathf.Clamp(gridMaxX + 1, 0, maxCellX);
        int cellMinZ = Mathf.Clamp(gridMinZ - 2, 0, maxCellZ);
        int cellMaxZ = Mathf.Clamp(gridMaxZ + 1, 0, maxCellZ);

        int cMinX = cellMinX / CHUNK_CELL_SIZE;
        int cMaxX = cellMaxX / CHUNK_CELL_SIZE;
        int cMinZ = cellMinZ / CHUNK_CELL_SIZE;
        int cMaxZ = cellMaxZ / CHUNK_CELL_SIZE;

        for (int cx = cMinX; cx <= cMaxX; cx++)
        {
            for (int cz = cMinZ; cz <= cMaxZ; cz++)
            {
                m_chunks[cx, cz].m_isMeshDirty = true;
            }
        }

        m_hasDirtyChunks = true;
    }

    /// <summary>
    /// 1フレーム分の崩れシミュレーションを実行します。
    /// </summary>
    private void SimulateSettle()
    {
        if (m_heightField == null)
        {
            m_isSettling = false;
            return;
        }

        bool moved = false;
        int totalMinX = int.MaxValue;
        int totalMaxX = int.MinValue;
        int totalMinZ = int.MaxValue;
        int totalMaxZ = int.MinValue;
        Action<Vector3, Vector3, float> slideCallback = SandSlid != null ? HandleSandSlidLocal : null;

        for (int i = 0; i < m_settleIterationsPerFrame; i++)
        {
            if (m_heightField.SimulateSettleStep(
                m_reposeAngle,
                slideCallback,
                out int stepMinX,
                out int stepMaxX,
                out int stepMinZ,
                out int stepMaxZ))
            {
                moved = true;
                if (stepMinX < totalMinX) totalMinX = stepMinX;
                if (stepMaxX > totalMaxX) totalMaxX = stepMaxX;
                if (stepMinZ < totalMinZ) totalMinZ = stepMinZ;
                if (stepMaxZ > totalMaxZ) totalMaxZ = stepMaxZ;
            }
            else
            {
                break;
            }
        }

        if (moved)
        {
            MarkChunksDirtyInGridRect(totalMinX, totalMaxX, totalMinZ, totalMaxZ);
        }
        else
        {
            m_isSettling = false;
            if (m_deferColliderDuringSettle && m_hasDirtyChunks && m_chunks != null)
            {
                float now = Time.time;
                for (int cx = 0; cx < m_chunkCountX; cx++)
                {
                    for (int cz = 0; cz < m_chunkCountZ; cz++)
                    {
                        if (m_chunks[cx, cz].m_isColliderDirty)
                        {
                            m_chunks[cx, cz].m_nextColliderTime = now;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// ハイトマップからの崩れ通知をワールド空間へ変換してイベント発火します。
    /// </summary>
    private void HandleSandSlidLocal(Vector3 localPos, Vector3 localDir, float removedHeight)
    {
        float scale = Mathf.Max(MIN_SCALE, Mathf.Abs(transform.lossyScale.x));
        Vector3 worldPos = transform.TransformPoint(localPos);
        Vector3 worldDir = transform.TransformDirection(localDir).normalized;
        float volume = removedHeight * m_gridParams.m_voxelSizeX * m_gridParams.m_voxelSizeZ * scale * scale * scale;
        SandSlid?.Invoke(worldPos, worldDir, volume);
    }

    /// <summary>
    /// チャンクの描画メッシュからダブルバッファ用の裏メッシュへ形状をコピーし、null を挟まず差し替えます。
    /// </summary>
    private void UpdateChunkCollider(SandChunk chunk)
    {
        if (chunk.m_meshCollider == null)
        {
            return;
        }

        if (chunk.m_visualMesh == null || chunk.m_visualMesh.vertexCount == 0)
        {
            if (chunk.m_meshCollider.sharedMesh != null)
            {
                chunk.m_meshCollider.sharedMesh = null;
            }
            return;
        }

        chunk.m_visualMesh.GetVertices(m_tempVertices);
        chunk.m_visualMesh.GetTriangles(m_tempTriangles, 0);

        if (m_tempTriangles.Count == 0)
        {
            if (chunk.m_meshCollider.sharedMesh != null)
            {
                chunk.m_meshCollider.sharedMesh = null;
            }
            return;
        }

        int nextIndex = (chunk.m_activeColliderIndex + 1) % COLLIDER_MESH_COUNT;
        Mesh nextMesh = chunk.m_colliderMeshes[nextIndex];
        nextMesh.Clear();
        nextMesh.SetVertices(m_tempVertices);
        nextMesh.SetTriangles(m_tempTriangles, 0);
        nextMesh.RecalculateBounds();

        chunk.m_meshCollider.sharedMesh = nextMesh;
        chunk.m_activeColliderIndex = nextIndex;
    }

    /// <summary>
    /// ローカルバウンディングボックスをワールド空間へ変換します。
    /// </summary>
    private Bounds CalculateWorldBounds()
    {
        Vector3 center = transform.TransformPoint(m_localBounds.center);
        Vector3 extents = m_localBounds.extents;
        Vector3 axisX = transform.TransformVector(extents.x, 0f, 0f);
        Vector3 axisY = transform.TransformVector(0f, extents.y, 0f);
        Vector3 axisZ = transform.TransformVector(0f, 0f, extents.z);

        Vector3 worldExtents = new Vector3(
            Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x),
            Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y),
            Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z)
        );

        return new Bounds(center, worldExtents * 2f);
    }

#if UNITY_EDITOR
    // ---- エディタ専用プレビュー・ギズモ処理 ----
    private void Reset()
    {
        if (!Application.isPlaying)
        {
            ScheduleEditorPreviewUpdate();
        }
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            ScheduleEditorPreviewUpdate();
        }
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying && m_showEditorPreview && m_previewMesh == null)
        {
            ScheduleEditorPreviewUpdate();
        }

        if (m_showBoundsGizmo)
        {
            DrawBoundsGizmos(UNSELECTED_GIZMO_ALPHA);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (m_showBoundsGizmo)
        {
            DrawBoundsGizmos(SELECTED_GIZMO_ALPHA);
        }
    }

    /// <summary>
    /// エディタプレビューメッシュの更新を1フレームに1回へ集約して予約します。
    /// </summary>
    private void ScheduleEditorPreviewUpdate()
    {
        if (m_previewUpdateQueued)
        {
            return;
        }

        m_previewUpdateQueued = true;
        EditorApplication.delayCall += ExecuteQueuedEditorPreviewUpdate;
    }

    /// <summary>
    /// 予約されたエディタプレビューメッシュの更新を実行します。
    /// </summary>
    private void ExecuteQueuedEditorPreviewUpdate()
    {
        EditorApplication.delayCall -= ExecuteQueuedEditorPreviewUpdate;
        m_previewUpdateQueued = false;

        if (this == null || Application.isPlaying)
        {
            return;
        }

        UpdateEditorPreview();
    }

    /// <summary>
    /// シーンビュー配置用のプレビューメッシュを構築して親の MeshFilter へ反映します。
    /// </summary>
    private void UpdateEditorPreview()
    {
        if (Application.isPlaying || !gameObject.scene.IsValid() || EditorUtility.IsPersistent(this))
        {
            return;
        }

        if (!m_showEditorPreview || !enabled || !gameObject.activeInHierarchy)
        {
            ClearEditorPreview();
            return;
        }

        GridParameters gridParams = ComputeGridParameters();

        SandHeightField previewField = new SandHeightField(
            gridParams.m_gridSizeX,
            gridParams.m_gridSizeZ,
            gridParams.m_voxelSizeX,
            gridParams.m_voxelSizeZ,
            gridParams.m_mountainHeight,
            gridParams.m_sink
        );
        previewField.InitializeShape(m_initShapeType, m_initialHeightmap, m_reposeAngle);

        MarchingCubesMeshBuilder previewBuilder = new MarchingCubesMeshBuilder(
            gridParams.m_gridSizeX,
            gridParams.m_gridSizeZ,
            gridParams.m_ny,
            gridParams.m_voxelSizeX,
            gridParams.m_voxelSizeY,
            gridParams.m_voxelSizeZ,
            gridParams.m_cellCountX,
            gridParams.m_cellCountZ
        );

        if (m_previewMesh == null)
        {
            m_previewMesh = new Mesh
            {
                name = "MarchingCubesMountain_EditorPreview",
                hideFlags = HideFlags.DontSave
            };
            m_previewMesh.indexFormat = IndexFormat.UInt32;
        }

        previewBuilder.RebuildChunkMesh(previewField, 0, gridParams.m_cellCountX, 0, gridParams.m_cellCountZ, m_previewMesh);

        MeshFilter parentFilter = GetComponent<MeshFilter>();
        if (parentFilter != null)
        {
            parentFilter.sharedMesh = m_previewMesh;
        }

        MeshRenderer parentRenderer = GetComponent<MeshRenderer>();
        if (parentRenderer != null && !parentRenderer.enabled)
        {
            parentRenderer.enabled = true;
        }
    }

    /// <summary>
    /// エディタプレビュー用のメッシュを破棄し、MeshFilter の参照をクリアします。
    /// </summary>
    private void ClearEditorPreview()
    {
        if (m_previewMesh == null)
        {
            return;
        }

        MeshFilter parentFilter = GetComponent<MeshFilter>();
        if (parentFilter != null && parentFilter.sharedMesh == m_previewMesh)
        {
            parentFilter.sharedMesh = null;
        }

        DestroyImmediate(m_previewMesh);
        m_previewMesh = null;
    }

    /// <summary>
    /// シーンビュー上に砂山の全体範囲（最大サイズと地面基準面）のギズモを描画します。
    /// </summary>
    private void DrawBoundsGizmos(float alpha)
    {
        GridParameters gridParams = ComputeGridParameters();
        Bounds bounds = CalculateLocalBounds(gridParams);
        Color color = m_boundsGizmoColor;
        color.a = alpha;

        Gizmos.color = color;
        Gizmos.matrix = transform.localToWorldMatrix;

        // 全体の外枠（最大高さ・XZスケール）
        Gizmos.DrawWireCube(bounds.center, bounds.size);

        // 床レベル（Y = 0）の配置フットプリント枠
        Vector3 groundCenter = new Vector3(bounds.center.x, 0f, bounds.center.z);
        Vector3 groundSize = new Vector3(bounds.size.x, 0f, bounds.size.z);
        Gizmos.DrawWireCube(groundCenter, groundSize);

        Gizmos.matrix = Matrix4x4.identity;
    }
#endif
}