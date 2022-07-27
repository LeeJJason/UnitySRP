# Creating a custom render pipeline based on the Scriptable Render Pipeline

[原文地址](https://docs.unity3d.com/2021.3/Documentation/Manual/srp-custom-getting-started.html)

本页面包含有关如何开始基于可编程渲染管线 (SRP) 创建自己的自定义渲染管线的信息。

## Creating a new project and installing the packages needed for a custom render pipeline

以下说明信息说明如何使用 SRP Core 包来创建自定义渲染管线。SRP Core 是 Unity 创建的包，其中包含可复用代码来帮助您创建自己的渲染管线，包括用于与平台特定的图形 API 结合使用的样板代码、用于常见渲染操作的实用函数以及供 URP 和 HDRP 使用的着色器库。有关 SRP Core 的更多信息，请参阅 [SRP Core 包文档](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@latest)。

1. 创建新的 Unity 项目。 

2. 使用 Git 来创建 [SRP 源代码仓库](https://github.com/Unity-Technologies/Graphics)的克隆体。可以将 SRP 源代码放在磁盘中的任何位置，只要不在任何 [reserved Project sub-folders](https://docs.unity3d.com/2021.3/Documentation/Manual/upm-ui-local.html#PkgLocation)内即可。 

3. 使用 Git 将您的 SRP 源代码副本更新到与您的 Unity 编辑器版本兼容的分支。 阅读[Using the latest version](https://github.com/Unity-Technologies/Graphics#branches-and-package-releases)以获取有关分支和版本的信息。

4.  在 Unity 中打开您的项目，然后按以下顺序从磁盘上的 SRP 源代码文件夹安装以下包。有关从磁盘安装包的信息，请参阅 [Installing a package from a local folder](https://docs.unity3d.com/2021.3/Documentation/Manual/upm-ui-local.html)。 

    * *com.unity.render-pipelines.core*。 

    	* 可选：*com.unity.render-pipelines.shadergraph*。作为自定义 SRP 的一部分，如果要使用 Shader Graph 或修改 Shader Graph 源代码，请安装此包。 
    	* 可选：*com.unity.render-pipelines.visualeffectgraph*。作为自定义 SRP 的一部分，如果要使用 Visual Effect Graph 或修改 Visual Effect Graph 源代码，请安装此包。

现在，您可以调试和修改 SRP 源代码副本中的脚本，并在 Unity 项目中查看更改的结果。

## Creating a custom version of URP or HDRP

通用渲染管线 (URP) 和高清渲染管线 (HDRP) 提供广泛的自定义选项，可帮助您获得所需的图形和性能。但是，如果您希望获得更多控制权，可为这些渲染管线之一创建自定义版本，并修改源代码。

遵循以上部分（**创建新项目并安装自定义 SRP 所需的包**）中的第 1–3 步。到达第 4 步时，请按顺序安装以下包：

**URP：**

- *com.unity.render-pipelines.core*
- *com.unity.render-pipelines.shadergraph*
- *com.unity.render-pipelines.universal*

**HDRP：**

- *com.unity.render-pipelines.core*
- *com.unity.render-pipelines.shadergraph*
- *com.unity.render-pipelines.high-defintion*