#ifndef MYRP_LIT_INCLUDED
#define MYRP_LIT_INCLUDED
#include "UnityShaderVariables.cginc"
#include "UnityShaderUtilities.cginc"
#include "HLSLSupport.cginc"
#include "UnityInstancing.cginc"

/*
// 模型-视图-投影（model-view-projection）
// 1. 对象空间转换为世界空间, M矩阵放入每次绘制（per-draw）缓冲区
CBUFFER_START(UnityPerDraw)//cbuffer UnityPerDraw {
	float4x4 unity_ObjectToWorld;
CBUFFER_END//};

#define UNITY_MATRIX_M unity_ObjectToWorld

// 2. 从世界空间转换为剪辑空间, VP矩阵放入每帧（per-frame）缓冲区
CBUFFER_START(UnityPerFrame)//cbuffer UnityPerFrame {
	float4x4 unity_MatrixVP;
CBUFFER_END//};
*/
// 3. 根据每种材质定义的，因此可以放入一个恒定的缓冲区中，仅在切换材质时才需要更改
/*
CBUFFER_START(UnityPerMaterial)//cbuffer UnityPerMaterial {
	float4 _Color;
CBUFFER_END//};
*/

UNITY_INSTANCING_BUFFER_START(PerInstance)
	UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
UNITY_INSTANCING_BUFFER_END(PerInstance)

#define MAX_VISIBLE_LIGHTS 4
CBUFFER_START(_LightBuffer)
	float4 _VisibleLightColors[MAX_VISIBLE_LIGHTS];
	float4 _VisibleLightDirectionsOrPositions[MAX_VISIBLE_LIGHTS];
	float4 _VisibleLightAttenuations[MAX_VISIBLE_LIGHTS];
	float4 _VisibleLightSpotDirections[MAX_VISIBLE_LIGHTS];
CBUFFER_END

float3 DiffuseLight(int index, float3 normal, float3 worldPos) {
	float3 lightColor = _VisibleLightColors[index].rgb;
	float4 lightPositionOrDirection = _VisibleLightDirectionsOrPositions[index];
	float4 lightAttenuation = _VisibleLightAttenuations[index];
	float3 spotDirection = _VisibleLightSpotDirections[index].xyz;

	float3 lightVector = lightPositionOrDirection.xyz - worldPos * lightPositionOrDirection.w;
	float3 lightDirection = normalize(lightVector);
	float diffuse = saturate(dot(normal, lightDirection));

	float rangeFade = dot(lightVector, lightVector) * lightAttenuation.x;
	rangeFade = saturate(1.0 - rangeFade * rangeFade);
	rangeFade *= rangeFade;

	float spotFade = dot(spotDirection, lightDirection);
	spotFade = saturate(spotFade * lightAttenuation.z + lightAttenuation.w);
	spotFade *= spotFade;

	float distanceSqr = max(dot(lightVector, lightVector), 0.00001);
	diffuse *= spotFade * rangeFade / distanceSqr;

	return diffuse * lightColor;
}

struct VertextInput {
	float4 pos : POSITION;
	float3 normal : NORMAL;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertextOutput {
	float4 clipPos : SV_POSITION;
	float3 normal : TEXCOORD0;
	float3 worldPos : TEXCOORD1;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};


VertextOutput LitPassVertex(VertextInput input)
{
	VertextOutput output;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	//float4 worldPos = mul(unity_ObjectToWorld, input.pos);
	float4 worldPos = mul(UNITY_MATRIX_M, float4(input.pos.xyz, 1));
	output.clipPos = mul(unity_MatrixVP, worldPos);
	output.normal = mul((float3x3)UNITY_MATRIX_M, input.normal);
	output.worldPos = worldPos.xyz;
	return output;
}

float4 LitPassFragment(VertextOutput input) : SV_TARGET
{
	UNITY_SETUP_INSTANCE_ID(input);
	input.normal = normalize(input.normal);
	float3 albedo = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _Color);
	
	float3 diffuseLight = 0;
	for (int i = 0; i < MAX_VISIBLE_LIGHTS; i++) {
		diffuseLight += DiffuseLight(i, input.normal, input.worldPos);
	}
	float3 color = diffuseLight * albedo;
	return float4(color, 1);
}

#endif