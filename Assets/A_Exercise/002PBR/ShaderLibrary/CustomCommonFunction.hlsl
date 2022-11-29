#ifndef UNITY_CUSTOMCOMMONFUNCTION_INCLUDE
#define UNITY_CUSTOMCOMMONFUNCTION_INCLUDE

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

//CommonFunction
float Pow5(float a)
{
    return a*a*a*a*a;
}                
//TBN
// float3 normalWS=TransformObjectToWorldNormal(v.normalOS);
// float3 tangentWS=TransformObjectToWorldDir(v.tangentOS.xyz);
// float3 bitNormal=cross(normalWS,tangentWS)*v.tangentOS.w * unity_WorldTransformParams.w;

// o.T1=float4(tangentWS.x,bitNormal.x,normalWS.x,positionWS.x);
// o.T2=float4(tangentWS.y,bitNormal.y,normalWS.y,positionWS.y);
// o.T3=float4(tangentWS.z,bitNormal.z,normalWS.z,positionWS.z);



//SSS_Lut
#ifdef _SSS_LUT_ON

    #ifdef _CURMODE_CURTEX
        TEXTURE2D(_CurTex); SAMPLER(sampler_CurTex);
    #endif

    TEXTURE2D(_Lut); SAMPLER(sampler_Lut);

 half3 SimpleDiffuseFormSSSLut(half3 normalH,half3 normalL,float _CurScale,float _CurOffset,Light light,float3 worldPos, half3 diffuse,float2 uv)
 {
    float curvature;
    half3 L=normalize(light.direction);
    #ifdef _CURMODE_CURTEX
        curvature=SAMPLE_TEXTURE2D(_CurTex,sampler_CurTex,uv).r * _CurScale + _CurOffset;
    #else 
        curvature=saturate((length(fwidth(normalL)) / length(fwidth(worldPos)))*0.1)*_CurScale+_CurOffset;
    #endif

     //使用模糊法线计算:通过法线贴图和顶点法线也可以有不错的效果
    float  rNoL = dot(normalL,normalize(light.direction));
    float3 BlurFactor = saturate(1.0f - rNoL);
    BlurFactor *= BlurFactor;
    float3 gN = lerp(normalH, normalL, 0.3f + 0.7f * BlurFactor);
    float3 bN = lerp(normalH, normalL, BlurFactor);

    float3 NoL = float3( rNoL, dot(gN, L), dot(bN, L) );
    float3 lookup = NoL * 0.5f + 0.5f;

    //采样LUT
    half3 lutCol;
    lutCol.r = SAMPLE_TEXTURE2D( _Lut, sampler_Lut, float2(lookup.r, curvature) ).r;
    lutCol.g = SAMPLE_TEXTURE2D( _Lut, sampler_Lut, float2(lookup.g, curvature) ).g;
    lutCol.b = SAMPLE_TEXTURE2D( _Lut, sampler_Lut,  float2(lookup.b, curvature) ).b;

    return diffuse*lutCol * light.color *(light.distanceAttenuation * light.shadowAttenuation);
 }
#endif

#endif