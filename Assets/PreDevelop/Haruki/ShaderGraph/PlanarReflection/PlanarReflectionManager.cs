using System;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

/// <summary>
/// 水平な反射面の反射を専用カメラで生成します。
/// 描画対象はRendering Layerで選択し、GameObject Layerは変更しません。
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class PlanarReflectionManager : MonoBehaviour
{
    // カメラ視錐台の平面数
    private const int FRUSTUM_PLANE_COUNT = 6;

    // RenderTextureの最小サイズ
    private const int MIN_TEXTURE_SIZE = 1;

    // 描画距離の下限
    private const float MIN_CLIP_DISTANCE = 0.01f;

    // Rendering Layer 1を反射専用として使用する初期値
    private const uint DEFAULT_REFLECTION_RENDERING_LAYER = 1u << 1;

    // Shader Graphに渡す既存のテクスチャ名
    private const string REFLECTION_TEXTURE_NAME = "_PlanarReflectionTex";
    private static readonly int REFLECTION_TEX_ID = Shader.PropertyToID(REFLECTION_TEXTURE_NAME);

    // Profilerから反射描画を識別するマーカー
    private static readonly ProfilerMarker REFLECTION_RENDER_MARKER =
        new ProfilerMarker("PlanarReflectionManager.RenderReflection");

    // 複数Managerが存在する場合にも再帰呼び出しを防止
    private static bool s_isReflectionRendering;

    [Header("反射元カメラ")]
    [SerializeField, Tooltip("未指定の場合、MainCameraタグを持つGameカメラを使用します。")]
    private Camera m_sourceCamera;

    [SerializeField, Tooltip("編集モード中も反射を更新します。")]
    private bool m_enableInEditMode = false;

    [SerializeField, Tooltip("編集モード中のSceneビューも反射の更新元として許可します。")]
    private bool m_previewSceneViewInEditMode = false;

    [Header("反射面")]
    [FormerlySerializedAs("planeHeight")]
    [SerializeField, Tooltip("水平反射面のワールドY座標です。")]
    private float m_planeHeight = 0.0f;

    [SerializeField, Tooltip("反射を表示するRendererです。未指定なら可視判定を行いません。")]
    private Renderer[] m_reflectionSurfaces = Array.Empty<Renderer>();

    [SerializeField, Tooltip("反射面がすべて視野外なら描画をスキップします。")]
    private bool m_skipWhenSurfaceInvisible = true;

    [Header("反射描画の対象")]
    [SerializeField, Tooltip("反射するRendererのRendering Layer Mask。初期値はLayer 1です。")]
    private RenderingLayerMask m_reflectionRenderingLayers =
        (RenderingLayerMask)DEFAULT_REFLECTION_RENDERING_LAYER;

    [SerializeField, Tooltip("URP AssetのRenderer Listにある反射専用Rendererの番号。必須です。")]
    private int m_reflectionRendererIndex = -1;

    [FormerlySerializedAs("maxReflectionDistance")]
    [SerializeField, Min(MIN_CLIP_DISTANCE), Tooltip("反射カメラのFar Clipです。")]
    private float m_maxReflectionDistance = 150.0f;

    [Header("解像度と更新間隔")]
    [FormerlySerializedAs("resolutionScale")]
    [SerializeField, Range(0.2f, 1.0f)]
    private float m_resolutionScale = 0.5f;

    [SerializeField, Min(1), Tooltip("2ならPlay中は最大2フレームに1回更新します。")]
    private int m_updateIntervalFrames = 1;

    [Header("LOD")]
    [FormerlySerializedAs("reflectionLodBias")]
    [SerializeField, Range(0.1f, 1.0f), Tooltip("LOD Groupの描画品質。Mesh LODには直接適用されません。")]
    private float m_reflectionLodBias = 0.4f;

    [SerializeField, Tooltip("反射の描画中のみMesh LOD Thresholdを変更します。")]
    private bool m_overrideMeshLodThreshold = false;

    [SerializeField, Min(0.01f), Tooltip("値を大きくするとMesh LODが粗くなりやすくなります。")]
    private float m_reflectionMeshLodThreshold = 1.0f;

    // 生成カメラと反射テクスチャ
    private Camera m_reflectionCamera;
    private UniversalAdditionalCameraData m_reflectionCameraData;
    private PlanarReflectionCameraMarker m_cameraMarker;
    private RenderTexture m_reflectionTexture;

    // 可視判定用の再利用バッファ
    private readonly Plane[] m_frustumPlanes = new Plane[FRUSTUM_PLANE_COUNT];

    // カメラと描画更新状態
    private int m_appliedRendererIndex = int.MinValue;
    private int m_lastRenderedFrame = -1;
    private bool m_isRendering;
    private bool m_hasWarnedMissingRenderer;

    // 前バージョンの公開APIとの互換性を維持します（描画対象LayerはRendering Layerへ移行）。
    /// <summary>旧API：反射面の高さ。</summary>
    [Obsolete("SetPlaneHeight()を使用してください。")]
    public float planeHeight { get => m_planeHeight; set => SetPlaneHeight(value); }

    /// <summary>旧API：反射テクスチャの解像度倍率。</summary>
    [Obsolete("InspectorのResolution Scaleを使用してください。")]
    public float resolutionScale
    {
        get => m_resolutionScale;
        set => m_resolutionScale = Mathf.Clamp(value, 0.2f, 1.0f);
    }

    /// <summary>旧API：反射カメラの最大描画距離。</summary>
    [Obsolete("InspectorのMax Reflection Distanceを使用してください。")]
    public float maxReflectionDistance
    {
        get => m_maxReflectionDistance;
        set => m_maxReflectionDistance = Mathf.Max(MIN_CLIP_DISTANCE, value);
    }

    /// <summary>旧API：LOD Groupに適用するバイアス。</summary>
    [Obsolete("InspectorのReflection LOD Biasを使用してください。")]
    public float reflectionLodBias
    {
        get => m_reflectionLodBias;
        set => m_reflectionLodBias = Mathf.Clamp(value, 0.1f, 1.0f);
    }

    /// <summary>反射面の高さを設定します。</summary>
    public void SetPlaneHeight(float height)
    {
        m_planeHeight = height;
    }

    /// <summary>反射を描画するRendering Layerを変更します。</summary>
    public void SetRenderingLayers(RenderingLayerMask renderingLayers)
    {
        m_reflectionRenderingLayers = renderingLayers;
    }

    /// <summary>
    /// Cameraの描画イベントを購読します。
    /// </summary>
    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        m_lastRenderedFrame = -1;

        if (m_reflectionTexture == null)
        {
            Shader.SetGlobalTexture(REFLECTION_TEX_ID, Texture2D.blackTexture);
        }
    }

    /// <summary>
    /// 生成したリソースを解放します。
    /// </summary>
    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        ReleaseResources();
    }

    /// <summary>
    /// コンポーネント破棄時にもリソースを解放します。
    /// </summary>
    private void OnDestroy()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        ReleaseResources();
    }

    /// <summary>
    /// Inspector値を検証します。
    /// </summary>
    private void OnValidate()
    {
        m_resolutionScale = Mathf.Clamp(m_resolutionScale, 0.2f, 1.0f);
        m_updateIntervalFrames = Mathf.Max(1, m_updateIntervalFrames);
        m_reflectionRendererIndex = Mathf.Max(-1, m_reflectionRendererIndex);
        m_maxReflectionDistance = Mathf.Max(MIN_CLIP_DISTANCE, m_maxReflectionDistance);
        m_reflectionMeshLodThreshold = Mathf.Max(0.01f, m_reflectionMeshLodThreshold);
        m_hasWarnedMissingRenderer = false;
    }

    /// <summary>
    /// 必要なカメラの描画開始時だけ反射を生成します。
    /// </summary>
    private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (!ShouldRenderForCamera(camera) || !IsReflectionSurfaceVisible(camera))
        {
            return;
        }

        if (m_reflectionRendererIndex < 0)
        {
            if (!m_hasWarnedMissingRenderer)
            {
                Debug.LogError(
                    "[PlanarReflectionManager] 反射専用Renderer Indexを設定してください。" +
                    " 専用RendererにはReflectionRenderingLayerFeatureが必要です。", this);
                m_hasWarnedMissingRenderer = true;
            }
            return;
        }

        if (Application.isPlaying && m_updateIntervalFrames > 1 &&
            m_reflectionTexture != null && m_reflectionTexture.IsCreated() &&
            m_lastRenderedFrame >= 0 &&
            Time.frameCount - m_lastRenderedFrame < m_updateIntervalFrames)
        {
            return;
        }

        RenderReflection(context, camera);
    }

    /// <summary>
    /// カメラが反射更新の対象か判定します。
    /// </summary>
    private bool ShouldRenderForCamera(Camera camera)
    {
        if (s_isReflectionRendering || m_isRendering || camera == null ||
            camera == m_reflectionCamera)
        {
            return false;
        }

        if (!Application.isPlaying && !m_enableInEditMode)
        {
            return false;
        }

        if (camera.cameraType == CameraType.SceneView)
        {
            return !Application.isPlaying && m_previewSceneViewInEditMode;
        }

        if (camera.cameraType != CameraType.Game)
        {
            return false;
        }

        if (m_sourceCamera != null)
        {
            return camera == m_sourceCamera;
        }

        return camera.targetTexture == null && camera.CompareTag("MainCamera");
    }

    /// <summary>
    /// 反射面がカメラの視野に入っているか確認します。
    /// </summary>
    private bool IsReflectionSurfaceVisible(Camera camera)
    {
        if (!m_skipWhenSurfaceInvisible || m_reflectionSurfaces == null ||
            m_reflectionSurfaces.Length == 0)
        {
            return true;
        }

        GeometryUtility.CalculateFrustumPlanes(camera, m_frustumPlanes);

        foreach (Renderer surface in m_reflectionSurfaces)
        {
            if (surface == null || !surface.enabled || !surface.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (GeometryUtility.TestPlanesAABB(m_frustumPlanes, surface.bounds))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 反射用カメラを更新してRenderTextureへ描画します。
    /// </summary>
    private void RenderReflection(ScriptableRenderContext context, Camera sourceCamera)
    {
        m_isRendering = true;
        s_isReflectionRendering = true;

        bool previousInvertCulling = GL.invertCulling;
        float previousLodBias = QualitySettings.lodBias;
        float previousMeshLodThreshold = QualitySettings.meshLodThreshold;

        try
        {
            using (REFLECTION_RENDER_MARKER.Auto())
            {
                EnsureResources(sourceCamera);
                CopyCameraSettings(sourceCamera);

                // Rendering Layerはカメラに設定できないため、専用Passへ値を渡します。
                m_cameraMarker.SetRenderingLayers(m_reflectionRenderingLayers);

                Vector4 plane = new Vector4(0.0f, 1.0f, 0.0f, -m_planeHeight);
                Matrix4x4 reflectionMatrix = CalculateReflectionMatrix(plane);
                m_reflectionCamera.worldToCameraMatrix =
                    sourceCamera.worldToCameraMatrix * reflectionMatrix;

                Vector3 position = sourceCamera.transform.position;
                float mirrorDistance = 2.0f * (position.y - m_planeHeight);
                m_reflectionCamera.transform.position = position - Vector3.up * mirrorDistance;

                QualitySettings.lodBias = m_reflectionLodBias;
                if (m_overrideMeshLodThreshold)
                {
                    QualitySettings.meshLodThreshold = m_reflectionMeshLodThreshold;
                }

                GL.invertCulling = !previousInvertCulling;

                // Unity 6の現行URPでは非推奨ですが、既存の反射描画方式を維持します。
#pragma warning disable CS0618
                UniversalRenderPipeline.RenderSingleCamera(context, m_reflectionCamera);
#pragma warning restore CS0618

                Shader.SetGlobalTexture(REFLECTION_TEX_ID, m_reflectionTexture);
                m_lastRenderedFrame = Time.frameCount;
            }
        }
        finally
        {
            GL.invertCulling = previousInvertCulling;
            QualitySettings.lodBias = previousLodBias;
            if (m_overrideMeshLodThreshold)
            {
                QualitySettings.meshLodThreshold = previousMeshLodThreshold;
            }

            m_isRendering = false;
            s_isReflectionRendering = false;
        }
    }

    /// <summary>
    /// 解像度とRendererの変更を確認して必要なリソースを確保します。
    /// </summary>
    private void EnsureResources(Camera sourceCamera)
    {
        int width = Mathf.Max(MIN_TEXTURE_SIZE,
            Mathf.RoundToInt(sourceCamera.pixelWidth * m_resolutionScale));
        int height = Mathf.Max(MIN_TEXTURE_SIZE,
            Mathf.RoundToInt(sourceCamera.pixelHeight * m_resolutionScale));

        if (m_reflectionTexture == null || m_reflectionTexture.width != width ||
            m_reflectionTexture.height != height || !m_reflectionTexture.IsCreated())
        {
            ReleaseReflectionTexture();
            m_reflectionTexture = new RenderTexture(width, height, 16, RenderTextureFormat.DefaultHDR)
            {
                name = REFLECTION_TEXTURE_NAME,
                useMipMap = false,
                autoGenerateMips = false,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            m_reflectionTexture.Create();
            m_lastRenderedFrame = -1;
        }

        if (m_reflectionCamera != null && m_appliedRendererIndex != m_reflectionRendererIndex)
        {
            ReleaseReflectionCamera();
        }

        if (m_reflectionCamera == null)
        {
            GameObject cameraObject = new GameObject("Reflection Camera (Generated)",
                typeof(Camera), typeof(PlanarReflectionCameraMarker));
            cameraObject.hideFlags = HideFlags.DontSave;

            m_reflectionCamera = cameraObject.GetComponent<Camera>();
            m_cameraMarker = cameraObject.GetComponent<PlanarReflectionCameraMarker>();
            m_reflectionCamera.enabled = false;

            m_reflectionCameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            m_reflectionCameraData.renderShadows = false;
            m_reflectionCameraData.renderPostProcessing = false;
            m_reflectionCameraData.antialiasing = AntialiasingMode.None;
            m_reflectionCameraData.SetRenderer(m_reflectionRendererIndex);
            m_appliedRendererIndex = m_reflectionRendererIndex;
        }

        m_reflectionCamera.targetTexture = m_reflectionTexture;
    }

    /// <summary>
    /// 反射カメラの投影と描画設定を元カメラに合わせます。
    /// </summary>
    private void CopyCameraSettings(Camera sourceCamera)
    {
        m_reflectionCamera.fieldOfView = sourceCamera.fieldOfView;
        m_reflectionCamera.orthographic = sourceCamera.orthographic;
        m_reflectionCamera.orthographicSize = sourceCamera.orthographicSize;
        m_reflectionCamera.aspect = sourceCamera.aspect;
        m_reflectionCamera.nearClipPlane = Mathf.Max(MIN_CLIP_DISTANCE,
            Mathf.Min(sourceCamera.nearClipPlane, m_maxReflectionDistance - MIN_CLIP_DISTANCE));
        m_reflectionCamera.farClipPlane = Mathf.Max(
            m_reflectionCamera.nearClipPlane + MIN_CLIP_DISTANCE, m_maxReflectionDistance);

        // GameObject Layerは物理用途のまま保持し、カメラ側では選別しません。
        // 標準Opaque/Transparent Passは反射専用Renderer側で無効化します。
        m_reflectionCamera.cullingMask = ~0;
        m_reflectionCamera.clearFlags = sourceCamera.clearFlags == CameraClearFlags.SolidColor
            ? CameraClearFlags.SolidColor : CameraClearFlags.Skybox;
        m_reflectionCamera.backgroundColor = sourceCamera.backgroundColor;
        m_reflectionCamera.allowMSAA = false;
        m_reflectionCamera.useOcclusionCulling = sourceCamera.useOcclusionCulling;
    }

    /// <summary>
    /// 水平面の鏡映行列を計算します。
    /// </summary>
    private static Matrix4x4 CalculateReflectionMatrix(Vector4 plane)
    {
        Matrix4x4 matrix = Matrix4x4.identity;

        matrix.m00 = 1.0f - 2.0f * plane.x * plane.x;
        matrix.m01 = -2.0f * plane.x * plane.y;
        matrix.m02 = -2.0f * plane.x * plane.z;
        matrix.m03 = -2.0f * plane.w * plane.x;

        matrix.m10 = -2.0f * plane.y * plane.x;
        matrix.m11 = 1.0f - 2.0f * plane.y * plane.y;
        matrix.m12 = -2.0f * plane.y * plane.z;
        matrix.m13 = -2.0f * plane.w * plane.y;

        matrix.m20 = -2.0f * plane.z * plane.x;
        matrix.m21 = -2.0f * plane.z * plane.y;
        matrix.m22 = 1.0f - 2.0f * plane.z * plane.z;
        matrix.m23 = -2.0f * plane.w * plane.z;

        return matrix;
    }

    /// <summary>
    /// 生成カメラと反射テクスチャを解放します。
    /// </summary>
    private void ReleaseResources()
    {
        ReleaseReflectionCamera();
        ReleaseReflectionTexture();
        Shader.SetGlobalTexture(REFLECTION_TEX_ID, Texture2D.blackTexture);
        m_lastRenderedFrame = -1;
    }

    /// <summary>
    /// 生成カメラを破棄します。
    /// </summary>
    private void ReleaseReflectionCamera()
    {
        if (m_reflectionCamera == null)
        {
            return;
        }

        GameObject cameraObject = m_reflectionCamera.gameObject;
        m_reflectionCamera = null;
        m_reflectionCameraData = null;
        m_cameraMarker = null;
        DestroyGeneratedObject(cameraObject);
    }

    /// <summary>
    /// 前の反射テクスチャを破棄します。
    /// </summary>
    private void ReleaseReflectionTexture()
    {
        if (m_reflectionTexture == null)
        {
            return;
        }

        RenderTexture oldTexture = m_reflectionTexture;
        m_reflectionTexture = null;
        Shader.SetGlobalTexture(REFLECTION_TEX_ID, Texture2D.blackTexture);
        oldTexture.Release();
        DestroyGeneratedObject(oldTexture);
    }

    /// <summary>
    /// Play中とEditor中で生成Objectを適切に破棄します。
    /// </summary>
    private static void DestroyGeneratedObject(UnityEngine.Object generatedObject)
    {
        if (generatedObject == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(generatedObject);
        }
        else
        {
            DestroyImmediate(generatedObject);
        }
    }
}
