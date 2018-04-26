Shader "MMO/RenderTexture/Blur"
{
    Properties
    {
    
        _BlurAmount ("BlurAmount", float) = 0.0005
        _Tex("InputTex", 2D) = "white" {}
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
            
            sampler2D   _Tex;
            uniform float _BlurAmount;
            
            float4 frag(v2f_customrendertexture IN) : COLOR
            {
                float4 texcol = float4(0, 0, 0, 0);
                float remaining = 1.0f;
                float coef = 1.0;
                float fI  =0;
                for (int j = 0; j < 3; j++) 
                {
                    fI++;
                    coef*=0.32;
                    texcol += tex2D(_Tex, float2(IN.localTexcoord.x, IN.localTexcoord.y - fI * _BlurAmount)) * coef;
                    texcol += tex2D(_Tex, float2(IN.localTexcoord.x - fI * _BlurAmount, IN.localTexcoord.y)) * coef;
                    texcol += tex2D(_Tex, float2(IN.localTexcoord.x + fI * _BlurAmount, IN.localTexcoord.y)) * coef;
                    texcol += tex2D(_Tex, float2(IN.localTexcoord.x, IN.localTexcoord.y + fI * _BlurAmount)) * coef;
                    
                    remaining-=4*coef;
                }
                texcol += tex2D(_Tex, float2(IN.localTexcoord.x, IN.localTexcoord.y)) * remaining;
                return texcol;
            }
            ENDCG
        }
    }
}