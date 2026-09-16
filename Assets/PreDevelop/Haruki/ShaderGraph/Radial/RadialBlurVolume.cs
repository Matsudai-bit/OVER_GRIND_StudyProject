using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Unity 2023.1以降対応の書き方
[Serializable]
[VolumeComponentMenu("Custom/Radial Blur")]
[SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
public class RadialBlurVolume : VolumeComponent, IPostProcessComponent
{
    public ClampedFloatParameter blurStrength = new ClampedFloatParameter(0f, 0f, 1f);
    public ClampedIntParameter sampleCount = new ClampedIntParameter(16, 4, 64);
    public ClampedFloatParameter maskRadius = new ClampedFloatParameter(0.2f, 0f, 1f);
    public ClampedFloatParameter maskContrast = new ClampedFloatParameter(2.5f, 0.1f, 10f);
    public ClampedFloatParameter ditherStrength = new ClampedFloatParameter(1.0f, 0f, 1f);
    public Vector2Parameter blurCenter = new Vector2Parameter(new Vector2(0.5f, 0.5f));

    // ブラーの強度が0より大きいときだけアクティブと判定
    public bool IsActive() => blurStrength.value > 0f;
    public bool IsTileCompatible() => false;
}