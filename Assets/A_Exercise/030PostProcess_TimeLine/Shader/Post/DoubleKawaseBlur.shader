Shader "LLY/DoubleKawaseBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        // _BlurSize("BlurSize",float)=1
        
    }
    SubShader
    {
        Tags{"RenderPipeline"="UniversalPipeline"}

        Cull Off 
        ZWrite Off 
        ZTest Always

        HLSLINCLUDE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_TexelSize;
            CBUFFER_END
                float _Blur;

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct a2v 
            {
                float2 texcoord:TEXCOORD0;
                float4 vertex:POSITION;
            };

            struct v2f_Down 
            {
                float2 uv:TEXCOORD0;
                float4 pos:SV_POSITION;
            };

            v2f_Down vert_Down(a2v i)
            {
                v2f_Down o;
                o.uv=i.texcoord;
                o.pos=TransformObjectToHClip(i.vertex.xyz);
                return o;
            }

            half4 frag_Down(v2f_Down i):SV_TARGET
            {
               float2 blurScale=_Blur*_MainTex_TexelSize.xy *0.5;

                half4 texColor=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv) *0.5;

                texColor+=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv+float2(-1,1)*blurScale)*0.125;
                texColor+=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv+float2(1,1)*blurScale)*0.125;
                texColor+=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv+float2(1,-1)*blurScale)*0.125;
                texColor+=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv+float2(-1,-1)*blurScale)*0.125;

                return texColor;
            }


            struct v2f_Up 
            {
                float2 uv[8]:TEXCOORD0;
                float4 pos:SV_POSITION;
            };

            v2f_Up vert_Up(a2v i)
            {
                v2f_Up o;
                o.pos=TransformObjectToHClip(i.vertex.xyz);
                float2 blurScale=_Blur*_MainTex_TexelSize.xy*0.5;

                o.uv[0]=i.texcoord +float2(1,1)*blurScale;
                o.uv[1]=i.texcoord+float2(1,-1)*blurScale;
                o.uv[2]=i.texcoord+float2(-1,1)*blurScale;
                o.uv[3]=i.texcoord+float2(-1,-1)*blurScale;

                o.uv[4]=i.texcoord+float2(0,2)*blurScale;
                o.uv[5]=i.texcoord+float2(0,-2)*blurScale;
                o.uv[6]=i.texcoord+float2(-2,0)*blurScale;
                o.uv[7]=i.texcoord+float2(2,0)*blurScale;

                return o;
            }

            half4 frag_Up(v2f_Up i):SV_TARGET
            {
               float2 blurScale=_Blur*_MainTex_TexelSize.xy;

                half4 texColor=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv[0])/6;
                texColor+=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv[1])/6;
                texColor+=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv[2])/6;
                texColor+=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv[3])/6;

                texColor+=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv[4])/12;
                texColor+=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv[5])/12;
                texColor+=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv[6])/12;
                texColor+=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv[7])/12;
                
                return texColor;
            }


        ENDHLSL


        Pass
        {
            Name "Dual_Down"
            HLSLPROGRAM
                #pragma vertex vert_Down
                #pragma fragment frag_Down
            ENDHLSL
        }

        Pass
        {
            Name "Dual_Up"
            HLSLPROGRAM
                #pragma vertex vert_Up
                #pragma fragment frag_Up
            ENDHLSL
        }
    }
}
