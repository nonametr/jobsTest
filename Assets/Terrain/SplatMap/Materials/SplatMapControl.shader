Shader "MMO/RenderTexture/SplatMapControl"
{
    Properties
    {
        _HeightMap("HeightMap", 2D) = "white" {}
        _RenderArea("RenderArea", Vector) = (0, 0, 1, 1)
    }

     SubShader
     {
        Lighting Off
        Blend One Zero      
        
        Pass
        {
            CGPROGRAM
            #include "MMO_Mobile.cginc"

            #pragma vertex CustomRenderTextureVertexShader
            #pragma fragment frag
            #pragma target 3.0
            
            static const int max_splat_count = 15;
			int active_splat_count = 0;
			float start_lvl[max_splat_count];			
			float end_lvl[max_splat_count];
			float channel_lvl[max_splat_count];
			float start_transition_lvl[max_splat_count];
			float end_transition_lvl[max_splat_count];
			          
            sampler2D   _HeightMap;
            float4      _RenderArea;
            
            float4 frag(v2f_customrendertexture IN) : COLOR
            {
                float2 uv = _RenderArea.xy + IN.localTexcoord * (_RenderArea.zw - _RenderArea.xy);
                float4 result = float4(0, 0, 0, 0);
				float height = tex2D(_HeightMap, uv).r;
				for(int i = 0; i < active_splat_count; ++i)
				{
					if(height > start_lvl[i] && height < end_lvl[i])
					{
						float rangle = end_lvl[i] - start_lvl[i];
						float startTransitionEndPoint = start_lvl[i] + start_transition_lvl[i] * rangle;
						float endTransitionStartPoint = end_lvl[i] - rangle * end_transition_lvl[i];

						float value = 1.0f;
						if(height < startTransitionEndPoint)
						{
							value = lerp(0.0f, 1.0f, (height - start_lvl[i]) / (startTransitionEndPoint - start_lvl[i]));
						}
						if(height > endTransitionStartPoint)
						{
							value = lerp(1.0f, 0.0f, (height - endTransitionStartPoint) / (end_lvl[i] - endTransitionStartPoint));
						}

						if(channel_lvl[i] < 1)
						{
							result.r = value;
						}
						else if(channel_lvl[i] < 2)
						{
							result.g = value;
						}
						else if(channel_lvl[i] < 3)
						{
							result.b = value;
						}
						else if(channel_lvl[i] < 4)
						{
							result.a = value;
						}
					}
				}
                return result;
            }
            ENDCG
        }
    }
}
