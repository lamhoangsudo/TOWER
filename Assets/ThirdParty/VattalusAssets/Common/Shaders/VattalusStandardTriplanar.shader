// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "VattalusAssets/StandardTriplanar"
{
	Properties
	{
		_Albedo("Albedo", 2D) = "white" {}
		_MetalRoughAO("MetalRoughAO", 2D) = "white" {}
		_NormalMap("NormalMap", 2D) = "bump" {}
		_Emissive("Emissive", 2D) = "black" {}
		_EmissionColor("EmissionColor", Color) = (1,1,1,0)
		_GrungeSide("GrungeSide", 2D) = "white" {}
		_GrungeTop("GrungeTop", 2D) = "white" {}
		_GrungeIntensity("GrungeIntensity", Range( 0 , 1)) = 0
		_GrungeAlbedoEffect("GrungeAlbedoEffect", Range( 0 , 1)) = 0.5
		_GrungeContrast("GrungeContrast", Range( -1 , 1)) = 0
		_GrungeTiling("GrungeTiling", Range( 0 , 15)) = 1
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IsEmissive" = "true"  }
		Cull Back
		ZTest LEqual
		CGINCLUDE
		#include "UnityShaderVariables.cginc"
		#include "UnityPBSLighting.cginc"
		#include "Lighting.cginc"
		#pragma target 3.0
		#ifdef UNITY_PASS_SHADOWCASTER
			#undef INTERNAL_DATA
			#undef WorldReflectionVector
			#undef WorldNormalVector
			#define INTERNAL_DATA half3 internalSurfaceTtoW0; half3 internalSurfaceTtoW1; half3 internalSurfaceTtoW2;
			#define WorldReflectionVector(data,normal) reflect (data.worldRefl, half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal)))
			#define WorldNormalVector(data,normal) half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal))
		#endif
		struct Input
		{
			float2 uv_texcoord;
			float3 worldPos;
			float3 worldNormal;
			INTERNAL_DATA
		};

		uniform sampler2D _NormalMap;
		uniform float4 _NormalMap_ST;
		uniform sampler2D _Albedo;
		uniform float4 _Albedo_ST;
		uniform float _GrungeContrast;
		uniform sampler2D _GrungeSide;
		uniform float _GrungeTiling;
		uniform sampler2D _GrungeTop;
		uniform float _GrungeIntensity;
		uniform float _GrungeAlbedoEffect;
		uniform sampler2D _Emissive;
		uniform float4 _Emissive_ST;
		uniform float4 _EmissionColor;
		uniform sampler2D _MetalRoughAO;
		uniform float4 _MetalRoughAO_ST;


		float4 CalculateContrast( float contrastValue, float4 colorTarget )
		{
			float t = 0.5 * ( 1.0 - contrastValue );
			return mul( float4x4( contrastValue,0,0,t, 0,contrastValue,0,t, 0,0,contrastValue,t, 0,0,0,1 ), colorTarget );
		}

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 uv_NormalMap = i.uv_texcoord * _NormalMap_ST.xy + _NormalMap_ST.zw;
			o.Normal = UnpackNormal( tex2D( _NormalMap, uv_NormalMap ) );
			float2 uv_Albedo = i.uv_texcoord * _Albedo_ST.xy + _Albedo_ST.zw;
			float4 tex2DNode342 = tex2D( _Albedo, uv_Albedo );
			float4 color352 = IsGammaSpace() ? float4(0.1698113,0.1305625,0.1305625,0) : float4(0.0244657,0.01543727,0.01543727,0);
			float3 ase_vertex3Pos = mul( unity_WorldToObject, float4( i.worldPos , 1 ) );
			float2 appendResult336 = (float2(ase_vertex3Pos.y , ase_vertex3Pos.z));
			float GTiling355 = _GrungeTiling;
			float3 ase_worldNormal = WorldNormalVector( i, float3( 0, 0, 1 ) );
			float3 temp_output_72_0 = abs( mul( unity_WorldToObject, float4( ase_worldNormal , 0.0 ) ).xyz );
			float dotResult73 = dot( temp_output_72_0 , float3(1,1,1) );
			float3 temp_output_75_0 = ( temp_output_72_0 / dotResult73 );
			float2 appendResult335 = (float2(ase_vertex3Pos.x , ase_vertex3Pos.z));
			float2 appendResult334 = (float2(ase_vertex3Pos.x , ase_vertex3Pos.y));
			float4 temp_cast_2 = (( ( ( tex2D( _GrungeSide, ( appendResult336 * GTiling355 ) ).r * temp_output_75_0.x ) + ( tex2D( _GrungeSide, ( appendResult335 * GTiling355 ) ).r * temp_output_75_0.y ) ) + ( tex2D( _GrungeTop, ( appendResult334 * GTiling355 ) ).r * temp_output_75_0.z ) )).xxxx;
			float4 lerpResult344 = lerp( float4( 0,0,0,0 ) , CalculateContrast(_GrungeContrast,temp_cast_2) , _GrungeIntensity);
			float4 temp_output_345_0 = ( 1.0 - lerpResult344 );
			float4 lerpResult366 = lerp( color352 , tex2DNode342 , temp_output_345_0);
			float4 lerpResult369 = lerp( tex2DNode342 , lerpResult366 , _GrungeAlbedoEffect);
			o.Albedo = lerpResult369.rgb;
			float2 uv_Emissive = i.uv_texcoord * _Emissive_ST.xy + _Emissive_ST.zw;
			o.Emission = ( tex2D( _Emissive, uv_Emissive ) * _EmissionColor ).rgb;
			float2 uv_MetalRoughAO = i.uv_texcoord * _MetalRoughAO_ST.xy + _MetalRoughAO_ST.zw;
			float4 tex2DNode372 = tex2D( _MetalRoughAO, uv_MetalRoughAO );
			o.Metallic = ( tex2DNode372.r * temp_output_345_0 ).r;
			o.Smoothness = ( ( 1.0 - tex2DNode372.g ) * temp_output_345_0 ).r;
			o.Occlusion = tex2DNode372.b;
			o.Alpha = 1;
		}

		ENDCG
		CGPROGRAM
		#pragma exclude_renderers xboxseries playstation switch nomrt 
		#pragma surface surf Standard keepalpha fullforwardshadows 

		ENDCG
		Pass
		{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" }
			ZWrite On
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile_shadowcaster
			#pragma multi_compile UNITY_PASS_SHADOWCASTER
			#pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
			#include "HLSLSupport.cginc"
			#if ( SHADER_API_D3D11 || SHADER_API_GLCORE || SHADER_API_GLES || SHADER_API_GLES3 || SHADER_API_METAL || SHADER_API_VULKAN )
				#define CAN_SKIP_VPOS
			#endif
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "UnityPBSLighting.cginc"
			struct v2f
			{
				V2F_SHADOW_CASTER;
				float2 customPack1 : TEXCOORD1;
				float4 tSpace0 : TEXCOORD2;
				float4 tSpace1 : TEXCOORD3;
				float4 tSpace2 : TEXCOORD4;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
			v2f vert( appdata_full v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID( v );
				UNITY_INITIALIZE_OUTPUT( v2f, o );
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );
				UNITY_TRANSFER_INSTANCE_ID( v, o );
				Input customInputData;
				float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				half3 worldNormal = UnityObjectToWorldNormal( v.normal );
				half3 worldTangent = UnityObjectToWorldDir( v.tangent.xyz );
				half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
				half3 worldBinormal = cross( worldNormal, worldTangent ) * tangentSign;
				o.tSpace0 = float4( worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x );
				o.tSpace1 = float4( worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y );
				o.tSpace2 = float4( worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z );
				o.customPack1.xy = customInputData.uv_texcoord;
				o.customPack1.xy = v.texcoord;
				TRANSFER_SHADOW_CASTER_NORMALOFFSET( o )
				return o;
			}
			half4 frag( v2f IN
			#if !defined( CAN_SKIP_VPOS )
			, UNITY_VPOS_TYPE vpos : VPOS
			#endif
			) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				Input surfIN;
				UNITY_INITIALIZE_OUTPUT( Input, surfIN );
				surfIN.uv_texcoord = IN.customPack1.xy;
				float3 worldPos = float3( IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w );
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				surfIN.worldPos = worldPos;
				surfIN.worldNormal = float3( IN.tSpace0.z, IN.tSpace1.z, IN.tSpace2.z );
				surfIN.internalSurfaceTtoW0 = IN.tSpace0.xyz;
				surfIN.internalSurfaceTtoW1 = IN.tSpace1.xyz;
				surfIN.internalSurfaceTtoW2 = IN.tSpace2.xyz;
				SurfaceOutputStandard o;
				UNITY_INITIALIZE_OUTPUT( SurfaceOutputStandard, o )
				surf( surfIN, o );
				#if defined( CAN_SKIP_VPOS )
				float2 vpos = IN.pos;
				#endif
				SHADOW_CASTER_FRAGMENT( IN )
			}
			ENDCG
		}
	}
	Fallback "Standard"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18935
0;0;2560;1379;237.0708;-23.2501;1.3;True;False
Node;AmplifyShaderEditor.WorldNormalVector;144;-2304.142,844.7473;Inherit;False;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.WorldToObjectMatrix;329;-2304.142,748.7474;Inherit;False;0;1;FLOAT4x4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;145;-2089.951,820.1588;Inherit;False;2;2;0;FLOAT4x4;0,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.Vector3Node;264;-1963.069,1000.609;Float;False;Constant;_Vector0;Vector 0;-1;0;Create;True;0;0;0;False;0;False;1,1,1;0,0,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.AbsOpNode;72;-1929.951,820.1588;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.DotProductOpNode;73;-1756.051,886.5565;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;75;-1593.951,820.1588;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;353;-1254.031,1208.821;Float;False;Property;_GrungeTiling;GrungeTiling;10;0;Create;True;0;0;0;False;0;False;1;0.3;0;15;0;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;238;-1402.381,689.7797;Inherit;False;FLOAT3;1;0;FLOAT3;0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.PosVertexDataNode;97;-1214.548,690.0286;Inherit;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;355;-938.4299,1208.479;Inherit;False;GTiling;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PosVertexDataNode;98;-1211.548,417.0287;Inherit;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.BreakToComponentsNode;240;-1403.856,955.0555;Inherit;False;FLOAT3;1;0;FLOAT3;0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.WireNode;198;-1253.76,641.5642;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;336;-1002.813,465.06;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PosVertexDataNode;96;-1214.548,933.2895;Inherit;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;335;-1009.813,721.0598;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.WireNode;298;-1263.391,1100.745;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;357;-1009.93,813.2791;Inherit;False;355;GTiling;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;239;-1403.856,819.0304;Inherit;False;FLOAT3;1;0;FLOAT3;0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.GetLocalVarNode;358;-1003.43,554.5789;Inherit;False;355;GTiling;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;90;-1220.688,631.0018;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;89;-431.5234,626.7899;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;296;-1223.888,1112.379;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;360;-827.9299,464.879;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.WireNode;319;-430.5234,881.7897;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;356;-1012.53,1048.579;Inherit;False;355;GTiling;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;359;-827.9297,715.779;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;334;-1008.913,956.6314;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;354;-830.5306,954.9793;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.WireNode;320;-399.5234,594.7899;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;337;-686.3151,686.6896;Inherit;True;Property;_Tex2;Tex2;5;0;Create;True;0;0;0;False;0;False;-1;None;9f8d9d9e60979574ea22974d2e2c08d4;True;0;False;white;Auto;False;Instance;1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.WireNode;295;-398.5234,849.7898;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;1;-684.2228,437.8899;Inherit;True;Property;_GrungeSide;GrungeSide;5;0;Create;True;0;0;0;False;0;False;-1;None;f77db0a408437ff44a8dcc75fecfbc9f;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.WireNode;318;-429.5234,1110.067;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;31;-319.5234,690.7899;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;338;-688.9152,925.8613;Inherit;True;Property;_GrungeTop;GrungeTop;6;0;Create;True;0;0;0;False;0;False;-1;None;f77db0a408437ff44a8dcc75fecfbc9f;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;28;-319.5234,418.7902;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;297;-397.5234,1078.067;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;34;-319.5234,930.7897;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;32;-79.52346,530.7902;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;35;176.4766,786.7899;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;346;174.361,1197.847;Float;False;Property;_GrungeContrast;GrungeContrast;9;0;Create;True;0;0;0;False;0;False;0;0;-1;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;347;172.177,1289.579;Float;False;Property;_GrungeIntensity;GrungeIntensity;7;0;Create;True;0;0;0;False;0;False;0;0.4;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleContrastOpNode;343;526.6202,1052.678;Inherit;False;2;1;COLOR;0,0,0,0;False;0;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;344;750.8464,1105.698;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;372;958.9292,1234.85;Inherit;True;Property;_MetalRoughAO;MetalRoughAO;1;0;Create;True;0;0;0;False;0;False;-1;None;b86d66511afb5e248a57f7d780879736;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.WireNode;376;1700.83,1341.05;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;352;982.4415,77.95489;Inherit;False;Constant;_GrungeColor;GrungeColor;9;0;Create;True;0;0;0;False;0;False;0.1698113,0.1305625,0.1305625,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;342;902.1129,255.2435;Inherit;True;Property;_Albedo;Albedo;0;0;Create;True;0;0;0;False;0;False;1;None;ade5568d14e3fac4abdb3e0cd31b559d;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;345;946.1827,1106.011;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;364;1284.731,369.479;Float;False;Property;_GrungeAlbedoEffect;GrungeAlbedoEffect;8;0;Create;True;0;0;0;False;0;False;0.5;0.6;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;366;1287.056,160.7103;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;341;1267.267,671.78;Inherit;True;Property;_Emissive;Emissive;3;0;Create;True;0;0;0;False;0;False;-1;None;645a2a13430b0c94f801e8be0619b1f7;True;0;False;black;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;370;1351.529,863.0501;Inherit;False;Property;_EmissionColor;EmissionColor;4;0;Create;True;0;0;0;False;0;False;1,1,1,0;1,1,1,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;373;1348.929,1288.15;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;375;1846.429,1321.55;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;349;1617.269,1141.647;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;348;1619.719,1035.386;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;369;1564.498,256.7901;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;340;1269.171,474.7683;Inherit;True;Property;_NormalMap;NormalMap;2;0;Create;True;0;0;0;False;0;False;-1;None;bd53834390004bb4c812a29752b86758;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;371;1615.429,675.8499;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.WireNode;374;1912.729,1221.449;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;1956.572,178.2452;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;VattalusAssets/StandardTriplanar;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;-1;3;False;-1;False;0;False;-1;0;False;-1;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;14;d3d9;d3d11_9x;d3d11;glcore;gles;gles3;metal;vulkan;xbox360;xboxone;ps4;psp2;n3ds;wiiu;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;0;4;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;1;False;-1;1;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;Standard;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;145;0;329;0
WireConnection;145;1;144;0
WireConnection;72;0;145;0
WireConnection;73;0;72;0
WireConnection;73;1;264;0
WireConnection;75;0;72;0
WireConnection;75;1;73;0
WireConnection;238;0;75;0
WireConnection;355;0;353;0
WireConnection;240;0;75;0
WireConnection;198;0;238;0
WireConnection;336;0;98;2
WireConnection;336;1;98;3
WireConnection;335;0;97;1
WireConnection;335;1;97;3
WireConnection;298;0;240;2
WireConnection;239;0;75;0
WireConnection;90;0;198;0
WireConnection;89;0;90;0
WireConnection;296;0;298;0
WireConnection;360;0;336;0
WireConnection;360;1;358;0
WireConnection;319;0;239;1
WireConnection;359;0;335;0
WireConnection;359;1;357;0
WireConnection;334;0;96;1
WireConnection;334;1;96;2
WireConnection;354;0;334;0
WireConnection;354;1;356;0
WireConnection;320;0;89;0
WireConnection;337;1;359;0
WireConnection;295;0;319;0
WireConnection;1;1;360;0
WireConnection;318;0;296;0
WireConnection;31;0;337;1
WireConnection;31;1;295;0
WireConnection;338;1;354;0
WireConnection;28;0;1;1
WireConnection;28;1;320;0
WireConnection;297;0;318;0
WireConnection;34;0;338;1
WireConnection;34;1;297;0
WireConnection;32;0;28;0
WireConnection;32;1;31;0
WireConnection;35;0;32;0
WireConnection;35;1;34;0
WireConnection;343;1;35;0
WireConnection;343;0;346;0
WireConnection;344;1;343;0
WireConnection;344;2;347;0
WireConnection;376;0;372;3
WireConnection;345;0;344;0
WireConnection;366;0;352;0
WireConnection;366;1;342;0
WireConnection;366;2;345;0
WireConnection;373;0;372;2
WireConnection;375;0;376;0
WireConnection;349;0;373;0
WireConnection;349;1;345;0
WireConnection;348;0;372;1
WireConnection;348;1;345;0
WireConnection;369;0;342;0
WireConnection;369;1;366;0
WireConnection;369;2;364;0
WireConnection;371;0;341;0
WireConnection;371;1;370;0
WireConnection;374;0;375;0
WireConnection;0;0;369;0
WireConnection;0;1;340;0
WireConnection;0;2;371;0
WireConnection;0;3;348;0
WireConnection;0;4;349;0
WireConnection;0;5;374;0
ASEEND*/
//CHKSM=170CD7C93F21303DD47DF572E91AE8EA89832ED2