Shader "LLY/Bloom_DualBlur"
{
    Properties
    {
        [HideInInspector]_MainTex("MainTex",2D)="white"{}

        _SoildColor("SoildColor",Color)=(1,1,1,1)

        [HideInInspector]_Blur("Blur",float)=1
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalRenderPipeline"
        }

        Cull Off ZWrite Off ZTest Always

        HLSLINCLUDE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)

            float4 _MainTex_TexelSize;

            real4 _SoildColor;

            float _Blur;

            CBUFFER_END

            TEXTURE2D(_MainTex);

            SAMPLER(sampler_MainTex);

            TEXTURE2D(_BlurTex);

            SAMPLER(sampler_BlurTex);

            TEXTURE2D(_SourTex);

            SAMPLER(sampler_SourTex);   

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 uv[4] : TEXCOORD0;
                float4 pos : SV_POSITION;
            };

        ENDHLSL
        
        //Bloom颜色
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = TransformObjectToHClip(v.vertex.xyz);
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                return _SoildColor;
            }
            ENDHLSL
        }

        //Down
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            v2f vert (appdata i)
            {
                v2f o;
                o.pos = TransformObjectToHClip(i.vertex.xyz);
                
                o.uv[2].xy=i.uv;

                o.uv[0].xy=i.uv+float2(1,1)*_MainTex_TexelSize.xy*(1+_Blur)*0.5;

                o.uv[0].zw=i.uv+float2(-1,1)*_MainTex_TexelSize.xy*(1+_Blur)*0.5;

                o.uv[1].xy=i.uv+float2(1,-1)*_MainTex_TexelSize.xy*(1+_Blur)*0.5;

                o.uv[1].zw=i.uv+float2(-1,-1)*_MainTex_TexelSize.xy*(1+_Blur)*0.5;

                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half4 tex=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv[2].xy);

                tex+=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv[0].xy)*0.125;
                tex+=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv[0].zw)*0.125;
                tex+=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv[1].xy)*0.125;
                tex+=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv[1].zw)*0.125;
                
                return tex;
            }
            ENDHLSL
        }

        //Up
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            v2f vert (appdata i)
            {
                v2f o;
                o.pos = TransformObjectToHClip(i.vertex.xyz);
                
                o.uv[0].xy=i.uv+float2(1,1)*_MainTex_TexelSize.xy*(1+_Blur)*0.5;
                o.uv[0].zw=i.uv+float2(-1,1)*_MainTex_TexelSize.xy*(1+_Blur)*0.5;
                o.uv[1].xy=i.uv+float2(1,-1)*_MainTex_TexelSize.xy*(1+_Blur)*0.5;
                o.uv[1].zw=i.uv+float2(-1,-1)*_MainTex_TexelSize.xy*(1+_Blur)*0.5;
                o.uv[2].zw=i.uv+float2(-2,0)*_MainTex_TexelSize.xy*(1+_Blur)*0.5;
                o.uv[2].zw=i.uv+float2(0,-2)*_MainTex_TexelSize.xy*(1+_Blur)*0.5;
                o.uv[3].zw=i.uv+float2(0,2)*_MainTex_TexelSize.xy*(1+_Blur)*0.5;
                o.uv[3].zw=i.uv+float2(2,0)*_MainTex_TexelSize.xy*(1+_Blur)*0.5;

                return o;
            }

            half4 frag (v2f i) : SV_Target
            {

                half4  tex=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv[0].xy)/6;
                tex+=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv[0].zw)/6;
                tex+=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv[1].xy)/6;
                tex+=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv[1].zw)/6;

                tex+=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv[2].xy)/12;
                tex+=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv[2].zw)/12;
                tex+=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv[3].xy)/12;
                tex+=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv[3].zw)/12;
                
                return tex;
            }
            ENDHLSL
        }

        pass
        {
            HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag

                #pragma multi_compile_local _INCOLOR_ON _INCOLOR_OFF
                v2f vert(appdata v)
                {
                    v2f o;
                    o.pos=TransformObjectToHClip(v.vertex.xyz);
                    o.uv[0].xy=v.uv;
                    return o;
                }

                half4 frag(v2f i):SV_TARGET
                {
                    real4 blur=SAMPLE_TEXTURE2D(_BlurTex,sampler_BlurTex,i.uv[0].xy);

                    real4 sour=SAMPLE_TEXTURE2D(_SourTex,sampler_SourTex,i.uv[0].xy);

                    real4 soild=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv[0].xy);

                    real4 color=saturate(blur-soild)+sour;

                    #ifdef _INCOLOR_ON
                        color = abs(blur-soild)+sour;
                    #endif

                    return color;
                }
            ENDHLSL
        }
    }
}
