// https://github.com/leon196/PointCloudExporter/blob/master/Assets/Shaders/PointCloud.shader

Shader "Custom/PointCloud"
{
	Properties
	{
		_PointColor ("Point Color", Color) = (0, 0, 0, 0)
		_Size ("Size", Range(0.05, 0.5)) = 0.15
		_ColorNear ("ColorNear", Range(0, 100)) = 20
		_ColorFar ("ColorFar", Range(10, 1000)) = 250
		_AtmosphereValue ("AtmosphereValue", Range(0, 1)) = 0.5

	}
	SubShader
	{
		Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" "IgnoreProjector"="True" }
		Blend SrcAlpha OneMinusSrcAlpha
		AlphaToMask On
		Cull Off
		
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float4 _PointColor;
			float _Size;
			float _ColorNear;
			float _ColorFar;
			float _AtmosphereValue;

			struct GS_INPUT
			{
				float4 vertex : POSITION;
				float3 normal	: NORMAL;
				float4 color	: COLOR;
				float2 texcoord : TEXCOORD0;
				float2 texcoord1 : TEXCOORD1;
			};

			struct FS_INPUT {
				float4 vertex : SV_POSITION;
				float3 normal : NORMAL;
				float4 color : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			GS_INPUT vert (appdata_full v)
			{
				GS_INPUT o = (GS_INPUT)0;
				o.vertex = v.vertex;
				o.normal = v.normal;

				float dist = length(_WorldSpaceCameraPos - mul(unity_ObjectToWorld, v.vertex).xyz);
				o.color = lerp(_PointColor, float4(1,1,1,1), clamp((dist - _ColorNear) / (_ColorFar - _ColorNear), 0, 1.0) * _AtmosphereValue);

				return o;
			}


			[maxvertexcount(3)]
			void geom (point GS_INPUT tri[1], inout TriangleStream<FS_INPUT> triStream)
			{
				FS_INPUT pIn = (FS_INPUT)0;
				pIn.normal = UNITY_MATRIX_IT_MV[2].xyz;
				pIn.color = tri[0].color;

				float4 vertex = mul(unity_ObjectToWorld, tri[0].vertex);
				float3 tangent = normalize(cross(float3(0,1,0), pIn.normal));
				float3 up = normalize(cross(tangent, pIn.normal));

				pIn.vertex = mul(UNITY_MATRIX_VP, vertex + float4(up * -0.5 * _Size + tangent * -0.866 * _Size, 0));
				pIn.texcoord = float2(-0.866, -0.5);
				triStream.Append(pIn);

				pIn.vertex = mul(UNITY_MATRIX_VP, vertex + float4(up * _Size, 0));
				pIn.texcoord = float2(0.0, 1);
				triStream.Append(pIn);

				pIn.vertex = mul(UNITY_MATRIX_VP, vertex + float4(up * -0.5 * _Size + tangent *  0.866 * _Size, 0));
				pIn.texcoord = float2(0.866, -0.5);
				triStream.Append(pIn);
			}

			float4 frag (FS_INPUT i) : COLOR
			{
				float4 color = i.color;
				float dst = length(i.texcoord);
				color.a = 1 - step(0.5, dst);
				return color;
			}
			ENDCG
		}
	}
}