Shader "LLY/Water01"
{
    Properties
    {
        _mainTex ("MainTex", 2D) = "white" {}
        _reflectTex("planeReflect",2D)="white"{}
        _factorParam("Factor",Range(0,1))=0
        _mainColor("maincolor",Color)=(1,1,1,1)

    }
    SubShader
    {
        Pass
        {
         Tags {"LightMode"="UniversalForward"}

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
           
            struct a2v
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float2 uv_water : TEXCOORD1;
                float4 pos : SV_POSITION;
            };

            TEXTURE2D(_mainTex);
            SAMPLER(sampler_mainTex);

            TEXTURE2D(_reflectTex);
            SAMPLER(sampler_reflectTex);

            CBUFFER_START(UnityPerMaterial)

                half4 _mainColor;
                half4 _mainTex_ST;
                half4 _reflectTex_ST;
                float _factorParam;
            CBUFFER_END

            v2f vert (a2v v)
            {
                v2f o = (v2f) 0;

                o.pos = TransformObjectToHClip(v.vertex.xyz);

                float3 worldPos = TransformObjectToWorld(v.vertex.xyz);
               
                o.uv =TRANSFORM_TEX(v.uv,_mainTex);
                o.uv_water =TRANSFORM_TEX(v.uv,_reflectTex);
                o.uv_water.x=1-o.uv_water.x;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half3 waterColor = SAMPLE_TEXTURE2D(_mainTex , sampler_mainTex , i.uv).rgb *_mainColor.rbg;
                half3 reflectColor = SAMPLE_TEXTURE2D(_reflectTex , sampler_reflectTex , i.uv_water).rgb;

                half3 finalColor = lerp(waterColor,reflectColor,_factorParam);
                return half4(finalColor,1);
            }
            ENDHLSL
        }
    }
}
