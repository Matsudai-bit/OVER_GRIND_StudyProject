using System.IO;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;

public class SplineMeshDeformStatic : MonoBehaviour
{
    // スプライン情報を格納するコンポーネント
    [SerializeField] private SplineContainer _container;

    // 変形対象のメッシュ
    [SerializeField] private Mesh _sourceMesh;

    // 変形後のメッシュ
    [SerializeField] private Mesh _deformedMesh;

    // スプライン上の位置を示すパラメータ（0～1の範囲で指定）
    [SerializeField, Range(0, 1)] private float _t;

    // メッシュの前方向の軸
    [SerializeField] private Vector3 _forwardAxis = Vector3.forward;

    // メッシュの上方向の軸
    [SerializeField] private Vector3 _upAxis = Vector3.up;

    private bool _isStarted;

    // メッシュコライダー（オプション）
    private MeshCollider _meshCollider;

    // 変形されたメッシュ情報
    private Vector3[] _originalVertices;
    private Vector3[] _originalNormals;

    // メッシュを変形する
    public void Deform()
    {
        if (_container == null || _sourceMesh == null || _deformedMesh == null)
            return;

        // メッシュ情報のコピー
        _originalVertices ??= _sourceMesh.vertices;
        _originalNormals ??= _sourceMesh.normals;

        // 元軸からの回転クォータニオンを計算
        var axisRotation = Quaternion.Inverse(Quaternion.LookRotation(_forwardAxis, _upAxis));

        // スプラインのローカル座標からメッシュのローカル座標に変換する行列
        // スプラインのローカル座標→ワールド座標→メッシュのローカル座標の順で変換する必要がある
        var splineToLocalMatrix = transform.worldToLocalMatrix * _container.transform.localToWorldMatrix;

        // メッシュの頂点情報を一時的に格納する配列
        NativeArray<float3> deformedVertices = default;
        NativeArray<float3> deformedNormals = default;

        try
        {
            // 配列の確保
            deformedVertices = new NativeArray<float3>(_originalVertices.Length, Allocator.Temp);
            deformedNormals = new NativeArray<float3>(_originalNormals.Length, Allocator.Temp);

            // スプラインの情報を格納するNativeSplineを作成
            using var spline = new NativeSpline(_container.Spline, splineToLocalMatrix);

            // スプラインの長さを計算
            var splineLength = spline.CalculateLength(splineToLocalMatrix);

            // メッシュの頂点情報をスプラインに沿って変形
            for (var i = 0; i < deformedVertices.Length; i++)
            {
                // 元の頂点座標
                var originalVertex = _originalVertices[i];

                // 軸の回転補正
                originalVertex = math.mul(axisRotation, originalVertex);

                // 頂点座標の前方向成分をスプライン上の位置に変換
                var t = originalVertex.z / splineLength + _t;

                // 囲まれたスプラインなら、tの値をループさせる
                if (spline.Closed) t = Mathf.Repeat(t, 1);

                // 計算されたtに置ける位置・接線・上向きベクトルを取得
                spline.Evaluate(
                    t,
                    out var splinePos,
                    out var splineTangent,
                    out var splineUp
                );

                // スプラインに対するオフセットの回転クォータニオン
                var rotation = quaternion.LookRotationSafe(splineTangent, splineUp);

                // スプライン上の位置に対して水平垂直なずらし位置を計算
                var offset = math.mul(rotation, new float3(originalVertex.x, originalVertex.y, 0));

                // スプライン位置に対する頂点座標を計算
                deformedVertices[i] = splinePos + offset;

                // スプライン位置に対する法線ベクトルを計算
                // 法線にも軸の回転補正を適用
                var normal = math.mul(axisRotation, _originalNormals[i]);
                deformedNormals[i] = math.mul(rotation, normal);
            }

            // メッシュ情報を更新
            _deformedMesh.SetVertices(deformedVertices);
            _deformedMesh.SetNormals(deformedNormals);

            // バウンディングボックスの再計算
            _deformedMesh.RecalculateBounds();

            // メッシュコライダーの更新
            if (_meshCollider != null)
                _meshCollider.sharedMesh = _deformedMesh;
        }
        finally
        {
            // メモリ解放
            if (deformedVertices.IsCreated) deformedVertices.Dispose();
            if (deformedNormals.IsCreated) deformedNormals.Dispose();
        }
    }

    // 初期化処理
    private void Start()
    {
        _isStarted = true;

        TryGetComponent(out _meshCollider);

        // 初回に変形を行う
        Deform();
    }

#if UNITY_EDITOR
    // インスペクターから操作された際に変形を反映
    private void OnValidate()
    {
        if (_sourceMesh == null)
            return;

        if (UnityEditor.EditorApplication.isPlaying)
        {
            // 開始前にOnValidateが呼ばれることがあるため、_isStartedを確認
            if (_isStarted)
                Deform();
        }
        else
        {
            // 新しいメッシュアセットを作成
            if (_deformedMesh == null)
                _deformedMesh = CreateMeshAsset();

            if (_deformedMesh == null)
                return;

            Deform();
        }
    }

    private void Reset()
    {
        // MeshFilterがあれば、そのメッシュを入力とする
        if (TryGetComponent<MeshFilter>(out var meshFilter))
        {
            _sourceMesh = meshFilter.sharedMesh;
            _deformedMesh = CreateMeshAsset();

            // MeshFilterを設定
            meshFilter.sharedMesh = _deformedMesh;
        }

        // MeshColliderがあれば、同様に設定
        if (TryGetComponent<MeshCollider>(out var meshCollider))
            meshCollider.sharedMesh = _deformedMesh;
    }

    // メッシュアセットの作成処理
    // 実装はSplinesExtrudeのものを参考にしました
    private Mesh CreateMeshAsset()
    {
        if (_sourceMesh == null)
            return null;

        // 新しいメッシュアセットを複製
        var mesh = Instantiate(_sourceMesh);
        mesh.name = name;

        // メッシュアセットの保存パスを決定
        var scene = SceneManager.GetActiveScene();
        var sceneDataDir = "Assets";

        if (!string.IsNullOrEmpty(scene.path))
        {
            var dir = Path.GetDirectoryName(scene.path);
            sceneDataDir = $"{dir}/{Path.GetFileNameWithoutExtension(scene.path)}";
            if (!Directory.Exists(sceneDataDir))
                Directory.CreateDirectory(sceneDataDir);
        }

        var path = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(
            $"{sceneDataDir}/SplineMesh_{mesh.name}.asset");

        // メッシュアセットの保存
        UnityEditor.AssetDatabase.CreateAsset(mesh, path);

        // メッシュアセットを選択状態にする
        UnityEditor.EditorGUIUtility.PingObject(mesh);

        return mesh;
    }
#endif
}