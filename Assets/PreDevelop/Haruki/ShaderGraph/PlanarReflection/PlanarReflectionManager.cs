using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
public class PlanarReflectionManager : MonoBehaviour
{
    [Header("水面（地面）の設定")]
    [Tooltip("反射面（地面）のワールドY座標")]
    public float planeHeight = 0.0f;

    [Header("品質・解像度")]
    [Range(0.2f, 1.0f)]
    [Tooltip("画面解像度に対するスケール（0.5で負荷が大幅に軽減されます）")]
    public float resolutionScale = 0.5f;

    [Header("描画対象・カリング")]
    [Tooltip("反射カメラで描画するレイヤー（地面自身や小物は除外推奨）")]
    public LayerMask reflectionLayers = -1;

    [Tooltip("反射カメラの最大描画距離")]
    public float maxReflectionDistance = 150.0f;

    [Header("レイヤー別カリング距離（0ならmaxReflectionDistanceを使用）")]
    [Tooltip("小石・がれき・木箱などのレイヤー名")]
    public string smallPropsLayerName = "SmallProps";
    [Tooltip("小物の消える距離（m）")]
    public float smallPropsCullDistance = 20.0f;

    [Tooltip("中型プロップ（フェンス・街灯等）のレイヤー名")]
    public string mediumPropsLayerName = "MediumProps";
    [Tooltip("中型プロップの消える距離（m）")]
    public float mediumPropsCullDistance = 50.0f;

    [Header("LOD制御")]
    [Range(0.1f, 1.0f)]
    [Tooltip("反射描画時のLOD Bias")]
    public float reflectionLodBias = 0.4f;

    private Camera _reflectionCamera;
    private RenderTexture _reflectionTexture;
    private bool _isRendering = false;

    private static readonly int ReflectionTexID = Shader.PropertyToID("_PlanarReflectionTex");

    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        Cleanup();
    }

    private void OnDestroy()
    {
        Cleanup();
    }

    private void Cleanup()
    {
        if (_reflectionTexture != null)
        {
            _reflectionTexture.Release();
            DestroyImmediate(_reflectionTexture);
            _reflectionTexture = null;
        }

        if (_reflectionCamera != null)
        {
            DestroyImmediate(_reflectionCamera.gameObject);
            _reflectionCamera = null;
        }
    }

    private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        // 反射カメラ自身、またはプレビューカメラからの再帰描画を防止
        if (_isRendering || camera == _reflectionCamera || camera.cameraType == CameraType.Preview)
            return;

        UpdateReflection(context, camera);
    }

    private void UpdateReflection(ScriptableRenderContext context, Camera mainCamera)
    {
        _isRendering = true;

        CreateResources(mainCamera);
        CopyCameraSettings(mainCamera);

        // 反射面の方程式（ax + by + cz + d = 0）
        // 法線 (0, 1, 0)、水面の高さ planeHeight
        Vector4 plane = new Vector4(0.0f, 1.0f, 0.0f, -planeHeight);

        // 反射行列を行列乗算してカメラのViewMatrixを設定
        Matrix4x4 reflectionMatrix = CalculateReflectionMatrix(plane);
        _reflectionCamera.worldToCameraMatrix = mainCamera.worldToCameraMatrix * reflectionMatrix;

        // 反射カメラのワールド位置も対称位置に合わせておく（LODや距離カリング判定のため）
        Vector3 camPos = mainCamera.transform.position;
        float dist = 2.0f * (camPos.y - planeHeight);
        _reflectionCamera.transform.position = camPos - Vector3.up * dist;

        // --- 軽量化処理 ---
        float originalLodBias = QualitySettings.lodBias;
        QualitySettings.lodBias = reflectionLodBias;

        // 鏡像描画のため、ポリゴンの裏表カリングを一時的に反転
        GL.invertCulling = true;

#pragma warning disable CS0618
        UniversalRenderPipeline.RenderSingleCamera(context, _reflectionCamera);
#pragma warning restore CS0618

        GL.invertCulling = false;

        // 設定をメインカメラ用に戻す
        QualitySettings.lodBias = originalLodBias;

        // シェーダー側にテクスチャを送信
        Shader.SetGlobalTexture(ReflectionTexID, _reflectionTexture);

        _isRendering = false;
    }

    private void CreateResources(Camera mainCamera)
    {
        int targetWidth = Mathf.Max(1, (int)(mainCamera.pixelWidth * resolutionScale));
        int targetHeight = Mathf.Max(1, (int)(mainCamera.pixelHeight * resolutionScale));

        if (_reflectionTexture == null || _reflectionTexture.width != targetWidth || _reflectionTexture.height != targetHeight)
        {
            if (_reflectionTexture != null)
                _reflectionTexture.Release();

            _reflectionTexture = new RenderTexture(targetWidth, targetHeight, 16, RenderTextureFormat.DefaultHDR)
            {
                name = "_PlanarReflectionTex",
                useMipMap = false,
                autoGenerateMips = false
            };
        }

        if (_reflectionCamera == null)
        {
            GameObject camGo = new GameObject("Reflection Camera (Generated)", typeof(Camera));
            camGo.hideFlags = HideFlags.DontSave;
            _reflectionCamera = camGo.GetComponent<Camera>();
            _reflectionCamera.enabled = false;

            var cameraData = _reflectionCamera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderShadows = false;
            cameraData.renderPostProcessing = false;
            cameraData.antialiasing = AntialiasingMode.None;
        }

        _reflectionCamera.targetTexture = _reflectionTexture;
    }

    private void CopyCameraSettings(Camera src)
    {
        _reflectionCamera.fieldOfView = src.fieldOfView;
        _reflectionCamera.nearClipPlane = src.nearClipPlane;
        _reflectionCamera.farClipPlane = maxReflectionDistance;
        _reflectionCamera.aspect = src.aspect;
        _reflectionCamera.orthographic = src.orthographic;
        _reflectionCamera.orthographicSize = src.orthographicSize;
        _reflectionCamera.cullingMask = reflectionLayers;
        _reflectionCamera.clearFlags = CameraClearFlags.Skybox;

        ApplyLayerCullDistances(_reflectionCamera);
    }

    private void ApplyLayerCullDistances(Camera cam)
    {
        float[] distances = new float[32];

        int smallLayer = LayerMask.NameToLayer(smallPropsLayerName);
        if (smallLayer != -1 && smallPropsCullDistance > 0)
        {
            distances[smallLayer] = smallPropsCullDistance;
        }

        int mediumLayer = LayerMask.NameToLayer(mediumPropsLayerName);
        if (mediumLayer != -1 && mediumPropsCullDistance > 0)
        {
            distances[mediumLayer] = mediumPropsCullDistance;
        }

        cam.layerCullDistances = distances;
    }

    // 平面反射行列の計算
    private static Matrix4x4 CalculateReflectionMatrix(Vector4 plane)
    {
        Matrix4x4 mat = Matrix4x4.identity;

        mat.m00 = (1.0f - 2.0f * plane[0] * plane[0]);
        mat.m01 = (-2.0f * plane[0] * plane[1]);
        mat.m02 = (-2.0f * plane[0] * plane[2]);
        mat.m03 = (-2.0f * plane[3] * plane[0]);

        mat.m10 = (-2.0f * plane[1] * plane[0]);
        mat.m11 = (1.0f - 2.0f * plane[1] * plane[1]);
        mat.m12 = (-2.0f * plane[1] * plane[2]);
        mat.m13 = (-2.0f * plane[3] * plane[1]);

        mat.m20 = (-2.0f * plane[2] * plane[0]);
        mat.m21 = (-2.0f * plane[2] * plane[1]);
        mat.m22 = (1.0f - 2.0f * plane[2] * plane[2]);
        mat.m23 = (-2.0f * plane[3] * plane[2]);

        mat.m30 = 0.0f;
        mat.m31 = 0.0f;
        mat.m32 = 0.0f;
        mat.m33 = 1.0f;

        return mat;
    }
}