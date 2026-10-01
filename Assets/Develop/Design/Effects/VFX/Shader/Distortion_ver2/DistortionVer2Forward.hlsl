// Derived from NOVA Shader 3.6.0. Copyright (c) CyberAgent, Inc. MIT License.
// Project-local Distortion ver2 extension; the installed NOVA package is unchanged.
#ifndef PROJECT_EFFECT_DISTORTION_VER2_FORWARD_INCLUDED
#define PROJECT_EFFECT_DISTORTION_VER2_FORWARD_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "DistortionVer2.hlsl"

Varyings vert(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    SETUP_VERTEX;
    SETUP_CUSTOM_COORD(input)
    TRANSFER_CUSTOM_COORD(input, output);

    output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
    output.projectedPosition = ComputeScreenPos(output.positionHCS);
    #ifdef _TRANSPARENCY_BY_RIM
    output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    #endif

    float2 baseMapUv = input.texcoord.xy;
    if (_BaseMapRotation > 0.0 || _BaseMapRotationCoord > 0.0)
    {
        half angle = _BaseMapRotation + GET_CUSTOM_COORD(_BaseMapRotationCoord);
        baseMapUv = RotateUV(baseMapUv, angle * PI * 2, _BaseMapRotationOffsets.xy);
    }

    baseMapUv.xy = TRANSFORM_TEX(baseMapUv, _BaseMap);
    baseMapUv.x += GET_CUSTOM_COORD(_BaseMapOffsetXCoord);
    baseMapUv.y += GET_CUSTOM_COORD(_BaseMapOffsetYCoord);
    output.baseUv.xy = baseMapUv;

    #if defined(_FLOW_MAP_ENABLED) || defined(_FLOW_MAP_TARGET_BASE) || defined(_FLOW_MAP_TARGET_ALPHA_TRANSITION)
    float2 flowMapUv = input.texcoord.xy;
    if (_FlowMapRotation > 0.0 || _FlowMapRotationCoord > 0.0)
    {
        half flowAngle = _FlowMapRotation + GET_CUSTOM_COORD(_FlowMapRotationCoord);
        flowMapUv = RotateUV(flowMapUv, flowAngle * PI * 2, _FlowMapRotationOffsets.xy);
    }
    flowMapUv = TRANSFORM_TEX(flowMapUv, _FlowMap);
    flowMapUv.x += GET_CUSTOM_COORD(_FlowMapOffsetXCoord);
    flowMapUv.y += GET_CUSTOM_COORD(_FlowMapOffsetYCoord);
    output.flowTransitionUVs.xy = flowMapUv;
    #endif

    #if defined(_FADE_TRANSITION_ENABLED) || defined(_DISSOLVE_TRANSITION_ENABLED)
    float2 alphaTransitionMapUv = input.texcoord.xy;
    if (_AlphaTransitionMapRotation > 0.0 || _AlphaTransitionMapRotationCoord > 0.0)
    {
        half alphaAngle = _AlphaTransitionMapRotation + GET_CUSTOM_COORD(_AlphaTransitionMapRotationCoord);
        alphaTransitionMapUv = RotateUV(alphaTransitionMapUv, alphaAngle * PI * 2, _AlphaTransitionMapRotationOffsets.xy);
    }
    alphaTransitionMapUv = TRANSFORM_TEX(alphaTransitionMapUv, _AlphaTransitionMap);
    alphaTransitionMapUv.x += GET_CUSTOM_COORD(_AlphaTransitionMapOffsetXCoord);
    alphaTransitionMapUv.y += GET_CUSTOM_COORD(_AlphaTransitionMapOffsetYCoord);
    output.flowTransitionUVs.zw = alphaTransitionMapUv;
    #endif

    return output;
}

// NOVA-style Progress / Sharpness / Inverse. Avoid undefined smoothstep at width=0.
half DistortionVer2Mask(half value, half progress, half sharpness, half inverse)
{
    value = saturate(value);
    if (inverse >= 0.5h) value = 1.0h - value;
    progress = saturate(progress);
    if (progress <= 0.0h) return 1.0h;
    if (progress >= 1.0h) return 0.0h;
    half width = max(1.0h - saturate(sharpness), 0.0001h);
    return smoothstep(lerp(-width, 1.0h, progress), lerp(0.0h, 1.0h + width, progress), value);
}

half4 frag(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    SETUP_FRAGMENT;
    SETUP_CUSTOM_COORD(input);

    #if defined(_FLOW_MAP_ENABLED) || defined(_FLOW_MAP_TARGET_BASE) || defined(_FLOW_MAP_TARGET_ALPHA_TRANSITION)
    half4 flowMapUvOffsetSrc = SAMPLE_TEXTURE2D(_FlowMap, sampler_FlowMap, input.flowTransitionUVs.xy); 
    half2 flowMapUvOffset;
    flowMapUvOffset.x = flowMapUvOffsetSrc[(uint)_FlowMapChannelsX];
    flowMapUvOffset.y = flowMapUvOffsetSrc[(uint)_FlowMapChannelsY];
    flowMapUvOffset = flowMapUvOffset * 2 - 1;
    flowMapUvOffset *= _FlowIntensity + GET_CUSTOM_COORD(_FlowIntensityCoord);
    #if defined(_FLOW_MAP_ENABLED) || defined(_FLOW_MAP_TARGET_BASE)
    input.baseUv.xy += flowMapUvOffset;
    #endif
    #ifdef _FLOW_MAP_TARGET_ALPHA_TRANSITION
    input.flowTransitionUVs.zw += flowMapUvOffset;
    #endif
    #endif

    SamplerState baseMapSamplerState;
    #ifdef BASE_SAMPLER_STATE_OVERRIDE_ENABLED
    baseMapSamplerState = BASE_SAMPLER_STATE_NAME;
    #else
    baseMapSamplerState = sampler_BaseMap;
    #endif
    half4 distortionSrc = SAMPLE_TEXTURE2D(_BaseMap, baseMapSamplerState, input.baseUv.xy);
    if (_BaseMapUnpackNormal)
    {
        // [???] => [-1, 1]
        half3 unpackedNormal = UnpackNormal(distortionSrc);
        // [-1, 1] => [0, 1]
        distortionSrc = half4((unpackedNormal + 1) * 0.5f, 1);
    }

    half2 distortion;
    distortion.x = distortionSrc[(uint)_BaseMapChannelsX];
    distortion.y = distortionSrc[(uint)_BaseMapChannelsY];
    distortion = distortion * 2.0 - 1.0;

    half2 distortionIntensity;
    if (_DistortionIntensityMode > 0.5) // XY mode
    {
        distortionIntensity = half2(
            _DistortionIntensityX + GET_CUSTOM_COORD(_DistortionIntensityXCoord),
            _DistortionIntensityY + GET_CUSTOM_COORD(_DistortionIntensityYCoord)
        );
    }
    else // Single mode
    {
        half intensity = _DistortionIntensity + GET_CUSTOM_COORD(_DistortionIntensityCoord);
        distortionIntensity = half2(intensity, intensity);
    }
    distortion *= 0.1 * distortionIntensity;

    #if defined(_FADE_TRANSITION_ENABLED) || defined(_DISSOLVE_TRANSITION_ENABLED)
    half transitionAlpha = SAMPLE_TEXTURE2D(_AlphaTransitionMap, sampler_AlphaTransitionMap, input.flowTransitionUVs.zw)[_AlphaTransitionMapChannelsX];
    half progress = _AlphaTransitionProgress + GET_CUSTOM_COORD(_AlphaTransitionProgressCoord);
    #ifdef _VERTEX_ALPHA_AS_TRANSITION_PROGRESS
    progress += 1.0 - input.color.a;
    #endif
    progress = min(1.0, progress);

    #ifdef _FADE_TRANSITION_ENABLED
    progress = (progress * 2 - 1) * -1;
    transitionAlpha += progress;
    transitionAlpha = saturate(transitionAlpha);
    #elif _DISSOLVE_TRANSITION_ENABLED
    half dissolveWidth = lerp(0.5, 0.0001, _DissolveSharpness);
    progress = lerp(-dissolveWidth, 1.0 + dissolveWidth, progress);
    transitionAlpha = smoothstep(progress - dissolveWidth, progress + dissolveWidth, transitionAlpha);
    #endif
    distortion *= transitionAlpha;
    #endif

    // Multiply the distortion vector itself: output alpha does not attenuate NOVA distortion.
    #ifdef _TRANSPARENCY_BY_RIM
    half3 normalWS = SafeNormalize(input.normalWS);
    half3 viewWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
    half facing = abs(dot(normalWS, viewWS));
    distortion *= DistortionVer2Mask(facing,
        _RimTransparencyProgress + GET_CUSTOM_COORD(_RimTransparencyProgressCoord),
        _RimTransparencySharpness + GET_CUSTOM_COORD(_RimTransparencySharpnessCoord),
        _InverseRimTransparency);
    #endif

    #ifdef _TRANSPARENCY_BY_LUMINANCE
    // RGB of the sampled distortion map; normal maps use decoded [0,1] RGB.
    distortion *= DistortionVer2Mask(GetLuminance(distortionSrc.rgb),
        _LuminanceTransparencyProgress + GET_CUSTOM_COORD(_LuminanceTransparencyProgressCoord),
        _LuminanceTransparencySharpness + GET_CUSTOM_COORD(_LuminanceTransparencySharpnessCoord),
        _InverseLuminanceTransparency);
    #endif

    #ifdef _SOFT_PARTICLES_ENABLED
    distortion *= SoftParticles(input.projectedPosition, _SoftParticlesIntensity);
    #endif

    #ifdef _DEPTH_FADE_ENABLED
    distortion *= DepthFade(_DepthFadeNear, _DepthFadeFar, _DepthFadeWidth, input.projectedPosition);
    #endif

    return half4(distortion, 0, 1);
}

#endif
