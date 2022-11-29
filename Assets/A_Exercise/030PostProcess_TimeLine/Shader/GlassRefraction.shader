Shader "LLY/GlassRefraction"
{
    Properties
    {
        _MainTex("MainTex",2D)="white"{}
        _BaseColor("BaseColor",Color)=(1,1,1,1)

        _NormalTex("Normal",2D)="bump"{}
        // [KeywordEnum(WS_N,TS_N)]_NORMAL_STAGE("NormalStage",float)=1
        _NormalScale("NormalScale",Range(-5,5))=1

        _ReflectCube3D("CubeMap",Cube)="_Skybox"{}
        _Amount("OffsetAmount",Range(0,100))=10
        _ReflectIntensity("_ReflectIntensity",Range(0,1))=0
        _RefractIntensity("_RefractIntensity",Range(0,1))=0

    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
       
        HLSLINCLUDE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _BaseColor;
                float _NormalScale;
                float _ReflectIntensity;
                float _RefractIntensity;
                float _Amount;
            CBUFFER_END

            float4 _CameraColorTexture_TexelSize;
            //非本shader独有，不能放在常量缓冲区
            SAMPLER(_CameraColorTexture);

            samplerCUBE _ReflectCube3D;

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_NormalTex);
            SAMPLER(sampler_NormalTex);

        ENDHLSL

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature _NORMAL_STAGE_WS_N 
            
            struct a2v
            {
                float4 vertex:POSITION;
                float2 uv:TEXCOORD0;
                float3 normal:NORMAL;
                float4 tangent:TANGENT;
            };

            struct v2f
            {
             float4 pos:SV_POSITION;
             float2 uv:TEXCOORD0;
             float4 T1:TEXCOORD1;
             float4 T2:TEXCOORD2;
             float4 T3:TEXCOORD3;  
            };

            v2f vert (a2v v)
            {
                v2f o;
                o.pos=TransformObjectToHClip(v.vertex.xyz);
                o.uv=TRANSFORM_TEX(v.uv,_MainTex);

                float3 wp=TransformObjectToWorld(v.vertex.xyz);
                float3 wn=TransformObjectToWorldNormal(v.normal);
                float3 wt=TransformObjectToWorldDir(v.tangent.xyz);
                float3 wb=cross(wn,wt)*v.tangent.w*unity_WorldTransformParams.w;

                o.T1=float4(wt.x,wb.x,wn.x,wp.x);
                o.T2=float4(wt.y,wb.y,wn.y,wp.y);
                o.T3=float4(wt.z,wb.z,wn.z,wp.z);

                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                Light light=GetMainLight();
                float3 worldPos=float3(i.T1.w,i.T2.w,i.T3.w);

                half4 aldobe=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv) * _BaseColor;
                float3 bump=UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalTex,sampler_NormalTex,i.uv),_NormalScale);

                //得到屏幕UV
                float2 screenUV=i.pos.xy/_ScreenParams.xy;

                //仿折射
                float2 offset=bump.xy*_Amount*_CameraColorTexture_TexelSize.xy;
                //half4 refractColor=tex2D(_CameraColorTexture,screenUV+offset);
                half4 refractColor=half4(SampleSceneColor(screenUV+offset),1);
                // return refractColor;

                // #ifdef _NORMAL_STAGE_WS_N
                bump=normalize(float3(dot(i.T1.xyz,bump),dot(i.T2.xyz,bump),dot(i.T3.xyz,bump)));
                bump.z=sqrt(1-saturate(dot(bump.xy,bump.xy)));
                // #endif
                
                float3 V=normalize(_WorldSpaceCameraPos-worldPos);
                float3 L=normalize(light.direction);
                float3 H=normalize(L+V);
                float3 N=normalize(bump);

                //反射
                half3 reflec=reflect(-V,N);
                half4 reflectColor=texCUBE(_ReflectCube3D,reflec) ;

                float4 finalColor=lerp(aldobe,reflectColor,_ReflectIntensity);
                finalColor=lerp(finalColor,refractColor,_RefractIntensity);

                return half4(finalColor.rgb,1);
            }

            ENDHLSL
        }
    }
}
