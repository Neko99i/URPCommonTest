#ifndef UNITY_CUSTOMSURFACE_INCLUDE
#define UNITY_CUSTOMSURFACE_INCLUDE

//金属度——粗糙度
struct SurfaceData
{
    //SurfaceColor
    half3 albedo;
    half3 specularColor;
    half3 emissionColor;
    
    half metallic;
    half roughness;
    half occlusion;
    half alpha;

};

void InitSurfaceData(half4 mianColor,half3 specularColor,half3 emissionColor,
                     half4 mixColor)
{
    SurfaceData surfaceData=(SurfaceData)0; 


}
#endif