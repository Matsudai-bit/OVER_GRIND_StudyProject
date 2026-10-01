// Derived from NOVA Shader 3.6.0. Copyright (c) CyberAgent, Inc. MIT License.
// Project-local Distortion ver2 extension; the installed NOVA package is unchanged.
#ifndef PROJECT_EFFECT_DISTORTION_VER2_INCLUDED
#define PROJECT_EFFECT_DISTORTION_VER2_INCLUDED

#include "Packages/jp.co.cyberagent.nova/Runtime/Core/Shaders/Particles.hlsl"

struct Attributes
{
    float4 positionOS : POSITION;
    #ifdef _TRANSPARENCY_BY_RIM
    float3 normalOS : NORMAL;
    #endif
    float2 texcoord : TEXCOORD0;
    #ifndef NOVA_PARTICLE_INSTANCING_ENABLED
    INPUT_CUSTOM_COORD(1, 2)
    #endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionHCS : SV_POSITION;
    INPUT_CUSTOM_COORD(0, 1)
    float2 baseUv : TEXCOORD2;
    float4 flowTransitionUVs : TEXCOORD3; // xy: FlowMap UV, zw: TransitionMap UV
    float4 projectedPosition: TEXCOORD4;
    #ifdef _TRANSPARENCY_BY_RIM
    float3 positionWS : TEXCOORD5;
    half3 normalWS : TEXCOORD6;
    #endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);
TEXTURE2D(_FlowMap);
SAMPLER(sampler_FlowMap);
TEXTURE2D(_AlphaTransitionMap);
SAMPLER(sampler_AlphaTransitionMap);

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    DECLARE_CUSTOM_COORD(_BaseMapOffsetXCoord);
    DECLARE_CUSTOM_COORD(_BaseMapOffsetYCoord);
    half _BaseMapChannelsX;
    half _BaseMapChannelsY;

    float _BaseMapRotation;
    DECLARE_CUSTOM_COORD(_BaseMapRotationCoord);
    float4 _BaseMapRotationOffsets;
    half _DistortionIntensityMode;
    float _DistortionIntensity;
    DECLARE_CUSTOM_COORD(_DistortionIntensityCoord);
    float _DistortionIntensityX;
    DECLARE_CUSTOM_COORD(_DistortionIntensityXCoord);
    float _DistortionIntensityY;
    DECLARE_CUSTOM_COORD(_DistortionIntensityYCoord);
    half _BaseMapUnpackNormal;

    float4 _FlowMap_ST;
    DECLARE_CUSTOM_COORD(_FlowMapOffsetXCoord);
    DECLARE_CUSTOM_COORD(_FlowMapOffsetYCoord);
    half _FlowMapChannelsX;
    half _FlowMapChannelsY;

    float _FlowIntensity;
    DECLARE_CUSTOM_COORD(_FlowIntensityCoord);
    float _FlowMapRotation;
    DECLARE_CUSTOM_COORD(_FlowMapRotationCoord);
    float4 _FlowMapRotationOffsets;

    float4 _AlphaTransitionMap_ST;
    DECLARE_CUSTOM_COORD(_AlphaTransitionMapOffsetXCoord);
    DECLARE_CUSTOM_COORD(_AlphaTransitionMapOffsetYCoord);
    half _AlphaTransitionMapChannelsX;

    float _AlphaTransitionProgress;
    DECLARE_CUSTOM_COORD(_AlphaTransitionProgressCoord);
    float _DissolveSharpness;
    float _AlphaTransitionMapRotation;
    DECLARE_CUSTOM_COORD(_AlphaTransitionMapRotationCoord);
    float4 _AlphaTransitionMapRotationOffsets;

    float _SoftParticlesIntensity;
    float _DepthFadeNear;
    float _DepthFadeFar;
    float _DepthFadeWidth;
    float _RimTransparencyProgress;
    DECLARE_CUSTOM_COORD(_RimTransparencyProgressCoord);
    float _RimTransparencySharpness;
    DECLARE_CUSTOM_COORD(_RimTransparencySharpnessCoord);
    float _InverseRimTransparency;
    float _LuminanceTransparencyProgress;
    DECLARE_CUSTOM_COORD(_LuminanceTransparencyProgressCoord);
    float _LuminanceTransparencySharpness;
    DECLARE_CUSTOM_COORD(_LuminanceTransparencySharpnessCoord);
    float _InverseLuminanceTransparency;
CBUFFER_END

#endif
