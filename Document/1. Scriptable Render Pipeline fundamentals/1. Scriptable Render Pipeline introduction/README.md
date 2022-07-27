# Scriptable Render Pipeline introduction

[原文地址](https://docs.unity3d.com/2019.4/Documentation/Manual/scriptable-render-pipeline-introduction.html)

本页说明 Unity 的[可编程渲染管线 (SRP)](https://docs.unity3d.com/cn/current/Manual/ScriptableRenderPipeline.html) 的工作原理，并介绍一些关键概念和术语。本页面上的信息适用于通用渲染管线 (URP)、高清渲染管线 (HDRP) 和基于 SRP 的自定义渲染管线。

可编程渲染管线是一个薄 API 层，允许使用 C# 脚本来调度和配置渲染命令。Unity 将这些命令传递给它的低级图形架构，后者随后将指令发送给图形 API。

URP 和 HDRP 建立在 SRP 之上。您还可以在 SRP 之上创建自己的自定义渲染管线。

## Render Pipeline Instance and Render Pipeline Asset

每个基于 SRP 的渲染管线都有两个关键的自定义元素：

- **渲染管线实例**。这是定义渲染管线功能的类的实例。它的脚本继承自 [RenderPipeline](https://docs.unity3d.com/cn/current/ScriptReference/Rendering.RenderPipeline.html) 并覆盖其 `Render()` 方法。
- **渲染管线资源**。这是 Unity 项目中的一项资源，用于存储有关所使用的渲染管线实例以及如何对其进行配置的数据。它的脚本继承自 [RenderPipelineAsset](https://docs.unity3d.com/cn/current/ScriptReference/Rendering.RenderPipelineAsset.html) 并覆盖其 `CreatePipeline()` 方法。

有关这些元素的更多信息，以及如何在自定义渲染管线中进行创建的说明，请参阅[创建渲染管线资源和渲染管线实例](https://docs.unity3d.com/cn/current/Manual/srp-creating-render-pipeline-asset-and-render-pipeline-instance.html)。

## ScriptableRenderContext

`ScriptableRenderContext` 是一个类，用作渲染管线中的自定义 C# 代码与 Unity 的低级图形代码之间的接口。

使用 [ScriptableRenderContext](https://docs.unity3d.com/cn/current/ScriptReference/Rendering.ScriptableRenderContext.html) API 可以调度和执行渲染命令。有关信息，请参阅[在可编程渲染管线中调度和执行渲染命令](https://docs.unity3d.com/cn/current/Manual/srp-using-scriptable-render-context.html)。

## Entry points and callbacks

使用 SRP 时，使用它们可让 Unity 在特定时间调用您的 C# 代码。

- [RenderPipeline.Render](https://docs.unity3d.com/cn/current/ScriptReference/Rendering.RenderPipeline.Render.html) 是 SRP 的主要入口点。Unity 会自动调用此方法。如果要编写自定义渲染管线，这就是开始编写代码的地方。
- RenderPipelineManager 类有以下事件可供您订阅，以便您可以在渲染循环中的特定点执行自定义代码：
    - [beginFrameRendering](https://docs.unity3d.com/cn/current/ScriptReference/Rendering.RenderPipelineManager-beginFrameRendering.html) - **Note:** This can generate garbage. Use `beginContextRendering` instead.
    - [endFrameRendering](https://docs.unity3d.com/cn/current/ScriptReference/Rendering.RenderPipelineManager-endFrameRendering.html) - **Note:** This can generate garbage. Use `endContextRendering` instead.
    - [beginContextRendering](https://docs.unity3d.com/cn/current/ScriptReference/Rendering.RenderPipelineManager-beginContextRendering.html)
    - [endContextRendering](https://docs.unity3d.com/cn/current/ScriptReference/Rendering.RenderPipelineManager-endContextRendering.html)
    - [beginCameraRendering](https://docs.unity3d.com/cn/current/ScriptReference/Rendering.RenderPipelineManager-beginCameraRendering.html)
    - [endCameraRendering](https://docs.unity3d.com/cn/current/ScriptReference/Rendering.RenderPipelineManager-endCameraRendering.html)