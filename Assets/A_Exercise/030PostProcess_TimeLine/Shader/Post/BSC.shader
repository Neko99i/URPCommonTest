Shader "LLY/BSC"
{
     Properties
    {
       [HideInInspector]_MainTex ("Texture", 2D) = "white" {}
        _Brightness("Brightness",Range(0,5))=1
        _Saturation("Saturation",Range(0,5))=1
        _Contrast("Contrast",Range(0,5))=1
    }
    SubShader
    {

        Pass
        {
            Name "BSC"
            Tags{"RenderPipeline"="UniversalPipeline"}

            Cull Off 
            ZTest Always
            ZWrite Off 
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)

            float _Brightness;

            float _Saturation;

            float _Contrast;

            CBUFFER_END

            TEXTURE2D( _MainTex);

            SAMPLER(sampler_MainTex);

            struct a2v
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
            };

            v2f vert (a2v v)
            {
                v2f o;
                o.pos = TransformObjectToHClip(v.vertex.xyz);
                o.uv=v.uv;
                return o;
            }

            float MyLuminance(half3 color)
            {
                return 0.21*color.x+0.72*color.y+0.072*color.z;
            }

            half4 frag (v2f i) : SV_Target
            {
                
                half4 col = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);

                half3 grayColor=MyLuminance(col.rgb);

                //饱和度
                half3 finalColor=lerp(grayColor,col.rgb,_Saturation);

                //明度
                finalColor= finalColor * _Brightness;

                //对比度
                finalColor=lerp(half3(0.5,0.5,0.5),finalColor,_Contrast);

                return half4(finalColor,col.a);
            }
            ENDHLSL
        }
    }
}
