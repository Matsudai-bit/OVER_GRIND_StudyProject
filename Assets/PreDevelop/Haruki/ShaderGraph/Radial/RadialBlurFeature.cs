using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class RadialBlurFeature : ScriptableRendererFeature
{
    class RadialBlurPass : ScriptableRenderPass
    {
        public Material blurMaterial;

        public RadialBlurPass(Material mat)
        {
            blurMaterial = mat;

            // ポストプロセス時にカメラの映像を取得できるようにする設定
            requiresIntermediateTexture = true;
        }

        // Render Graph用のデータ受け渡しクラス
        private class PassData
        {
            public TextureHandle source;
            public Material material;
        }

        // 描画処理（Render Graph専用）
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var volume = VolumeManager.instance.stack.GetComponent<RadialBlurVolume>();
            if (volume == null || !volume.IsActive()) return;
            if (blurMaterial == null) return;

            // 1. Volumeの値をマテリアルにセット
            blurMaterial.SetFloat("_BlurStrength", volume.blurStrength.value);
            blurMaterial.SetInt("_SampleCount", volume.sampleCount.value);
            blurMaterial.SetFloat("_MaskRadius", volume.maskRadius.value);
            blurMaterial.SetFloat("_MaskContrast", volume.maskContrast.value);
            blurMaterial.SetFloat("_DitherStrength", volume.ditherStrength.value);
            blurMaterial.SetVector("_BlurCenter", volume.blurCenter.value);

            // 2. カメラの映像を取得
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            TextureHandle source = resourceData.activeColorTexture;
            if (!source.IsValid()) return;

            // 3. 一時的なテクスチャ（作業用キャンバス）を作成
            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0; // カラーのみ
            TextureHandle tempTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "RadialBlur_Temp", false);

            // 4. 【1回目の描画】 カメラ映像 → 一時テクスチャ（ブラーをかける）
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("RadialBlur_Apply", out var passData))
            {
                passData.source = source;
                passData.material = blurMaterial;

                builder.UseTexture(source, AccessFlags.Read);
                builder.SetRenderAttachment(tempTexture, 0);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, 0);
                });
            }

            // 5. 【2回目の描画】 一時テクスチャ → カメラ映像（画面に戻す）
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("RadialBlur_CopyBack", out var passData))
            {
                passData.source = tempTexture;

                builder.UseTexture(tempTexture, AccessFlags.Read);
                builder.SetRenderAttachment(source, 0);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), 0.0f, false);
                });
            }
        }

        // ※古い形式のメソッドはエラー防止のために空にして残しておきます
        [Obsolete]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) { }
    }

    [SerializeField] private Material material;
    [SerializeField] private RenderPassEvent renderEvent = RenderPassEvent.BeforeRenderingPostProcessing;

    private RadialBlurPass customPass;

    public override void Create()
    {
        customPass = new RadialBlurPass(material);
        customPass.renderPassEvent = renderEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (material == null) return;

        var volume = VolumeManager.instance.stack.GetComponent<RadialBlurVolume>();
        if (volume != null && volume.IsActive())
        {
            renderer.EnqueuePass(customPass);
        }
    }
}