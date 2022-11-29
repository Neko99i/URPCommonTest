Shader "LLY/GlitchBlit"
{
    Properties
    {
        [HideInInspector]_MainTex("MainTex",2D)="white"{}

        [HideInInspector]_Instensity("Instensity",Range(0,1))=0.5
    }
    SubShader
    {
        Tags{ "RenderPipeline"="UniversalRenderPipeline"}

        Cull Off ZWrite Off ZTest Always
        HLSLINCLUDE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)

            float4 _MainTex_TexelSize;

            float _Instensity;

            CBUFFER_END

            TEXTURE2D( _MainTex);

            SAMPLER(sampler_MainTex);

            struct a2v
            {
             float4 vertex:POSITION;
             float2 texcoord:TEXCOORD;
            };

            struct v2f
            {
             float4 pos:SV_POSITION;
             float2 uv:TEXCOORD;
            };

        ENDHLSL

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            v2f vert (a2v v)
            {
                v2f o;

                VertexPositionInputs vi=GetVertexPositionInputs(v.vertex.xyz);
                o.pos=vi.positionCS;
                o.uv = v.texcoord;
                return o;
            }

            float cur(float x)
            {
                return -4.71*pow(x,3) +6.8 *pow(x,2) -2.65*x +0.13+0.8*sin(48.15*x -0.38);
            }

            half4 frag (v2f i) : SV_Target
            {
                float noise=cur(frac(_Time.y));
                half R=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv +float2(0.01,0.01)*noise).r;
                half G=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv ).g;
                half B=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv -float2(0.01,0.01)*noise).b;

                half4 mainTEX=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);

                float2 center=i.uv*2-1;
                float mask=saturate(dot(center,center));
                mask=lerp(0,mask,_Instensity);

                half4 splitRGB=half4(R,G,B,1);

                splitRGB=lerp(mainTEX,splitRGB,mask);

                return splitRGB;
            }
            ENDHLSL
        }
    }
}
