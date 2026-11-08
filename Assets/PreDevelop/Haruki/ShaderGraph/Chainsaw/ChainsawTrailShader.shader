Shader "OverGraind/ChainsawShoeTrail_Lit"
{
    Properties
    {
        _BaseColor ("Color Tint", Color) = (1, 1, 1, 1)
        [NoScaleOffset] _BaseMap ("Base Map (RGB & Alpha Mask)", 2D) = "white" {}

        _Brightness ("Brightness", Range(0, 2)) = 1.0
        _Contrast ("Contrast", Range(0, 2)) = 1.0

        [NoScaleOffset] _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Range(-5, 5)) = 1.0

        [NoScaleOffset] _ORMMap ("ORM Map (R:AO, G:Smoothness, B:Metalness)", 2D) = "white" {}
        _SmoothnessMulti ("Smoothness Multiplier", Range(0, 1)) = 1.0
        _MetallicMulti ("Metallic Multiplier", Range(0, 1)) = 1.0

        [Toggle(_FORCE_DERIVATIVE_TBN)] _ForceTBN ("Ignore Mesh Normal/Tangent (Derivative TBN)", Float) = 0

        _Lifetime ("Lifetime", Float) = 2
        _FadeStartRatio ("Fade start ratio", Range(0, 0.99)) = 0.5
        _TrailTime ("Trail time", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-10" "RenderType"="Transparent" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            Offset -1, -1

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag

            // 透明扱い（スクリーンスペースシャドウを使わせない）
            #define _SURFACE_TYPE_TRANSPARENT 1

            #pragma shader_feature_local_fragment _FORCE_DERIVATIVE_TBN

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile_fragment _ DEBUG_DISPLAY
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);  SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);  SAMPLER(sampler_BumpMap);
            TEXTURE2D(_ORMMap);   SAMPLER(sampler_ORMMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _Lifetime;
                float _FadeStartRatio;
                float _TrailTime;
                float _BumpScale;
                float _Brightness;
                float _Contrast;
                float _SmoothnessMulti;
                float _MetallicMulti;
                float _ForceTBN;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;   // 実質ワールド座標
                float2 uv         : TEXCOORD0;
                float2 birth      : TEXCOORD1;
                float3 normalOS   : NORMAL;     // 実質ワールド空間の法線
                float4 tangentOS  : TANGENT;    // 実質ワールド空間の接線
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float  age        : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float3 normalWS   : TEXCOORD3;  // 正規化しない（0判定に使う）
                float4 tangentWS  : TEXCOORD4;  // xyz:接線, w:符号
                half   fogFactor  : TEXCOORD5;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;

                float3 positionWS = input.positionOS.xyz;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.positionWS = positionWS;

                output.uv  = input.uv;
                output.age = _TrailTime - input.birth.x;

                output.normalWS  = input.normalOS;
                output.tangentWS = input.tangentOS;

                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            // メッシュの法線/接線が有効ならそれを使い、無効（0）なら画面微分から生成する
            void BuildFrame(float3 positionWS, float2 uv, float3 meshN, float4 meshT, float3 viewDirWS,
                            out float3 N, out float3 T, out float3 B)
            {
                float3 dp1  = ddx(positionWS);
                float3 dp2  = ddy(positionWS);
                float2 duv1 = ddx(uv);
                float2 duv2 = ddy(uv);

                // 幾何法線（微分から）
                float3 Ng = SafeNormalize(cross(dp1, dp2));

                // 微分ベースの接線フレーム
                float3 dp2perp = cross(dp2, Ng);
                float3 dp1perp = cross(Ng, dp1);
                float3 Td = dp2perp * duv1.x + dp1perp * duv2.x;
                float3 Bd = dp2perp * duv1.y + dp1perp * duv2.y;
                float invmax = rsqrt(max(max(dot(Td, Td), dot(Bd, Bd)), 1e-20));
                Td *= invmax;
                Bd *= invmax;

                float nLen2 = dot(meshN, meshN);
                float tLen2 = dot(meshT.xyz, meshT.xyz);

                #if defined(_FORCE_DERIVATIVE_TBN)
                    bool useMeshN = false;
                    bool useMeshT = false;
                #else
                    bool useMeshN = nLen2 > 0.25;
                    bool useMeshT = useMeshN && (tLen2 > 0.25);
                #endif

                N = useMeshN ? meshN * rsqrt(max(nLen2, 1e-20)) : Ng;

                // Cull Off なので、カメラ側を向くように法線を反転
                float flip = (dot(N, viewDirWS) < 0.0) ? -1.0 : 1.0;
                N *= flip;

                if (useMeshT)
                {
                    T = meshT.xyz * rsqrt(max(tLen2, 1e-20));
                    T = SafeNormalize(T - N * dot(N, T));
                    B = cross(N, T) * (meshT.w * flip);
                }
                else
                {
                    T = SafeNormalize(Td - N * dot(N, Td));
                    float handed = (dot(cross(N, T), Bd) < 0.0) ? -1.0 : 1.0;
                    B = cross(N, T) * handed;
                }
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 positionWS = input.positionWS;
                float3 viewDirWS  = GetWorldSpaceNormalizeViewDir(positionWS);

                // 1. テクスチャサンプリング
                half4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                half4 ormTex  = SAMPLE_TEXTURE2D(_ORMMap,  sampler_ORMMap,  input.uv);
                half4 nrmTex  = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv);

                // 2. TBN構築（微分を使うので clip より前に実行）
                float3 N, T, B;
                BuildFrame(positionWS, input.uv, input.normalWS, input.tangentWS, viewDirWS, N, T, B);

                // 3. 寿命フェード
                float fade = 1.0 - smoothstep(_Lifetime * _FadeStartRatio, _Lifetime, input.age);
                half alpha = baseTex.a * fade;
                clip(alpha - 0.01);

                // 4. アルベド調整
                half3 albedo = baseTex.rgb;
                albedo = (albedo - 0.5) * _Contrast + 0.5;
                albedo = saturate(albedo * _Brightness);

                // 5. ノーマルマップ展開
                float3 normalTS = UnpackNormalScale(nrmTex, _BumpScale);
                float3 normalWS = NormalizeNormalPerPixel(TransformTangentToWorld(normalTS, float3x3(T, B, N)));

                // 6. SurfaceData
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo     = albedo;
                surfaceData.specular   = half3(0, 0, 0);
                surfaceData.metallic   = ormTex.b * _MetallicMulti;
                surfaceData.smoothness = ormTex.g * _SmoothnessMulti;
                surfaceData.normalTS   = normalTS;
                surfaceData.occlusion  = ormTex.r;
                surfaceData.alpha      = alpha;

                // 7. InputData
                InputData inputData = (InputData)0;
                inputData.positionWS = positionWS;
                inputData.positionCS = input.positionCS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = viewDirWS;
                inputData.shadowCoord = TransformWorldToShadowCoord(positionWS);
                inputData.fogCoord = input.fogFactor;
                inputData.vertexLighting = half3(0, 0, 0);
                inputData.bakedGI = SampleSH(normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                // 8. URP標準PBRライティング
                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                color.a = alpha;
                return color;
            }
            ENDHLSL
        }
    }
}