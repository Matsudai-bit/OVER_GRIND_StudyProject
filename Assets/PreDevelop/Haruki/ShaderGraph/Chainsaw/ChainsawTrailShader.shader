Shader "OverGraind/ChainsawShoeTrail_Decal_Color"
{
    Properties
    {
        _Color ("Color Tint", Color) = (1, 1, 1, 1)
        _BaseMap ("Base Map (RGB & Alpha Mask)", 2D) = "white" {}
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Float) = 1.0
        _ORMMap ("ORM Map (R:AO, G:Smoothness, B:Metalness)", 2D) = "white" {}
        
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

            // テクスチャとサンプラーの定義
            TEXTURE2D(_BaseMap);  SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);  SAMPLER(sampler_BumpMap);
            TEXTURE2D(_ORMMap);   SAMPLER(sampler_ORMMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Lifetime;
                float _FadeStartRatio;
                float _TrailTime;
                float _BumpScale;
            CBUFFER_END

            struct Attributes 
            { 
                float4 positionOS : POSITION; 
                float2 uv : TEXCOORD0; 
                float2 birth : TEXCOORD1; 
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
            };

            struct Varyings 
            { 
                float4 positionCS : SV_POSITION; 
                float2 uv : TEXCOORD0; 
                float age : TEXCOORD1; 
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                // タイリングとオフセットを行わず、UVをそのまま渡す
                output.uv = input.uv;
                output.age = _TrailTime - input.birth.x;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // 1. ベースカラーとテクスチャアルファのサンプリング
                half4 baseTex = _BaseMap.Sample(sampler_BaseMap, input.uv) * _Color;
                
                // 2. ORMマップからAmbient Occlusion (Rチャンネル) を取得
                half4 ormTex = _ORMMap.Sample(sampler_ORMMap, input.uv);
                float ao = ormTex.r;

                // 3. 寿命（Lifetime）による時間経過のフェードアウト計算
                float fade = 1 - smoothstep(_Lifetime * _FadeStartRatio, _Lifetime, input.age);

                // 4. ベースカラーテクスチャのアルファを形状マスクとして使用し、フェードを掛ける
                half alpha = baseTex.a * fade;

                // 5. 不要なピクセルをクリップ
                clip(alpha - 0.01);

                // ベースカラーにAO（陰影）を適用
                half3 finalRgb = baseTex.rgb * ao;

                return half4(finalRgb, alpha);
            }
            ENDHLSL
        }
    }
}