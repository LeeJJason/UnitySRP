# Creating a Render Pipeline Asset and Render Pipeline Instance in a custom render pipeline

[原文地址](https://docs.unity3d.com/2021.3/Documentation/Manual/srp-creating-render-pipeline-asset-and-render-pipeline-instance.html)

如果要基于[可编程渲染管线](https://docs.unity3d.com/cn/current/Manual/ScriptableRenderPipeline.html) (SRP) 创建自己的渲染管线，项目必须包含：

- 一个继承自 [RenderPipelineAsset](https://docs.unity3d.com/cn/current/ScriptReference/Rendering.RenderPipelineAsset.html) 并覆盖其 `CreatePipeline()` 方法的脚本。此脚本用于定义渲染管线资源。
- 一个继承自 [RenderPipeline](https://docs.unity3d.com/cn/current/ScriptReference/Rendering.RenderPipeline.html) 并覆盖其 `Render()` 方法的脚本。此脚本定义渲染管线实例，是编写自定义渲染代码的地方。
- 一个从 [RenderPipelineAsset](https://docs.unity3d.com/cn/current/ScriptReference/Rendering.RenderPipelineAsset.html) 脚本创建的渲染管线资源。此资源充当渲染管线实例的工厂类。

因为这些元素非常紧密相关，所以应该同时创建它们。

## Creating a basic Render Pipeline Asset and Render Pipeline Instance

以下示例显示了如何为实例化渲染管线实例的基本自定义渲染管线资源创建脚本、如何创建可定义渲染管线实例的脚本以及如何创建渲染管线资源本身。

1. 创建一个名为 *ExampleRenderPipelineAsset.cs* 的 C# 脚本。 
2. 将以下代码复制并粘贴到新脚本中：

```cs
using UnityEngine;
using UnityEngine.Rendering;
    
// The CreateAssetMenu attribute lets you create instances of this class in the Unity Editor.
[CreateAssetMenu(menuName = "Rendering/ExampleRenderPipelineAsset")]
public class ExampleRenderPipelineAsset : RenderPipelineAsset
{
    // Unity calls this method before rendering the first frame.
    // If a setting on the Render Pipeline Asset changes, Unity destroys the current Render Pipeline Instance and calls this method again before rendering the next frame.
    protected override RenderPipeline CreatePipeline() {
        // Instantiate the Render Pipeline that this custom SRP uses for rendering.
        return new ExampleRenderPipelineInstance();
    }
}
```

3. 创建一个名为 *ExampleRenderPipelineInstance.cs* 的 C# 脚本。 
4. 将以下代码复制并粘贴到新脚本中：

```cs
using UnityEngine;
using UnityEngine.Rendering;

public class ExampleRenderPipelineInstance : RenderPipeline
{
    public ExampleRenderPipelineInstance() {
    }

    protected override void Render (ScriptableRenderContext context, Camera[] cameras) {
        // 可以在此处编写自定义渲染代码。通过自定义此方法可以自定义 SRP。
    }
}
```

5. 在 Project 视图中，单击添加 (+) 按钮，或者打开上下文菜单并导航至 **Create**，然后选择 **Rendering** > **Example Render Pipeline Asset**。Unity 在 Project 视图中创建新的渲染管线资源。

## Creating a configurable Render Pipeline Asset and Render Pipeline Instance

默认情况下，渲染管线资源存储有关用于渲染的渲染管线实例以及在编辑器中使用的默认材质和着色器的信息。在 `RenderPipelineAsset` 脚本中，您可以扩展渲染管线资源以存储更多数据，并且可以在项目中拥有多个具有不同配置的不同渲染管线资源。例如，可使用渲染管线资源来保存每个不同硬件层的配置数据。高清渲染管线 (HDRP) 和通用渲染管线 (URP) 包含这方面的示例。

以下示例显示了如何创建 `RenderPipelineAsset` 脚本（该脚本使用公共数据来定义渲染管线资源，而公共数据则可以通过 Inspector 针对每个实例加以设置）和渲染管线实例（该实例在其构造函数中接收渲染管线资源，并使用此渲染管线资源的数据）。

1. 创建一个名为 *ExampleRenderPipelineAsset.cs* 的 C# 脚本。 
2. 将以下代码复制并粘贴到新脚本中：

```cs
using UnityEngine;
using UnityEngine.Rendering;

// CreateAssetMenu 属性让您可以在 Unity 编辑器中创建此类的实例。
[CreateAssetMenu(menuName = "Rendering/ExampleRenderPipelineAsset")]
public class ExampleRenderPipelineAsset : RenderPipelineAsset
{
    // 可以在 Inspector 中为每个渲染管线资源定义此数据
    public Color exampleColor;
    public string exampleString;

        // Unity 在渲染第一帧之前调用此方法。
       // 如果渲染管线资源上的设置改变，Unity 将销毁当前的渲染管线实例，并在渲染下一帧之前再次调用此方法。
    protected override RenderPipeline CreatePipeline() {
        // 实例化此自定义 SRP 用于渲染的渲染管线，然后传递对此渲染管线资源的引用。
        // 然后，渲染管线实例可以访问上方定义的配置数据。
        return new ExampleRenderPipelineInstance(this);
    }
}
```

3. 创建一个名为 *ExampleRenderPipelineInstance.cs* 的 C# 脚本。 
4. 将以下代码复制并粘贴到新脚本中：

```cs
using UnityEngine;
using UnityEngine.Rendering;

public class ExampleRenderPipelineInstance : RenderPipeline
{
    // 使用此变量来引用传递给构造函数的渲染管线资源
    private ExampleRenderPipelineAsset renderPipelineAsset;

    // 构造函数将 ExampleRenderPipelineAsset 类的实例作为其参数。
    public ExampleRenderPipelineInstance(ExampleRenderPipelineAsset asset) {
        renderPipelineAsset = asset;
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras) {
        // 这是使用渲染管线资源数据的示例。
        Debug.Log(renderPipelineAsset.exampleString);

        // 可以在此处编写自定义渲染代码。通过自定义此方法可以自定义 SRP。
    }
}
```

5. 在 Project 视图中，单击添加 (+) 按钮，或者打开上下文菜单并导航至 **Create**，然后选择 **Rendering** > **Example Render Pipeline Asset**。Unity 在 Project 视图中创建新的渲染管线资源。