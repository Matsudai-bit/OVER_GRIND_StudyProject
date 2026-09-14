Shader "PostProcess/RadialBlurAdvanced"
{
    Properties
    {
        [Header(Blur Settings)]
        _BlurCenter ("Blur Center", Vector) = (0.5, 0.5, 0, 0)
        _BlurStrength ("Total Strength", Range(0, 1.0)) = 0.15
        _SampleCount ("Sample Count", Range(4, 64)) = 16
        
        [Header(Mask Settings)]
        _MaskRadius ("Mask Radius (Clear Area)", Range(0, 1.0)) = 0.2
        _MaskContrast ("Mask Contrast", Range(0.1, 10.0)) = 2.5
        
        [Header(Dither Settings)]
        _DitherStrength ("Dither Strength", Range(0, 1.0)) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZWrite Off Cull Off ZTest Always

        Pass
        {
            Name "RadialBlurAdvancedPass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float2 _BlurCenter;
            float _BlurStrength;
            int _SampleCount;
            
            float _MaskRadius;
            float _MaskContrast;
            
            float _DitherStrength;

            // ディザリング用のピクセルごとの疑似乱数生成関数
            float Random(float2 pos)
            {
                return frac(sin(dot(pos, float2(12.9898, 78.233))) * 43758.5453);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                float2 uv = input.texcoord;
                float2 dir = uv - _BlurCenter;

                // --- 1 & 2: 画面端マスクとコントラストの計算 ---
                // アスペクト比を補正して、マスクが縦長/横長に潰れるのを防ぎ真円にする
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 aspectDir = dir * float2(aspect, 1.0);
                float dist = length(aspectDir);

                // _MaskRadius以下は0（ブラーなし）、それ以上は_MaskContrastに応じてグラデーション
                float mask = saturate((dist - _MaskRadius) * _MaskContrast);

                // --- 3: 最終的なブラー強度の決定 ---
                float finalStrength = _BlurStrength * mask;

                // --- 5: ディザリングの計算 ---
                // 画面のピクセル座標(positionCS)を使ってランダム値を生成
                float dither = Random(input.positionCS.xy);
                // -0.5 ～ +0.5 の間でサンプリング位置を微小に前後にズラす（ジッター）
                float offset = (dither - 0.5) * _DitherStrength;

                // --- 4: 指定サンプル数でのサンプリング処理 ---
                half4 color = half4(0, 0, 0, 0);
                int samples = max(1, _SampleCount);

                for (int i = 0; i < samples; i++)
                {
                    // ディザオフセットを加えてサンプリングの偏りを散らす
                    float t = ((float)i + offset) / (float)samples;
                    
                    // 中心へ向かってUVを引っ張る
                    float2 sampleUV = uv - dir * (finalStrength * t);
                    
                    color += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, sampleUV);
                }

                return color / (float)samples;
            }
            ENDHLSL
        }
    }
}