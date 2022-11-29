Shader "LLY/Shield_Depth"
{
    Properties
    {
        _MainColor("MainColor",Color)=(1,1,1,1)
        _Alpha("Alpha",Range(0,1))=1
        _DepthOffset("_DepthOffset",float)=1
    }
    SubShader
    {   
        Tags { "RenderType"="Opaque" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
       
        HLSLINCLUDE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _MainColor;
                float _Alpha;
                float _DepthOffset;
            CBUFFER_END

            half4 _CameraDepthTexture_TexelSize;
            SAMPLER(_CameraDepthTexture);

        ENDHLSL
        Pass
        {
           Tags{ "LightMode"="UniversalForward"}
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal:NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 sspos:TEXCOORD1;
                float3 normal:TEXCOORD2;
                float3 wp:TEXCOORD3;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = TransformObjectToHClip(v.vertex.xyz);
                o.uv=v.uv;

                //屏幕坐标sspos，xy保存为未透除的屏幕uv，zw不变

                o.sspos.xy=o.pos.xy*0.5+0.5*float2(o.pos.w,o.pos.w);

                o.sspos.zw=o.pos.zw;

                o.wp=TransformObjectToWorld(v.vertex.xyz);

                o.normal=normalize(TransformObjectToWorldNormal(v.normal.xyz));
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                float2 screenUV=i.pos.xy/_ScreenParams.xy;
                // #ifdef UNITY_UV_STARTS_AT_TOP
                //     screenUV.y=1 - screenUV.y;
                // #endif
                float depth=Linear01Depth( tex2D(_CameraDepthTexture,screenUV),_ZBufferParams);
                float objDepth=i.pos.z;
                objDepth=Linear01Depth(objDepth,_ZBufferParams);
                float edge=saturate(objDepth-depth+0.005)*100*_DepthOffset;

                half4 finalColor=half4(_MainColor.rgb,_Alpha);
                finalColor.rgb+=_MainColor*edge;

                return finalColor;
            }
            ENDHLSL
        }
    }
}
