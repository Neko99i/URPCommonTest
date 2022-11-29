Shader "LLY/Shadow_Multi_Light"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MainColor("MainColor",Color)=(1,1,1,1)
        _BaseColor("BaseColor",Color)=(1,1,1,1)
        [HDR]_SpecularColor("SpeColor",Color)=(1,1,1,1)
        _Gloss("Gloss",Range(8,100))=8
        [KeywordEnum(OFF,ON)]_AddLight("AddLight",float)=0
        [KeywordEnum(OFF,ON)]_AlphaCut("AlphaCut",float)=1
        _Cutoff("Cutoff",Range(0,1))=0
    }

    SubShader
    {
        Tags{"RenderPipeline"="UniversialPipeline"}

        HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"


            #pragma shader_feature_local _ALPHACUT_ON 
            #pragma shader_feature_local _ADDLIGHT_ON 

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT

            CBUFFER_START(UnityPerMaterial)
            
            float4 _MainTex_ST;
            half4 _MainColor;
            half4 _BaseColor;
            half4 _SpecularColor;
            half _Gloss;
            half _Cutoff;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

        ENDHLSL

        Tags{"RenderType"="TransparentCutout" "Queue"="AlphaTest"}

        Pass
        {
            Tags{"LightMode"="UniversalForward" }

            Cull Off

            HLSLPROGRAM
            
                #pragma vertex vert
                #pragma fragment frag 

            struct a2v
            {
                float4 vertex:POSITION;
                float3 normal:NORMAL;
                float2 texcoord:TEXCOORD0;
            };

            struct v2f
            {
                float4 pos:SV_POSITION;

                float2 uv:TEXCOORD0;

                #ifdef _MAIN_LIGHT_SHADOWS
                    float4 shadowcoord:TEXCOORD5;
                #endif

                float3 worldNormal:TEXCOORD2;
                float3 worldPos:TEXCOORD3; 

            };
                v2f vert(a2v v)
                {
                    v2f o;
                    o.pos=TransformObjectToHClip(v.vertex.xyz);
                    o.uv=TRANSFORM_TEX(v.texcoord,_MainTex);
                    o.worldNormal=normalize(TransformObjectToWorldNormal(v.normal));
                    o.worldPos=TransformObjectToWorld(v.vertex.xyz);

                    #ifdef _MAIN_LIGHT_SHADOWS
                        o.shadowcoord=TransformWorldToShadowCoord(o.worldPos);
                    #endif

                    return o;
                }

                half4 frag(v2f i):SV_TARGET
                {
                    float3 N=normalize(i.worldNormal);
                    float3 V=normalize(_WorldSpaceCameraPos-i.worldPos);        

                    half4 albedo=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv) * _MainColor;

                    #ifdef _ALPHACUT_ON
                        clip(albedo.a - _Cutoff);
                    #endif

                    //主光源
                    Light mainLight;
                    #ifdef _MAIN_LIGHT_SHADOWS
                        mainLight=GetMainLight(i.shadowcoord);
                    #else
                        mainLight=GetMainLight();
                    #endif

                    float3 L=normalize(mainLight.direction);
                    float3 H=normalize(L+V);

                    half3 mainDiffuse=(dot(L,N) +1) *0.5 *mainLight.color *mainLight.shadowAttenuation;
                    half3 mainSpecular=pow(saturate(dot(N,H)),_Gloss)  * mainLight.color *mainLight.shadowAttenuation *_SpecularColor.rgb;

                    half3 otherLDiffuse = half3(0,0,0);
                    half3 otherLSpecular= half3(0,0,0);

                    //其他光源
                    #ifdef _ADDLIGHT_ON

                        for(int j=0;j<GetAdditionalLightsCount();++j)
                        {
                            Light otherLight=GetAdditionalLight(j,i.worldPos);
                            otherLDiffuse +=(dot(normalize(otherLight.direction),N) +1) *0.5
                                             *otherLight.color *otherLight.shadowAttenuation*otherLight.distanceAttenuation;

                            otherLSpecular +=pow(saturate(dot(N,normalize(V + normalize(otherLight.direction)))),_Gloss)
                                            *_SpecularColor.rgb *otherLight.color *otherLight.shadowAttenuation*otherLight.distanceAttenuation;
                        }
                    #endif

                    half3 finalColor=albedo.rgb * (mainDiffuse +mainSpecular+ otherLDiffuse +otherLSpecular);
                    
                    return half4(finalColor,albedo.a);

                    
                }   
            ENDHLSL
        }

        Pass
        {
            Name"MYSHADOW"
            //目前不支持点光阴影
            Tags{ "LightMode"="ShadowCaster"}

            HLSLPROGRAM

                #pragma vertex vert
                #pragma fragment frag

                half3 _LightDirection;

                struct a2v
                {
                    float4 vertex:POSITION;
                    float3 normal:NORMAL;
                    float2 texcoord:TEXCOORD0;
                };

                struct v2f
                {
                    float4 pos:SV_POSITION;
                    float2 uv:TEXCOORD0;
                };

                v2f vert(a2v v)
                {
                    v2f o;

                    float3 worldPos=TransformObjectToWorld(v.vertex.xyz);
                    float3 worldNorml=normalize(TransformObjectToWorldNormal(v.normal));
                    o.uv=TRANSFORM_TEX(v.texcoord,_MainTex);

                    o.pos=TransformWorldToHClip(ApplyShadowBias(worldPos,worldNorml,_LightDirection));

                    #if UNITY_REVERSED_Z
                    o.pos.z=min(o.pos.z,o.pos.w*UNITY_NEAR_CLIP_VALUE);
                    #else
                    o.pos.z=max(o.pos.z,o.pos.w*UNITY_NEAR_CLIP_VALUE);
                    #endif

                    return o;
                }

                half4 frag(v2f i):SV_TARGET
                {
                    #ifdef _ALPHACUT_ON
                    float alpha=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).a;
                    clip(alpha-_Cutoff);
                    #endif

                    return 0;
                }
            ENDHLSL
        }

        
    }
}
