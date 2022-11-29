
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
//保存了空间转换的函数
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
#include "Surface.hlsl"
#include "Lighting.hlsl"


TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

CBUFFER_START(UnityPerMaterial)
    float4 _MainColor; 
CBUFFER_END


struct a2v
{
    float4 vertex : POSITION;
    float3 normalOS:NORMAL;
    float2 uv : TEXCOORD0;
};

struct v2f
{
    float2 uv : TEXCOORD0;
    float4 vertex : SV_POSITION;
    float3 normalWS:V2F_NORMAL;
};


v2f vert (a2v v)
{
    v2f o;
    o.vertex = TransformObjectToHClip(v.vertex.xyz);
    o.normalWS=TransformObjectToWorldNormal(v.vertex.xyz);
    o.uv = v.uv ;
    return o;
}

half4 frag (v2f i) : SV_Target
{
    Surface surface;
	surface.normal = normalize(i.normalWS);

	half3 albedo = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv)*_MainColor.rgb;

	surface.alpha = 1;

    half3 diffuse=(0,0,0);
    
    for(int j=0; j < GetDirectionalLightCount(); j++)
    {
        diffuse += LambertLight(surface,GetLighting(j)) * albedo;
    }
    surface.color=diffuse;
    return half4(surface.color,surface.alpha);
}