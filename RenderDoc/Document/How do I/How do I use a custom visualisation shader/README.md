[原文地址](https://renderdoc.org/docs/how/how_custom_visualisation.html)

# How do I use a custom visualisation shader

本页详细介绍了如何设置自定义着色器以进行可视化。这可以在[Texture Viewer](https://renderdoc.org/docs/window/texture_viewer.html)中用于解压缩或解码复杂格式，或者简单地将自定义复杂转换应用于默认控件之外的图像。

## Introduction

设置自定义着色器的基本过程包括编写将由 RenderDoc 编译和使用的着色器文件。请注意，您可以使用所用图形 API 本机接受的任何语言，或者可以编译为可接受的着色器的任何语言。

例如，在 D3D11 或 D3D12 上，hlsl 是默认情况下唯一可用的语言。在 OpenGL 上只能使用 glsl，但在 Vulkan 上，您可以使用 glsl 或使用 hlsl，只要编译器可用。

有几个特殊的全局帮助器可以声明和使用，它们将由 RenderDoc 实现并返回值。此外，还有一些自动宏可用于绑定资源并编写着色器，这些着色器可在具有不同绑定的不同 API 上工作。

您的像素着色器定义了一个操作，将输入纹理的原始值转换为一个值，然后由纹理查看器显示。用于范围适应和通道的常用纹理查看器控件仍可用于生成的纹理。

要设置着色器，建议您使用[Texture Viewer](https://renderdoc.org/docs/window/texture_viewer.html)文档中定义的 UI，但您可以在应用程序存储目录（在 windows `%APPDATA%/qrenderdoc/`或其他地方 `~/.local/share/qrenderdoc`）中手动创建一个`.hlsl`或`.glsl`文件。该文件必须包含一个返回`float4`的入口点`main()`，并使用以下任何输入。这些着色器在 RenderDoc 加载捕获时加载，RenderDoc 监视文件的任何更改（在外部或在 RenderDoc 的着色器编辑器中）并自动重新加载它们。

> **Note**
>
> 由于`.glsl`用于 Vulkan 和 OpenGL 着色器，因此`VULKAN`如果您正在编写用于两者的着色器，则可以通过预定义的宏进行区分。

> **Warning**
>
> 以前，自定义着色器允许更直接的绑定，而无需辅助函数或绑定宏。这些着色器将在保持向后兼容性的情况下继续工作，但是请注意，这些绑定是特定于 API 的，因此例如在 glsl 中为 OpenGL 编写的着色器将无法在 Vulkan 上运行，除非已采取措施，或者 Vulkan 和 D3D 之间的 HLSL 着色器. 如果需要可移植性，请更新并使用新的帮助程序和绑定宏，否则请注意仅将自定义着色器与为其编写的 API 一起使用。

> **Note**
>
> 有关完整示例，请参阅 [contrib repository](https://github.com/baldurk/renderdoc-contrib/tree/main/baldurk/custom-shader-templates) 中可用的自定义着色器模板 。

## Predefined inputs

有几个预定义的输入可以作为着色器入口点的参数，或者定义为具有特定名称的全局变量，然后将其填充。还有不同类型的输入纹理的定义。

> **Warning**
>
> 类型和大小写对这些变量很重要，因此请确保使用正确的声明！

使用 UI 时的着色器编辑器可用于为您插入这些片段，并使用正确的类型和拼写。对于 GLSL，这些片段被插入到文件顶部的任何`#version`语句之后。

### UV co-ordinates

```c
/* HLSL */
float4 main(float4 pos : SV_Position, float4 uv : TEXCOORD0) : SV_Target0
{
  return 1;
}

/* GLSL */
layout (location = 0) in vec2 uv;

void main()
{
  // ...
}
```

此输入在 HLSL 中定义为着色器入口点的第二个参数。第一个定义了通常的`SV_Position`系统语义，第二个`TEXCOORD0`参数的前两个分量给出了纹理（或纹理切片）大小上每个维度中从 0 到 1 的 UV 坐标。

在 GLSL 中，它被绑定为`vec2`location 的输入`0`。

您还可以使用自动生成的系统坐标 -`SV_Position`或者`gl_FragCoord`如果您需要坐标`0`到`N,M`用于`NxM`纹理。

> **Note**
>
> 您必须按此顺序绑定这些参数，以确保与顶点着色器的链接匹配。

### Constant Parameters

有几个可用的常量参数，每个参数都可以通过一个辅助函数获得。下面详细介绍了它们及其包含的值。

### Texture dimensions

```c
uint4 RD_TexDim(); // hlsl
uvec4 RD_TexDim(); // glsl

uint4 RD_YUVDownsampleRate(); // hlsl
uvec4 RD_YUVDownsampleRate(); // vulkan glsl only
uint4 RD_YUVAChannels(); // hlsl
uvec4 RD_YUVAChannels(); // vulkan glsl only
```

`RD_TexDim` will return the following values:

- `.x` Width
- `.y` Height (if 2D or 3D)
- `.z` Depth if 3D or array size if an array
- `.w` Number of mip levels

`RD_YUVDownsampleRate` will return the following values:

- `.x` Horizontal downsample rate. 1 for equal luma and chroma width, 2 for half rate.
- `.y` Vertical downsample rate. 1 for equal luma and chroma height, 2 for half rate.
- `.z` Number of planes in the input texture, 1 for packed, 2+ for planar
- `.w` Number of bits per component, e.g. 8, 10 or 16.

`RD_YUVAChannels` will return an index indicating where each channel comes from in the source textures. The order is `.x` for `Y`, `.y` for `U`, `.z` for `V` and `.w` for `A`.

正常 2D 槽中第一个纹理中通道的索引是`0, 1, 2, 3`. `4`到`7`指示第二个纹理中的通道的索引，依此类推。

如果通道不存在，例如 alpha 通常不可用，它将被设置为。`0xff == 255`

### Selected Mip level

```c
uint RD_SelectedMip(); // hlsl or glsl
```

这将在 UI 中返回选定的 mip 级别。

### Selected Slice/Face

```c
uint RD_SelectedSliceFace(); // hlsl or glsl
```

此变量将在 UI 中使用选定的纹理阵列切片（或立方体贴图面）填充。

### Selected Multisample sample

```c
int RD_SelectedSample(); // hlsl or glsl
```

此变量将使用 UI 中选择的选定多样本样本索引填充。如果 UI 选择了“平均值”，则此变量将为负数，其绝对值等于样本数。

因此，例如在 4x MSAA 纹理中，有效值为`0`, `1`, `2`,`3`以选择样本或`-4`“平均值”。

### Selected RangeMin, RangeMax

```c
float2 RD_SelectedRange(); // hlsl
vec2 RD_SelectedRange(); // glsl
```

此函数将返回一对，其中包含纹理查看器中范围选择器的当前最小值和最大值。

### Current texture type

```c
uint RD_TextureType(); // hlsl or glsl
```

此变量将设置为给定的整数值，具体取决于当前显示的纹理类型。这可用于从正确的资源中采样。

> **Note**
>
> 该值取决于此着色器将用于的 API，因为每个 API 都有不同的资源绑定。您应该使用下面的定义来检查，这将是跨 API 可移植的

#### D3D11 or D3D12

- `RD_TextureType_1D` - 1D texture
- `RD_TextureType_2D` - 2D texture
- `RD_TextureType_3D` - 3D texture
- `RD_TextureType_Depth` - Depth
- `RD_TextureType_DepthStencil` - Depth + Stencil
- `RD_TextureType_DepthMS` - Depth (Multisampled)
- `RD_TextureType_DepthStencilMS` - Depth + Stencil (Multisampled)
- `RD_TextureType_2DMS` - 2D texture (Multisampled)

在 D3D 上的所有情况下，绑定都可以用于数组或不用于数组，可互换。

#### OpenGL

- `RD_TextureType_1D` - 1D texture
- `RD_TextureType_2D` - 2D texture
- `RD_TextureType_3D` - 3D texture
- `RD_TextureType_Cube` - Cubemap
- `RD_TextureType_1D_Array` - 1D array texture
- `RD_TextureType_2D_Array` - 2D array texture
- `RD_TextureType_Cube_Array` - Cube array texture
- `RD_TextureType_Rect` - Rectangle texture
- `RD_TextureType_Buffer` - Buffer texture
- `RD_TextureType_2DMS` - 2D texture (Multisampled)
- `RD_TextureType_2DMS_Array` - 2D array texture (Multisampled)

OpenGL 对阵列和非阵列纹理具有不同的类型和绑定。

#### Vulkan

- `RD_TextureType_1D` - 1D texture
- `RD_TextureType_2D` - 2D texture
- `RD_TextureType_3D` - 3D texture
- `RD_TextureType_2DMS` - 2D texture (Multisampled)

在 Vulkan 上的所有情况下，绑定都可用于数组或不用于数组，可互换。

### Samplers

```c
/* HLSL */
SamplerState pointSampler : register(RD_POINT_SAMPLER_BINDING);
SamplerState linearSampler : register(RD_LINEAR_SAMPLER_BINDING);


/* GLSL */
#ifdef VULKAN

layout(binding = RD_POINT_SAMPLER_BINDING) uniform sampler pointSampler;
layout(binding = RD_LINEAR_SAMPLER_BINDING) uniform sampler linearSampler;

#endif
```

提供这些采样器是为了让您从资源中进行采样，而不是直接加载。采样器在 OpenGL 上不可用，因此建议`#ifdef VULKAN`保护 glsl 定义，如图所示。

### Resources

#### HLSL

```c
// Float Textures
Texture1DArray<float4> texDisplayTex1DArray : register(RD_FLOAT_1D_ARRAY_BINDING);
Texture2DArray<float4> texDisplayTex2DArray : register(RD_FLOAT_2D_ARRAY_BINDING);
Texture3D<float4> texDisplayTex3D : register(RD_FLOAT_3D_BINDING);
Texture2DMSArray<float4> texDisplayTex2DMSArray : register(RD_FLOAT_2DMS_ARRAY_BINDING);
Texture2DArray<float4> texDisplayYUVArray : register(RD_FLOAT_YUV_ARRAY_BINDING);

// only used on D3D
Texture2DArray<float2> texDisplayTexDepthArray : register(RD_FLOAT_DEPTH_ARRAY_BINDING);
Texture2DArray<uint2> texDisplayTexStencilArray : register(RD_FLOAT_STENCIL_ARRAY_BINDING);
Texture2DMSArray<float2> texDisplayTexDepthMSArray : register(RD_FLOAT_DEPTHMS_ARRAY_BINDING);
Texture2DMSArray<uint2> texDisplayTexStencilMSArray : register(RD_FLOAT_STENCILMS_ARRAY_BINDING);

// Int Textures
Texture1DArray<int4> texDisplayIntTex1DArray : register(RD_INT_1D_ARRAY_BINDING);
Texture2DArray<int4> texDisplayIntTex2DArray : register(RD_INT_2D_ARRAY_BINDING);
Texture3D<int4> texDisplayIntTex3D : register(RD_INT_3D_BINDING);
Texture2DMSArray<int4> texDisplayIntTex2DMSArray : register(RD_INT_2DMS_ARRAY_BINDING);

// Unsigned int Textures
Texture1DArray<uint4> texDisplayUIntTex1DArray : register(RD_UINT_1D_ARRAY_BINDING);
Texture2DArray<uint4> texDisplayUIntTex2DArray : register(RD_UINT_2D_ARRAY_BINDING);
Texture3D<uint4> texDisplayUIntTex3D : register(RD_UINT_3D_BINDING);
Texture2DMSArray<uint4> texDisplayUIntTex2DMSArray : register(RD_UINT_2DMS_ARRAY_BINDING);
```

#### GLSL

```c
// Float Textures
layout (binding = RD_FLOAT_1D_ARRAY_BINDING) uniform sampler1DArray tex1DArray;
layout (binding = RD_FLOAT_2D_ARRAY_BINDING) uniform sampler2DArray tex2DArray;
layout (binding = RD_FLOAT_3D_BINDING) uniform sampler3D tex3D;
layout (binding = RD_FLOAT_2DMS_ARRAY_BINDING) uniform sampler2DMSArray tex2DMSArray;

// YUV textures only supported on vulkan
#ifdef VULKAN
layout(binding = RD_FLOAT_YUV_ARRAY_BINDING) uniform sampler2DArray texYUVArray[2];
#endif

// OpenGL has more texture types to match
#ifndef VULKAN
layout (binding = RD_FLOAT_1D_BINDING) uniform sampler1D tex1D;
layout (binding = RD_FLOAT_2D_BINDING) uniform sampler2D tex2D;
layout (binding = RD_FLOAT_CUBE_BINDING) uniform samplerCube texCube;
layout (binding = RD_FLOAT_CUBE_ARRAY_BINDING) uniform samplerCubeArray texCubeArray;
layout (binding = RD_FLOAT_RECT_BINDING) uniform sampler2DRect tex2DRect;
layout (binding = RD_FLOAT_BUFFER_BINDING) uniform samplerBuffer texBuffer;
layout (binding = RD_FLOAT_2DMS_BINDING) uniform sampler2DMS tex2DMS;
#endif

// Int Textures
layout (binding = RD_INT_1D_ARRAY_BINDING) uniform isampler1DArray texSInt1DArray;
layout (binding = RD_INT_2D_ARRAY_BINDING) uniform isampler2DArray texSInt2DArray;
layout (binding = RD_INT_3D_BINDING) uniform isampler3D texSInt3D;
layout (binding = RD_INT_2DMS_ARRAY_BINDING) uniform isampler2DMSArray texSInt2DMSArray;

#ifndef VULKAN
layout (binding = RD_INT_1D_BINDING) uniform isampler1D texSInt1D;
layout (binding = RD_INT_2D_BINDING) uniform isampler2D texSInt2D;
layout (binding = RD_INT_RECT_BINDING) uniform isampler2DRect texSInt2DRect;
layout (binding = RD_INT_BUFFER_BINDING) uniform isamplerBuffer texSIntBuffer;
layout (binding = RD_INT_2DMS_BINDING) uniform isampler2DMS texSInt2DMS;
#endif

// Unsigned int Textures
layout (binding = RD_UINT_1D_ARRAY_BINDING) uniform usampler1DArray texUInt1DArray;
layout (binding = RD_UINT_2D_ARRAY_BINDING) uniform usampler2DArray texUInt2DArray;
layout (binding = RD_UINT_3D_BINDING) uniform usampler3D texUInt3D;
layout (binding = RD_UINT_2DMS_ARRAY_BINDING) uniform usampler2DMSArray texUInt2DMSArray;

#ifndef VULKAN
layout (binding = RD_UINT_1D_BINDING) uniform usampler1D texUInt1D;
layout (binding = RD_UINT_2D_BINDING) uniform usampler2D texUInt2D;
layout (binding = RD_UINT_RECT_BINDING) uniform usampler2DRect texUInt2DRect;
layout (binding = RD_UINT_BUFFER_BINDING) uniform usamplerBuffer texUIntBuffer;
layout (binding = RD_UINT_2DMS_BINDING) uniform usampler2DMS texUInt2DMS;
#endif
```

> **Note**
>
> YUV 纹理可能有额外的平面绑定为单独的纹理 - 对于 D3D，这是`texDisplayYUVArray`，对于 Vulkan，它在`texYUVArray`上面。是否使用这些平面在纹理维度变量中指定。

## See Also

- [Texture Viewer](https://renderdoc.org/docs/window/texture_viewer.html)