[GPUInstancing 文档](https://docs.unity3d.com/cn/2019.4/Manual/GPUInstancing.html)

## shader 限制

1. shader 必须包含 `#pragma multi_compile_instancing` 指令
2. 材质需要启用 **Enable Instancing**
3. 默认管线自动使用 **Instancing**，SRP 需要 DrawingSettings 设置 enableInstancing
4. 防止动态合批影响，*关闭*动态合批

## Shader 代码

```c
#ifndef MYRP_UNLIT_INCLUDED
#define MYRP_UNLIT_INCLUDED

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

// 3. 根据每种材质定义的，因此可以放入一个恒定的缓冲区中，仅在切换材质时才需要更改
CBUFFER_START(UnityPerMaterial)//cbuffer UnityPerMaterial {
	float4 _Color;
CBUFFER_END//};
*/

// 实例化时使用的每个材质属性
UNITY_INSTANCING_BUFFER_START(PerInstance)
	UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
UNITY_INSTANCING_BUFFER_END(PerInstance)

struct VertextInput {
	float4 pos : POSITION;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertextOutput {
	float4 clipPos : SV_POSITION;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};


VertextOutput UnlitPassVertex(VertextInput input)
{
	VertextOutput output;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	//float4 worlPos = mul(unity_ObjectToWorld, input.pos);
	float4 worlPos = mul(UNITY_MATRIX_M, float4(input.pos.xyz, 1));
	output.clipPos = mul(unity_MatrixVP, worlPos);
	return output;
}

float4 UnlitPassFragment(VertextOutput input) : SV_TARGET
{
	UNITY_SETUP_INSTANCE_ID(input);
	return UNITY_ACCESS_INSTANCED_PROP(PerInstance, _Color);
}

#endif
```

