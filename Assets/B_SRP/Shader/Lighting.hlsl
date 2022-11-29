
//定义最大灯数
#define MAX_DIRECTIONAL_LIGHT_COUNT 4

CBUFFER_START(_CustomLight)
	// float3 _DirectionalLightColor;
	// float3 _DirectionalLightDirection;

	int _DirectionalLightCount;
	float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
	float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
CBUFFER_END

struct Light
{
	float3 color;
	float3 direction;
};

//光源数量
int GetDirectionalLightCount () 
{
	return _DirectionalLightCount;
}

Light GetLighting (int index)
{
	Light light;

	light.color = _DirectionalLightColors[index].xyz;
	light.direction = _DirectionalLightDirections[index].xyz;

	return light;
}


float3 LambertLight (Surface surface, Light light) 
{
	return saturate(dot(surface.normal, light.direction)) * light.color;
}

float3 HalfLambertLight (Surface surface, Light light) 
{
	return (dot(surface.normal, light.direction) +1)* 0.5 * light.color;
}
