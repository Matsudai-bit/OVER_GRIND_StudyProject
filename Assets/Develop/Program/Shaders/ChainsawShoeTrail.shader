Shader "OverGraind/ChainsawShoeTrail"
{
    Properties
    {
        _Color ("Cut color", Color) = (0.065, 0.045, 0.025, 0.9)
        _Lifetime ("Lifetime", Float) = 2
        _FadeStartRatio ("Fade start ratio", Range(0, 0.99)) = 0.5
        _TrailTime ("Trail time", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-10" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            Offset -1, -1
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Lifetime;
                float _FadeStartRatio;
                float _TrailTime;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; float2 birth : TEXCOORD1; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float age : TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.age = _TrailTime - input.birth.x;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                // 中央の切削溝に、左右の刃が刻んだ斜めの傷を重ねます。
                float lateral = abs(input.uv.x - 0.5) * 2;
                float groove = 1 - smoothstep(0.13, 0.3, lateral);
                float toothPhase = frac(input.uv.y * 24 - lateral * 0.48);
                float teeth = (1 - smoothstep(0.12, 0.32, toothPhase)) * (1 - smoothstep(0.65, 1, lateral));
                float scratches = 0.65 + 0.35 * sin(input.uv.x * 97 + sin(input.uv.y * 31));
                float fade = 1 - smoothstep(_Lifetime * _FadeStartRatio, _Lifetime, input.age);
                half alpha = saturate(max(groove, teeth * 0.85) * scratches * fade * _Color.a);
                clip(alpha - 0.01);
                return half4(_Color.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
