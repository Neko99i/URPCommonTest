Shader "LLY/KawaseBlur"
{
    Properties
    {
        _MainTex("MainTex",2D)="white"{}
       _Blur("Blur",float)=1
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
                float _Blur;
                float4 _MainTex_TexelSize;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct a2v
            {
                float2 texcoord:TEXCOORD0;
                float4 vertex:POSITION;
            };

            struct v2f 
            {
                float2 uv:TEXCOORD0;
                float4 pos:SV_POSITION;
            };

            v2f vert(a2v i)
            {
                v2f o;
                o.uv=i.texcoord;
                o.pos=TransformObjectToHClip(i.vertex.xyz);
                return o;
            }

            half4 frag(v2f i):SV_TARGET
            {
                //2x2的均值模糊
               float2 blurScale=_Blur*_MainTex_TexelSize.xy;

                half4 texColor=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);

                texColor+=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv+float2(-1,1)*blurScale);
                texColor+=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv+float2(1,1)*blurScale);
                texColor+=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv+float2(1,-1)*blurScale);
                texColor+=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv+float2(-1,-1)*blurScale);

                return texColor*0.2;
            }
        ENDHLSL

        pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }
    }
}
