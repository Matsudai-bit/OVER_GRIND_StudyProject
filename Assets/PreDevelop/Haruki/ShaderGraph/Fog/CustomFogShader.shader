Shader "Hidden/CustomHeightFog"
{
    Properties
    {
        _FogColor ("Fog Base Color", Color) = (0.5, 0.55, 0.6, 1)
        _FogDensity ("Overall Density", Range(0, 0.05)) = 0.003
        _FogStartDist ("Start Distance", Float) = 5.0

        _FogHeight ("Fog Base Height (Y)", Float) = 0.0
        _FogHeightFalloff ("Height Falloff", Range(0.001, 0.5)) = 0.05

        _SunColor ("Sun Scattering Color", Color) = (1.0, 0.9, 0.7, 1)
        _SunIntensity ("Sun Intensity", Range(0, 20)) = 2.0
        _SunDir ("Sun Direction (World Space)", Vector) = (0, 0.5, 1, 0)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "CustomHeightFogPass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            // 頂点データの構造体（Fullscreen Pass用の頂点IDを受け取る）
            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _FogColor;
                float _FogDensity;
                float _FogStartDist;
                float _FogHeight;
                float _FogHeightFalloff;
                float4 _SunColor;
                float _SunIntensity;
                float4 _SunDir;
            CBUFFER_END

            TEXTURE2D(_BlitTexture);
            SAMPLER(sampler_BlitTexture);

            // URPの標準関数を使って確実に画面全体にクアッドを描画する頂点シェーダー
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.uv = GetFullScreenTriangleTexCoord(input.vertexID);
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {

                // 1. 元の画面カラーのサンプリング
                float4 screenColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, input.uv);

                // 2. 深度の取得とワールド座標の復元
                float depth = SampleSceneDepth(input.uv);
                if (depth == 0.0) return screenColor; // 背景（空など）の除外

                float3 worldPos = ComputeWorldSpacePosition(input.uv, depth, UNITY_MATRIX_I_VP);
                float3 cameraPos = GetCameraPositionWS();
                
                float3 ray = worldPos - cameraPos;
                float dist = length(ray);
                float3 viewDir = ray / max(dist, 0.0001);

                // 3. 距離によるフォグ係数の計算
                float distFactor = saturate((dist - _FogStartDist) * _FogDensity);

                // 4. 指数関数的ハイトフォグの計算 (Exponential Height Fog)
                float heightDiff = worldPos.y - _FogHeight;
                float heightFactor = exp(-heightDiff * _FogHeightFalloff);
                
                float fogFactor = 1.0 - exp(-dist * _FogDensity * heightFactor);
                fogFactor = saturate(fogFactor);

                // 5. ディレクショナル・イン・スキャッタリング（太陽光の方向による輝き）
                float3 sunDirNormalized = normalize(_SunDir.xyz);
                float sunDot = saturate(dot(viewDir, sunDirNormalized));
                float sunScattering = pow(sunDot, 8.0) * _SunIntensity;

                // フォグの最終カラー（ベースカラー ＋ 太陽光の照り返し）
                float3 finalFogColor = _FogColor.rgb + (_SunColor.rgb * sunScattering);

                // 6. 画面の色とフォグのブレンド
                float3 finalColor = lerp(screenColor.rgb, finalFogColor, fogFactor);

                return float4(finalColor, screenColor.a);
            }
            ENDHLSL
        }
    }
}