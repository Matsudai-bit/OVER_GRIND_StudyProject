Shader "Custom/URP/LocalHeightFog"
{
    Properties
    {
        _FogColor ("Fog Color", Color) = (0.75, 0.8, 0.85, 1.0)
        _BottomDarkness ("Bottom Darkness", Range(0.0, 0.8)) = 0.35
        _BaseDensity ("Base Density", Range(0.0, 5.0)) = 1.8
        _MinHeight ("Min Height (World Y)", Float) = 0.0
        _MaxHeight ("Max Height (World Y)", Float) = 1.6
        _HeightFalloff ("Height Falloff", Range(0.5, 5.0)) = 2.0
        _NearClearDistance ("Near Clear Distance", Float) = 1.2
        _VisibilityDistance ("Visibility Distance", Float) = 4.5
        _NoiseScale ("Noise Scale", Range(0.1, 3.0)) = 0.65
        _NoiseSpeed ("Noise Speed", Vector) = (0.15, -0.05, 0.1, 0.0)
        _NoiseDetailStrength ("Noise Detail Strength", Range(0.0, 1.0)) = 0.6
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+50"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "LocalHeightFogPass"

            // 裏面を描画することで、カメラが常に霧のボックス（室内）の中にあっても描画を維持する
            Cull Front
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            // レイマーチングの分割ステップ数（品質と負荷のバランスに優れた8ステップ）
            #define RAYMARCH_STEPS 8
            #define EPSILON 0.0001

            CBUFFER_START(UnityPerMaterial)
                half4 _FogColor;
                float _BottomDarkness;
                float _BaseDensity;
                float _MinHeight;
                float _MaxHeight;
                float _HeightFalloff;
                float _NearClearDistance;
                float _VisibilityDistance;
                float _NoiseScale;
                float4 _NoiseSpeed;
                float _NoiseDetailStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            /// <summary>
            /// 3D座標から擬似乱数（0.0〜1.0）を生成するハッシュ関数。
            /// </summary>
            float CalculateHash3D(float3 gridPos)
            {
                gridPos = frac(gridPos * float3(0.1031, 0.1030, 0.0973));
                gridPos += dot(gridPos, gridPos.yxz + 33.33);
                return frac((gridPos.x + gridPos.y) * gridPos.z);
            }

            /// <summary>
            /// 滑らかな3Dバリューノイズを生成します（外部テクスチャ不要で霧のうねりを表現）。
            /// </summary>
            float SampleValueNoise3D(float3 worldPos)
            {
                float3 cellInt = floor(worldPos);
                float3 cellFrac = frac(worldPos);

                // エルミート補間で滑らかに繋ぐ
                float3 smoothWeight = cellFrac * cellFrac * (3.0 - 2.0 * cellFrac);

                float n000 = CalculateHash3D(cellInt + float3(0.0, 0.0, 0.0));
                float n100 = CalculateHash3D(cellInt + float3(1.0, 0.0, 0.0));
                float n010 = CalculateHash3D(cellInt + float3(0.0, 1.0, 0.0));
                float n110 = CalculateHash3D(cellInt + float3(1.0, 1.0, 0.0));
                float n001 = CalculateHash3D(cellInt + float3(0.0, 0.0, 1.0));
                float n101 = CalculateHash3D(cellInt + float3(1.0, 0.0, 1.0));
                float n011 = CalculateHash3D(cellInt + float3(0.0, 1.0, 1.0));
                float n111 = CalculateHash3D(cellInt + float3(1.0, 1.0, 1.0));

                float nx00 = lerp(n000, n100, smoothWeight.x);
                float nx10 = lerp(n010, n110, smoothWeight.x);
                float nx01 = lerp(n001, n101, smoothWeight.x);
                float nx11 = lerp(n011, n111, smoothWeight.x);

                float nxy0 = lerp(nx00, nx10, smoothWeight.y);
                float nxy1 = lerp(nx01, nx11, smoothWeight.y);

                return lerp(nxy0, nxy1, smoothWeight.z);
            }

            /// <summary>
            /// 2オクターブの3Dノイズを重ねてリアルな霧の濃淡（FBM）を計算します。
            /// </summary>
            float SampleFogFbm(float3 samplePos)
            {
                float3 scrollOffset = _Time.y * _NoiseSpeed.xyz;
                float3 p1 = samplePos * _NoiseScale + scrollOffset;
                float3 p2 = samplePos * (_NoiseScale * 2.15) - scrollOffset * 1.4;

                float noise1 = SampleValueNoise3D(p1);
                float noise2 = SampleValueNoise3D(p2);

                return noise1 * 0.65 + noise2 * 0.35;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // 濃度が0以下の場合は描画しない
                if (_BaseDensity <= 0.001)
                {
                    return half4(0.0, 0.0, 0.0, 0.0);
                }

                // 画面のUV座標からシーンの深度を取得
                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
                float rawDepth = SampleSceneDepth(screenUV);

                #if !UNITY_REVERSED_Z
                rawDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, rawDepth);
                #endif

                // 深度バッファから奥の壁・床・キャラクターのワールド座標を復元
                float3 sceneWorldPos = ComputeWorldSpacePosition(screenUV, rawDepth, UNITY_MATRIX_I_VP);
                float3 cameraWorldPos = GetCameraPositionWS();

                // ボックス（Cube: ローカル座標 -0.5 〜 0.5）と視線線分の交差判定（スラブ法）
                float3 localCamPos = TransformWorldToObject(cameraWorldPos);
                float3 localScenePos = TransformWorldToObject(sceneWorldPos);
                float3 localRayVec = localScenePos - localCamPos;

                // ゼロ除算防止用の安全なベクトル生成
                float3 safeRay = localRayVec;
                safeRay.x = abs(safeRay.x) < EPSILON ? (safeRay.x >= 0.0 ? EPSILON : -EPSILON) : safeRay.x;
                safeRay.y = abs(safeRay.y) < EPSILON ? (safeRay.y >= 0.0 ? EPSILON : -EPSILON) : safeRay.y;
                safeRay.z = abs(safeRay.z) < EPSILON ? (safeRay.z >= 0.0 ? EPSILON : -EPSILON) : safeRay.z;
                float3 invRay = 1.0 / safeRay;

                float3 t0 = (-0.5 - localCamPos) * invRay;
                float3 t1 = ( 0.5 - localCamPos) * invRay;
                float3 tMin3 = min(t0, t1);
                float3 tMax3 = max(t0, t1);

                // tEnter=0.0はカメラ位置、tExit=1.0は壁やキャラクターの表面位置
                float tEnter = max(0.0, max(tMin3.x, max(tMin3.y, tMin3.z)));
                float tExit  = min(1.0, min(tMax3.x, min(tMax3.y, tMax3.z)));

                // ボックスを通過していない、または手前の壁に完全に遮られている場合はスキップ
                if (tExit <= tEnter)
                {
                    return half4(0.0, 0.0, 0.0, 0.0);
                }

                // ワールド空間でのレイ開始点・終了点・距離を算出
                float3 worldSceneVec = sceneWorldPos - cameraWorldPos;
                float totalSceneDist = length(worldSceneVec);
                if (totalSceneDist < EPSILON)
                {
                    return half4(0.0, 0.0, 0.0, 0.0);
                }

                float3 rayDir = worldSceneVec / totalSceneDist;
                float startDist = totalSceneDist * tEnter;
                float endDist   = totalSceneDist * tExit;

                // 三人称カメラ配慮：カメラ直前の視界確保距離（NearClearDistance）を適用
                startDist = max(startDist, _NearClearDistance);
                if (endDist <= startDist)
                {
                    return half4(0.0, 0.0, 0.0, 0.0);
                }

                // 近距離の視界制限を優先するため、計算する最大レイ長さを適切にクランプして精度を高める
                float maxMarchDist = startDist + (_VisibilityDistance * 3.5);
                endDist = min(endDist, maxMarchDist);

                float marchLength = endDist - startDist;
                float stepSize = marchLength / (float)RAYMARCH_STEPS;
                float heightRange = max(0.01, _MaxHeight - _MinHeight);

                float accumulatedDensity = 0.0;
                float weightedHeightSum = 0.0;
                float weightSum = 0.0;

                // 画面上のピクセル位置による微小ジッター（縞模様・バンディングを防止）
                float jitter = frac(sin(dot(input.positionCS.xy, float2(12.9898, 78.233))) * 43758.5453);

                // レイマーチングで霧の厚みと3Dノイズを積分
                [unroll]
                for (int i = 0; i < RAYMARCH_STEPS; i++)
                {
                    float currentDist = startDist + ((float)i + jitter) * stepSize;
                    float3 samplePos = cameraWorldPos + rayDir * currentDist;

                    // 3Dノイズを取得
                    float noiseVal = SampleFogFbm(samplePos);

                    // 霧の上面が平らな板に見えないよう、ノイズで上限高さを波打たせる
                    float noisyHeightOffset = (noiseVal - 0.5) * heightRange * 0.45 * _NoiseDetailStrength;
                    float normalizedHeight = (samplePos.y - _MinHeight + noisyHeightOffset) / heightRange;

                    // 指定した高さ範囲内（底面〜上限高さ）にある場合のみ霧を加算
                    if (normalizedHeight < 1.0)
                    {
                        float clampedHeight = saturate(normalizedHeight);

                        // 下に溜まる減衰計算（底面ほど濃く、上限に向かうにつれて滑らかに消える）
                        float heightFactor = pow(1.0 - clampedHeight, _HeightFalloff);

                        // 空間の濃淡ムラ（塊感）を適用
                        float detailFactor = lerp(1.0, noiseVal * 1.7, _NoiseDetailStrength);

                        // 近距離で視界を制限するための距離重み付け
                        float distFromNear = max(0.0, currentDist - _NearClearDistance);
                        float visibilityScale = saturate(distFromNear / max(0.1, _VisibilityDistance));
                        float distanceBoost = lerp(0.5, 2.0, visibilityScale);

                        float localDensity = heightFactor * detailFactor * _BaseDensity * distanceBoost;
                        float stepContribution = localDensity * stepSize;

                        accumulatedDensity += stepContribution;
                        weightedHeightSum += clampedHeight * stepContribution;
                        weightSum += stepContribution;
                    }
                }

                if (accumulatedDensity <= 0.0001)
                {
                    return half4(0.0, 0.0, 0.0, 0.0);
                }

                // Beer-Lambertの法則によるリアルな透過率・不透明度変換
                float fogAlpha = saturate(1.0 - exp(-accumulatedDensity / max(0.25, _VisibilityDistance * 0.35)));

                // 底に溜まっている部分ほど少し暗く落ち着かせ、上層は明るくして立体感を出す
                float avgHeight = weightSum > EPSILON ? saturate(weightedHeightSum / weightSum) : 0.5;
                float brightness = lerp(1.0 - _BottomDarkness, 1.05, avgHeight);
                half3 finalColor = _FogColor.rgb * brightness;

                return half4(finalColor, fogAlpha * _FogColor.a);
            }
            ENDHLSL
        }
    }
}