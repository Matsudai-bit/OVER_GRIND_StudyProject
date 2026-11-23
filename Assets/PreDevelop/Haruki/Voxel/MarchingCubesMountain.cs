using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// オーバーハングや空洞ができないハイトマップベースの砂山コンポーネント。
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MarchingCubesMountain : MonoBehaviour
{
    // ---- 定数 ----
    private const int MIN_GRID_SIZE = 3;                      // 最小グリッドサイズ
    private const float MIN_VOXEL_SIZE = 0.01f;               // 最小ボクセルサイズ
    private const float MIN_MOUNTAIN_HEIGHT = 0.1f;           // 最小の高さ
    private const float MIN_SCALE = 1e-5f;                    // スケールゼロ除算防止用の最小値
    private const float SINK_VOXEL_RATIO = 0.5f;              // ボクセルサイズに対する沈め量の割合
    private const int EXTRA_Y_LAYERS = 4;                     // Y方向の余裕層数
    private const int COLLIDER_MESH_COUNT = 2;                // コライダーダブルバッファ用のメッシュ数

    // ---- 静的メンバー ----
    // シーン内の有効な砂山の一覧
    private static readonly List<MarchingCubesMountain> m_instances = new List<MarchingCubesMountain>();

    /// <summary>シーン内の有効な砂山の一覧。</summary>
    public static IReadOnlyList<MarchingCubesMountain> Instances => m_instances;

    // ---- Inspector 設定項目 ----
    [Header("グリッド")]
    [Tooltip("XZ方向のサンプル点数")]
    [SerializeField] private int m_gridSize = 72;
    [Tooltip("サンプル点の間隔(m)")]
    [SerializeField] private float m_voxelSize = 0.15f;
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
    [Tooltip("削り跡の縁のなめらかさ(voxelSize単位)")]
    [Range(0f, 2f)]
    [SerializeField] private float m_carveSmoothness = 0.5f;

    [Header("パフォーマンス・コライダー更新")]
    [Tooltip("MeshColliderを作り直す最小間隔(秒)。見た目のメッシュは毎フレーム更新される")]
    [Min(0.05f)]
    [SerializeField] private float m_colliderUpdateInterval = 0.25f;
    [Tooltip("ONにすると、崩れが完全に落ち着くまでコライダーの再構築を遅らせ、歩行中のかくつきを防ぐ")]
    [SerializeField] private bool m_deferColliderDuringSettle = false;
    [Tooltip("崩れ中(settling)のコライダー更新間隔(秒)。m_deferColliderDuringSettle が OFF の場合に使用")]
    [Min(0.1f)]
    [SerializeField] private float m_settleColliderUpdateInterval = 0.5f;

    // ---- 内部データ ----
    private SandHeightField m_heightField;          // ハイトマップ・シミュレーション担当
    private MarchingCubesMeshBuilder m_meshBuilder; // メッシュ構築担当
    private Mesh m_mesh;                            // 描画専用メッシュ
    private readonly Mesh[] m_colliderMeshes = new Mesh[COLLIDER_MESH_COUNT]; // コライダー専用ダブルバッファメッシュ
    private int m_activeColliderMeshIndex;          // 現在MeshColliderに設定中のバッファインデックス
    private MeshFilter m_meshFilter;                // メッシュフィルター参照
    private MeshRenderer m_meshRenderer;            // メッシュレンダラー参照
    private MeshCollider m_meshCollider;            // メッシュコライダー参照
    private bool m_isSettling;                      // 崩れ処理中フラグ
    private bool m_isMeshDirty;                     // 描画メッシュ再構築が必要か
    private bool m_isColliderDirty;                 // コライダー再構築が必要か
    private bool m_colliderDeferredOnce;            // 描画更新と同フレームでのベイクを1フレームずらすためのフラグ
    private float m_nextColliderTime;               // 次にコライダーを更新可能な時刻

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
    public Bounds WorldBounds => m_meshRenderer != null ? m_meshRenderer.bounds : new Bounds(transform.position, Vector3.zero);

    /// <summary>サンプル点の間隔（ワールド単位）。</summary>
    public float WorldVoxelSize => m_voxelSize * Mathf.Abs(transform.lossyScale.x);

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
    }

    private void OnDisable()
    {
        m_instances.Remove(this);
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
        bool rebuiltMeshThisFrame = false;

        // 見た目のメッシュ：変更があれば1フレームに1回だけ作り直す
        if (m_isMeshDirty)
        {
            m_meshBuilder.RebuildMesh(m_heightField, m_mesh);
            m_isMeshDirty = false;
            m_isColliderDirty = true;
            rebuiltMeshThisFrame = true;
        }

        // コライダー：ダブルバッファで安全に更新し、負荷分散のため描画再構築フレームとは1フレームずらす
        if (!m_isColliderDirty)
        {
            return;
        }

        if (m_deferColliderDuringSettle && m_isSettling)
        {
            return;
        }

        float interval = m_isSettling ? m_settleColliderUpdateInterval : m_colliderUpdateInterval;
        if (Time.time < m_nextColliderTime)
        {
            return;
        }

        if (rebuiltMeshThisFrame && !m_colliderDeferredOnce)
        {
            m_colliderDeferredOnce = true;
            return;
        }

        UpdateCollider();
        m_isColliderDirty = false;
        m_colliderDeferredOnce = false;
        m_nextColliderTime = Time.time + interval;
    }

    private void OnDestroy()
    {
        if (m_mesh != null)
        {
            Destroy(m_mesh);
            m_mesh = null;
        }

        for (int i = 0; i < m_colliderMeshes.Length; i++)
        {
            if (m_colliderMeshes[i] != null)
            {
                Destroy(m_colliderMeshes[i]);
                m_colliderMeshes[i] = null;
            }
        }
    }

    // ---- 公開メソッド ----
    /// <summary>
    /// 砂山を指定されたパラメータで初期化・再生成します。
    /// </summary>
    public void InitializeMountain()
    {
        m_gridSize = Mathf.Max(MIN_GRID_SIZE, m_gridSize);
        m_voxelSize = Mathf.Max(MIN_VOXEL_SIZE, m_voxelSize);
        m_mountainHeight = Mathf.Max(MIN_MOUNTAIN_HEIGHT, m_mountainHeight);

        m_meshFilter = GetComponent<MeshFilter>();
        m_meshRenderer = GetComponent<MeshRenderer>();
        m_meshCollider = GetComponent<MeshCollider>();
        if (m_meshCollider == null)
        {
            m_meshCollider = gameObject.AddComponent<MeshCollider>();
        }

        if (m_mesh == null)
        {
            m_mesh = new Mesh { name = "MarchingCubesMountain_Visual" };
            m_mesh.indexFormat = IndexFormat.UInt32;
        }

        for (int i = 0; i < COLLIDER_MESH_COUNT; i++)
        {
            if (m_colliderMeshes[i] == null)
            {
                m_colliderMeshes[i] = new Mesh { name = "MarchingCubesMountain_Collider_" + i };
                m_colliderMeshes[i].indexFormat = IndexFormat.UInt32;
            }
        }

        if (m_meshFilter != null)
        {
            m_meshFilter.sharedMesh = m_mesh;
        }

        float sink = m_voxelSize * SINK_VOXEL_RATIO;
        int ny = Mathf.CeilToInt(m_mountainHeight / m_voxelSize) + EXTRA_Y_LAYERS;

        m_heightField = new SandHeightField(m_gridSize, m_voxelSize, m_mountainHeight, sink);
        m_heightField.InitializeShape(m_initShapeType, m_initialHeightmap, m_reposeAngle);

        m_meshBuilder = new MarchingCubesMeshBuilder(m_gridSize, ny, m_voxelSize);
        m_meshBuilder.RebuildMesh(m_heightField, m_mesh);
        UpdateCollider();

        m_isSettling = false;
        m_isMeshDirty = false;
        m_isColliderDirty = false;
        m_colliderDeferredOnce = false;
        m_nextColliderTime = Time.time;
    }

    /// <summary>
    /// 指定されたワールド座標のXZ位置にある砂の表面の点と法線を返します。
    /// </summary>
    /// <param name="worldPos">問い合わせるワールド座標（Yは無視されます）。</param>
    /// <param name="surfacePoint">砂表面のワールド座標。</param>
    /// <param name="surfaceNormal">砂表面のワールド法線。</param>
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
    /// 球の中心をワールド座標に置いて砂を削ります（深さ = 半径）。
    /// </summary>
    /// <param name="worldPoint">削る中心位置（ワールド座標）。</param>
    /// <param name="worldRadius">削り球の半径（m）。</param>
    public void Carve(Vector3 worldPoint, float worldRadius)
    {
        Carve(worldPoint, worldRadius, worldRadius);
    }

    /// <summary>
    /// 接触点から指定された深さだけ砂を掘り下げます。
    /// </summary>
    /// <param name="worldPoint">砂表面上の接触点（ワールド座標）。</param>
    /// <param name="worldRadius">削り球の半径（m）。</param>
    /// <param name="worldDepth">掘り下げる深さ（m）。</param>
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

        // 削る前の表面法線を取得
        TrySampleSurface(worldPoint, out _, out Vector3 surfaceNormal);

        Vector3 localPos = transform.InverseTransformPoint(worldPoint) + Vector3.up * (radius - depth);
        float smoothK = m_carveSmoothness * m_voxelSize;

        if (m_heightField.Carve(localPos, radius, smoothK, out float removedHeight))
        {
            m_isMeshDirty = true;
            if (m_settleAfterCarve)
            {
                m_isSettling = true;
            }

            if (Carved != null)
            {
                float volume = removedHeight * m_voxelSize * m_voxelSize * scale * scale * scale;
                float footprint = Mathf.Sqrt(Mathf.Max(0f, depth * (2f * radius - depth))) * scale;
                Carved.Invoke(worldPoint, surfaceNormal, footprint, volume);
            }
        }
    }

    // ---- 内部処理 ----
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
        Action<Vector3, Vector3, float> slideCallback = SandSlid != null ? HandleSandSlidLocal : null;

        for (int i = 0; i < m_settleIterationsPerFrame; i++)
        {
            if (m_heightField.SimulateSettleStep(m_reposeAngle, slideCallback))
            {
                moved = true;
            }
            else
            {
                break;
            }
        }

        if (moved)
        {
            m_isMeshDirty = true;
        }
        else
        {
            m_isSettling = false;
            // 崩れ完了時にも未反映のコライダー変更があれば遅延なく更新可能にする
            if (m_deferColliderDuringSettle && m_isColliderDirty)
            {
                m_nextColliderTime = Time.time;
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
        float volume = removedHeight * m_voxelSize * m_voxelSize * scale * scale * scale;
        SandSlid?.Invoke(worldPos, worldDir, volume);
    }

    /// <summary>
    /// ダブルバッファ用の裏メッシュへ形状をコピーし、null を挟まず MeshCollider へ差し替えます。
    /// </summary>
    private void UpdateCollider()
    {
        if (m_meshCollider == null)
        {
            return;
        }

        if (m_meshBuilder == null || !m_meshBuilder.HasTriangles)
        {
            if (m_meshCollider.sharedMesh != null)
            {
                m_meshCollider.sharedMesh = null;
            }
            return;
        }

        int nextIndex = (m_activeColliderMeshIndex + 1) % COLLIDER_MESH_COUNT;
        Mesh nextColliderMesh = m_colliderMeshes[nextIndex];
        m_meshBuilder.CopyCollisionGeometryTo(nextColliderMesh);

        // 異なるメッシュインスタンスを直接代入することで、接触判定を途切れさせずに更新する
        m_meshCollider.sharedMesh = nextColliderMesh;
        m_activeColliderMeshIndex = nextIndex;
    }
}