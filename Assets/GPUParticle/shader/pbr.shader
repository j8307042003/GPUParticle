Shader "GPUParticle/PBR" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Emission("Emission", Range(0,1)) = 0.0
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0

	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		//#pragma surface surf Standard fullforwardshadows
		#pragma surface surf Standard addshadow fullforwardshadows
		#pragma multi_compile_instancing
		#pragma instancing_options procedural:setup

		#include "UnityInstancing.cginc"
		#include "UnityCG.cginc"
		
		sampler2D _MainTex;

		struct Particle {
			float lifespan;
			float3 position;
			float3 velocity;
			float4x4 model;
			float3 scale;
			float4 quaternion;
			uint id;
		};

		struct Input {
			float2 uv_MainTex;
		};

		half _Glossiness;
		half _Metallic;
		half _Emission;
		fixed4 _Color;
		
#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
		StructuredBuffer<Particle> particlePool;
		StructuredBuffer<uint> alivelist;
#endif

		void setup()
		{
#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
			uint particleId = alivelist[unity_InstanceID];
			Particle data = particlePool[particleId];

			unity_ObjectToWorld = data.model;
			unity_WorldToObject = unity_ObjectToWorld;
			unity_WorldToObject._14_24_34 *= -1;
			unity_WorldToObject._11_22_33 = 1.0f / unity_WorldToObject._11_22_33;
#endif
		}


		void surf (Input IN, inout SurfaceOutputStandard o) {
			// Albedo comes from a texture tinted by color
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb;
			o.Emission = _Emission * _Color.xyz;
			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = 1.0;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
