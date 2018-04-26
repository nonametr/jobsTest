Shader "Mess/test"
{
	Properties
	{
        _DmgColor ("Damage Diffuse Material Color", Color) = (1,1,1,1)   
        _DmgHoleSize ("Damage size", Float) = 0.04 
        _DmgTintSize ("Damage tint size", Float) = 0.001
		_MainTex ("Texture", 2D) = "white" {}		
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _Color ("Diffuse Material Color", Color) = (1,1,1,1) 
        _SpecColor ("Specular Material Color", Color) = (1,1,1,1) 
        _Shininess ("Shininess", Float) = 10
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		Cull [_CullVar]
		LOD 100

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
				float3 normal : NORMAL;
                float4 tangent : TANGENT;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float3 worldPos: TEXCOORD1;
                half3 tangentWorld : TEXCOORD2;
                half3 normalWorld : TEXCOORD3;
                half3 binormalWorld : TEXCOORD4;
				float4 clipPos : SV_POSITION;
			};
			
			uniform float4 _LightColor0; 
			
			uniform float holeSize;
			uniform float3 start;
			uniform float3 end;
			
			uniform float _DmgHoleSize;
			uniform float _DmgTintSize;
            uniform float4 _Color; 
            uniform float4 _SpecColor; 
            uniform float _Shininess;
            uniform float4 _DmgColor; 
			sampler2D _MainTex;
			sampler2D _BumpMap;
			float4 _MainTex_ST;
			float4 _BumpMap_ST;
					
			
			v2f vert (appdata v)
			{
				v2f o;
				o.clipPos = UnityObjectToClipPos(v.vertex);
				o.worldPos = mul (unity_ObjectToWorld, v.vertex);
				
				float4x4 modelMatrix = unity_ObjectToWorld;
                float4x4 modelMatrixInverse = unity_WorldToObject;

                o.tangentWorld = normalize(mul(modelMatrix, float4(v.tangent.xyz, 0.0)).xyz);
                o.normalWorld = normalize(mul(float4(v.normal, 0.0), modelMatrixInverse).xyz);
                o.binormalWorld = normalize(cross(o.normalWorld, o.tangentWorld) * v.tangent.w);
                
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			
			fixed4 defaultFrag(v2f i)
			{
			    float4 encodedNormal = tex2D(_BumpMap, _BumpMap_ST.xy * i.uv + _BumpMap_ST.zw);
                float3 decodedNormal = float3(2.0 * encodedNormal.a - 1.0, 2.0 * encodedNormal.g - 1.0, 0.0);
                decodedNormal.z = sqrt(1.0 - dot(decodedNormal, decodedNormal));
    
                float3x3 local2WorldTranspose = float3x3(i.tangentWorld, i.binormalWorld, i.normalWorld);
                
                float3 worldNormal = normalize(mul(decodedNormal, local2WorldTranspose));
    
                float3 viewDirection = normalize(_WorldSpaceCameraPos - i.worldPos.xyz);
                float3 lightDirection;
                float attenuation;
    
                 if (0.0 == _WorldSpaceLightPos0.w) // directional light?
                 { 
                    attenuation = 1.0; // no attenuation
                    lightDirection = normalize(_WorldSpaceLightPos0.xyz);
                 } 
                 else // point or spot light
                 {
                    float3 vertexToLightSource = _WorldSpaceLightPos0.xyz - i.worldPos.xyz;
                    float distance = length(vertexToLightSource);
                    attenuation = 1.0 / distance; // linear attenuation 
                    lightDirection = normalize(vertexToLightSource);
                 }
    
                float3 ambientLighting = UNITY_LIGHTMODEL_AMBIENT.rgb * _Color.rgb;
    
                float3 diffuseReflection = attenuation * _LightColor0.rgb * _Color.rgb * max(0.0, dot(worldNormal, lightDirection));
    
                float3 specularReflection;
                if (dot(worldNormal, lightDirection) < 0.0) 
                // light source on the wrong side?
                {
                    specularReflection = float3(0.0, 0.0, 0.0); 
                // no specular reflection
                }
                else // light source on the right side
                {
                    specularReflection = attenuation * _LightColor0.rgb * _SpecColor.rgb * pow(max(0.0, dot(reflect(-lightDirection, worldNormal), viewDirection)), _Shininess);
                }
                return tex2D(_MainTex, _MainTex_ST.xy * i.uv + _MainTex_ST.zw) * float4(ambientLighting + diffuseReflection + specularReflection, 1.0);
			}
						
			float4 frag (v2f i) : COLOR
			{
			    float4 result = float4(0, 0, 0, 0);
			    float cos = abs(dot(normalize(i.worldPos - start), normalize(i.worldPos - end)));
			    if(cos > 1 - _DmgHoleSize && cos < 1)
			    {
			        if(cos > 1 - _DmgHoleSize && cos < 1 - _DmgHoleSize + _DmgTintSize)
			        {
			            result = _DmgColor;
			        }
			        else 
			        {
			            discard;
                    }
                }
                else
                {                
                    result = defaultFrag(i);
                }
				return result;
			}
			ENDCG
		}
	}
}
