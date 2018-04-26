// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "MMO/TerrainGPU"
{
	Properties
	{
	    _Color("Diffuse Color", Color) = (1,1,1,1)
	    _SpecColor("Specular Material Color", Color) = (1,1,1,1)
	    _Shininess("Shininess", Float) = 10
	    _Attenuation("Attenuation", Float) = 1.5
	    _Ambient("Ambient", Float) = 1.5
	    _MaxHeight("Maximum Height value", float) = 0
		_MainTex ("Main Texture", 2D) = "white" {}		
		_NormalTex("NormalMap Texture", 2D) = "blue" {}
		_HeightMap("HeightMap Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType" = "Opaque" }
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
			uniform float _Attenuation;
			uniform float _Ambient;

			struct vIn
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{                
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float4 posWorld : TEXCOORD1;
				float3 normalWorld : TEXCOORD2;
			};
			
			float _MaxHeight;

			sampler2D _MainTex;
			float4 _MainTex_ST;
			
            sampler2D _HeightMap;
			float4 _HeightMap_ST;
			
            sampler2D _NormalTex;
			float4 _NormalTex_ST;
			
			v2f vert (vIn input)
			{
				v2f result;
 
                float4x4 modelMatrix = unity_ObjectToWorld;
                float4x4 modelMatrixInverse = unity_WorldToObject; 
                
                float4 hmData = tex2Dlod(_HeightMap, float4(input.uv, 0, 0));              
                float height = hmData.w;
				hmData.x = (hmData.x - 0.5f) * 2;
				hmData.z = (hmData.z - 0.5f) * 2;

				input.vertex.y = height * _MaxHeight;
				
                result.normalWorld = normalize(hmData.xyz);
  
				result.pos = UnityObjectToClipPos(input.vertex);				
                result.posWorld = mul(modelMatrix, input.vertex);
				result.uv = input.uv;
                
                return result;
			} 
			
			fixed4 frag (v2f input) : SV_Target
			{			
			    float4 color = tex2D(_MainTex, input.uv);          
			    float3 localCoords = tex2D(_NormalTex, _NormalTex_ST.xy * input.uv + _NormalTex_ST.zw);
			    
                float3 normalDirection = localCoords + input.normalWorld;
                normalDirection = input.normalWorld;
            
                float3 viewDirection = normalize(_WorldSpaceCameraPos - input.posWorld.xyz);
                float3 lightDirection;
                float attenuation;
                
                if (0.0 == _WorldSpaceLightPos0.w) // directional light?
                { 
                    attenuation = 1.0; // no attenuation
                    lightDirection = normalize(_WorldSpaceLightPos0.xyz);
                } 
                else // point or spot light
                {
                    float3 vertexToLightSource = _WorldSpaceLightPos0.xyz - input.posWorld.xyz;
                    float distance = length(vertexToLightSource);
                    attenuation = 1.0 / distance; // linear attenuation 
                    lightDirection = normalize(vertexToLightSource);
                }
                
                float3 ambientLighting = UNITY_LIGHTMODEL_AMBIENT.rgb * _Color.rgb;
                
                float3 diffuseReflection = attenuation * _LightColor0.rgb * _Color.rgb * max(0.0, dot(normalDirection, lightDirection));
                
                float3 specularReflection;
                if (dot(normalDirection, lightDirection) < 0.0) 
                {
                    specularReflection = float3(0.0, 0.0, 0.0); 
                }
                else
                {
                    specularReflection = attenuation * _LightColor0.rgb * _SpecColor.rgb * pow(max(0.0, dot(reflect(-lightDirection, normalDirection), viewDirection)), _Shininess);
                }    
         //return color;   
                return float4(ambientLighting + diffuseReflection * color , 1.0);                
			}
			ENDCG
		}
	}
}
