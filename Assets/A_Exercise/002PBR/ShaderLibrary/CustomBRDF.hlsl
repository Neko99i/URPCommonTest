#ifndef UNITY_CUSTOMBRDF_INCLUDE
#define UNITY_CUSTOMBRDF_INCLUDE

#include "CustomSurface.hlsl"
#include "CustomCommonFunction.hlsl"

struct BRDF_Value
{
    half3 F;
    half G;
    half D;
    half3 Dirspecular;
    half3 Dirdiffsue;
    half3 DirBRDFColor;
};

struct BRDF_InputData
{
    half r2;
    half3 L;
    half3 V;
    half3 N;
    half3 H;
    float3 ReflectDir;
    float NdotL;
    float NdotV;
    float NdotH;
    float VdotH;
    float LdotH;
    half3 radiance;
    half metallic;
    half3 albedo;
    half3 specularColor;
};

inline void InitBRDFData(float3 N,float3 V,Light light,half3 specularColor,half roughness,half3 albedo,half metallic,out BRDF_InputData brdfData )
{
    brdfData.r2=max(pow(roughness,2),HALF_MIN);
    brdfData.L=normalize(light.direction);
    brdfData.H=SafeNormalize(V+brdfData.L);
    brdfData.V=V;
    brdfData.N=N;
    brdfData.ReflectDir=reflect(-V,N);

    brdfData.NdotL=max(HALF_MIN,dot(N,brdfData.L));
    brdfData.NdotV=max(HALF_MIN,dot(N,V));
    brdfData.NdotH=max(HALF_MIN,dot(N,brdfData.H));
    brdfData.VdotH=max(HALF_MIN,dot(V,brdfData.H));
    brdfData.LdotH=max(HALF_MIN,dot(brdfData.L,brdfData.H));

    brdfData.radiance=light.color *(light.distanceAttenuation * light.shadowAttenuation * brdfData.NdotL);
    brdfData.metallic=metallic;
    brdfData.albedo=albedo;
    brdfData.specularColor=specularColor;
}
//电解质F0
#define KDieletricSpec half4(0.04,0.04,0.04,1-0.04)

half OneMinusReflectivityFormMetallic(half metallic)
{
    half OneMinusDieletricSpec=KDieletricSpec.a;
    return OneMinusDieletricSpec-metallic*OneMinusDieletricSpec;
}

half3 GetF0(half3 albedo,half metallic)
{
    return lerp(KDieletricSpec.rgb,albedo,metallic);
}

//漫反射部分：Kd*（c/π） 因为后面要乘上半球积分为π，所以在此直接约掉
half3 SimpleDiffuse(half metallic,half3 albedo,half3 F)
{
    return OneMinusReflectivityFormMetallic(metallic) * (half3(1,1,1)-F) * albedo;
}
            
float CustomDisneyDiffuse(float Ndotv,float NdotL,float LdotH,float roughness)
{
    float fd90=0.5 + 2 * LdotH * LdotH * roughness;
    float lightScatter=(1+(fd90-1)*Pow5(1-NdotL));
    float viewScatter=(1+(fd90-1)*Pow5(1-Ndotv));
    return lightScatter*viewScatter;
}

float3 F_Ue(float3 F0, float VdotH)
{
    return F0 + (1 - F0) * exp2((-5.55473 * VdotH - 6.98316) * VdotH);
} 


float3 F_Ue0(float3 F0, float VdotH)
{
    return F0 + (1 - F0) * exp2((-5.55473 * VdotH - 6.98316) * VdotH);
} 

float3 F_Ue_HL(float3 F0, float HdotL)
{
    return F0 + (1 - F0) * exp2((-5.55473 * HdotL - 6.98316) * HdotL);
} 

float3 F_Schilick(float3 F0, float NdotV)
{
    return F0 + (1-F0) * Pow5(1-NdotV);
} 

float3 F_LerpU(float3 F0, float3 F90,half NdotV)
{
    return lerp(F0,F90,Pow5(1-NdotV));
} 
            
float3 FresnelSchlickRoughness(float NdotV, float3 F0, float roughness)
{
    return F0 + (max(float3(1.0 - roughness, 1.0 - roughness, 1.0 - roughness), F0) - F0) * pow(1.0 - NdotV, 5.0);
}

float3 FresnelSchlickRoughnessUE(float NdotV, float3 F0, float roughness)
{
    float Fre = exp2((-5.55473*NdotV-6.98316)*NdotV);

    return F0 + Fre*saturate(1-roughness-F0);
}


//法线分布函数
float BRDF_D_GGX(float NdotH, float r2)
{
    float a2 = r2*r2;
    float NdotH2 = NdotH*NdotH;
    float denom = pow((NdotH2 * (a2 - 1.0) + 1.0),2);
    return a2 / denom ;
}

//几何函数=几何阴影*几何遮挡
float G_SchlickGGx(float xdoty,float r2)
{
    float a = (r2 + 1.0);
    float k = (a*a) /8;
    float denom = xdoty * (1.0 - k) + k;
    return (xdoty)/ (denom);
}

float BRDF_G_Smith(float NdotV,float NdotL,float r2)
{
    return G_SchlickGGx(NdotV,r2) * G_SchlickGGx(NdotL,r2);   
}

//球谐函数采样
real3 SH_IndirectionDiff(float3 normalWS)
{
    real4 SHCoefficients[7];

    SHCoefficients[0]=unity_SHAr;
    SHCoefficients[1]=unity_SHAg;
    SHCoefficients[2]=unity_SHAb;
    SHCoefficients[3]=unity_SHBr;
    SHCoefficients[4]=unity_SHBg;
    SHCoefficients[5]=unity_SHBb;
    SHCoefficients[6]=unity_SHC;

    float3 Color=SampleSH9(SHCoefficients,normalWS);
    return max(0,Color);
}


inline  void GetPBR_DirectLightData(BRDF_InputData brdfData ,out BRDF_Value brdfValue)
{
    half3 F0=GetF0(brdfData.albedo,brdfData.metallic);

//DirectLight
    float  D=BRDF_D_GGX(brdfData.NdotH,brdfData.r2);
    //float3 F=F_Ue(F0,VdotH);
    // float3 F=F_Ue0(F0,VdotH);
    float3 F=F_Ue_HL(F0,brdfData.LdotH);
    float  G=BRDF_G_Smith(brdfData.NdotV,brdfData.NdotL,brdfData.r2);
    //return G;
    float3 denom= 4 * brdfData.NdotV * brdfData.NdotL;
    half3 specular=((D * F * G)/denom) * brdfData.specularColor;

    half3 diffuse = SimpleDiffuse(brdfData.metallic,brdfData.albedo,F);

    brdfValue.F=F;
    brdfValue.G=G;
    brdfValue.D=D;
    brdfValue.Dirspecular=specular;
    brdfValue.Dirdiffsue=diffuse;
    brdfValue.DirBRDFColor=(specular + diffuse) * brdfData.radiance;
}


half3 GetPBR_GI(float3 N,float3 V,half3 specularColor,half roughness,half3 albedo,half metallic)
{
    //BRDFData
    half r2=max(pow(roughness,2),HALF_MIN);
    float3 ReflectDir=reflect(-V,N);
    float NdotV=max(HALF_MIN,dot(N,V));
    half3 F0=GetF0(albedo,metallic);

    //IndirectDiffuse
    half3   SHcolor= SH_IndirectionDiff(N);
    //注：在URP中并没有乘折射率(1-ks)    
    // float3  Ks=FresnelSchlickRoughness(NdotV,F0,r2);
    float3  Ks=FresnelSchlickRoughnessUE(NdotV,F0,r2);
    half3 inDirDiffuse=albedo * OneMinusReflectivityFormMetallic(metallic) * SHcolor;

    //IndirectSpecular
    float percetualRoughness = roughness * (1.7 - 0.7 * roughness);
    float mip = percetualRoughness * 6;
    float4 speCubeColor=SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0,ReflectDir,mip);
    
    float3 inDirSpecColor=float3(0,0,0);
    
    #if !defined(UNITY_USE_NATIVE_HDR)
        inDirSpecColor= DecodeHDREnvironment(speCubeColor,unity_SpecCube0_HDR);//用DecodeHDREnvironment将颜色从HDR编码下解码。可以看到采样出的rgbm是一个4通道的值，最后一个m存的是一个参数，解码时将前三个通道表示的颜色乘上xM^y，x和y都是由环境贴图定义的系数，存储在unity_SpecCube0_HDR这个结构中。
    #else
        inDirSpecColor= speCubeColor.xyz;
    #endif

    #ifdef UNITY_COLORSPACE_GAMMA
        float surfaceReduction=1 - 0.28 * r2 *roughness;
    #else
        float surfaceReduction=1/(r2 * r2 + 1);
    #endif

    #ifdef _SPECULAR_SETUP
        // half reflectivity=ReflectivitySpecularCustom(F0);
        half reflectivity=max(max(F0.r,F0.g),F0.b);
    #else
        half reflectivity=1-OneMinusReflectivityFormMetallic(metallic);
    #endif

    half grazing=saturate(reflectivity + (1-roughness));
    half3 inDirSpecFactor=F_LerpU(F0 ,grazing * specularColor,NdotV) * surfaceReduction;
    inDirSpecColor*=inDirSpecFactor;

    half3 inDirBRDFColor= inDirDiffuse + inDirSpecColor;

    return inDirBRDFColor ;
}

half ReflectivitySpecularCustom(float3 specular)
{
    #if defined(SHADER_API_GLES)
        return specular.r;
    #else 
        return max(max(specular.r,specular.g),specular.b);
    #endif
}

#endif