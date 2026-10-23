// Made with Amplify Shader Editor v1.9.9.5
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Vefects/SH_Vefects_Impact_Frame_Advanced_01_Always"
{
	Properties
	{
		[HideInInspector] _EmissionColor("Emission Color", Color) = (1,1,1,1)
		_ContrastErosion( "Contrast Erosion", Float ) = 0
		_ContrastErosionSmoothness( "Contrast Erosion Smoothness", Float ) = 0.1
		[Space(33)][Header(LUT Color)][Space(13)] _LUTColor( "LUT Color", 2D ) = "white" {}
		_LUTColorAmplitude( "LUT Color Amplitude", Float ) = 1
		_LUTColorLerp( "LUT Color Lerp", Float ) = 0
		_LUTColorOffset( "LUT Color Offset", Float ) = 0
		_LUTColorSpeed( "LUT Color Speed", Float ) = 0
		[Space(33)][Header(LUT Core)][Space(13)] _LUTCoreMask( "LUT Core Mask", 2D ) = "white" {}
		_LUTCoreAmplitude( "LUT Core Amplitude", Float ) = 2
		_LUTCoreCanInvert( "LUT Core Can Invert", Float ) = 1
		_LUTCoreColor( "LUT Core Color", Color ) = ( 0, 0, 0, 0 )
		_LUTCoreColorInverted( "LUT Core Color Inverted", Color ) = ( 1, 1, 1, 0 )
		_LUTCoreDistortAmount( "LUT Core Distort Amount", Float ) = 0.2
		_LUTCoreLerp( "LUT Core Lerp", Float ) = 0
		_LUTCoreNoiseSize( "LUT Core Noise Size", Vector ) = ( 1, 0.01, 0, 0 )
		_LUTCoreNoiseSpeed( "LUT Core Noise Speed", Vector ) = ( 3.333, 10, 0, 0 )
		_LUTCoreOffset( "LUT Core Offset", Float ) = 0
		_LUTCoreOverParticleColorLerp( "LUT Core Over Particle Color Lerp", Float ) = 0
		_LUTCoreQuantizeUVRadius( "LUT Core Quantize UV Radius", Float ) = 2048
		_LUTCoreSpeed( "LUT Core Speed", Float ) = 0
		[Space(33)][Header(Mask)][Space(13)] _MaskRadius( "Mask Radius", Float ) = 0
		_MaskHardness( "Mask Hardness", Float ) = 1
		_MaskErosion( "Mask Erosion", Float ) = 0
		_MaskErosionSmoothness( "Mask Erosion Smoothness", Float ) = 0.1
		[Space(33)][Header(Radial)][Space(33)] _NoiseTexture( "Noise Texture", 2D ) = "white" {}
		_Boost( "Boost", Float ) = 1
		_QuantizeUVRadius( "Quantize UV Radius", Float ) = 2048
		_QuantizedRadiusLerp( "Quantized Radius Lerp", Float ) = 0
		_RadialDistortionAmount( "Radial Distortion Amount", Float ) = 0.05
		_RadialDistortionCheapContrastLerp( "Radial Distortion Cheap Contrast Lerp", Float ) = 1
		_RadialMask( "Radial Mask", Float ) = 0
		_RadialMaskStrength( "Radial Mask Strength", Float ) = 1
		_Size( "Size", Vector ) = ( 10, 0.01, 0, 0 )
		_Speed( "Speed", Vector ) = ( 3.333, 10, 0, 0 )
		[Space(33)][Header(AR)][Space(13)] _Cull( "Cull", Float ) = 2
		_Src( "Src", Float ) = 5
		_Dst( "Dst", Float ) = 10
		_ZWrite( "ZWrite", Float ) = 0
		_ZTest( "ZTest", Float ) = 8


		//_TessPhongStrength( "Tess Phong Strength", Range( 0, 1 ) ) = 0.5
		//_TessValue( "Tess Max Tessellation", Range( 1, 32 ) ) = 16
		//_TessMin( "Tess Min Distance", Float ) = 10
		//_TessMax( "Tess Max Distance", Float ) = 25
		//_TessEdgeLength ( "Tess Edge length", Range( 2, 50 ) ) = 16
		//_TessMaxDisp( "Tess Max Displacement", Float ) = 25

		[HideInInspector] _QueueOffset("_QueueOffset", Float) = 0
        [HideInInspector] _QueueControl("_QueueControl", Float) = -1

        [HideInInspector][NoScaleOffset] unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}

		//[HideInInspector][ToggleUI] _AddPrecomputedVelocity("Add Precomputed Velocity", Float) = 1
		[ToggleUI] _ReceiveShadows("Receive Shadows", Float) = 1.0
	}

	SubShader
	{
		LOD 0

		

		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" "UniversalMaterialType"="Unlit" }

		Cull Back
		AlphaToMask Off

		

		HLSLINCLUDE
		#pragma target 4.5
		#pragma prefer_hlslcc gles
		// ensure rendering platforms toggle list is visible

		#if ( SHADER_TARGET > 35 ) && defined( SHADER_API_GLES3 )
			#error For WebGL2/GLES3, please set your shader target to 3.5 via SubShader options. URP shaders in ASE use target 4.5 by default.
		#endif

		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"

		#ifndef ASE_TESS_FUNCS
		#define ASE_TESS_FUNCS
		float4 FixedTess( float tessValue )
		{
			return tessValue;
		}

		float CalcDistanceTessFactor (float4 vertex, float minDist, float maxDist, float tess, float4x4 o2w, float3 cameraPos )
		{
			float3 wpos = mul(o2w,vertex).xyz;
			float dist = distance (wpos, cameraPos);
			float f = clamp(1.0 - (dist - minDist) / (maxDist - minDist), 0.01, 1.0) * tess;
			return f;
		}

		float4 CalcTriEdgeTessFactors (float3 triVertexFactors)
		{
			float4 tess;
			tess.x = 0.5 * (triVertexFactors.y + triVertexFactors.z);
			tess.y = 0.5 * (triVertexFactors.x + triVertexFactors.z);
			tess.z = 0.5 * (triVertexFactors.x + triVertexFactors.y);
			tess.w = (triVertexFactors.x + triVertexFactors.y + triVertexFactors.z) / 3.0f;
			return tess;
		}

		float CalcEdgeTessFactor (float3 wpos0, float3 wpos1, float edgeLen, float3 cameraPos, float4 scParams )
		{
			float dist = distance (0.5 * (wpos0+wpos1), cameraPos);
			float len = distance(wpos0, wpos1);
			float f = max(len * scParams.y / (edgeLen * dist), 1.0);
			return f;
		}

		float DistanceFromPlane (float3 pos, float4 plane)
		{
			float d = dot (float4(pos,1.0f), plane);
			return d;
		}

		bool WorldViewFrustumCull (float3 wpos0, float3 wpos1, float3 wpos2, float cullEps, float4 planes[6] )
		{
			float4 planeTest;
			planeTest.x = (( DistanceFromPlane(wpos0, planes[0]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos1, planes[0]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos2, planes[0]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.y = (( DistanceFromPlane(wpos0, planes[1]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos1, planes[1]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos2, planes[1]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.z = (( DistanceFromPlane(wpos0, planes[2]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos1, planes[2]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos2, planes[2]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.w = (( DistanceFromPlane(wpos0, planes[3]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos1, planes[3]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos2, planes[3]) > -cullEps) ? 1.0f : 0.0f );
			return !all (planeTest);
		}

		float4 DistanceBasedTess( float4 v0, float4 v1, float4 v2, float tess, float minDist, float maxDist, float4x4 o2w, float3 cameraPos )
		{
			float3 f;
			f.x = CalcDistanceTessFactor (v0,minDist,maxDist,tess,o2w,cameraPos);
			f.y = CalcDistanceTessFactor (v1,minDist,maxDist,tess,o2w,cameraPos);
			f.z = CalcDistanceTessFactor (v2,minDist,maxDist,tess,o2w,cameraPos);

			return CalcTriEdgeTessFactors (f);
		}

		float4 EdgeLengthBasedTess( float4 v0, float4 v1, float4 v2, float edgeLength, float4x4 o2w, float3 cameraPos, float4 scParams )
		{
			float3 pos0 = mul(o2w,v0).xyz;
			float3 pos1 = mul(o2w,v1).xyz;
			float3 pos2 = mul(o2w,v2).xyz;
			float4 tess;
			tess.x = CalcEdgeTessFactor (pos1, pos2, edgeLength, cameraPos, scParams);
			tess.y = CalcEdgeTessFactor (pos2, pos0, edgeLength, cameraPos, scParams);
			tess.z = CalcEdgeTessFactor (pos0, pos1, edgeLength, cameraPos, scParams);
			tess.w = (tess.x + tess.y + tess.z) / 3.0f;
			return tess;
		}

		float4 EdgeLengthBasedTessCull( float4 v0, float4 v1, float4 v2, float edgeLength, float maxDisplacement, float4x4 o2w, float3 cameraPos, float4 scParams, float4 planes[6] )
		{
			float3 pos0 = mul(o2w,v0).xyz;
			float3 pos1 = mul(o2w,v1).xyz;
			float3 pos2 = mul(o2w,v2).xyz;
			float4 tess;

			if (WorldViewFrustumCull(pos0, pos1, pos2, maxDisplacement, planes))
			{
				tess = 0.0f;
			}
			else
			{
				tess.x = CalcEdgeTessFactor (pos1, pos2, edgeLength, cameraPos, scParams);
				tess.y = CalcEdgeTessFactor (pos2, pos0, edgeLength, cameraPos, scParams);
				tess.z = CalcEdgeTessFactor (pos0, pos1, edgeLength, cameraPos, scParams);
				tess.w = (tess.x + tess.y + tess.z) / 3.0f;
			}
			return tess;
		}
		#endif //ASE_TESS_FUNCS
		ENDHLSL

		
		Pass
		{
			
			Name "Forward"
			Tags { "LightMode"="UniversalForwardOnly" }

			Blend [_Src] [_Dst], One OneMinusSrcAlpha
			ZWrite [_ZWrite]
			ZTest [_ZTest]
			Offset 0 , 0
			ColorMask RGBA

			

			HLSLPROGRAM

			#pragma shader_feature_local_fragment _RECEIVE_SHADOWS_OFF
			#pragma multi_compile_instancing
			#pragma instancing_options renderinglayer
			#define _SURFACE_TYPE_TRANSPARENT 1
			#define ASE_VERSION 19905
			#define ASE_SRP_VERSION 170003
			#define REQUIRE_OPAQUE_TEXTURE 1


			#pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3

			#pragma multi_compile_fragment _ DEBUG_DISPLAY

			#pragma vertex vert
			#pragma fragment frag

			#define SHADERPASS SHADERPASS_UNLIT

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging3D.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"

			#if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			#define ASE_NEEDS_TEXTURE_COORDINATES1
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES1
			#define ASE_NEEDS_FRAG_SCREEN_POSITION
			#define ASE_NEEDS_TEXTURE_COORDINATES0
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES0
			#define ASE_NEEDS_TEXTURE_COORDINATES2
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES2
			#define ASE_NEEDS_FRAG_COLOR


			#if defined(ASE_EARLY_Z_DEPTH_OPTIMIZE) && (SHADER_TARGET >= 45)
				#define ASE_SV_DEPTH SV_DepthLessEqual
				#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
			#else
				#define ASE_SV_DEPTH SV_Depth
				#define ASE_SV_POSITION_QUALIFIERS
			#endif

			struct Attributes
			{
				float4 positionOS : POSITION;
				half3 normalOS : NORMAL;
				float4 ase_color : COLOR;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord2 : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				ASE_SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
				float4 positionWSAndFogFactor : TEXCOORD0;
				half3 normalWS : TEXCOORD1;
				float4 ase_color : COLOR;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_texcoord4 : TEXCOORD4;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _LUTCoreColorInverted;
			float4 _LUTCoreColor;
			float2 _LUTCoreNoiseSize;
			float2 _Speed;
			float2 _LUTCoreNoiseSpeed;
			float2 _Size;
			float _Cull;
			float _LUTCoreCanInvert;
			float _LUTCoreSpeed;
			float _LUTCoreAmplitude;
			float _LUTCoreOffset;
			float _LUTCoreQuantizeUVRadius;
			float _LUTCoreLerp;
			float _LUTCoreOverParticleColorLerp;
			float _MaskErosion;
			float _MaskErosionSmoothness;
			float _LUTCoreDistortAmount;
			float _LUTColorLerp;
			float _LUTColorAmplitude;
			float _MaskRadius;
			float _Src;
			float _Dst;
			float _ZWrite;
			float _ZTest;
			float _ContrastErosion;
			float _ContrastErosionSmoothness;
			float _LUTColorOffset;
			float _QuantizeUVRadius;
			float _RadialDistortionAmount;
			float _RadialMaskStrength;
			float _RadialMask;
			float _RadialDistortionCheapContrastLerp;
			float _Boost;
			float _LUTColorSpeed;
			float _QuantizedRadiusLerp;
			float _MaskHardness;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			sampler2D _NoiseTexture;
			sampler2D _LUTColor;
			sampler2D _LUTCoreMask;


			inline float4 ASE_ComputeGrabScreenPos( float4 pos )
			{
				#if UNITY_UV_STARTS_AT_TOP
				float scale = -1.0;
				#else
				float scale = 1.0;
				#endif
				float4 o = pos;
				o.y = pos.w * 0.5f;
				o.y = ( pos.y - o.y ) * _ProjectionParams.x * scale + o.y;
				return o;
			}
			
			float4 CalculateContrast( float contrastValue, float4 colorTarget )
			{
				float t = 0.5 * ( 1.0 - contrastValue );
				return mul( float4x4( contrastValue,0,0,t, 0,contrastValue,0,t, 0,0,contrastValue,t, 0,0,0,1 ), colorTarget );
			}

			PackedVaryings VertexFunction( Attributes input  )
			{
				PackedVaryings output = (PackedVaryings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				output.ase_color = input.ase_color;
				output.ase_texcoord2 = input.ase_texcoord1;
				output.ase_texcoord3 = input.ase_texcoord;
				output.ase_texcoord4 = input.ase_texcoord2;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				input.normalOS = input.normalOS;

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );
				VertexNormalInputs normalInput = GetVertexNormalInputs( input.normalOS );

				float fogFactor = 0;
				#if defined(ASE_FOG) && !defined(_FOG_FRAGMENT)
					fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
				#endif

				output.positionCS = vertexInput.positionCS;
				output.positionWSAndFogFactor = float4( vertexInput.positionWS, fogFactor );
				output.normalWS = normalInput.normalWS;
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 positionOS : INTERNALTESSPOS;
				half3 normalOS : NORMAL;
				float4 ase_color : COLOR;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord2 : TEXCOORD2;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionOS = input.positionOS;
				output.normalOS = input.normalOS;
				output.ase_color = input.ase_color;
				output.ase_texcoord1 = input.ase_texcoord1;
				output.ase_texcoord = input.ase_texcoord;
				output.ase_texcoord2 = input.ase_texcoord2;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				output.ase_texcoord1 = patch[0].ase_texcoord1 * bary.x + patch[1].ase_texcoord1 * bary.y + patch[2].ase_texcoord1 * bary.z;
				output.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				output.ase_texcoord2 = patch[0].ase_texcoord2 * bary.x + patch[1].ase_texcoord2 * bary.y + patch[2].ase_texcoord2 * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			half4 frag ( PackedVaryings input
						#if defined( ASE_DEPTH_WRITE_ON )
						,out float outputDepth : ASE_SV_DEPTH
						#endif
						#ifdef _WRITE_RENDERING_LAYERS
						, out float4 outRenderingLayers : SV_Target1
						#endif
						 ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				#if defined( _SURFACE_TYPE_TRANSPARENT )
					const bool isTransparent = true;
				#else
					const bool isTransparent = false;
				#endif

				#if defined(LOD_FADE_CROSSFADE)
					LODFadeCrossFade( input.positionCS );
				#endif

				#if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
					float4 shadowCoord = TransformWorldToShadowCoord( input.positionWSAndFogFactor.xyz );
				#else
					float4 shadowCoord = float4(0, 0, 0, 0);
				#endif

				float3 PositionWS = input.positionWSAndFogFactor.xyz;
				float3 PositionRWS = GetCameraRelativePositionWS( PositionWS );
				half3 ViewDirWS = GetWorldSpaceNormalizeViewDir( PositionWS );
				float4 ShadowCoord = shadowCoord;
				float4 ScreenPosNorm = float4( GetNormalizedScreenSpaceUV( input.positionCS ), input.positionCS.zw );
				float4 ClipPos = ComputeClipSpacePosition( ScreenPosNorm.xy, input.positionCS.z ) * input.positionCS.w;
				float4 ScreenPos = ComputeScreenPos( ClipPos );
				half3 NormalWS = normalize( input.normalWS );

				float temp_output_233_0 = ( _ContrastErosion + _ContrastErosionSmoothness );
				float Contrast25 = input.ase_texcoord2.x;
				float4 ase_grabScreenPos = ASE_ComputeGrabScreenPos( ScreenPos );
				float4 ase_grabScreenPosNorm = ase_grabScreenPos / ase_grabScreenPos.w;
				float2 texCoord188 = input.ase_texcoord3.xy * float2( 2,2 ) + float2( 0,0 );
				float2 temp_output_190_0 = ( texCoord188 - float2( 1,1 ) );
				float2 appendResult205 = (float2(frac( ( atan2( (temp_output_190_0).x , (temp_output_190_0).y ) / TWO_PI ) ) , length( temp_output_190_0 )));
				float2 radialCoords215 = appendResult205;
				float2 appendResult164 = (float2(( floor( ( (radialCoords215).x * _QuantizeUVRadius ) ) / _QuantizeUVRadius ) , (radialCoords215).y));
				float2 lerpResult150 = lerp( (radialCoords215).xy , appendResult164 , _QuantizedRadiusLerp);
				float Size28 = input.ase_texcoord2.w;
				float2 appendResult157 = (float2(( _Size.x * Size28 ) , _Size.y));
				float2 panner158 = ( 1.0 * _Time.y * _Speed + ( lerpResult150 * appendResult157 ));
				float2 texCoord123 = input.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				float2 temp_output_124_0 = ddx( texCoord123 );
				float2 temp_output_125_0 = ddy( texCoord123 );
				float2 texCoord116 = input.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				float clampResult9_g6 = clamp( ( ( length(  (float2( -1,-1 ) + ( texCoord116 - float2( 0,0 ) ) * ( float2( 1,1 ) - float2( -1,-1 ) ) / ( float2( 1,1 ) - float2( 0,0 ) ) ) ) + -0.0 ) * 1.0 ) , 0.0 , 1.0 );
				float temp_output_118_0 = saturate( ( 1.0 - clampResult9_g6 ) );
				float Intensity23 = input.ase_texcoord3.z;
				float2 temp_output_103_0 = ( ( ( ( (tex2D( _NoiseTexture, panner158, temp_output_124_0, temp_output_125_0 ).rgb).xy + -0.5 ) * 2.0 ) * temp_output_118_0 ) * ( _RadialDistortionAmount * Intensity23 ) );
				float4 lerpResult100 = lerp( float4( temp_output_103_0, 0.0 , 0.0 ) , ( float4( temp_output_103_0, 0.0 , 0.0 ) * CalculateContrast(saturate( ( saturate( ( temp_output_118_0 * _RadialMaskStrength ) ) - _RadialMask ) ),float4( 0,0,0,0 )) ) , _RadialDistortionCheapContrastLerp);
				float4 fetchOpaqueVal60 = float4( SHADERGRAPH_SAMPLE_SCENE_COLOR( ( ase_grabScreenPosNorm + lerpResult100 ).xy.xy ), 1.0 );
				float Boost27 = input.ase_texcoord2.z;
				float4 temp_output_98_0 = saturate( CalculateContrast(Contrast25,( fetchOpaqueVal60 * ( _Boost * Boost27 ) )) );
				float Invert26 = input.ase_texcoord2.y;
				float4 lerpResult89 = lerp( temp_output_98_0 , ( 1.0 - temp_output_98_0 ) , Invert26);
				float Desaturate24 = input.ase_texcoord3.w;
				float3 desaturateInitialColor87 = lerpResult89.rgb;
				float desaturateDot87 = dot( desaturateInitialColor87, float3( 0.299, 0.587, 0.114 ));
				float3 desaturateVar87 = lerp( desaturateInitialColor87, desaturateDot87.xxx, Desaturate24 );
				float smoothstepResult227 = smoothstep( _ContrastErosion , temp_output_233_0 , (desaturateVar87).x);
				float4 temp_cast_5 = (saturate( smoothstepResult227 )).xxxx;
				float2 temp_cast_6 = (_LUTColorSpeed).xx;
				float3 desaturateInitialColor84 = lerpResult89.rgb;
				float desaturateDot84 = dot( desaturateInitialColor84, float3( 0.299, 0.587, 0.114 ));
				float3 desaturateVar84 = lerp( desaturateInitialColor84, desaturateDot84.xxx, 1.0 );
				float smoothstepResult229 = smoothstep( _ContrastErosion , temp_output_233_0 , (desaturateVar84).x);
				float2 temp_cast_8 = (( ( saturate( smoothstepResult229 ) * _LUTColorAmplitude ) + _LUTColorOffset )).xx;
				float2 panner80 = ( 1.0 * _Time.y * temp_cast_6 + temp_cast_8);
				float4 lerpResult66 = lerp( temp_cast_5 , tex2D( _LUTColor, panner80 ) , _LUTColorLerp);
				float4 lerpResult71 = lerp( _LUTCoreColorInverted , _LUTCoreColor , Invert26);
				float4 lerpResult70 = lerp( _LUTCoreColor , lerpResult71 , _LUTCoreCanInvert);
				float2 temp_cast_9 = (_LUTCoreSpeed).xx;
				float2 temp_cast_10 = (( ( 1.0 - ( (radialCoords215).y * _LUTCoreAmplitude ) ) + _LUTCoreOffset )).xx;
				float2 panner143 = ( 1.0 * _Time.y * temp_cast_9 + temp_cast_10);
				float LUT_Core_Pan29 = input.ase_texcoord4.x;
				float2 appendResult181 = (float2(( floor( ( (radialCoords215).x * _LUTCoreQuantizeUVRadius ) ) / _LUTCoreQuantizeUVRadius ) , (radialCoords215).y));
				float2 lerpResult166 = lerp( (radialCoords215).xy , appendResult181 , _QuantizedRadiusLerp);
				float2 appendResult171 = (float2(( _LUTCoreNoiseSize.x * Size28 ) , _LUTCoreNoiseSize.y));
				float2 panner172 = ( 1.0 * _Time.y * _LUTCoreNoiseSpeed + ( lerpResult166 * appendResult171 ));
				float LUTCoreMaskOut39 = tex2D( _LUTCoreMask, ( ( panner143 + LUT_Core_Pan29 ) + saturate( ( saturate( ( saturate( tex2D( _NoiseTexture, panner172, temp_output_124_0, temp_output_125_0 ).r ) * temp_output_118_0 ) ) * _LUTCoreDistortAmount ) ) ), temp_output_124_0, temp_output_125_0 ).r;
				float4 lerpResult64 = lerp( lerpResult66 , lerpResult70 , LUTCoreMaskOut39);
				float4 lerpResult42 = lerp( lerpResult66 , lerpResult64 , _LUTCoreLerp);
				float4 temp_output_33_0 = ( input.ase_color * lerpResult42 );
				float4 lerpResult34 = lerp( temp_output_33_0 , lerpResult70 , LUTCoreMaskOut39);
				float4 lerpResult31 = lerp( temp_output_33_0 , lerpResult34 , _LUTCoreOverParticleColorLerp);
				
				float2 texCoord51 = input.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				float clampResult9_g8 = clamp( ( ( length(  (float2( -1,-1 ) + ( texCoord51 - float2( 0,0 ) ) * ( float2( 1,1 ) - float2( -1,-1 ) ) / ( float2( 1,1 ) - float2( 0,0 ) ) ) ) + -_MaskRadius ) * _MaskHardness ) , 0.0 , 1.0 );
				float smoothstepResult54 = smoothstep( _MaskErosion , ( _MaskErosion + _MaskErosionSmoothness ) , saturate( ( 1.0 - saturate( clampResult9_g8 ) ) ));
				
				float3 BakedAlbedo = 0;
				float3 BakedEmission = 0;
				float3 Color = lerpResult31.rgb;
				float Alpha = saturate( ( input.ase_color.a * saturate( smoothstepResult54 ) ) );
				float AlphaClipThreshold = 0.5;
				float AlphaClipThresholdShadow = 0.5;

				#if defined( ASE_DEPTH_WRITE_ON )
					float DeviceDepth = input.positionCS.z;
				#endif

				#if defined( _ALPHATEST_ON )
					AlphaDiscard( Alpha, AlphaClipThreshold );
				#endif

				#if defined(MAIN_LIGHT_CALCULATE_SHADOWS) && defined(ASE_CHANGES_WORLD_POS)
					ShadowCoord = TransformWorldToShadowCoord( PositionWS );
				#endif

				InputData inputData = (InputData)0;
				inputData.positionWS = PositionWS;
				inputData.positionCS = float4( input.positionCS.xy, ClipPos.zw / ClipPos.w );
				inputData.normalizedScreenSpaceUV = ScreenPosNorm.xy;
				inputData.normalWS = NormalWS;
				inputData.viewDirectionWS = ViewDirWS;

				#if defined(_SCREEN_SPACE_OCCLUSION) && !defined(_SURFACE_TYPE_TRANSPARENT)
					float2 normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
					AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(normalizedScreenSpaceUV);
					Color.rgb *= aoFactor.directAmbientOcclusion;
				#endif

				#ifdef ASE_FOG
					inputData.fogCoord = InitializeInputDataFog(float4(inputData.positionWS, 1.0), input.positionWSAndFogFactor.w);
				#endif

				#if defined(_DBUFFER)
					ApplyDecalToBaseColor(input.positionCS, Color);
				#endif

				#ifdef ASE_FOG
					#ifdef TERRAIN_SPLAT_ADDPASS
						Color.rgb = MixFogColor(Color.rgb, half3(0,0,0), inputData.fogCoord);
					#else
						Color.rgb = MixFog(Color.rgb, inputData.fogCoord);
					#endif
				#endif

				#if defined( ASE_DEPTH_WRITE_ON )
					outputDepth = DeviceDepth;
				#endif

				#ifdef _WRITE_RENDERING_LAYERS
					uint renderingLayers = GetMeshRenderingLayer();
					outRenderingLayers = float4( EncodeMeshRenderingLayer( renderingLayers ), 0, 0, 0 );
				#endif

				#if defined( ASE_OPAQUE_KEEP_ALPHA )
					return half4( Color, Alpha );
				#else
					return half4( Color, OutputAlpha( Alpha, isTransparent ) );
				#endif
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "DepthOnly"
			Tags { "LightMode"="DepthOnly" }

			ZWrite On
			ColorMask 0
			AlphaToMask Off

			HLSLPROGRAM

			#pragma multi_compile_instancing
			#define _SURFACE_TYPE_TRANSPARENT 1
			#define ASE_VERSION 19905
			#define ASE_SRP_VERSION 170003


			#pragma vertex vert
			#pragma fragment frag

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

			#if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			#define ASE_NEEDS_TEXTURE_COORDINATES0


			#if defined(ASE_EARLY_Z_DEPTH_OPTIMIZE) && (SHADER_TARGET >= 45)
				#define ASE_SV_DEPTH SV_DepthLessEqual
				#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
			#else
				#define ASE_SV_DEPTH SV_Depth
				#define ASE_SV_POSITION_QUALIFIERS
			#endif

			struct Attributes
			{
				float4 positionOS : POSITION;
				half3 normalOS : NORMAL;
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				ASE_SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _LUTCoreColorInverted;
			float4 _LUTCoreColor;
			float2 _LUTCoreNoiseSize;
			float2 _Speed;
			float2 _LUTCoreNoiseSpeed;
			float2 _Size;
			float _Cull;
			float _LUTCoreCanInvert;
			float _LUTCoreSpeed;
			float _LUTCoreAmplitude;
			float _LUTCoreOffset;
			float _LUTCoreQuantizeUVRadius;
			float _LUTCoreLerp;
			float _LUTCoreOverParticleColorLerp;
			float _MaskErosion;
			float _MaskErosionSmoothness;
			float _LUTCoreDistortAmount;
			float _LUTColorLerp;
			float _LUTColorAmplitude;
			float _MaskRadius;
			float _Src;
			float _Dst;
			float _ZWrite;
			float _ZTest;
			float _ContrastErosion;
			float _ContrastErosionSmoothness;
			float _LUTColorOffset;
			float _QuantizeUVRadius;
			float _RadialDistortionAmount;
			float _RadialMaskStrength;
			float _RadialMask;
			float _RadialDistortionCheapContrastLerp;
			float _Boost;
			float _LUTColorSpeed;
			float _QuantizedRadiusLerp;
			float _MaskHardness;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			

			
			PackedVaryings VertexFunction( Attributes input  )
			{
				PackedVaryings output = (PackedVaryings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				output.ase_color = input.ase_color;
				output.ase_texcoord.xy = input.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord.zw = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );

				output.positionCS = vertexInput.positionCS;
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 positionOS : INTERNALTESSPOS;
				half3 normalOS : NORMAL;
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionOS = input.positionOS;
				output.normalOS = input.normalOS;
				output.ase_color = input.ase_color;
				output.ase_texcoord = input.ase_texcoord;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				output.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			half4 frag(PackedVaryings input
						#if defined( ASE_DEPTH_WRITE_ON )
						,out float outputDepth : ASE_SV_DEPTH
						#endif
						 ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( input );

				float4 ScreenPosNorm = float4( GetNormalizedScreenSpaceUV( input.positionCS ), input.positionCS.zw );
				float4 ClipPos = ComputeClipSpacePosition( ScreenPosNorm.xy, input.positionCS.z ) * input.positionCS.w;
				float4 ScreenPos = ComputeScreenPos( ClipPos );

				float2 texCoord51 = input.ase_texcoord.xy * float2( 1,1 ) + float2( 0,0 );
				float clampResult9_g8 = clamp( ( ( length(  (float2( -1,-1 ) + ( texCoord51 - float2( 0,0 ) ) * ( float2( 1,1 ) - float2( -1,-1 ) ) / ( float2( 1,1 ) - float2( 0,0 ) ) ) ) + -_MaskRadius ) * _MaskHardness ) , 0.0 , 1.0 );
				float smoothstepResult54 = smoothstep( _MaskErosion , ( _MaskErosion + _MaskErosionSmoothness ) , saturate( ( 1.0 - saturate( clampResult9_g8 ) ) ));
				

				float Alpha = saturate( ( input.ase_color.a * saturate( smoothstepResult54 ) ) );
				float AlphaClipThreshold = 0.5;

				#if defined( ASE_DEPTH_WRITE_ON )
					float DeviceDepth = input.positionCS.z;
				#endif

				#if defined( _ALPHATEST_ON )
					AlphaDiscard( Alpha, AlphaClipThreshold );
				#endif

				#if defined(LOD_FADE_CROSSFADE)
					LODFadeCrossFade( input.positionCS );
				#endif

				#if defined( ASE_DEPTH_WRITE_ON )
					outputDepth = DeviceDepth;
				#endif

				return 0;
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "SceneSelectionPass"
			Tags { "LightMode"="SceneSelectionPass" }

			Cull Off
			AlphaToMask Off

			HLSLPROGRAM

			#define _SURFACE_TYPE_TRANSPARENT 1
			#define ASE_VERSION 19905
			#define ASE_SRP_VERSION 170003


			#pragma vertex vert
			#pragma fragment frag

			#define ATTRIBUTES_NEED_NORMAL
			#define ATTRIBUTES_NEED_TANGENT
			#define SHADERPASS SHADERPASS_DEPTHONLY

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#define ASE_NEEDS_TEXTURE_COORDINATES0


			struct Attributes
			{
				float4 positionOS : POSITION;
				half3 normalOS : NORMAL;
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				float4 positionCS : SV_POSITION;
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _LUTCoreColorInverted;
			float4 _LUTCoreColor;
			float2 _LUTCoreNoiseSize;
			float2 _Speed;
			float2 _LUTCoreNoiseSpeed;
			float2 _Size;
			float _Cull;
			float _LUTCoreCanInvert;
			float _LUTCoreSpeed;
			float _LUTCoreAmplitude;
			float _LUTCoreOffset;
			float _LUTCoreQuantizeUVRadius;
			float _LUTCoreLerp;
			float _LUTCoreOverParticleColorLerp;
			float _MaskErosion;
			float _MaskErosionSmoothness;
			float _LUTCoreDistortAmount;
			float _LUTColorLerp;
			float _LUTColorAmplitude;
			float _MaskRadius;
			float _Src;
			float _Dst;
			float _ZWrite;
			float _ZTest;
			float _ContrastErosion;
			float _ContrastErosionSmoothness;
			float _LUTColorOffset;
			float _QuantizeUVRadius;
			float _RadialDistortionAmount;
			float _RadialMaskStrength;
			float _RadialMask;
			float _RadialDistortionCheapContrastLerp;
			float _Boost;
			float _LUTColorSpeed;
			float _QuantizedRadiusLerp;
			float _MaskHardness;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			

			
			int _ObjectId;
			int _PassValue;

			struct SurfaceDescription
			{
				float Alpha;
				float AlphaClipThreshold;
			};

			PackedVaryings VertexFunction(Attributes input  )
			{
				PackedVaryings output;
				ZERO_INITIALIZE(PackedVaryings, output);

				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				output.ase_color = input.ase_color;
				output.ase_texcoord.xy = input.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord.zw = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );

				output.positionCS = vertexInput.positionCS;
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 positionOS : INTERNALTESSPOS;
				half3 normalOS : NORMAL;
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionOS = input.positionOS;
				output.normalOS = input.normalOS;
				output.ase_color = input.ase_color;
				output.ase_texcoord = input.ase_texcoord;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				output.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			half4 frag(PackedVaryings input ) : SV_Target
			{
				SurfaceDescription surfaceDescription = (SurfaceDescription)0;

				float2 texCoord51 = input.ase_texcoord.xy * float2( 1,1 ) + float2( 0,0 );
				float clampResult9_g8 = clamp( ( ( length(  (float2( -1,-1 ) + ( texCoord51 - float2( 0,0 ) ) * ( float2( 1,1 ) - float2( -1,-1 ) ) / ( float2( 1,1 ) - float2( 0,0 ) ) ) ) + -_MaskRadius ) * _MaskHardness ) , 0.0 , 1.0 );
				float smoothstepResult54 = smoothstep( _MaskErosion , ( _MaskErosion + _MaskErosionSmoothness ) , saturate( ( 1.0 - saturate( clampResult9_g8 ) ) ));
				

				surfaceDescription.Alpha = saturate( ( input.ase_color.a * saturate( smoothstepResult54 ) ) );
				surfaceDescription.AlphaClipThreshold = 0.5;

				#ifdef _ALPHATEST_ON
					clip(surfaceDescription.Alpha - surfaceDescription.AlphaClipThreshold);
				#endif

				half4 outColor = half4(_ObjectId, _PassValue, 1.0, 1.0);
				return outColor;
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "ScenePickingPass"
			Tags { "LightMode"="Picking" }

			AlphaToMask Off

			HLSLPROGRAM

			#define _SURFACE_TYPE_TRANSPARENT 1
			#define ASE_VERSION 19905
			#define ASE_SRP_VERSION 170003


			#pragma vertex vert
			#pragma fragment frag

			#define ATTRIBUTES_NEED_NORMAL
			#define ATTRIBUTES_NEED_TANGENT

			#define SHADERPASS SHADERPASS_DEPTHONLY

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			#define ASE_NEEDS_TEXTURE_COORDINATES0


			struct Attributes
			{
				float4 positionOS : POSITION;
				half3 normalOS : NORMAL;
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				float4 positionCS : SV_POSITION;
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _LUTCoreColorInverted;
			float4 _LUTCoreColor;
			float2 _LUTCoreNoiseSize;
			float2 _Speed;
			float2 _LUTCoreNoiseSpeed;
			float2 _Size;
			float _Cull;
			float _LUTCoreCanInvert;
			float _LUTCoreSpeed;
			float _LUTCoreAmplitude;
			float _LUTCoreOffset;
			float _LUTCoreQuantizeUVRadius;
			float _LUTCoreLerp;
			float _LUTCoreOverParticleColorLerp;
			float _MaskErosion;
			float _MaskErosionSmoothness;
			float _LUTCoreDistortAmount;
			float _LUTColorLerp;
			float _LUTColorAmplitude;
			float _MaskRadius;
			float _Src;
			float _Dst;
			float _ZWrite;
			float _ZTest;
			float _ContrastErosion;
			float _ContrastErosionSmoothness;
			float _LUTColorOffset;
			float _QuantizeUVRadius;
			float _RadialDistortionAmount;
			float _RadialMaskStrength;
			float _RadialMask;
			float _RadialDistortionCheapContrastLerp;
			float _Boost;
			float _LUTColorSpeed;
			float _QuantizedRadiusLerp;
			float _MaskHardness;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			

			
			float4 _SelectionID;

			struct SurfaceDescription
			{
				float Alpha;
				float AlphaClipThreshold;
			};

			PackedVaryings VertexFunction(Attributes input  )
			{
				PackedVaryings output;
				ZERO_INITIALIZE(PackedVaryings, output);

				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				output.ase_color = input.ase_color;
				output.ase_texcoord.xy = input.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord.zw = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );

				output.positionCS = vertexInput.positionCS;
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 positionOS : INTERNALTESSPOS;
				half3 normalOS : NORMAL;
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionOS = input.positionOS;
				output.normalOS = input.normalOS;
				output.ase_color = input.ase_color;
				output.ase_texcoord = input.ase_texcoord;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				output.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			half4 frag(PackedVaryings input ) : SV_Target
			{
				SurfaceDescription surfaceDescription = (SurfaceDescription)0;

				float2 texCoord51 = input.ase_texcoord.xy * float2( 1,1 ) + float2( 0,0 );
				float clampResult9_g8 = clamp( ( ( length(  (float2( -1,-1 ) + ( texCoord51 - float2( 0,0 ) ) * ( float2( 1,1 ) - float2( -1,-1 ) ) / ( float2( 1,1 ) - float2( 0,0 ) ) ) ) + -_MaskRadius ) * _MaskHardness ) , 0.0 , 1.0 );
				float smoothstepResult54 = smoothstep( _MaskErosion , ( _MaskErosion + _MaskErosionSmoothness ) , saturate( ( 1.0 - saturate( clampResult9_g8 ) ) ));
				

				surfaceDescription.Alpha = saturate( ( input.ase_color.a * saturate( smoothstepResult54 ) ) );
				surfaceDescription.AlphaClipThreshold = 0.5;

				#ifdef _ALPHATEST_ON
					clip(surfaceDescription.Alpha - surfaceDescription.AlphaClipThreshold);
				#endif

				half4 outColor = 0;
				outColor = unity_SelectionID;

				return outColor;
			}

			ENDHLSL
		}

		
		Pass
		{
			
			Name "DepthNormals"
			Tags { "LightMode"="DepthNormalsOnly" }

			ZTest LEqual
			ZWrite On

			HLSLPROGRAM

        	#pragma multi_compile_instancing
        	#define _SURFACE_TYPE_TRANSPARENT 1
        	#define ASE_VERSION 19905
        	#define ASE_SRP_VERSION 170003


        	#pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

			#pragma vertex vert
			#pragma fragment frag

			#define ATTRIBUTES_NEED_NORMAL
			#define ATTRIBUTES_NEED_TANGENT
			#define VARYINGS_NEED_NORMAL_WS

			#define SHADERPASS SHADERPASS_DEPTHNORMALSONLY

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

            #if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			#define ASE_NEEDS_TEXTURE_COORDINATES0


			#if defined(ASE_EARLY_Z_DEPTH_OPTIMIZE) && (SHADER_TARGET >= 45)
				#define ASE_SV_DEPTH SV_DepthLessEqual
				#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
			#else
				#define ASE_SV_DEPTH SV_Depth
				#define ASE_SV_POSITION_QUALIFIERS
			#endif

			struct Attributes
			{
				float4 positionOS : POSITION;
				half3 normalOS : NORMAL;
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				ASE_SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
				half3 normalWS : TEXCOORD0;
				float4 ase_color : COLOR;
				float4 ase_texcoord1 : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _LUTCoreColorInverted;
			float4 _LUTCoreColor;
			float2 _LUTCoreNoiseSize;
			float2 _Speed;
			float2 _LUTCoreNoiseSpeed;
			float2 _Size;
			float _Cull;
			float _LUTCoreCanInvert;
			float _LUTCoreSpeed;
			float _LUTCoreAmplitude;
			float _LUTCoreOffset;
			float _LUTCoreQuantizeUVRadius;
			float _LUTCoreLerp;
			float _LUTCoreOverParticleColorLerp;
			float _MaskErosion;
			float _MaskErosionSmoothness;
			float _LUTCoreDistortAmount;
			float _LUTColorLerp;
			float _LUTColorAmplitude;
			float _MaskRadius;
			float _Src;
			float _Dst;
			float _ZWrite;
			float _ZTest;
			float _ContrastErosion;
			float _ContrastErosionSmoothness;
			float _LUTColorOffset;
			float _QuantizeUVRadius;
			float _RadialDistortionAmount;
			float _RadialMaskStrength;
			float _RadialMask;
			float _RadialDistortionCheapContrastLerp;
			float _Boost;
			float _LUTColorSpeed;
			float _QuantizedRadiusLerp;
			float _MaskHardness;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			

			
			struct SurfaceDescription
			{
				float Alpha;
				float AlphaClipThreshold;
			};

			PackedVaryings VertexFunction( Attributes input  )
			{
				PackedVaryings output;
				ZERO_INITIALIZE(PackedVaryings, output);

				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				output.ase_color = input.ase_color;
				output.ase_texcoord1.xy = input.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord1.zw = 0;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				input.normalOS = input.normalOS;

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );
				VertexNormalInputs normalInput = GetVertexNormalInputs( input.normalOS );

				output.positionCS = vertexInput.positionCS;
				output.normalWS = normalInput.normalWS;
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 positionOS : INTERNALTESSPOS;
				half3 normalOS : NORMAL;
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionOS = input.positionOS;
				output.normalOS = input.normalOS;
				output.ase_color = input.ase_color;
				output.ase_texcoord = input.ase_texcoord;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				output.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			void frag(PackedVaryings input
						, out half4 outNormalWS : SV_Target0
						#if defined( ASE_DEPTH_WRITE_ON )
						,out float outputDepth : ASE_SV_DEPTH
						#endif
						#ifdef _WRITE_RENDERING_LAYERS
						, out float4 outRenderingLayers : SV_Target1
						#endif
						 )
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( input );

				half3 NormalWS = normalize( input.normalWS );
				float4 ScreenPosNorm = float4( GetNormalizedScreenSpaceUV( input.positionCS ), input.positionCS.zw );
				float4 ClipPos = ComputeClipSpacePosition( ScreenPosNorm.xy, input.positionCS.z ) * input.positionCS.w;
				float4 ScreenPos = ComputeScreenPos( ClipPos );

				float2 texCoord51 = input.ase_texcoord1.xy * float2( 1,1 ) + float2( 0,0 );
				float clampResult9_g8 = clamp( ( ( length(  (float2( -1,-1 ) + ( texCoord51 - float2( 0,0 ) ) * ( float2( 1,1 ) - float2( -1,-1 ) ) / ( float2( 1,1 ) - float2( 0,0 ) ) ) ) + -_MaskRadius ) * _MaskHardness ) , 0.0 , 1.0 );
				float smoothstepResult54 = smoothstep( _MaskErosion , ( _MaskErosion + _MaskErosionSmoothness ) , saturate( ( 1.0 - saturate( clampResult9_g8 ) ) ));
				

				float Alpha = saturate( ( input.ase_color.a * saturate( smoothstepResult54 ) ) );
				float AlphaClipThreshold = 0.5;

				#if defined( ASE_DEPTH_WRITE_ON )
					float DeviceDepth = input.positionCS.z;
				#endif

				#ifdef _ALPHATEST_ON
					clip(Alpha - AlphaClipThreshold);
				#endif

				#if defined(LOD_FADE_CROSSFADE)
					LODFadeCrossFade( input.positionCS );
				#endif

				#if defined( ASE_DEPTH_WRITE_ON )
					outputDepth = DeviceDepth;
				#endif

				#if defined(_GBUFFER_NORMALS_OCT)
					float2 octNormalWS = PackNormalOctQuadEncode(NormalWS);
					float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);
					half3 packedNormalWS = PackFloat2To888(remappedOctNormalWS);
					outNormalWS = half4(packedNormalWS, 0.0);
				#else
					outNormalWS = half4(NormalizeNormalPerPixel( NormalWS ), 0.0);
				#endif

				#ifdef _WRITE_RENDERING_LAYERS
					uint renderingLayers = GetMeshRenderingLayer();
					outRenderingLayers = float4(EncodeMeshRenderingLayer(renderingLayers), 0, 0, 0);
				#endif
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "MotionVectors"
			Tags { "LightMode"="MotionVectors" }

			ColorMask RG

			HLSLPROGRAM

			#define _SURFACE_TYPE_TRANSPARENT 1
			#define ASE_VERSION 19905
			#define ASE_SRP_VERSION 170003


			#pragma vertex vert
			#pragma fragment frag

            #define SHADERPASS SHADERPASS_MOTION_VECTORS

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
		    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
		    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
		    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
		    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
		    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
		    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
		    #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#if defined(LOD_FADE_CROSSFADE)
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
			#endif

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MotionVectorsCommon.hlsl"

			#define ASE_NEEDS_TEXTURE_COORDINATES0


			#if defined(ASE_EARLY_Z_DEPTH_OPTIMIZE) && (SHADER_TARGET >= 45)
				#define ASE_SV_DEPTH SV_DepthLessEqual
				#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
			#else
				#define ASE_SV_DEPTH SV_Depth
				#define ASE_SV_POSITION_QUALIFIERS
			#endif

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 positionOld : TEXCOORD4;
				#if _ADD_PRECOMPUTED_VELOCITY
					float3 alembicMotionVector : TEXCOORD5;
				#endif
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				float4 positionCS : SV_POSITION;
				float4 positionCSNoJitter : TEXCOORD0;
				float4 previousPositionCSNoJitter : TEXCOORD1;
				float3 positionWS : TEXCOORD2;
				float4 ase_color : COLOR;
				float4 ase_texcoord3 : TEXCOORD3;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _LUTCoreColorInverted;
			float4 _LUTCoreColor;
			float2 _LUTCoreNoiseSize;
			float2 _Speed;
			float2 _LUTCoreNoiseSpeed;
			float2 _Size;
			float _Cull;
			float _LUTCoreCanInvert;
			float _LUTCoreSpeed;
			float _LUTCoreAmplitude;
			float _LUTCoreOffset;
			float _LUTCoreQuantizeUVRadius;
			float _LUTCoreLerp;
			float _LUTCoreOverParticleColorLerp;
			float _MaskErosion;
			float _MaskErosionSmoothness;
			float _LUTCoreDistortAmount;
			float _LUTColorLerp;
			float _LUTColorAmplitude;
			float _MaskRadius;
			float _Src;
			float _Dst;
			float _ZWrite;
			float _ZTest;
			float _ContrastErosion;
			float _ContrastErosionSmoothness;
			float _LUTColorOffset;
			float _QuantizeUVRadius;
			float _RadialDistortionAmount;
			float _RadialMaskStrength;
			float _RadialMask;
			float _RadialDistortionCheapContrastLerp;
			float _Boost;
			float _LUTColorSpeed;
			float _QuantizedRadiusLerp;
			float _MaskHardness;
			#ifdef ASE_TRANSMISSION
				float _TransmissionShadow;
			#endif
			#ifdef ASE_TRANSLUCENCY
				float _TransStrength;
				float _TransNormal;
				float _TransScattering;
				float _TransDirect;
				float _TransAmbient;
				float _TransShadow;
			#endif
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			#ifdef SCENEPICKINGPASS
				float4 _SelectionID;
			#endif

			#ifdef SCENESELECTIONPASS
				int _ObjectId;
				int _PassValue;
			#endif

			

			
			PackedVaryings VertexFunction( Attributes input  )
			{
				PackedVaryings output = (PackedVaryings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				output.ase_color = input.ase_color;
				output.ase_texcoord3.xy = input.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord3.zw = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );

				#if defined(APLICATION_SPACE_WARP_MOTION)
					output.positionCSNoJitter = mul(_NonJitteredViewProjMatrix, mul(UNITY_MATRIX_M, input.positionOS));
					output.positionCS = output.positionCSNoJitter;
				#else
					output.positionCS = vertexInput.positionCS;
					output.positionCSNoJitter = mul(_NonJitteredViewProjMatrix, mul(UNITY_MATRIX_M, input.positionOS));
				#endif

				float4 prevPos = ( unity_MotionVectorsParams.x == 1 ) ? float4( input.positionOld, 1 ) : input.positionOS;

				#if _ADD_PRECOMPUTED_VELOCITY
					prevPos = prevPos - float4(input.alembicMotionVector, 0);
				#endif

				output.previousPositionCSNoJitter = mul( _PrevViewProjMatrix, mul( UNITY_PREV_MATRIX_M, prevPos ) );
				output.positionWS = vertexInput.positionWS;
				return output;
			}

			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}

			half4 frag(	PackedVaryings input
				#if defined( ASE_DEPTH_WRITE_ON )
				,out float outputDepth : ASE_SV_DEPTH
				#endif
				 ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( input );

				float3 PositionWS = input.positionWS;
				float3 PositionRWS = GetCameraRelativePositionWS( PositionWS );
				float4 ScreenPosNorm = float4( GetNormalizedScreenSpaceUV( input.positionCS ), input.positionCS.zw );
				float4 ClipPos = ComputeClipSpacePosition( ScreenPosNorm.xy, input.positionCS.z ) * input.positionCS.w;

				float2 texCoord51 = input.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				float clampResult9_g8 = clamp( ( ( length(  (float2( -1,-1 ) + ( texCoord51 - float2( 0,0 ) ) * ( float2( 1,1 ) - float2( -1,-1 ) ) / ( float2( 1,1 ) - float2( 0,0 ) ) ) ) + -_MaskRadius ) * _MaskHardness ) , 0.0 , 1.0 );
				float smoothstepResult54 = smoothstep( _MaskErosion , ( _MaskErosion + _MaskErosionSmoothness ) , saturate( ( 1.0 - saturate( clampResult9_g8 ) ) ));
				

				float Alpha = saturate( ( input.ase_color.a * saturate( smoothstepResult54 ) ) );
				float AlphaClipThreshold = 0.5;

				#if defined( ASE_DEPTH_WRITE_ON )
					float DeviceDepth = input.positionCS.z;
				#endif

				#ifdef _ALPHATEST_ON
					clip(Alpha - AlphaClipThreshold);
				#endif

				#if defined( ASE_CHANGES_WORLD_POS )
					float3 positionOS = mul( GetWorldToObjectMatrix(),  float4( PositionWS, 1.0 ) ).xyz;
					float3 previousPositionWS = mul( GetPrevObjectToWorldMatrix(),  float4( positionOS, 1.0 ) ).xyz;
					input.positionCSNoJitter = mul( _NonJitteredViewProjMatrix, float4( PositionWS, 1.0 ) );
					input.previousPositionCSNoJitter = mul( _PrevViewProjMatrix, float4( previousPositionWS, 1.0 ) );
				#endif

				#if defined( LOD_FADE_CROSSFADE )
					LODFadeCrossFade( input.positionCS );
				#endif

				#if defined( ASE_DEPTH_WRITE_ON )
					outputDepth = DeviceDepth;
				#endif

				#if defined(APLICATION_SPACE_WARP_MOTION)
					return float4( CalcAswNdcMotionVectorFromCsPositions( input.positionCSNoJitter, input.previousPositionCSNoJitter ), 1 );
				#else
					return float4( CalcNdcMotionVectorFromCsPositions( input.positionCSNoJitter, input.previousPositionCSNoJitter ), 0, 0 );
				#endif
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "GBuffer"
			Tags { "LightMode"="UniversalGBuffer" }

			Blend [_Src] [_Dst], One OneMinusSrcAlpha
			ZWrite [_ZWrite]
			ZTest [_ZTest]
			Offset 0 , 0
			ColorMask RGBA

			

			HLSLPROGRAM

			#pragma shader_feature_local_fragment _RECEIVE_SHADOWS_OFF
			#define _SURFACE_TYPE_TRANSPARENT 1
			#define ASE_VERSION 19905
			#define ASE_SRP_VERSION 170003
			#define REQUIRE_OPAQUE_TEXTURE 1


			// Deferred Rendering Path does not support the OpenGL-based graphics API:
			// Desktop OpenGL, OpenGL ES 3.0, WebGL 2.0.
			#pragma exclude_renderers glcore gles3 

			#pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
			#pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
			#pragma multi_compile_fragment _ _RENDER_PASS_ENABLED

			#pragma vertex vert
			#pragma fragment frag

			#define SHADERPASS SHADERPASS_GBUFFER

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"

			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			#if defined(UNITY_INSTANCING_ENABLED) && defined(_TERRAIN_INSTANCED_PERPIXEL_NORMAL)
				#define ENABLE_TERRAIN_PERPIXEL_NORMAL
			#endif

			#define ASE_NEEDS_TEXTURE_COORDINATES1
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES1
			#define ASE_NEEDS_FRAG_SCREEN_POSITION
			#define ASE_NEEDS_TEXTURE_COORDINATES0
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES0
			#define ASE_NEEDS_TEXTURE_COORDINATES2
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES2
			#define ASE_NEEDS_FRAG_COLOR


			#if defined(ASE_EARLY_Z_DEPTH_OPTIMIZE) && (SHADER_TARGET >= 45)
				#define ASE_SV_DEPTH SV_DepthLessEqual
				#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
			#else
				#define ASE_SV_DEPTH SV_Depth
				#define ASE_SV_POSITION_QUALIFIERS
			#endif

			struct Attributes
			{
				float4 positionOS : POSITION;
				half3 normalOS : NORMAL;
				float4 ase_color : COLOR;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord2 : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				ASE_SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				half3 normalWS : TEXCOORD1;
				float4 ase_color : COLOR;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_texcoord4 : TEXCOORD4;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _LUTCoreColorInverted;
			float4 _LUTCoreColor;
			float2 _LUTCoreNoiseSize;
			float2 _Speed;
			float2 _LUTCoreNoiseSpeed;
			float2 _Size;
			float _Cull;
			float _LUTCoreCanInvert;
			float _LUTCoreSpeed;
			float _LUTCoreAmplitude;
			float _LUTCoreOffset;
			float _LUTCoreQuantizeUVRadius;
			float _LUTCoreLerp;
			float _LUTCoreOverParticleColorLerp;
			float _MaskErosion;
			float _MaskErosionSmoothness;
			float _LUTCoreDistortAmount;
			float _LUTColorLerp;
			float _LUTColorAmplitude;
			float _MaskRadius;
			float _Src;
			float _Dst;
			float _ZWrite;
			float _ZTest;
			float _ContrastErosion;
			float _ContrastErosionSmoothness;
			float _LUTColorOffset;
			float _QuantizeUVRadius;
			float _RadialDistortionAmount;
			float _RadialMaskStrength;
			float _RadialMask;
			float _RadialDistortionCheapContrastLerp;
			float _Boost;
			float _LUTColorSpeed;
			float _QuantizedRadiusLerp;
			float _MaskHardness;
			#ifdef ASE_TRANSMISSION
				float _TransmissionShadow;
			#endif
			#ifdef ASE_TRANSLUCENCY
				float _TransStrength;
				float _TransNormal;
				float _TransScattering;
				float _TransDirect;
				float _TransAmbient;
				float _TransShadow;
			#endif
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			#ifdef SCENEPICKINGPASS
				float4 _SelectionID;
			#endif

			#ifdef SCENESELECTIONPASS
				int _ObjectId;
				int _PassValue;
			#endif

			sampler2D _NoiseTexture;
			sampler2D _LUTColor;
			sampler2D _LUTCoreMask;


			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityGBuffer.hlsl"

			inline float4 ASE_ComputeGrabScreenPos( float4 pos )
			{
				#if UNITY_UV_STARTS_AT_TOP
				float scale = -1.0;
				#else
				float scale = 1.0;
				#endif
				float4 o = pos;
				o.y = pos.w * 0.5f;
				o.y = ( pos.y - o.y ) * _ProjectionParams.x * scale + o.y;
				return o;
			}
			
			float4 CalculateContrast( float contrastValue, float4 colorTarget )
			{
				float t = 0.5 * ( 1.0 - contrastValue );
				return mul( float4x4( contrastValue,0,0,t, 0,contrastValue,0,t, 0,0,contrastValue,t, 0,0,0,1 ), colorTarget );
			}

			PackedVaryings VertexFunction( Attributes input  )
			{
				PackedVaryings output = (PackedVaryings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				output.ase_color = input.ase_color;
				output.ase_texcoord2 = input.ase_texcoord1;
				output.ase_texcoord3 = input.ase_texcoord;
				output.ase_texcoord4 = input.ase_texcoord2;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = defaultVertexValue;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );
				VertexNormalInputs normalInput = GetVertexNormalInputs( input.normalOS );

				output.positionCS = vertexInput.positionCS;
				output.positionWS = vertexInput.positionWS;
				output.normalWS = normalInput.normalWS;
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 positionOS : INTERNALTESSPOS;
				half3 normalOS : NORMAL;
				float4 ase_color : COLOR;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord2 : TEXCOORD2;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionOS = input.positionOS;
				output.normalOS = input.normalOS;
				output.ase_color = input.ase_color;
				output.ase_texcoord1 = input.ase_texcoord1;
				output.ase_texcoord = input.ase_texcoord;
				output.ase_texcoord2 = input.ase_texcoord2;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				output.ase_texcoord1 = patch[0].ase_texcoord1 * bary.x + patch[1].ase_texcoord1 * bary.y + patch[2].ase_texcoord1 * bary.z;
				output.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				output.ase_texcoord2 = patch[0].ase_texcoord2 * bary.x + patch[1].ase_texcoord2 * bary.y + patch[2].ase_texcoord2 * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			FragmentOutput frag ( PackedVaryings input
								#if defined( ASE_DEPTH_WRITE_ON )
								,out float outputDepth : ASE_SV_DEPTH
								#endif
								 )
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				#if defined(LOD_FADE_CROSSFADE)
					LODFadeCrossFade( input.positionCS );
				#endif

				float3 PositionWS = input.positionWS;
				float3 ViewDirWS = GetWorldSpaceNormalizeViewDir( PositionWS );
				float4 ScreenPosNorm = float4( GetNormalizedScreenSpaceUV( input.positionCS ), input.positionCS.zw );
				float4 ClipPos = ComputeClipSpacePosition( ScreenPosNorm.xy, input.positionCS.z ) * input.positionCS.w;
				float4 ScreenPos = ComputeScreenPos( ClipPos );
				half3 NormalWS = normalize( input.normalWS );

				float temp_output_233_0 = ( _ContrastErosion + _ContrastErosionSmoothness );
				float Contrast25 = input.ase_texcoord2.x;
				float4 ase_grabScreenPos = ASE_ComputeGrabScreenPos( ScreenPos );
				float4 ase_grabScreenPosNorm = ase_grabScreenPos / ase_grabScreenPos.w;
				float2 texCoord188 = input.ase_texcoord3.xy * float2( 2,2 ) + float2( 0,0 );
				float2 temp_output_190_0 = ( texCoord188 - float2( 1,1 ) );
				float2 appendResult205 = (float2(frac( ( atan2( (temp_output_190_0).x , (temp_output_190_0).y ) / TWO_PI ) ) , length( temp_output_190_0 )));
				float2 radialCoords215 = appendResult205;
				float2 appendResult164 = (float2(( floor( ( (radialCoords215).x * _QuantizeUVRadius ) ) / _QuantizeUVRadius ) , (radialCoords215).y));
				float2 lerpResult150 = lerp( (radialCoords215).xy , appendResult164 , _QuantizedRadiusLerp);
				float Size28 = input.ase_texcoord2.w;
				float2 appendResult157 = (float2(( _Size.x * Size28 ) , _Size.y));
				float2 panner158 = ( 1.0 * _Time.y * _Speed + ( lerpResult150 * appendResult157 ));
				float2 texCoord123 = input.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				float2 temp_output_124_0 = ddx( texCoord123 );
				float2 temp_output_125_0 = ddy( texCoord123 );
				float2 texCoord116 = input.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				float clampResult9_g6 = clamp( ( ( length(  (float2( -1,-1 ) + ( texCoord116 - float2( 0,0 ) ) * ( float2( 1,1 ) - float2( -1,-1 ) ) / ( float2( 1,1 ) - float2( 0,0 ) ) ) ) + -0.0 ) * 1.0 ) , 0.0 , 1.0 );
				float temp_output_118_0 = saturate( ( 1.0 - clampResult9_g6 ) );
				float Intensity23 = input.ase_texcoord3.z;
				float2 temp_output_103_0 = ( ( ( ( (tex2D( _NoiseTexture, panner158, temp_output_124_0, temp_output_125_0 ).rgb).xy + -0.5 ) * 2.0 ) * temp_output_118_0 ) * ( _RadialDistortionAmount * Intensity23 ) );
				float4 lerpResult100 = lerp( float4( temp_output_103_0, 0.0 , 0.0 ) , ( float4( temp_output_103_0, 0.0 , 0.0 ) * CalculateContrast(saturate( ( saturate( ( temp_output_118_0 * _RadialMaskStrength ) ) - _RadialMask ) ),float4( 0,0,0,0 )) ) , _RadialDistortionCheapContrastLerp);
				float4 fetchOpaqueVal60 = float4( SHADERGRAPH_SAMPLE_SCENE_COLOR( ( ase_grabScreenPosNorm + lerpResult100 ).xy.xy ), 1.0 );
				float Boost27 = input.ase_texcoord2.z;
				float4 temp_output_98_0 = saturate( CalculateContrast(Contrast25,( fetchOpaqueVal60 * ( _Boost * Boost27 ) )) );
				float Invert26 = input.ase_texcoord2.y;
				float4 lerpResult89 = lerp( temp_output_98_0 , ( 1.0 - temp_output_98_0 ) , Invert26);
				float Desaturate24 = input.ase_texcoord3.w;
				float3 desaturateInitialColor87 = lerpResult89.rgb;
				float desaturateDot87 = dot( desaturateInitialColor87, float3( 0.299, 0.587, 0.114 ));
				float3 desaturateVar87 = lerp( desaturateInitialColor87, desaturateDot87.xxx, Desaturate24 );
				float smoothstepResult227 = smoothstep( _ContrastErosion , temp_output_233_0 , (desaturateVar87).x);
				float4 temp_cast_5 = (saturate( smoothstepResult227 )).xxxx;
				float2 temp_cast_6 = (_LUTColorSpeed).xx;
				float3 desaturateInitialColor84 = lerpResult89.rgb;
				float desaturateDot84 = dot( desaturateInitialColor84, float3( 0.299, 0.587, 0.114 ));
				float3 desaturateVar84 = lerp( desaturateInitialColor84, desaturateDot84.xxx, 1.0 );
				float smoothstepResult229 = smoothstep( _ContrastErosion , temp_output_233_0 , (desaturateVar84).x);
				float2 temp_cast_8 = (( ( saturate( smoothstepResult229 ) * _LUTColorAmplitude ) + _LUTColorOffset )).xx;
				float2 panner80 = ( 1.0 * _Time.y * temp_cast_6 + temp_cast_8);
				float4 lerpResult66 = lerp( temp_cast_5 , tex2D( _LUTColor, panner80 ) , _LUTColorLerp);
				float4 lerpResult71 = lerp( _LUTCoreColorInverted , _LUTCoreColor , Invert26);
				float4 lerpResult70 = lerp( _LUTCoreColor , lerpResult71 , _LUTCoreCanInvert);
				float2 temp_cast_9 = (_LUTCoreSpeed).xx;
				float2 temp_cast_10 = (( ( 1.0 - ( (radialCoords215).y * _LUTCoreAmplitude ) ) + _LUTCoreOffset )).xx;
				float2 panner143 = ( 1.0 * _Time.y * temp_cast_9 + temp_cast_10);
				float LUT_Core_Pan29 = input.ase_texcoord4.x;
				float2 appendResult181 = (float2(( floor( ( (radialCoords215).x * _LUTCoreQuantizeUVRadius ) ) / _LUTCoreQuantizeUVRadius ) , (radialCoords215).y));
				float2 lerpResult166 = lerp( (radialCoords215).xy , appendResult181 , _QuantizedRadiusLerp);
				float2 appendResult171 = (float2(( _LUTCoreNoiseSize.x * Size28 ) , _LUTCoreNoiseSize.y));
				float2 panner172 = ( 1.0 * _Time.y * _LUTCoreNoiseSpeed + ( lerpResult166 * appendResult171 ));
				float LUTCoreMaskOut39 = tex2D( _LUTCoreMask, ( ( panner143 + LUT_Core_Pan29 ) + saturate( ( saturate( ( saturate( tex2D( _NoiseTexture, panner172, temp_output_124_0, temp_output_125_0 ).r ) * temp_output_118_0 ) ) * _LUTCoreDistortAmount ) ) ), temp_output_124_0, temp_output_125_0 ).r;
				float4 lerpResult64 = lerp( lerpResult66 , lerpResult70 , LUTCoreMaskOut39);
				float4 lerpResult42 = lerp( lerpResult66 , lerpResult64 , _LUTCoreLerp);
				float4 temp_output_33_0 = ( input.ase_color * lerpResult42 );
				float4 lerpResult34 = lerp( temp_output_33_0 , lerpResult70 , LUTCoreMaskOut39);
				float4 lerpResult31 = lerp( temp_output_33_0 , lerpResult34 , _LUTCoreOverParticleColorLerp);
				
				float2 texCoord51 = input.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				float clampResult9_g8 = clamp( ( ( length(  (float2( -1,-1 ) + ( texCoord51 - float2( 0,0 ) ) * ( float2( 1,1 ) - float2( -1,-1 ) ) / ( float2( 1,1 ) - float2( 0,0 ) ) ) ) + -_MaskRadius ) * _MaskHardness ) , 0.0 , 1.0 );
				float smoothstepResult54 = smoothstep( _MaskErosion , ( _MaskErosion + _MaskErosionSmoothness ) , saturate( ( 1.0 - saturate( clampResult9_g8 ) ) ));
				

				float3 Color = lerpResult31.rgb;
				float Alpha = saturate( ( input.ase_color.a * saturate( smoothstepResult54 ) ) );
				float AlphaClipThreshold = 0.5;
				float AlphaClipThresholdShadow = 0.5;

				#if defined( ASE_DEPTH_WRITE_ON )
					float DeviceDepth = input.positionCS.z;
				#endif

				#ifdef _ALPHATEST_ON
					clip(Alpha - AlphaClipThreshold);
				#endif

				InputData inputData = (InputData)0;
				inputData.positionWS = PositionWS;
				inputData.positionCS = float4( input.positionCS.xy, ClipPos.zw / ClipPos.w );
				inputData.normalizedScreenSpaceUV = ScreenPosNorm.xy;
				inputData.normalWS = NormalWS;
				inputData.viewDirectionWS = ViewDirWS;

				#if defined(_DBUFFER)
					ApplyDecalToBaseColor(input.positionCS, Color);
				#endif

				#if defined( ASE_DEPTH_WRITE_ON )
					outputDepth = DeviceDepth;
				#endif

				SurfaceData surfaceData = (SurfaceData)0;
				surfaceData.albedo = Color;
				surfaceData.alpha = Alpha;

			#if defined( _SCREEN_SPACE_OCCLUSION ) // GBuffer never has transparents
				float2 normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV( input.positionCS );
				AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion( normalizedScreenSpaceUV );
				surfaceData.occlusion = aoFactor.directAmbientOcclusion;
			#else
				surfaceData.occlusion = 1;
			#endif

				return SurfaceDataToGbuffer( surfaceData, inputData, float3( 0, 0, 0 ), kLightingInvalid );
			}

			ENDHLSL
		}
		
	}
	
	CustomEditor "UnityEditor.ShaderGraphUnlitGUI"
	FallBack "Hidden/Shader Graph/FallbackError"
	
	Fallback Off
}
/*ASEBEGIN
Version=19905
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;186;-18736,-688;Inherit;False;1764;418.95;Radial Maths (Thanks Luos for the function);14;205;201;200;198;197;196;195;194;192;191;190;189;188;187;Radial Maths;0,0,0,1;0;0
Node;AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;187;-18688,-640;Float;False;Constant;_Vector3;Vector 1;0;0;Create;True;0;0;0;False;0;False;2,2;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;188;-18432,-640;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;189;-18176,-512;Float;False;Constant;_Vector4;Vector 0;0;0;Create;True;0;0;0;False;0;False;1,1;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleSubtractOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;190;-18176,-640;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;191;-17920,-640;Inherit;False;True;False;True;True;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;192;-17920,-512;Inherit;False;False;True;True;True;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ATan2OpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;195;-17664,-640;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TauNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;196;-17408,-512;Inherit;False;0;1;FLOAT;0
Node;AmplifyShaderEditor.LengthOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;194;-17920,-384;Inherit;False;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;197;-17408,-640;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;198;-17264,-368;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;30;-1072,-2352;Inherit;False;562.6801;827.2509;Dynamic Parameters;10;63;21;22;29;28;27;26;25;24;23;Dynamic Parameters;0,0,0,1;0;0
Node;AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;147;-14208,0;Inherit;False;True;False;False;False;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;161;-14208,-128;Inherit;False;Property;_QuantizeUVRadius;Quantize UV Radius;26;0;Create;True;0;0;0;False;0;False;2048;2048;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.FractNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;200;-17280,-640;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;201;-17200,-400;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;22;-1024,-2048;Inherit;False;1;4;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;160;-13952,0;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;205;-17152,-640;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;28;-768,-1984;Inherit;False;Size;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FloorOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;162;-13824,0;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;215;-16768,-640;Inherit;False;radialCoords;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;153;-12672,128;Inherit;False;Property;_Size;Size;32;0;Create;True;0;0;0;False;0;False;10,0.01;10,0.01;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;156;-12416,256;Inherit;False;28;Size;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;148;-14208,128;Inherit;False;False;True;False;False;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;163;-13696,0;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;216;-14464,0;Inherit;False;215;radialCoords;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;151;-13184,-384;Inherit;False;Property;_QuantizedRadiusLerp;Quantized Radius Lerp;27;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;154;-12416,128;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;149;-14208,256;Inherit;False;True;True;False;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;164;-13440,0;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;150;-13184,0;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;157;-12288,128;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;178;-14208,1280;Inherit;False;Property;_LUTCoreQuantizeUVRadius;LUT Core Quantize UV Radius;18;0;Create;True;0;0;0;False;0;False;2048;2048;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;174;-14208,1408;Inherit;False;True;False;False;False;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;113;-9344,896;Inherit;False;Property;_RadialMaskStrength;Radial Mask Strength;31;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;123;-10752,384;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;152;-12288,0;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;159;-11776,128;Inherit;False;Property;_Speed;Speed;33;0;Create;True;0;0;0;False;0;False;3.333,10;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;177;-13952,1408;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;116;-10368,768;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DdxOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;124;-10752,256;Inherit;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DdyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;125;-10752,320;Inherit;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;109;-9344,768;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;158;-11776,0;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TexturePropertyNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;119;-10752,-384;Inherit;True;Property;_NoiseTexture;Noise Texture;24;0;Create;True;0;0;0;False;3;Space(33);Header(Radial);Space(33);False;626373db048749fbb9072e3683f88883;626373db048749fbb9072e3683f88883;False;white;Auto;Texture2D;False;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.FloorOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;179;-13824,1408;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;21;-1024,-2304;Inherit;False;0;4;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;114;-8960,896;Inherit;False;Property;_RadialMask;Radial Mask;30;0;Create;True;0;0;0;False;0;False;0;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;115;-10112,768;Inherit;True;RadialGradient;-1;;6;ec972f7745a8353409da2eb8d000a2e3;0;3;1;FLOAT2;0,0;False;6;FLOAT;0;False;7;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;120;-10752,0;Inherit;True;Property;_TextureSample0;Texture Sample 0;23;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Instance;-1;Derivative;Texture2D;False;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;111;-9216,768;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;170;-12416,1664;Inherit;False;28;Size;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;180;-13696,1408;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;217;-14464,1408;Inherit;False;215;radialCoords;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;168;-12672,1536;Inherit;False;Property;_LUTCoreNoiseSize;LUT Core Noise Size;14;0;Create;True;0;0;0;False;0;False;1,0.01;1,0.01;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;175;-14208,1536;Inherit;False;False;True;False;False;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;23;-768,-2304;Inherit;False;Intensity;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;110;-8960,768;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;117;-9792,768;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;121;-10368,0;Inherit;False;True;True;False;True;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;176;-14208,1664;Inherit;False;True;True;False;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;169;-12416,1536;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;181;-13440,1408;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;106;-8832,128;Inherit;False;Property;_RadialDistortionAmount;Radial Distortion Amount;28;0;Create;True;0;0;0;False;0;False;0.05;0.05;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;107;-8832,256;Inherit;False;23;Intensity;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;112;-8832,768;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;122;-10112,0;Inherit;False;ConstantBiasScale;-1;;7;63208df05c83e8e49a48ffbdce2e43a0;0;3;3;FLOAT2;0,0;False;1;FLOAT;-0.5;False;2;FLOAT;2;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;118;-9600,768;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;166;-13184,1408;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;171;-12288,1536;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleContrastOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;104;-8576,640;Inherit;False;2;1;COLOR;0,0,0,0;False;0;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;105;-8448,128;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;108;-9344,0;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;167;-12288,1408;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;173;-11776,1536;Inherit;False;Property;_LUTCoreNoiseSpeed;LUT Core Noise Speed;15;0;Create;True;0;0;0;False;0;False;3.333,10;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;101;-7680,256;Inherit;False;2;2;0;FLOAT2;0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;102;-7552,640;Inherit;False;Property;_RadialDistortionCheapContrastLerp;Radial Distortion Cheap Contrast Lerp;29;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;103;-8448,0;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;172;-11776,1408;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;218;-13056,1920;Inherit;False;215;radialCoords;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;27;-768,-2048;Inherit;False;Boost;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GrabScreenPosition, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;61;-7680,0;Inherit;False;0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;100;-7552,512;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;138;-12544,2048;Inherit;False;Property;_LUTCoreAmplitude;LUT Core Amplitude;8;0;Create;True;0;0;0;False;0;False;2;30;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;219;-12800,1920;Inherit;True;False;True;True;True;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;126;-10752,1408;Inherit;True;Property;_TextureSample1;Texture Sample 0;23;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Instance;-1;Derivative;Texture2D;False;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;95;-6528,256;Inherit;False;Property;_Boost;Boost;25;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;97;-6528,384;Inherit;False;27;Boost;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;59;-7296,0;Inherit;False;2;2;0;FLOAT4;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;127;-10368,1408;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;136;-12544,1920;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;25;-768,-2176;Inherit;False;Contrast;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;96;-6272,256;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScreenColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;60;-7040,0;Inherit;False;Global;_GrabScreen0;Grab Screen 0;11;0;Create;True;0;0;0;False;0;False;Object;-1;False;False;False;False;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;85;-4736,384;Inherit;False;Constant;_Float0;Float 0;17;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;63;-1024,-1792;Inherit;False;2;4;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;128;-9344,1408;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;139;-12032,2048;Inherit;False;Property;_LUTCoreOffset;LUT Core Offset;16;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;137;-12288,1920;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;92;-6016,128;Inherit;False;25;Contrast;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;93;-6272,0;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.DesaturateOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;84;-4736,256;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;29;-768,-1920;Inherit;False;LUT Core Pan;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;131;-9088,1536;Inherit;False;Property;_LUTCoreDistortAmount;LUT Core Distort Amount;12;0;Create;True;0;0;0;False;0;False;0.2;0.2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;142;-12032,1920;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;140;-11776,2048;Inherit;False;Property;_LUTCoreSpeed;LUT Core Speed;19;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;222;-9216,1408;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;26;-768,-2112;Inherit;False;Invert;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleContrastOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;91;-6016,0;Inherit;False;2;1;COLOR;0,0,0,0;False;0;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;86;-4480,256;Inherit;False;True;False;False;True;1;0;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;129;-9088,1408;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;143;-11776,1920;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;135;-11392,2048;Inherit;False;29;LUT Core Pan;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;24;-768,-2240;Inherit;False;Desaturate;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;90;-5248,128;Inherit;False;26;Invert;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;98;-5760,0;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;99;-5504,128;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SmoothstepOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;229;-4224,256;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;51;-1664,1408;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;46;-1664,1536;Inherit;False;Property;_MaskRadius;Mask Radius;20;0;Create;True;0;0;0;False;3;Space(33);Header(Mask);Space(13);False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;47;-1664,1664;Inherit;False;Property;_MaskHardness;Mask Hardness;21;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;130;-8960,1408;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;134;-11392,1920;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;83;-3712,384;Inherit;False;Property;_LUTColorAmplitude;LUT Color Amplitude;3;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;89;-5248,0;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;88;-4736,128;Inherit;False;24;Desaturate;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;230;-3968,256;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;49;-1408,1408;Inherit;False;RadialGradient;-1;;8;ec972f7745a8353409da2eb8d000a2e3;0;3;1;FLOAT2;0,0;False;6;FLOAT;0;False;7;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;133;-11136,1920;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;78;-3712,256;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;82;-3456,384;Inherit;False;Property;_LUTColorOffset;LUT Color Offset;5;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DesaturateOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;87;-4736,0;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;232;-4224,-128;Inherit;False;Property;_ContrastErosionSmoothness;Contrast Erosion Smoothness;1;0;Create;True;0;0;0;False;0;False;0.1;0.1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;50;-1024,1408;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;73;-2432,1152;Inherit;False;26;Invert;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;72;-2944,1024;Inherit;False;Property;_LUTCoreColorInverted;LUT Core Color Inverted;11;0;Create;True;0;0;0;False;0;False;1,1,1,0;1,1,1,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;132;-10752,1920;Inherit;True;Property;_LUTCoreMask;LUT Core Mask;7;0;Create;True;0;0;0;False;3;Space(33);Header(LUT Core);Space(13);False;-1;701d9e02bf1a48c2a6d5fceee58ad7a8;701d9e02bf1a48c2a6d5fceee58ad7a8;True;0;False;white;Auto;False;Object;-1;Derivative;Texture2D;False;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;79;-3456,256;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;81;-3200,384;Inherit;False;Property;_LUTColorSpeed;LUT Color Speed;6;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;223;-4480,0;Inherit;False;True;False;False;True;1;0;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;231;-4224,-256;Inherit;False;Property;_ContrastErosion;Contrast Erosion;0;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;233;-3968,-256;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;52;-768,1408;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;56;-768,1792;Inherit;False;Property;_MaskErosionSmoothness;Mask Erosion Smoothness;23;0;Create;True;0;0;0;False;0;False;0.1;0.1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;71;-2432,1024;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;39;-10240,1920;Inherit;False;LUTCoreMaskOut;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;74;-2304,896;Inherit;False;Property;_LUTCoreCanInvert;LUT Core Can Invert;9;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;69;-2944,768;Inherit;False;Property;_LUTCoreColor;LUT Core Color;10;0;Create;True;0;0;0;False;0;False;0,0,0,0;0,0,0,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.PannerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;80;-3200,256;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SmoothstepOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;227;-4224,0;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;53;-512,1408;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;55;-768,1664;Inherit;False;Property;_MaskErosion;Mask Erosion;22;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;57;-256,1664;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;70;-2304,768;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;75;-2432,128;Inherit;False;Property;_LUTColorLerp;LUT Color Lerp;4;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;40;-2048,256;Inherit;True;39;LUTCoreMaskOut;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;77;-2816,256;Inherit;True;Property;_LUTColor;LUT Color;2;0;Create;True;0;0;0;False;3;Space(33);Header(LUT Color);Space(13);False;-1;ee8079530f87410882e0935f50e1d2f5;ee8079530f87410882e0935f50e1d2f5;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;False;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;228;-3968,0;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;54;-256,1408;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;64;-2048,128;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;66;-2432,0;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;65;-1664,128;Inherit;False;Property;_LUTCoreLerp;LUT Core Lerp;13;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;35;-1664,384;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;45;-1152,1024;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;42;-1664,0;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;15;462,-50;Inherit;False;1252;162.95;AR;5;10;11;12;13;14;;0,0,0,1;0;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;193;-17584,-48;Inherit;False;2091.465;618.6652;Radial Pan;13;214;213;212;211;210;209;208;207;206;204;203;202;199;Radial Pan;0,0,0,1;0;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;36;-1408,768;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;33;-1408,256;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;34;-1152,384;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;32;-896,128;Inherit;False;Property;_LUTCoreOverParticleColorLerp;LUT Core Over Particle Color Lerp;17;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;10;512,0;Inherit;False;Property;_Cull;Cull;36;0;Create;True;0;0;0;True;3;Space(33);Header(AR);Space(13);False;2;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;11;768,0;Inherit;False;Property;_Src;Src;37;0;Create;True;0;0;0;True;0;False;5;5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;12;1024,0;Inherit;False;Property;_Dst;Dst;38;0;Create;True;0;0;0;True;0;False;10;10;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;13;1280,0;Inherit;False;Property;_ZWrite;ZWrite;39;0;Create;True;0;0;0;True;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;14;1536,0;Inherit;False;Property;_ZTest;ZTest;40;0;Create;True;0;0;0;True;0;False;8;8;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;31;-896,0;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;38;-1152,768;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;199;-17536,0;Inherit;False;Property;_RadialUVPanSpeed;Radial UV Pan Speed;35;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;202;-17152,0;Inherit;False;True;False;True;True;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;203;-17152,384;Inherit;False;False;True;True;True;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;204;-17152,128;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;206;-16896,0;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;207;-16896,384;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;208;-16640,384;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,1;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;209;-16640,0;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;1,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;210;-16384,0;Inherit;False;True;False;True;True;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;211;-16384,384;Inherit;False;False;True;True;True;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;212;-16128,0;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;213;-16128,256;Inherit;False;Property;_RadialUVTile;Radial UV Tile;34;0;Create;True;0;0;0;False;0;False;1,1;1,1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;214;-15872,256;Inherit;True;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;0;0,0;Float;False;False;-1;3;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;ExtraPrePass;0;0;ExtraPrePass;5;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;14;all;0;False;True;1;1;False;;0;False;;0;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;1;False;;True;3;False;;True;True;0;False;;0;False;;True;0;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;1;0,0;Float;False;True;-1;3;UnityEditor.ShaderGraphUnlitGUI;0;13;Vefects/SH_Vefects_Impact_Frame_Advanced_01_Always;2992e84f91cbeb14eab234972e07ea9d;True;Forward;0;1;Forward;9;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Transparent=RenderType;Queue=Transparent=Queue=0;UniversalMaterialType=Unlit;True;5;True;14;all;0;True;True;1;5;True;_Src;10;True;_Dst;1;1;False;;10;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;True;True;2;True;_ZWrite;True;3;True;_ZTest;True;True;0;False;;0;False;;True;1;LightMode=UniversalForwardOnly;False;False;0;;0;0;Standard;30;Surface;1;639039173359139825;  Keep Alpha;0;0;  Blend;0;0;Two Sided;1;0;Alpha Clipping;0;639039173414538580;  Use Shadow Threshold;0;0;Forward Only;0;0;Cast Shadows;0;639039173409290340;Receive Shadows;2;0;Receive SSAO;0;639039173435769737;Motion Vectors;1;0;  Add Precomputed Velocity;0;0;  XR Motion Vectors;0;0;GPU Instancing;1;0;LOD CrossFade;0;639039173451891621;Built-in Fog;0;639039173458495155;Meta Pass;0;0;Extra Pre Pass;0;0;Tessellation;0;0;  Phong;0;0;  Strength;0.5,False,;0;  Type;0;0;  Tess;16,False,;0;  Min;10,False,;0;  Max;25,False,;0;  Edge Length;16,False,;0;  Max Displacement;25,False,;0;Write Depth;0;0;  Early Z;0;0;Vertex Position;1;0;0;13;False;True;False;True;False;False;True;True;True;False;True;False;True;False;;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;2;0,0;Float;False;False;-1;3;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;ShadowCaster;0;2;ShadowCaster;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;14;all;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;False;False;True;False;False;False;False;0;False;;False;False;False;False;False;False;False;False;False;True;1;False;;True;3;False;;False;True;1;LightMode=ShadowCaster;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;3;0,0;Float;False;False;-1;3;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;DepthOnly;0;3;DepthOnly;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;14;all;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;False;False;True;False;False;False;False;0;False;;False;False;False;False;False;False;False;False;False;True;1;False;;False;False;True;1;LightMode=DepthOnly;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;4;0,0;Float;False;False;-1;3;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;Meta;0;4;Meta;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;14;all;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=Meta;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;5;0,0;Float;False;False;-1;3;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;Universal2D;0;5;Universal2D;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;14;all;0;False;True;1;1;False;;0;False;;0;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;1;False;;True;3;False;;True;True;0;False;;0;False;;True;1;LightMode=Universal2D;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;6;0,0;Float;False;False;-1;3;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;SceneSelectionPass;0;6;SceneSelectionPass;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;14;all;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;2;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=SceneSelectionPass;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;7;0,0;Float;False;False;-1;3;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;ScenePickingPass;0;7;ScenePickingPass;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;14;all;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=Picking;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;8;0,0;Float;False;False;-1;3;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;DepthNormals;0;8;DepthNormals;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;14;all;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;False;;True;3;False;;False;True;1;LightMode=DepthNormalsOnly;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;9;0,0;Float;False;False;-1;3;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;DepthNormalsOnly;0;9;DepthNormalsOnly;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;14;all;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;False;;True;3;False;;False;True;1;LightMode=DepthNormalsOnly;False;True;9;d3d11;metal;vulkan;xboxone;xboxseries;playstation;ps4;ps5;switch;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;224;0,100;Float;False;False;-1;3;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;MotionVectors;0;10;MotionVectors;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;14;all;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;True;False;False;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=MotionVectors;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;225;0,100;Float;False;False;-1;3;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;XRMotionVectors;0;11;XRMotionVectors;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;14;all;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;True;1;False;;255;False;;1;False;;7;False;;3;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;1;LightMode=XRMotionVectors;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;226;0,100;Float;False;False;-1;3;UnityEditor.ShaderGraphUnlitGUI;0;1;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;GBuffer;0;12;GBuffer;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;14;all;0;False;True;1;5;True;_Src;10;True;_Dst;1;1;False;;10;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;2;True;;True;3;True;;True;True;0;False;;0;False;;True;1;LightMode=UniversalGBuffer;False;True;12;d3d11;gles;metal;vulkan;xboxone;xboxseries;playstation;ps4;ps5;switch;switch2;webgpu;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;185;-1664,-1152;Inherit;False;1151.229;133.6506;Check Clevender.com courses to learn about shaders <3;0;Check Clevender.com courses to learn about shaders <3;1,0,0.5490196,1;0;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;16;512,-384;Inherit;False;383;125.7968;Ge Lush was here! <3;0;;0,0,0,1;0;0
WireConnection;188;0;187;0
WireConnection;190;0;188;0
WireConnection;190;1;189;0
WireConnection;191;0;190;0
WireConnection;192;0;190;0
WireConnection;195;0;191;0
WireConnection;195;1;192;0
WireConnection;194;0;190;0
WireConnection;197;0;195;0
WireConnection;197;1;196;0
WireConnection;198;0;194;0
WireConnection;147;0;216;0
WireConnection;200;0;197;0
WireConnection;201;0;198;0
WireConnection;160;0;147;0
WireConnection;160;1;161;0
WireConnection;205;0;200;0
WireConnection;205;1;201;0
WireConnection;28;0;22;4
WireConnection;162;0;160;0
WireConnection;215;0;205;0
WireConnection;148;0;216;0
WireConnection;163;0;162;0
WireConnection;163;1;161;0
WireConnection;154;0;153;1
WireConnection;154;1;156;0
WireConnection;149;0;216;0
WireConnection;164;0;163;0
WireConnection;164;1;148;0
WireConnection;150;0;149;0
WireConnection;150;1;164;0
WireConnection;150;2;151;0
WireConnection;157;0;154;0
WireConnection;157;1;153;2
WireConnection;174;0;217;0
WireConnection;152;0;150;0
WireConnection;152;1;157;0
WireConnection;177;0;174;0
WireConnection;177;1;178;0
WireConnection;124;0;123;0
WireConnection;125;0;123;0
WireConnection;109;0;118;0
WireConnection;109;1;113;0
WireConnection;158;0;152;0
WireConnection;158;2;159;0
WireConnection;179;0;177;0
WireConnection;115;1;116;0
WireConnection;120;0;119;0
WireConnection;120;1;158;0
WireConnection;120;3;124;0
WireConnection;120;4;125;0
WireConnection;111;0;109;0
WireConnection;180;0;179;0
WireConnection;180;1;178;0
WireConnection;175;0;217;0
WireConnection;23;0;21;3
WireConnection;110;0;111;0
WireConnection;110;1;114;0
WireConnection;117;0;115;0
WireConnection;121;0;120;5
WireConnection;176;0;217;0
WireConnection;169;0;168;1
WireConnection;169;1;170;0
WireConnection;181;0;180;0
WireConnection;181;1;175;0
WireConnection;112;0;110;0
WireConnection;122;3;121;0
WireConnection;118;0;117;0
WireConnection;166;0;176;0
WireConnection;166;1;181;0
WireConnection;166;2;151;0
WireConnection;171;0;169;0
WireConnection;171;1;168;2
WireConnection;104;0;112;0
WireConnection;105;0;106;0
WireConnection;105;1;107;0
WireConnection;108;0;122;0
WireConnection;108;1;118;0
WireConnection;167;0;166;0
WireConnection;167;1;171;0
WireConnection;101;0;103;0
WireConnection;101;1;104;0
WireConnection;103;0;108;0
WireConnection;103;1;105;0
WireConnection;172;0;167;0
WireConnection;172;2;173;0
WireConnection;27;0;22;3
WireConnection;100;0;103;0
WireConnection;100;1;101;0
WireConnection;100;2;102;0
WireConnection;219;0;218;0
WireConnection;126;0;119;0
WireConnection;126;1;172;0
WireConnection;126;3;124;0
WireConnection;126;4;125;0
WireConnection;59;0;61;0
WireConnection;59;1;100;0
WireConnection;127;0;126;1
WireConnection;136;0;219;0
WireConnection;136;1;138;0
WireConnection;25;0;22;1
WireConnection;96;0;95;0
WireConnection;96;1;97;0
WireConnection;60;0;59;0
WireConnection;128;0;127;0
WireConnection;128;1;118;0
WireConnection;137;0;136;0
WireConnection;93;0;60;0
WireConnection;93;1;96;0
WireConnection;84;0;89;0
WireConnection;84;1;85;0
WireConnection;29;0;63;1
WireConnection;142;0;137;0
WireConnection;142;1;139;0
WireConnection;222;0;128;0
WireConnection;26;0;22;2
WireConnection;91;1;93;0
WireConnection;91;0;92;0
WireConnection;86;0;84;0
WireConnection;129;0;222;0
WireConnection;129;1;131;0
WireConnection;143;0;142;0
WireConnection;143;2;140;0
WireConnection;24;0;21;4
WireConnection;98;0;91;0
WireConnection;99;0;98;0
WireConnection;229;0;86;0
WireConnection;229;1;231;0
WireConnection;229;2;233;0
WireConnection;130;0;129;0
WireConnection;134;0;143;0
WireConnection;134;1;135;0
WireConnection;89;0;98;0
WireConnection;89;1;99;0
WireConnection;89;2;90;0
WireConnection;230;0;229;0
WireConnection;49;1;51;0
WireConnection;49;6;46;0
WireConnection;49;7;47;0
WireConnection;133;0;134;0
WireConnection;133;1;130;0
WireConnection;78;0;230;0
WireConnection;78;1;83;0
WireConnection;87;0;89;0
WireConnection;87;1;88;0
WireConnection;50;0;49;0
WireConnection;132;1;133;0
WireConnection;132;3;124;0
WireConnection;132;4;125;0
WireConnection;79;0;78;0
WireConnection;79;1;82;0
WireConnection;223;0;87;0
WireConnection;233;0;231;0
WireConnection;233;1;232;0
WireConnection;52;0;50;0
WireConnection;71;0;72;0
WireConnection;71;1;69;0
WireConnection;71;2;73;0
WireConnection;39;0;132;1
WireConnection;80;0;79;0
WireConnection;80;2;81;0
WireConnection;227;0;223;0
WireConnection;227;1;231;0
WireConnection;227;2;233;0
WireConnection;53;0;52;0
WireConnection;57;0;55;0
WireConnection;57;1;56;0
WireConnection;70;0;69;0
WireConnection;70;1;71;0
WireConnection;70;2;74;0
WireConnection;77;1;80;0
WireConnection;228;0;227;0
WireConnection;54;0;53;0
WireConnection;54;1;55;0
WireConnection;54;2;57;0
WireConnection;64;0;66;0
WireConnection;64;1;70;0
WireConnection;64;2;40;0
WireConnection;66;0;228;0
WireConnection;66;1;77;0
WireConnection;66;2;75;0
WireConnection;45;0;54;0
WireConnection;42;0;66;0
WireConnection;42;1;64;0
WireConnection;42;2;65;0
WireConnection;36;0;35;4
WireConnection;36;1;45;0
WireConnection;33;0;35;0
WireConnection;33;1;42;0
WireConnection;34;0;33;0
WireConnection;34;1;70;0
WireConnection;34;2;40;0
WireConnection;31;0;33;0
WireConnection;31;1;34;0
WireConnection;31;2;32;0
WireConnection;38;0;36;0
WireConnection;202;0;199;0
WireConnection;203;0;199;0
WireConnection;206;0;202;0
WireConnection;206;1;204;0
WireConnection;207;0;204;0
WireConnection;207;1;203;0
WireConnection;208;0;205;0
WireConnection;208;1;207;0
WireConnection;209;0;205;0
WireConnection;209;1;206;0
WireConnection;210;0;209;0
WireConnection;211;0;208;0
WireConnection;212;0;210;0
WireConnection;212;1;211;0
WireConnection;214;0;213;0
WireConnection;214;1;212;0
WireConnection;1;2;31;0
WireConnection;1;3;38;0
ASEEND*/
//CHKSM=829166428112080D9A4753A80145C95197AF31BF