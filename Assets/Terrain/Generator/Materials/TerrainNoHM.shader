Shader "MMO/TerrainNoHM"
{
	Properties
	{
	    _Color("Diffuse Color", Color) = (1,1,1,1)
	    _SpecColor("Specular Material Color", Color) = (1,1,1,1)
	    _Shininess("Shininess", Float) = 10
		_MainTex ("Main Texture", 2D) = "white" {}
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
			
			#include "UnityCG.cginc"
						
			uniform float4 _LightColor0;
			uniform float4 _Color;
			uniform float4 _SpecColor;
			uniform float _Shininess;

			struct vIn
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{                
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
                float3 diffuseColor : TEXCOORD1;
            	float3 specularColor : TEXCOORD2;
			};
			
			sampler2D _MainTex;
			float4 _MainTex_ST;
						
			v2f vert (vIn input)
			{
				v2f result;
 
                float4x4 modelMatrix = unity_ObjectToWorld;
                float4x4 modelMatrixInverse = unity_WorldToObject; 
                
				float3 normalDirection = input.normal;
				float3 viewDirection = normalize(_WorldSpaceCameraPos - mul(modelMatrix, input.vertex).xyz);
				float3 lightDirection;
				float attenuation;

				if (0.0 == _WorldSpaceLightPos0.w) // directional light?
				{
					attenuation = 1.0; // no attenuation
					lightDirection = normalize(_WorldSpaceLightPos0.xyz);
				}
				else // point or spot light
				{
					float3 vertexToLightSource = _WorldSpaceLightPos0.xyz - mul(modelMatrix, input.vertex).xyz;
					float distance = length(vertexToLightSource);
					attenuation = 1.0 / distance; // linear attenuation 
					lightDirection = normalize(vertexToLightSource);
				}

				float3 ambientLighting = UNITY_LIGHTMODEL_AMBIENT.rgb * _Color.rgb;

				float3 diffuseReflection = attenuation * _LightColor0.rgb * _Color.rgb * max(0.0, dot(normalDirection, lightDirection));

				float3 specularReflection;
				if (dot(normalDirection, lightDirection) < 0.0)
					// light source on the wrong side?
				{
					specularReflection = float3(0.0, 0.0, 0.0);
					// no specular reflection
				}
				else // light source on the right side
				{
					specularReflection = attenuation * _LightColor0.rgb	* _SpecColor.rgb * pow(max(0.0, dot(reflect(-lightDirection, normalDirection), viewDirection)), _Shininess);
				}

				result.diffuseColor = ambientLighting + diffuseReflection;
				result.specularColor = specularReflection;
				result.pos = UnityObjectToClipPos(input.vertex);
				result.uv = input.uv;
                
                return result;
			} 
			
			fixed4 frag (v2f input) : SV_Target
			{
			    float4 color = tex2D(_MainTex, input.uv);
                return float4(input.diffuseColor * color, 1.0);                
			}
			ENDCG
		}
	}
}
