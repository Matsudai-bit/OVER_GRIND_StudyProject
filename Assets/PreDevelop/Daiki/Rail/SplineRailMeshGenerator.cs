using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

/// <summary>
/// スプラインのノットを結ぶ正方形断面のチューブメッシュを生成します。
/// SplineExtrudeの軽量な代替として、レールの当たり判定用メッシュ生成に使用します。
/// </summary>
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(SplineContainer))]
[RequireComponent(typeof(MeshFilter))]
public class SplineRailMeshGenerator : MonoBehaviour
{
    // 1断面あたりの頂点数（正方形の角の数）
    private const int VERTICES_PER_KNOT = 4;

    // 側面の枚数（正方形なので4面）
    private const int SIDE_COUNT = 4;

    // 側面1枚あたりの三角形インデックス数（2三角形 × 3頂点）
    private const int TRIANGLE_INDICES_PER_SIDE = 6;

    // 接線ベクトルが有効（ゼロベクトルでない）とみなす最小二乗長のしきい値
    private const float TANGENT_VALID_SQR_LENGTH = 1e-8f;

    // 右方向ベクトルが有効とみなす最小二乗長のしきい値
    private const float RIGHT_VALID_SQR_LENGTH = 1e-6f;

    [Header("断面サイズ")]

    // 断面の半分の幅（左右方向）
    [SerializeField] private float m_halfWidth = 0.15f;

    // 断面の半分の高さ（上下方向）
    [SerializeField] private float m_halfHeight = 0.15f;

    [Header("コライダー設定")]

    // 形状を反映するMeshCollider。未設定の場合は同じGameObjectから自動取得する
    [Tooltip("未設定の場合は同じGameObjectのMeshColliderを自動取得します。")]
    [SerializeField] private MeshCollider m_meshCollider;

    // MeshColliderの形状更新を行うかどうか
    [SerializeField] private bool m_updateCollider = true;

    // MeshColliderへ形状を反映する間隔（フレーム数）
    // sharedMeshの再代入はPhysXの再ベイクが走り重いため、毎フレームではなく間引いて反映する
    [Tooltip("MeshColliderへのsharedMesh再代入はPhysXの再ベイクが走るため、Nフレームに1回だけ反映します。")]
    [SerializeField, Min(1)] private int m_colliderUpdateIntervalFrames = 4;

    // このオブジェクトが持つSplineContainerへの参照
    private SplineContainer m_splineContainer;

    // 生成したメッシュを表示するためのMeshFilterへの参照
    private MeshFilter m_meshFilter;

    // 実行時に生成する動的メッシュの実体
    private Mesh m_mesh;

    // 直近にトポロジ（頂点数・三角形インデックス）を構築した時点でのノット数
    // この値と現在のノット数を比較し、変化した場合のみ構造を再構築する
    private int m_lastKnotCount = -1;

    // MeshColliderの更新間隔を数えるためのフレームカウンタ
    private int m_colliderUpdateFrameCounter = 0;

    // 頂点座標の再利用バッファ（毎フレームのGCアロケーションを避けるため）
    private Vector3[] m_vertices;

    // 法線の再利用バッファ
    private Vector3[] m_normals;

    // 三角形インデックスの再利用バッファ（ノット数が変わらない限り再構築不要）
    private int[] m_triangles;

    // UV座標の再利用バッファ（ノット数が変わらない限り再構築不要）
    private Vector2[] m_uvs;

    private void Awake()
    {
        InitializeComponents();
        InitializeMesh();
    }

    private void LateUpdate()
    {
        // AnimatedTailSplineLinkerがノット位置を更新した後に実行される想定
        // （DefaultExecutionOrderで実行順を後ろにしている）
        Spline spline = m_splineContainer.Spline;
        int knotCount = spline.Count;

        // メッシュとして成立しない場合（ノットが1つ以下）は何もしない
        if (knotCount < 2)
        {
            return;
        }

        // ノット数が変化した時だけ、頂点数や三角形インデックスなどの構造を再構築する
        if (knotCount != m_lastKnotCount)
        {
            RebuildTopology(knotCount);
            m_lastKnotCount = knotCount;
        }

        // 頂点座標は毎フレーム更新する（尻尾のアニメーションに追従させるため）
        UpdateVertexPositions(spline, knotCount);
        ApplyMeshVertices();
        UpdateColliderIfNeeded();
    }

    /// <summary>
    /// 必要なコンポーネントの参照を取得します。
    /// </summary>
    private void InitializeComponents()
    {
        m_splineContainer = GetComponent<SplineContainer>();
        m_meshFilter = GetComponent<MeshFilter>();

        // Inspectorで未設定の場合のみ、同じGameObjectからMeshColliderを探す
        if (m_updateCollider && m_meshCollider == null)
        {
            m_meshCollider = GetComponent<MeshCollider>();
        }
    }

    /// <summary>
    /// 動的更新用のメッシュを生成し、MeshFilterに設定します。
    /// </summary>
    private void InitializeMesh()
    {
        m_mesh = new Mesh { name = "RailMesh (Generated)" };

        // 頻繁に頂点を書き換えることをUnity側に伝え、内部バッファの確保を最適化する
        m_mesh.MarkDynamic();

        if (m_meshFilter != null)
        {
            m_meshFilter.sharedMesh = m_mesh;
        }
    }

    /// <summary>
    /// ノット数に応じて頂点数・三角形インデックス・UVを再構築します。
    /// ノット数が変化した時のみ呼び出される想定です。
    /// </summary>
    /// <param name="knotCount">ノットの数。</param>
    private void RebuildTopology(int knotCount)
    {
        // 頂点・法線バッファをノット数に合わせて確保し直す
        int vertexCount = knotCount * VERTICES_PER_KNOT;
        m_vertices = new Vector3[vertexCount];
        m_normals = new Vector3[vertexCount];

        // UVと三角形インデックスは構造情報なので、ここで一度だけ構築する
        m_uvs = BuildUvCoordinates(knotCount);
        m_triangles = BuildTriangleIndices(knotCount);

        // メッシュをクリアしてから構造情報を設定する
        m_mesh.Clear();
        m_mesh.vertices = m_vertices;
        m_mesh.uv = m_uvs;
        m_mesh.triangles = m_triangles;
    }

    /// <summary>
    /// 各ノットのUV座標を生成します。
    /// </summary>
    /// <param name="knotCount">ノットの数。</param>
    /// <returns>UV座標の配列。</returns>
    private Vector2[] BuildUvCoordinates(int knotCount)
    {
        Vector2[] uvs = new Vector2[knotCount * VERTICES_PER_KNOT];

        for (int i = 0; i < knotCount; i++)
        {
            // レールの根本から先端までを0?1のV座標として割り当てる
            float v = (knotCount > 1) ? (float)i / (knotCount - 1) : 0f;
            int baseIndex = i * VERTICES_PER_KNOT;

            // 正方形の4頂点分のU座標を均等に割り当てる
            uvs[baseIndex + 0] = new Vector2(0.00f, v);
            uvs[baseIndex + 1] = new Vector2(0.33f, v);
            uvs[baseIndex + 2] = new Vector2(0.66f, v);
            uvs[baseIndex + 3] = new Vector2(1.00f, v);
        }

        return uvs;
    }

    /// <summary>
    /// 断面同士をつなぐ三角形インデックスを生成します。
    /// </summary>
    /// <param name="knotCount">ノットの数。</param>
    /// <returns>三角形インデックスの配列。</returns>
    private int[] BuildTriangleIndices(int knotCount)
    {
        // 断面と断面の間の区間数（セグメント数）
        int segmentCount = knotCount - 1;
        int[] triangles = new int[segmentCount * SIDE_COUNT * TRIANGLE_INDICES_PER_SIDE];
        int writeIndex = 0;

        // セグメントごとに、手前の断面（front）と奥の断面（back）を四角形（2三角形）でつなぐ
        for (int i = 0; i < segmentCount; i++)
        {
            int frontBase = i * VERTICES_PER_KNOT;
            int backBase = (i + 1) * VERTICES_PER_KNOT;

            // 正方形の4辺それぞれについて、側面の四角形ポリゴンを構築する
            for (int side = 0; side < SIDE_COUNT; side++)
            {
                int side0 = side;
                int side1 = (side + 1) % SIDE_COUNT;

                int frontSide0 = frontBase + side0;
                int frontSide1 = frontBase + side1;
                int backSide0 = backBase + side0;
                int backSide1 = backBase + side1;

                // 1枚目の三角形
                triangles[writeIndex++] = frontSide0;
                triangles[writeIndex++] = backSide0;
                triangles[writeIndex++] = frontSide1;

                // 2枚目の三角形
                triangles[writeIndex++] = frontSide1;
                triangles[writeIndex++] = backSide0;
                triangles[writeIndex++] = backSide1;
            }
        }

        return triangles;
    }

    /// <summary>
    /// 各ノット位置に正方形断面を配置して頂点座標と法線を更新します。
    /// </summary>
    /// <param name="spline">対象のスプライン。</param>
    /// <param name="knotCount">ノットの数。</param>
    private void UpdateVertexPositions(Spline spline, int knotCount)
    {
        // ツイスト防止用の基準ベクトル（ループ内で毎回計算しないよう先に求めておく）
        Vector3 referenceUp = GetLocalReferenceUp();

        for (int i = 0; i < knotCount; i++)
        {
            Vector3 knotPosition = spline[i].Position;

            // spline.EvaluateTangent()による補間計算は使わず、前後ノットの差分で近似する（軽量化のため）
            Vector3 tangent = CalculateTangent(spline, i, knotCount, knotPosition);

            // 接線と基準の上方向から、断面の右方向・上方向を求める
            Vector3 right = CalculateRightAxis(tangent, referenceUp);
            Vector3 up = Vector3.Cross(tangent, right).normalized;

            WriteCrossSectionVertices(i, knotPosition, right, up);
        }
    }

    /// <summary>
    /// ワールド上方向をこのオブジェクトのローカル空間に変換した基準ベクトルを取得します。
    /// </summary>
    /// <returns>ローカル空間での上方向ベクトル。</returns>
    private Vector3 GetLocalReferenceUp()
    {
        Vector3 localUp = transform.InverseTransformDirection(Vector3.up);

        // ローカル変換の結果がほぼゼロベクトルになる特殊なケースの保険
        if (localUp.sqrMagnitude < RIGHT_VALID_SQR_LENGTH)
        {
            return Vector3.up;
        }

        return localUp.normalized;
    }

    /// <summary>
    /// 指定ノットの接線方向を、前後のノット位置の差分から近似します。
    /// </summary>
    /// <param name="spline">対象のスプライン。</param>
    /// <param name="index">ノットのインデックス。</param>
    /// <param name="knotCount">ノットの数。</param>
    /// <param name="knotPosition">対象ノットの位置。</param>
    /// <returns>正規化された接線ベクトル。</returns>
    private Vector3 CalculateTangent(Spline spline, int index, int knotCount, Vector3 knotPosition)
    {
        Vector3 tangent;

        if (index == 0)
        {
            // 先頭ノットは次のノットとの差分のみで近似する
            tangent = (Vector3)spline[1].Position - knotPosition;
        }
        else if (index == knotCount - 1)
        {
            // 末尾ノットは前のノットとの差分のみで近似する
            tangent = knotPosition - (Vector3)spline[index - 1].Position;
        }
        else
        {
            // 中間ノットは前後のノットの差分から近似する
            tangent = (Vector3)spline[index + 1].Position - (Vector3)spline[index - 1].Position;
        }

        // ノット同士がほぼ同じ位置にある場合の保険（ゼロ除算・不定方向を避ける）
        if (tangent.sqrMagnitude < TANGENT_VALID_SQR_LENGTH)
        {
            return Vector3.forward;
        }

        return tangent.normalized;
    }

    /// <summary>
    /// 接線方向と基準の上方向から、断面の右方向ベクトルを算出します。
    /// </summary>
    /// <param name="tangent">正規化された接線ベクトル。</param>
    /// <param name="referenceUp">基準となる上方向ベクトル。</param>
    /// <returns>正規化された右方向ベクトル。</returns>
    private Vector3 CalculateRightAxis(Vector3 tangent, Vector3 referenceUp)
    {
        Vector3 right = Vector3.Cross(referenceUp, tangent);

        // レールがほぼ真上・真下を向く区間では外積がゼロに近くなるため、別の基準軸で計算し直す
        if (right.sqrMagnitude < RIGHT_VALID_SQR_LENGTH)
        {
            right = Vector3.Cross(Vector3.forward, tangent);
        }

        return right.normalized;
    }

    /// <summary>
    /// 1断面分の頂点座標と法線をバッファへ書き込みます。
    /// </summary>
    /// <param name="knotIndex">ノットのインデックス。</param>
    /// <param name="center">断面の中心位置。</param>
    /// <param name="right">断面の右方向ベクトル。</param>
    /// <param name="up">断面の上方向ベクトル。</param>
    private void WriteCrossSectionVertices(int knotIndex, Vector3 center, Vector3 right, Vector3 up)
    {
        int baseIndex = knotIndex * VERTICES_PER_KNOT;

        // 中心位置から右・上方向にオフセットして正方形の4頂点を求める
        m_vertices[baseIndex + 0] = center + right * m_halfWidth + up * m_halfHeight;
        m_vertices[baseIndex + 1] = center - right * m_halfWidth + up * m_halfHeight;
        m_vertices[baseIndex + 2] = center - right * m_halfWidth - up * m_halfHeight;
        m_vertices[baseIndex + 3] = center + right * m_halfWidth - up * m_halfHeight;

        // 正方形断面のため、法線は各面の向きをそのまま割り当てる
        // （RecalculateNormals()を毎フレーム呼ぶコストを避けるための解析的な計算）
        m_normals[baseIndex + 0] = up;
        m_normals[baseIndex + 1] = -right;
        m_normals[baseIndex + 2] = -up;
        m_normals[baseIndex + 3] = right;
    }

    /// <summary>
    /// 更新した頂点・法線をメッシュへ反映します。
    /// </summary>
    private void ApplyMeshVertices()
    {
        m_mesh.SetVertices(m_vertices);
        m_mesh.SetNormals(m_normals);

        // 描画のカリング判定などに使われるバウンディングボックスを更新する
        m_mesh.RecalculateBounds();
    }

    /// <summary>
    /// 設定した間隔でMeshColliderの形状を更新します。
    /// </summary>
    private void UpdateColliderIfNeeded()
    {
        if (!m_updateCollider || m_meshCollider == null)
        {
            return;
        }

        // 指定フレーム数に達するまではコライダーの更新をスキップする
        m_colliderUpdateFrameCounter++;
        if (m_colliderUpdateFrameCounter < m_colliderUpdateIntervalFrames)
        {
            return;
        }

        m_colliderUpdateFrameCounter = 0;

        // sharedMeshへの再代入をトリガーにPhysXが形状を再ベイクする
        // 一度nullを挟むことで、同一参照のままでは変更が検知されないケースを避ける
        m_meshCollider.sharedMesh = null;
        m_meshCollider.sharedMesh = m_mesh;
    }
}