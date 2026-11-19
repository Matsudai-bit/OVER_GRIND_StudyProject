using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 平面反射カメラに限り、指定Rendering LayerのRendererを描画します。
/// 反射専用Universal Renderer Dataに登録して使用します。
/// </summary>
public sealed class ReflectionRenderingLayerFeature : ScriptableRendererFeature
{
    // 不透明・半透明の2つの描画パス
    private RenderingLayerDrawPass m_opaquePass;
    private RenderingLayerDrawPass m_transparentPass;

    /// <summary>
    /// 反射用の描画パスを生成します。
    /// </summary>
    public override void Create()
    {
        m_opaquePass = new RenderingLayerDrawPass("Reflection Rendering Layers / Opaque", false)
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingOpaques
        };

        m_transparentPass = new RenderingLayerDrawPass("Reflection Rendering Layers / Transparent", true)
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingTransparents
        };
    }

    /// <summary>
    /// 反射専用カメラのときだけパスを登録します。
    /// </summary>
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        Camera camera = renderingData.cameraData.camera;
        if (camera == null ||
            !camera.TryGetComponent(out PlanarReflectionCameraMarker marker) ||
            marker.RenderingLayerMask == 0)
        {
            return;
        }

        if (m_opaquePass == null || m_transparentPass == null)
        {
            return;
        }

        m_opaquePass.SetRenderingLayerMask(marker.RenderingLayerMask);
        m_transparentPass.SetRenderingLayerMask(marker.RenderingLayerMask);

        renderer.EnqueuePass(m_opaquePass);
        renderer.EnqueuePass(m_transparentPass);
    }

    /// <summary>
    /// Render GraphでRendererListを使用して指定Rendering Layerのみ描画します。
    /// </summary>
    private sealed class RenderingLayerDrawPass : ScriptableRenderPass
    {
        /// <summary>RendererListの情報をRender Graphへ引き渡します。</summary>
        private sealed class PassData
        {
            public RendererListHandle rendererList;
        }

        // ShaderTagIdはScriptableObjectの静的初期化時に生成しない。
        // Create()から呼ばれるコンストラクタ内で初期化する。
        private readonly ShaderTagId m_universalForwardTag;
        private readonly ShaderTagId m_universalForwardOnlyTag;
        private readonly ShaderTagId m_srpDefaultUnlitTag;

        // 不透明と半透明の描画設定
        private readonly string m_passName;
        private readonly bool m_isTransparent;

        // 反射の描画対象になるRendering Layer
        private uint m_renderingLayerMask;

        /// <summary>描画パスを初期化します。</summary>
        public RenderingLayerDrawPass(string passName, bool isTransparent)
        {
            m_passName = passName;
            m_isTransparent = isTransparent;

            m_universalForwardTag = new ShaderTagId("UniversalForward");
            m_universalForwardOnlyTag = new ShaderTagId("UniversalForwardOnly");
            m_srpDefaultUnlitTag = new ShaderTagId("SRPDefaultUnlit");
        }

        /// <summary>このフレームの反射対象を設定します。</summary>
        public void SetRenderingLayerMask(uint renderingLayerMask)
        {
            m_renderingLayerMask = renderingLayerMask;
        }

        /// <summary>
        /// Rendering LayerでフィルタしたRendererListを登録します。
        /// </summary>
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
        {
            UniversalRenderingData renderingData = frameContext.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameContext.Get<UniversalCameraData>();
            UniversalLightData lightData = frameContext.Get<UniversalLightData>();
            UniversalResourceData resourceData = frameContext.Get<UniversalResourceData>();

            RenderQueueRange queueRange = m_isTransparent
                ? RenderQueueRange.transparent : RenderQueueRange.opaque;
            SortingCriteria sorting = m_isTransparent
                ? SortingCriteria.CommonTransparent : cameraData.defaultOpaqueSortFlags;

            // GameObject Layerは制限せず、Rendering Layerだけで描画対象を選びます。
            FilteringSettings filteringSettings = new FilteringSettings(
                queueRange, layerMask: ~0, renderingLayerMask: m_renderingLayerMask);

            DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(
                m_universalForwardTag, renderingData, cameraData, lightData, sorting);
            drawingSettings.SetShaderPassName(1, m_universalForwardOnlyTag);
            drawingSettings.SetShaderPassName(2, m_srpDefaultUnlitTag);

            RendererListParams rendererListParams = new RendererListParams(
                renderingData.cullResults, drawingSettings, filteringSettings);

            using (IRasterRenderGraphBuilder builder =
                renderGraph.AddRasterRenderPass<PassData>(m_passName, out PassData passData))
            {
                passData.rendererList = renderGraph.CreateRendererList(rendererListParams);

                builder.UseRendererList(passData.rendererList);
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Write);
                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                {
                    // URP側のカメラクリアとSkyboxを保持し、対象Rendererだけを追加します。
                    context.cmd.DrawRendererList(data.rendererList);
                });
            }
        }
    }
}
