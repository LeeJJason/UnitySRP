# Scheduling and executing rendering commands in the Scriptable Render Pipeline

[原文地址](https://docs.unity3d.com/2019.4/Documentation/Manual/srp-using-scriptable-render-context.html)

本页介绍了如何通过使用 CommandBuffers 或通过对 ScriptableRenderContext 进行直接 API 调用，在 [Scriptable Render Pipeline (SRP)](https://docs.unity3d.com/cn/current/Manual/ScriptableRenderPipeline.html) 中调度和执行渲染命令。 此页面上的信息适用于通用渲染管道(URP)、高清渲染管线 (HDRP) 和基于 SRP 的自定义渲染管线。

在 SRP 中，应使用 C# 脚本来配置和调度渲染命令。然后，需要告诉 Unity 的底层图形架构执行这些命令，此过程会将指令发送到图形 API。

主要做法是对 ScriptableRenderContext 进行 API 调用，不过也可以立即执行 CommandBuffers。

## Using the ScriptableRenderContext APIs

在 SRP 中，ScriptableRenderContext 类用作 C# 渲染管线代码与 Unity 的低级图形代码之间的接口。SRP 使用延迟执行的方式来实现渲染；您需要使用 ScriptableRenderContext 来构建渲染命令列表，然后告诉 Unity 执行这些命令。Unity 的低级图形架构随后将指令发送到图形 API。

要调度渲染命令，您可以：

- 使用 [ScriptableRenderContext.ExecuteCommandBuffer](https://docs.unity3d.com/cn/current/ScriptReference/Rendering.ScriptableRenderContext.ExecuteCommandBuffer.html) 将 [CommandBuffers](https://docs.unity3d.com/cn/current/ScriptReference/Rendering.CommandBuffer.html) 传递到 ScriptableRenderContext
- 对可编程渲染上下文进行直接 API 调用（例如 [ScriptableRenderContext.Cull](https://docs.unity3d.com/cn/current/ScriptReference/Rendering.ScriptableRenderContext.Cull.html) 或 [ScriptableRenderContext.DrawRenderers](https://docs.unity3d.com/cn/current/ScriptReference/Rendering.ScriptableRenderContext.DrawRenderers.html)）

为了告诉 Unity 执行您所调度的命令，请调用 [ScriptableRenderContext.Submit](https://docs.unity3d.com/cn/current/ScriptReference/Rendering.ScriptableRenderContext.Submit.html)。请注意，使用的是 CommandBuffer 还是通过调用 API 来调度命令，这并不重要；Unity 以相同方式在 ScriptableRenderContext 中调度所有渲染命令，并且在调用 `Submit()` 之前不会执行任何这些命令。

以下示例代码演示如何使用 CommandBuffer 来调度和执行命令以清除当前渲染目标。

```cs
using UnityEngine;
using UnityEngine.Rendering;

public class ExampleRenderPipeline : RenderPipeline
{
        public ExampleRenderPipeline() {
        }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras) {
        // Create and schedule a command to clear the current render target
        var cmd = new CommandBuffer();
        cmd.ClearRenderTarget(true, true, Color.red);
        context.ExecuteCommandBuffer(cmd);
        cmd.Release();

         // Tell the Scriptable Render Context to tell the graphics API to perform the scheduled commands
        context.Submit();
    }
}
```

## Executing CommandBuffers immediately

可通过调用 [Graphics.ExecuteCommandBuffer](https://docs.unity3d.com/cn/current/ScriptReference/Graphics.ExecuteCommandBuffer.html) 来立即执行 CommandBuffers，而不使用 ScriptableRenderContext。对该 API 的调用发生在渲染管线之外。

## Additional information

有关可以使用 CommandBuffers 来调度的命令的更多信息，请参阅 [CommandBuffers API 文档](https://docs.unity3d.com/cn/current/ScriptReference/Rendering.CommandBuffer.html)。