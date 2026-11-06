// Derived from NOVA Shader 3.6.0. Copyright (c) CyberAgent, Inc. MIT License.
// Project-local Distortion ver2 extension; the installed NOVA package is unchanged.
#ifndef PROJECT_EFFECT_DISTORTION_VER2_EDITOR_INCLUDED
#define PROJECT_EFFECT_DISTORTION_VER2_EDITOR_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "DistortionVer2Forward.hlsl"

float _ObjectId;
float _PassValue;
float4 _SelectionID;

Varyings vertEditor(Attributes input)
{
    return vert(input);
}

half4 fragSceneHighlight(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    
    // Call frag function to perform alpha test and other visibility checks
    frag(input);
    
    return float4(_ObjectId, _PassValue, 1, 1);
}

half4 fragScenePicking(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    
    // Call frag function to perform alpha test and other visibility checks
    frag(input);
    
    return _SelectionID;
}

#endif