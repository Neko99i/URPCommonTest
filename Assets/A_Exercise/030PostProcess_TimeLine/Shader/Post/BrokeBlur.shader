Shader "LLY/BrokeBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent"}

        HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_TexelSize;

            CBUFFER_END

                //没有申明在shader的属性中的变量不能放在Cbuffer中会打断SRPBatcher
                half _NearDis;
                half _FarDis;
                float _BlurSmoothness;
                int _Iteration;
                float _Radius;

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_SourTex);

            SAMPLER(sampler_SourTex);

            SAMPLER(_CameraDepthTexture);

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

            v2f vert(appdata v)
            {
                v2f o;
                o.uv=v.uv;
                o.pos=TransformObjectToHClip(v.vertex.xyz);
                return o;
            }

        ENDHLSL     

        Pass
        {
           //Blur
           HLSLPROGRAM
            #pragma vertex vert 
            #pragma fragment frag

            half4 frag(v2f i):SV_TARGET
            {
                float a=2.3398;
                float2x2 RM=float2x2(cos(a),-sin(a),sin(a),cos(a));
                
                float2 UVpos=float2(_Radius,0);
                float2 uv;
                float r;
                half4 tex=0;

                for(int j=1;j<_Iteration;j++)
                {
                    r=sqrt(j);
                    UVpos=mul(RM,UVpos);
                    uv=i.uv+_MainTex_TexelSize.xy*UVpos*r;
                    tex+=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv);
                }

                return tex/(_Iteration-1);
            }
           ENDHLSL
        }

        pass
        {
            //景深模糊
          HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            half4 frag(v2f i):SV_TARGET
            {
                float depth=Linear01Depth(tex2D(_CameraDepthTexture,i.uv).x,_ZBufferParams).x;
                
                half4 blur=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);
                half4 sour=SAMPLE_TEXTURE2D(_SourTex,sampler_SourTex,i.uv);

                //_ProjectionParams.w 相当于1/far(远裁剪面距离)
                _NearDis*=_ProjectionParams.w;
                _FarDis*=_ProjectionParams.w;
                _BlurSmoothness*=_ProjectionParams.w;

                //从近处和远处向中间靠拢
                float dis=1-smoothstep(_NearDis,saturate(_NearDis+_BlurSmoothness),depth);//计算近处的
                dis+=smoothstep(_FarDis,saturate(_FarDis+_BlurSmoothness),depth);//计算近处的

                // return dis;

                return lerp(sour,blur,dis);
            }
          ENDHLSL

        } 
    }
}
