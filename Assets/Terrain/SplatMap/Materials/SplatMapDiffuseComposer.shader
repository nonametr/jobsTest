Shader "MMO/RenderTexture/SplatMapDiffuseComposer"
{
    Properties
    {
        _ControlMap0("_ControlMap0", 2D) = "black" {}
        _ControlMap1("_ControlMap1", 2D) = "black" {}
        _ControlMap2("_ControlMap2", 2D) = "black" {}
        
        _Texture0("Texture0", 2D) = "black" {}
        _Texture1("Texture1", 2D) = "black" {}
        _Texture2("Texture2", 2D) = "black" {}
        _Texture3("Texture3", 2D) = "black" {}
        _Texture4("Texture4", 2D) = "black" {}
        _Texture5("Texture5", 2D) = "black" {}
        _Texture6("Texture6", 2D) = "black" {}
        _Texture7("Texture7", 2D) = "black" {}
        _Texture8("Texture8", 2D) = "black" {}
        _Texture9("Texture9", 2D) = "black" {}
        _Texture10("Texture10", 2D) = "black" {}
        _Texture11("Texture11", 2D) = "black" {}
        
        _TilingOffset("TilingOffset", Vector) = (0, 0, 1, 1)
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
            
            float4 _TilingOffset;
            			                       			                               			
            sampler2D _ControlMap0;
			float4 _ControlMap0_ST;			
            sampler2D _ControlMap1;
			float4 _ControlMap1_ST;			
            sampler2D _ControlMap2;
			float4 _ControlMap2_ST;
			
            sampler2D _Texture0;
			float4 _Texture0_ST;			
            sampler2D _Texture1;
			float4 _Texture1_ST;			
            sampler2D _Texture2;
			float4 _Texture2_ST;			
            sampler2D _Texture3;
			float4 _Texture3_ST;			
            sampler2D _Texture4;
			float4 _Texture4_ST;
            sampler2D _Texture5;
			float4 _Texture5_ST;
            sampler2D _Texture6;
			float4 _Texture6_ST;
            sampler2D _Texture7;
			float4 _Texture7_ST;
            sampler2D _Texture8;
			float4 _Texture8_ST;
			sampler2D _Texture9;
			float4 _Texture9_ST;
			sampler2D _Texture10;
			float4 _Texture10_ST;
			sampler2D _Texture11;
			float4 _Texture11_ST;			
			
            float4 frag(v2f_customrendertexture IN) : COLOR
            {                           
                float4 ctrl_0 = tex2D(_ControlMap0, IN.localTexcoord);
                float4 ctrl_1 = tex2D(_ControlMap1, IN.localTexcoord);
                float4 ctrl_2 = tex2D(_ControlMap2, IN.localTexcoord);
                
                float4 tex_0 = tex2D(_Texture0, IN.localTexcoord * _Texture0_ST.xy + _Texture0_ST.wz + _TilingOffset.zw);
                float4 tex_1 = tex2D(_Texture1, IN.localTexcoord * _Texture1_ST.xy + _Texture1_ST.wz + _TilingOffset.zw);
                float4 tex_2 = tex2D(_Texture2, IN.localTexcoord * _Texture2_ST.xy + _Texture2_ST.wz + _TilingOffset.zw);
                float4 tex_3 = tex2D(_Texture3, IN.localTexcoord * _Texture3_ST.xy + _Texture3_ST.wz + _TilingOffset.zw);
                float4 tex_4 = tex2D(_Texture4, IN.localTexcoord * _Texture4_ST.xy + _Texture4_ST.wz + _TilingOffset.zw);
                float4 tex_5 = tex2D(_Texture5, IN.localTexcoord * _Texture5_ST.xy + _Texture5_ST.wz + _TilingOffset.zw);
                float4 tex_6 = tex2D(_Texture6, IN.localTexcoord * _Texture6_ST.xy + _Texture6_ST.wz + _TilingOffset.zw);
                float4 tex_7 = tex2D(_Texture7, IN.localTexcoord * _Texture7_ST.xy + _Texture7_ST.wz + _TilingOffset.zw);
                float4 tex_8 = tex2D(_Texture8, IN.localTexcoord * _Texture8_ST.xy + _Texture8_ST.wz + _TilingOffset.zw);
                float4 tex_9 = tex2D(_Texture9, IN.localTexcoord * _Texture9_ST.xy + _Texture9_ST.wz + _TilingOffset.zw);
                float4 tex_10 = tex2D(_Texture10, IN.localTexcoord * _Texture10_ST.xy + _Texture10_ST.wz + _TilingOffset.zw);
                float4 tex_11 = tex2D(_Texture11, IN.localTexcoord * _Texture11_ST.xy + _Texture11_ST.wz + _TilingOffset.zw);
                
                float4 ctrl_0_result = ctrl_0.x * tex_0 + ctrl_0.y * tex_1 + ctrl_0.z * tex_2 + ctrl_0.a * tex_3;
                float4 ctrl_1_result = ctrl_1.x * tex_4 + ctrl_1.y * tex_5 + ctrl_1.z * tex_6 + ctrl_1.a * tex_7;
                float4 ctrl_2_result = ctrl_2.x * tex_8 + ctrl_2.y * tex_9 + ctrl_2.z * tex_10 + ctrl_2.a * tex_11;
                
                return ctrl_0_result + ctrl_1_result + ctrl_2_result;
            }
            ENDCG
        }
    }
}
