Shader "MMO/RenderTexture/NHMRT"
{
    Properties
    {
        _NormalStrongness ("NormalStrongness", float) = 0.15
        _SobelDelta ("Delta", float) = 0.00333333        
        _RenderArea("RenderArea", Vector) = (0, 0, 1, 1)
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

            float4      _RenderArea;
            sampler2D   _Tex;
            uniform float4 _Tex_ST;
            uniform float _SobelDelta;
            uniform float _NormalStrongness;
            
            static const float2 delta = float2(_SobelDelta, _SobelDelta);
            
            float3 sobel(float2 uv)
            {                
                float dx = 0;
                float dy = 0;
                
                float domain[9];
                /*
                float domain[3][3] = 
                {
                    { tex2D(_Tex, (uv + float2(-1.0, -1.0) * delta)).r,     tex2D(_Tex, (uv + float2(0.0, -1.0) * delta)).r,    tex2D(_Tex, (uv + float2(1.0, -1.0) * delta)).r }, 
                    { tex2D(_Tex, (uv + float2(-1.0, 0.0) * delta)).r,      tex2D(_Tex, (uv + float2(0.0, 0.0) * delta)).r,     tex2D(_Tex, (uv + float2(1.0, 0.0) * delta)).r },
                    { tex2D(_Tex, (uv + float2(-1.0, 1.0) * delta)).r,      tex2D(_Tex, (uv + float2(0.0, 1.0) * delta)).r,     tex2D(_Tex, (uv + float2(1.0, 1.0) * delta)).r }
                };
                */
                
                domain[0 * 3 + 0] = tex2D(_Tex, (uv + float2(-1.0, -1.0) * delta)).r;
                domain[0 * 3 + 1] = tex2D(_Tex, (uv + float2(0.0, -1.0) * delta)).r;
                domain[0 * 3 + 2] = tex2D(_Tex, (uv + float2(1.0, -1.0) * delta)).r;
                
                domain[1 * 3 + 0] = tex2D(_Tex, (uv + float2(-1.0, 0.0) * delta)).r;
                domain[1 * 3 + 1] = tex2D(_Tex, (uv + float2(0.0, 0.0) * delta)).r;
                domain[1 * 3 + 2] = tex2D(_Tex, (uv + float2(1.0, 0.0) * delta)).r;
                
                domain[2 * 3 + 0] = tex2D(_Tex, (uv + float2(-1.0, 1.0) * delta)).r;
                domain[2 * 3 + 1] = tex2D(_Tex, (uv + float2(0.0, 1.0) * delta)).r;
                domain[2 * 3 + 2] = tex2D(_Tex, (uv + float2(1.0, 1.0) * delta)).r;
                
                
                
                dx += domain[0 * 3 + 0] *  1.0;
                //dx += domain[0][1] *  0.0;
                dx += domain[0 * 3 + 2] * -1.0;
                dx += domain[1 * 3 + 0] *  2.0;
                //dx += domain[1][1] *  0.0;
                dx += domain[1 * 3 + 2] * -2.0;
                dx += domain[2 * 3 + 0] *  1.0;
                //dx += domain[2][1] *  0.0;
                dx += domain[2 * 3 + 2] * -1.0;
                
                dy += domain[0 * 3 + 0] *  1.0;
                dy += domain[0 * 3 + 1] *  2.0;
                dy += domain[0 * 3 + 2] *  1.0;
                //dy += domain[1][0] *  0.0;
                //dy += domain[1][1] *  0.0;
                //dy += domain[1][2] *  0.0;
                dy += domain[2 * 3 + 0] * -1.0;
                dy += domain[2 * 3 + 1] * -2.0;
                dy += domain[2 * 3 + 2] * -1.0;
                
                return float3(dx, dy, domain[1 * 3 + 1]);
            }
            
            float4 frag(v2f_customrendertexture IN) : COLOR
            {
                float2 uv = _RenderArea.xy + IN.localTexcoord * (_RenderArea.zw - _RenderArea.xy);
                 
                float3 sobelVal = sobel(uv);
                float3 n = normalize(float3(sobelVal.x, _NormalStrongness, sobelVal.y));
                n.x = n.x * 0.5f + 0.5f;
                n.z = n.z * 0.5f + 0.5f;
                
                return float4(n, sobelVal.z);
            }
            ENDCG
        }
    }
}