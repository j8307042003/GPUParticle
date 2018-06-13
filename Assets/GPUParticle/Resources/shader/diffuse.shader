Shader "GPUParticle/diffuse"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_BaseColor("BaseColor", COLOR) = (0.0, 0.0, 0.0, 0.0)
		_Ambient ("ambient", Range(0.0, 1.0)) = 0.0
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			#pragma multi_compile_instancing
			#pragma target 4.5

			#include "particle.h"
			#include "UnityCG.cginc"
			#include "UnityLightingCommon.cginc"

#if SHADER_TARGET >= 45
		StructuredBuffer<Particle> particlePool;
		StructuredBuffer<uint> alivelist;
#endif

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
				float3 worldNormal:NORMAL;
				float4 worldPos: TEXCOORD2;
				fixed4 diff : COLOR0;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float _Ambient;
			float4 _BaseColor;

			v2f vert (appdata v, uint instanceID : SV_InstanceID)
			{
				v2f o;
#if SHADER_TARGET >= 45
				uint particleId = alivelist[instanceID];
				Particle data = particlePool[particleId];
				o.worldPos = mul(data.model, v.vertex);
				o.vertex = mul(UNITY_MATRIX_VP, o.worldPos);
				float4x4 model = data.model;
				o.worldNormal = normalize(mul((float3x3)model, v.normal).xyz);
#else
				float4 data = 0;
				o.vertex = UnityObjectToClipPos(v.vertex);
#endif
				half3 worldNormal = -o.worldNormal;
				half nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));
				o.diff = nl * _LightColor0;
				o.diff.rgb += ShadeSH9(half4(worldNormal, 1));


				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);
				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);

				float3 camToThis = i.worldPos - _WorldSpaceCameraPos;

				//col = float4(i.worldNormal.xyz, 0.0);
				col = _BaseColor;
				col *= i.diff + _Ambient;
				
				return col;
			}
			ENDCG
		}
	}
}
