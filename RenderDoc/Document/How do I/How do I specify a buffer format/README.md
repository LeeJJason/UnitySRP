[原文地址](https://renderdoc.org/docs/how/how_buffer_format.html)

# How do I specify a buffer format

此页面记录了如何格式化缓冲区数据，以防默认反射格式丢失或您想要自定义它。

格式字符串可以自由包含 C 和 C++ 风格的注释，但不支持 C 预处理器。

默认情况下，最终解释格式由布局字符串中的全局变量列表定义，但是如果没有定义全局变量，则使用要定义的最终结构，就好像该结构有一个变量实例一样。

```c
struct data
{
  float a;
  int b;
};
```

相当于

```c
struct data
{
  float a;
  int b;
};

data d;
```

但是请注意，如果有全局变量，则**不会**自动实例化结构，必须将其声明为变量。

## Basic types

可以使用 GLSL 或 HLSL 中熟悉的语法声明变量。浮点数将被声明为：

```c
float myvalue;
```

最常见的基本类型可用：

- `bool`- 一个 4 字节的布尔值。
- `byte`, `short`, `int`, `long`- 分别是 1、2、4 和 8 字节有符号整数。
- `ubyte`, `ushort`, `uint`, `ulong`- 分别为 1、2、4 和 8 字节无符号整数。也可以`unsigned`在有符号类型之前添加前缀。
- `half`, `float`, `double`- 2、4 和 8 字节浮点值。

这些可以通过附加向量宽度来声明为向量。类似地，可以通过附加向量和矩阵维度来声明矩阵。

也支持浮点、整数和 uint 向量和矩阵的 GLSL 声明。

```c
float2 myvector2; // 2-vector of floats
float4 myvector4;
float2x2 mymat;   // 2x2 matrix
mat2 myGLmat;     // equivalent to float2x2
vec2 myGLvec;     // equivalent to float2
```

您还可以将这些类型中的任何一种声明为数组。数组可以具有固定大小，也可以具有无界大小 - 无界数组在[detailed below in the discussion of AoS vs SoA data](https://renderdoc.org/docs/how/how_buffer_format.html#aos-soa)。

```c
float fixedArray[2];    // array of two floats
float3 unboundedArray[]; // unbounded array of float3s
```

## Structs

结构的声明与 GLSL 或 HLSL 或 C 中的类似。

```c
struct MyStructName {
  float a;
  int b;
};

MyStructName str; // single instance of the struct
MyStructName arr[4]; // array of 4 instances
```

结构可以自由嵌套，但不能向前声明，因此结构只能引用在它之前定义的结构。

## Enums

枚举可以定义，但必须使用基本整数类型定义以声明其大小。枚举值必须是文字整数，可以是十进制或十六进制。

必须明确给出值，并且不支持自动编号或基于表达式的值。

```c
enum MyEnum : uint {
  FirstValue = 5,
  HexValue = 0xf,
};

MyEnum e; // A uint will be read and interpreted as the above enum
```

## Bitfields

整数值可以使用 C 风格的位域进行位打包。

```c
int first : 3;
int second : 5;
int third : 10;
int : 6; // anonymous values can be used to skip bits without declaring anything
int last : 8;
```

此声明将仅读取单个 32 位整数，并根据此打包解释这些位。

## Pointers

在 GPU 指针可以驻留在内存中的 API（例如 Vulkan）上，可以使用基本结构类型声明指针，这些指针将从底层缓冲区中的 64 位地址读取和解释。

```c
struct MyStructName {
  float a;
  int b;
};

MyStructName *pointer;
```

## Packing and layout rules

图形 API 定义了如何将数据打包到内存中的不同规则，这有时取决于 API 中缓冲区的使用情况。

RenderDoc 将尽可能使用最合理的默认值 - 例如，对于已知绑定为常量缓冲区的 D3D 缓冲区，将使用常量缓冲区打包，类似于使用 std140 的 OpenGL 统一缓冲区。但是，可以显式指定打包，并且任何基于反射的自动格式都将显式声明打包。

一旦指定了打包格式，RenderDoc 将为每个元素计算必要的对齐和填充以符合规则，否则紧密打包，就像普通着色器声明一样。

可以使用 指定缓冲区的格式`#pack(packing_format)`。这只能在全局范围内指定，而不是在结构内，并且打包规则将适用于所有后续声明。

支持的五种打包格式是：

- `cbuffer`,`d3dcbuffer`或`cb`- D3D 常量缓冲区打包。
- `structured`, `d3duav`, 或`uav`- D3D 结构化缓冲区打包（适用于缓冲区 SRV 和 UAV）。
- `std140`, `ubo`, 或`gl`- OpenGL std140 统一缓冲区打包。
- `std430`, `ssbo`- OpenGL std430 存储缓冲区打包。
- `scalar`- Vulkan 标量缓冲区打包。

也可以使用`#pack()`. 每个属性都可以通过`#pack(prop)`或启用或禁用`#pack(no_prop)`。每个属性都会*放宽*一些限制，所以最严格的包装是`std140`关闭所有属性，最宽松的包装是`scalar`打开所有属性。

可用的包装属性有：

- `vector_align_component`- 如果启用，向量仅与其组件对齐。如果禁用，2-vectors 对齐到 2x 其分量，3-vectors 和 4-vectors 对齐到 4x 其分量。这仅在默认情况下被`std140`禁用`std430`。
- `vector_straddle_16b`- 如果启用，则允许向量跨越 16 字节对齐边界。如果禁用，向量必须填充/对齐以不跨越。这仅对`std140`、`std430`和`cbuffer`默认情况下禁用。
- `tight_arrays`- 如果启用，数组元素仅与元素大小对齐。如果禁用，每个数组元素都对齐到 16 字节边界。`std140`默认情况下禁用此功能`cbuffer`。
- `trailing_overlap`- 如果启用，元素可以放置在前一个元素（如数组或结构）的尾随填充中。如果禁用，则保留每个元素的填充，并且下一个元素必须在填充之后。`std140`这对、`std430`和默认情况下禁用`structured`。

## Annotations

缓冲区格式支持声明注释以指定特殊属性。这些使用 C++`[[annotation(parameter)]]`语法。

结构定义支持以下注解：

- `[[size(number)]]`或`[[byte_size(number)]]`- 强制将结构填充到给定大小，即使内容不需要它也是如此。
- `[[single]]`或`[fixed]]`- 强制将结构视为固定的 SoA 定义，即使在上下文中缓冲区查看器可能默认为 AoS。有关详细信息，请参阅以下部分。带有此注解的结构**可能不会**被声明为变量，而应该是定义中的隐式最终结构。

变量声明支持以下注解：

- `[[offset(number)]]`或- 强制此成员位于**相对于其父级**`[[byte_offset(number)]]`的给定偏移处。这不能比根据当前包装规则的紧密包装更早地放置成员。

- `[[pad]]`或`[[padding]]`- 将此成员标记为填充，以便计算结构布局时考虑到它，但它不可见。

- `[[single]]`或`[fixed]]`- 强制将此变量视为固定的 SoA 定义，即使在上下文中缓冲区查看器可能默认为 AoS。有关详细信息，请参阅[以下部分。](https://renderdoc.org/docs/how/how_buffer_format.html#aos-soa)这必须是一个全局变量，并且它必须是格式定义中唯一的全局变量。

- `[[row_major]]`或`[[col_major]]`- 声明矩阵的内存顺序。

- `[[rgb]]`- 将通过将其内容解释为 RGB 颜色来为任何重复数据的背景着色。

- `[[hex]]`或`[[hexadecimal]]`- 将整数数据显示为十六进制。

- `[[bin]]`或`[[binary]]`- 将整数数据显示为二进制。

- `[[unorm]]`或`[[snorm]]`- 在 1 字节或 2 字节整数变量上，将它们分别解释为无符号或有符号规范化数据。

- `[[packed(format)]]`- 根据标准位压缩格式解释变量。支持的格式有：

    > - `r11g11b10`必须与`float3`类型一起使用。
    > - `r10g10b10a2`或者`r10g10b10a2_uint`必须与`uint4`类型一起使用。可以选择与`[[unorm]]`或结合使用`[[snorm]]`。
    > - `r10g10b10a2_unorm`必须与`uint4`类型一起使用。
    > - `r10g10b10a2_snorm`必须与`int4`类型一起使用。

## Array of Structs (AoS) vs Struct of Arrays (SoA)

 [Buffer Viewer](https://renderdoc.org/docs/window/buffer_viewer.html) 能够显示单一格式的重复数据 (AoS) 以及固定的非重复数据 (称为 SoA)。通常 AoS 用于大型缓冲区，其中一个小结构重复多次以形成缓冲区中的元素。SoA 最常用于具有固定数据量的常量缓冲区，但可以在任何上下文中使用。在某些 API 上，缓冲区可能在重复数据之前包含一些固定数据，因此它包含两种类型。

RenderDoc 尝试使用上下文来正确解释缓冲区格式，在可能的情况下默认为 AoS 解释。然而，这可以根据需要提示或覆盖。

要明确指定 AoS 数据，您可以声明一个无界数组：

```
float3 unboundedArray[]; // unbounded array of float3s
```

当 API 支持时，可以在重复 AoS 数据之前的缓冲区中的任何固定数据之前。缓冲区查看器将分别显示数据的两个部分，固定数据的树视图和重复数据的表。

在相反的方向上，通常没有任何此类无界数组的松散变量集合将被视为 AoS 视图中结构的定义：

```c
struct data
{
  float a;
  int2 b;
  float c;
};
```

但是，如果希望将其显示为单个固定元素，其中固定树视图更合适，则可以将其结构或变量注释为`[single]]`或`[[fixed]]`。

```c
[[single]]
struct data
{
  float a;
  int2 b;
  float c;
};
```

```c
struct data
{
  float a;
  int2 b;
  float c;
};

[[single]]
data fixed_data;
```

这将强制结构显示为单个实例，而不是重复的 AoS。

## Saving and loading formats

可以保存常用的格式，并且这些格式将在运行之间保持不变。

要将当前格式保存到现有条目，请选择它并单击![节省](save.png)按钮。

要保存到新条目，请直接`New...`在底部输入名称，双击它开始输入名称，或者选择它并单击保存，然后输入名称。

如果缓冲区视图以自动填充的格式打开，它将作为只读`<Auto-generated>`条目提供。

加载条目可以通过双击它或选择它并单击 来完成![戈罗](action_hover.png)。这将加载格式并自动将其应用到缓冲区视图。

如果您希望返回到以前的格式，您可以使用撤消/重做来撤消已保存格式的加载。

## See Also

- [Buffer Viewer](https://renderdoc.org/docs/window/buffer_viewer.html)