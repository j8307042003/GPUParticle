Shader "Unlit/depthDebug"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			sampler2D _MainTex;
			sampler2D _CameraDepthTexture;
			sampler2D _CameraDepthNormalsTexture;

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_CameraDepthTexture, i.uv);
				// just invert the colors
				float depth = (1 - DecodeFloatRG(col.zw)) / 5;
				//depth = LinearEyeDepth(depth);
				//col.rgb = 1 - col.rgb;
				//col = float4(depth, depth, depth, 1.0);
				col = float4(depth, depth, depth, 1.0);
				//col.rgb = DecodeViewNormalStereo(tex2D(_CameraDepthNormalsTexture, i.uv));
				//col.rgba = tex2D(_CameraDepthNormalsTexture, i.uv);
				return col;
			}
			ENDCG
		}
	}
}
