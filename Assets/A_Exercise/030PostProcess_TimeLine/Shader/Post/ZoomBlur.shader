Shader "LLY/PostEffect/ZoomBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {

        Tags { "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite Off 
        ZTest Always
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
           
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            // CBUFFER_START(UnityPerMaterial)
                float _FocusPower;
                int _FocusDetail;
                int _ReferenceResolutionX;
                float2 _FocusScreenPosition;
            // CBUFFER_END
            
            TEXTURE2D( _MainTex);

            SAMPLER(sampler_MainTex);
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                float2 screenPoint=_FocusScreenPosition + _ScreenParams.xy/2;
                float2 uv=i.uv;
                float2 mousePos=screenPoint.xy/_ScreenParams.xy;
                float2 focus=uv-mousePos;
                half aspectX=_ScreenParams.x/_ReferenceResolutionX;
                half4 color=float4(0,0,0,1);

                for(int j=0;j<_FocusDetail;++j)
                {
                    float power=1-_FocusPower*(1/_ScreenParams.x*aspectX) * j;
                    color.rgb+=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,focus*power+mousePos).rgb;    
                } 
                color.rgb/=_FocusDetail;
                return color;
            }
            ENDHLSL
        }
    }
}
