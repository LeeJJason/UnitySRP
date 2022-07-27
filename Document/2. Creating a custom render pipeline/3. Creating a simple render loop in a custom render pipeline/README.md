# Creating a simple render loop in a custom render pipeline

[原文地址](https://docs.unity3d.com/2021.3/Documentation/Manual/srp-creating-simple-render-loop.html)

渲染循环是指在单一帧中发生的所有渲染操作的术语。本页面包含有关在基于 Unity 可编程渲染管线的自定义渲染管线中创建简单渲染循环的信息。

本页面上的代码示例演示使用可编程渲染管线的基本原则。可以使用此信息构建自己的自定义可编程渲染管线，或了解 Unity 的预构建可编程渲染管线如何工作。

## Preparing your project

开始为渲染循环编写代码之前，必须准备好项目。

步骤如下所示：

1. [Create an SRP-compatible shader](https://docs.unity3d.com/2021.3/Documentation/Manual/srp-creating-simple-render-loop.html#creating-unity-shader).
2. [Create one or more GameObjects to render](https://docs.unity3d.com/2021.3/Documentation/Manual/srp-creating-simple-render-loop.html#creating-gameobject).
3. [Create the basic structure of your custom SRP](https://docs.unity3d.com/2021.3/Documentation/Manual/srp-creating-simple-render-loop.html#creating-srp).
4. *Optional:* If you plan to extend your simple custom SRP to add more complex functionality, install the SRP Core package. The SRP Core package includes the SRP Core **shader** library (which you can use to make your shaders SRP Batcher compatible), and utility functions for common operations. For more information, see the [SRP Core package documentation](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@latest).

### Creating an SRP-compatible shader

在可编程渲染管线中，使用 `LightMode` 通道标签确定如何绘制几何体。有关通道标签的更多信息，请参阅 [ShaderLab：向通道分配标签](https://docs.unity3d.com/cn/current/Manual/SL-PassTags.html)。

此任务演示如何创建非常简单的无光照 Shader 对象，其 LightMode 通道标签值为 `ExampleLightModeTag`。

1. 在项目中创建一个新着色器资源。有关创建着色器资源的说明，请参阅[着色器资源](https://docs.unity3d.com/cn/current/Manual/class-Shader.html)。

2. 在 Project 视图中，双击着色器资源以在文本编辑器中打开着色器源代码。 

3. 将现有代码替换为以下内容：

```c
// 这定义一个与自定义可编程渲染管线兼容的简单无光照 Shader 对象。
// 它应用硬编码颜色，并演示 LightMode 通道标签的使用。
// 它不与 SRP Batcher 兼容。

Shader "Examples/SimpleUnlitColor"
{
    SubShader
    {
        Pass
        {
            // LightMode 通道标签的值必须与 ScriptableRenderContext.DrawRenderers 中的 ShaderTagId 匹配
            Tags { "LightMode" = "ExampleLightModeTag"}

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

    float4x4 unity_MatrixVP;
            float4x4 unity_ObjectToWorld;

            struct Attributes
            {
                float4 positionOS   : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                float4 worldPos = mul(unity_ObjectToWorld, IN.positionOS);
                OUT.positionCS = mul(unity_MatrixVP, worldPos);
                return OUT;
            }

            float4 frag (Varyings IN) : SV_TARGET
            {
                return float4(0.5,1,0.5,1);
            }
            ENDHLSL
        }
    }
}
```

### Creating a GameObject to render

要测试渲染循环是否可正常工作，必须创建要渲染的内容。此任务演示如何在场景中放置使用在上一个任务中创建的 SRP 兼容着色器的游戏对象。

1. 在 Unity 项目中创建一个新材质资源。有关说明，请参阅[材质](https://docs.unity3d.com/cn/current/Manual/class-Material.html)。

2. 将着色器资源分配给材质资源。有关说明，请参阅[材质](https://docs.unity3d.com/cn/current/Manual/class-Material.html)。 

3. 在场景中创建一个立方体。有关说明，请参阅[原始对象](https://docs.unity3d.com/cn/current/Manual/PrimitiveObjects.html)。 

4. 将材质分配给它。有关说明，请参阅[材质](https://docs.unity3d.com/cn/current/Manual/class-Material.html)。

### Creating the basic structure of your custom SRP

准备的最后阶段是创建自定义 SRP 所需的基本源文件，并告知 Unity 开始使用自定义 SRP 进行渲染。

1. 按照 [How to get, set, and configure the active render pipeline](https://docs.unity3d.com/2021.3/Documentation/Manual/srp-setting-render-pipeline-asset.html)中的说明，创建一个继承自 `RenderPipeline` 的类和一个兼容渲染管线资源。
2. 按照如何获取、设置和配置活动渲染管道中的说明设置活动渲染管道资源。 Unity 将立即开始使用自定义 SRP 进行渲染，这意味着您的场景视图
      在您将代码添加到自定义 SRP 之前，游戏视图将是空白的。

## Creating the render loop

在简单渲染循环中，基本操作有：

- [Clearing the render target](https://docs.unity3d.com/2021.3/Documentation/Manual/srp-creating-simple-render-loop.html#clearing)，这意味着移除在最后一帧期间绘制的几何体。
- [Culling](https://docs.unity3d.com/2021.3/Documentation/Manual/srp-creating-simple-render-loop.html#culling)，这意味着过滤掉对摄像机不可见的几何体。
- [Drawing](https://docs.unity3d.com/2021.3/Documentation/Manual/srp-creating-simple-render-loop.html#drawing)，这意味着向 GPU 告知要绘制的几何体以及如何进行绘制。

### Clearing the render target

清除意味着移除在最后一帧期间绘制的内容。渲染目标通常是屏幕；但是，也可以渲染到纹理以创建“画中画”效果。这些示例演示如何渲染到屏幕，这是 Unity 的默认行为。

要清除可编程渲染管线中的渲染目标，请执行以下操作：

1. 使用 `Clear` 命令配置 `CommandBuffer`。 

2. 将 `CommandBuffer` 添加到 `ScriptableRenderContext` 上的命令队列；为此，请调用 [ScriptableRenderContext.ExecuteCommandBuffer](https://docs.unity3d.com/cn/current/ScriptReference/Rendering.ScriptableRenderContext.ExecuteCommandBuffer.html)。 

3. 指示图形 API 执行 `ScriptableRenderContext` 上的命令队列；为此，请调用 [ScriptableRenderContext.Submit](https://docs.unity3d.com/cn/current/ScriptReference/Rendering.ScriptableRenderContext.Submit.html)。

与所有可编程渲染管线操作一样，使用 [RenderPipeline.Render](https://docs.unity3d.com/cn/current/ScriptReference/Rendering.RenderPipeline.Render.html) 方法作为此代码的入口点。此示例代码演示如何执行此操作：

```cs
/* 
This is a simplified example of a custom Scriptable Render Pipeline.
It demonstrates how a basic render loop works.
It shows the clearest workflow, rather than the most efficient runtime performance.
*/

using UnityEngine;
using UnityEngine.Rendering;

public class ExampleRenderPipeline : RenderPipeline {
    public ExampleRenderPipeline() {
    }

    protected override void Render (ScriptableRenderContext context, Camera[] cameras) {
        // Create and schedule a command to clear the current render target
        var cmd = new CommandBuffer();
        cmd.ClearRenderTarget(true, true, Color.black);
        context.ExecuteCommandBuffer(cmd);
        cmd.Release();

        // Instruct the graphics API to perform all scheduled commands
        context.Submit();
    }
}
```

### Culling

剔除是过滤掉对摄像机不可见的几何体的过程。

要在可编程渲染管线中进行剔除，请执行以下操作：

1. 使用有关摄像机的数据填充 [ScriptableCullingParameters](https://docs.unity3d.com/cn/current/ScriptReference/Rendering.ScriptableCullingParameters.html) 结构；为此，请调用 [Camera.TryGetCullingParameters](https://docs.unity3d.com/cn/current/ScriptReference/Camera.TryGetCullingParameters.html)。 

2. 可选：手动更新 `ScriptableCullingParameters` 结构的值。 

3. 调用 [ScriptableRenderContext.Cull](https://docs.unity3d.com/cn/current/ScriptReference/Rendering.ScriptableRenderContext.Cull.html)，并将结果存储在一个 `CullingResults` 结构中。

此示例代码扩展了上面的示例，演示如何清除渲染目标，然后执行剔除操作：

```cs
/* 
这是自定义可编程渲染管线的简化示例。
它演示基本渲染循环的工作方式。
它演示最清晰的工作流程，而不是最高效的运行时性能。
*/

using UnityEngine;
using UnityEngine.Rendering;

public class ExampleRenderPipeline : RenderPipeline {
    public ExampleRenderPipeline() {
    }

    protected override void Render (ScriptableRenderContext context, Camera[] cameras) {
        // 创建并调度命令以清除当前渲染目标
        var cmd = new CommandBuffer();
        cmd.ClearRenderTarget(true, true, Color.black);
        context.ExecuteCommandBuffer(cmd);
        cmd.Release();

        // 遍历所有摄像机
        foreach (Camera camera in cameras)
        {
            // 从当前摄像机获取剔除参数
            camera.TryGetCullingParameters(out var cullingParameters);

            // 使用剔除参数执行剔除操作，并存储结果
            var cullingResults = context.Cull(ref cullingParameters);
        }

        // 指示图形 API 执行所有调度的命令
        context.Submit();
    }
}
```

### Drawing

绘制是指示图形 API 使用给定设置绘制一组给定几何体的过程。

要在 SRP 中进行绘制，请执行以下操作：

1. 如上所述执行剔除操作，并将结果存储在 `CullingResults` 结构中。 

2. 创建和配置 [FilteringSettings](https://docs.unity3d.com/cn/current/ScriptReference/Rendering.FilteringSettings.html) 结构，它描述如何过滤剔除结果。 

3. 创建并配置一个 [DrawingSettings](https://docs.unity3d.com/cn/current/ScriptReference/Rendering.DrawingSettings.html) 结构，它描述了要绘制的几何图形以及如何绘制它。
4. *可选*：默认情况下，Unity 基于 Shader 对象设置渲染状态。如果要覆盖即将绘制的部分或所有几何体的渲染状态，可以使用 [RenderStateBlock](https://docs.unity3d.com/cn/current/ScriptReference/Rendering.RenderStateBlock.html) 结构执行此操作。
5.  调用 [ScriptableRenderContext.DrawRenderers](https://docs.unity3d.com/cn/current/ScriptReference/Rendering.ScriptableRenderContext.DrawRenderers.html)，并将创建的结构作为参数进行传递。Unity 根据设置绘制过滤后的几何体集。

此示例代码基于上面的示例进行构建，演示如何清除渲染目标，执行剔除操作，然后绘制生成的几何体：

```cs
/* 
This is a simplified example of a custom Scriptable Render Pipeline.
It demonstrates how a basic render loop works.
It shows the clearest workflow, rather than the most efficient runtime performance.
*/

using UnityEngine;
using UnityEngine.Rendering;

public class ExampleRenderPipeline : RenderPipeline {
    public ExampleRenderPipeline() {
    }

    protected override void Render (ScriptableRenderContext context, Camera[] cameras) {
        // Create and schedule a command to clear the current render target
        var cmd = new CommandBuffer();
        cmd.ClearRenderTarget(true, true, Color.black);
        context.ExecuteCommandBuffer(cmd);
        cmd.Release();

        // Iterate over all Cameras
        foreach (Camera camera in cameras)
        {
            // Get the culling parameters from the current Camera
            camera.TryGetCullingParameters(out var cullingParameters);

            // Use the culling parameters to perform a cull operation, and store the results
            var cullingResults = context.Cull(ref cullingParameters);

            // Update the value of built-in shader variables, based on the current Camera
            context.SetupCameraProperties(camera);

            // Tell Unity which geometry to draw, based on its LightMode Pass tag value
            ShaderTagId shaderTagId = new ShaderTagId("ExampleLightModeTag");

            // Tell Unity how to sort the geometry, based on the current Camera
            var sortingSettings = new SortingSettings(camera);

            // Create a DrawingSettings struct that describes which geometry to draw and how to draw it
            DrawingSettings drawingSettings = new DrawingSettings(shaderTagId, sortingSettings);

            // Tell Unity how to filter the culling results, to further specify which geometry to draw
            // Use FilteringSettings.defaultValue to specify no filtering
            FilteringSettings filteringSettings = FilteringSettings.defaultValue;
        
            // Schedule a command to draw the geometry, based on the settings you have defined
            context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

            // Schedule a command to draw the Skybox if required
            if (camera.clearFlags == CameraClearFlags.Skybox && RenderSettings.skybox != null)
            {
                context.DrawSkybox(camera);
            }

            // Instruct the graphics API to perform all scheduled commands
            context.Submit();
        }
    }
}
```

