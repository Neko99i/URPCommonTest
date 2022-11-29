Shader "LLY/RadialBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurScale("BlurScale",float)=1
        _Iteration("Iteration",int)=4
        _CenterOffset("CenterOffset",vector)=(0.5,0.5,0,0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline"}
        Cull Off 
        ZTest Always
        ZWrite Off

        Pass
        {
            HLSLPROGRAM

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #pragma vertex vert
            #pragma fragment frag

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)

            float4 _MainTex_TexelSize;
            float4 _CenterOffset;
            float _BlurScale;
            int _Iteration;
            CBUFFER_END
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = TransformObjectToHClip(v.vertex.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                float2 uv=i.uv-_CenterOffset.xy;
                
                half3 col=half3(0,0,0);
                
                float offset=1;

                _BlurScale*=_MainTex_TexelSize;

                for(int j=0;j<_Iteration;j++)
                {
                    offset += j * _BlurScale;

                    col +=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv*offset +_CenterOffset.xy);
                }

               return half4(col/_Iteration,1)  ;
            }
            ENDHLSL
        }
    }
}
