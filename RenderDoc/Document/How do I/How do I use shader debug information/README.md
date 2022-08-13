[原文地址](https://renderdoc.org/docs/how/how_shader_debug_info.html)

# How do I use shader debug information

RenderDoc 广泛使用着色器调试信息来提供更流畅的调试体验。这种信息为着色器接口中的任何内容命名，例如常量、资源绑定、插值输入和输出。它包含在许多情况下不可用的更好的类型信息，它甚至还可以包含用于源级调试的着色器源的嵌入式副本。

在大多数 API 上，此调试信息会增加大量信息，这些信息对于功能来说是不必要的，因此可以选择性地删除它。许多着色器编译管道会自动执行此操作，因此当 RenderDoc 可以在 API 级别拦截它时，信息就会丢失。出于这个原因，有几种方法可以在编译时单独保存未剥离的着色器 blob 或仅单独保存调试信息，并提供方法让 RenderDoc 将它看到的传递给 API 的剥离 blob 与磁盘上的调试信息相关联。

> **Note**
>
> OpenGL 被有效地排除在此考虑之外，因为它没有单独的调试信息，一切都是从上传到 API 的源生成的。如果源已被删除信息或被混淆，则必须由您的应用程序处理。

> **Warning**
>
> 由于此调试信息是单独存储的，因此它*不是*捕获的一部分，因此如果调试信息被移动或删除，RenderDoc 将无法找到它，并且捕获将仅显示没有调试信息的情况。

## Specifying debug shader blobs

以使用路径和特定于 API 的机制指定分离的调试着色器 blob。路径本身可以是绝对路径，每次都直接使用，也可以是相对路径，允许相对于可自定义的搜索文件夹指定 blob 标识符。如果使用相对路径，它可以像文件名一样简单。

着色器调试信息的搜索文件夹在设置窗口的`Core`类别下指定。您可以根据需要配置任意数量的搜索目录，它们将按列出的顺序进行搜索。

如果尝试了所有目录后没有找到匹配项，则将删除指定路径中的第一个子目录，并按顺序重新尝试目录。这样，如果在配置的搜索路径之一中存在尾随子集，则绝对路径可以匹配。类似地，对于具有与路径不匹配的前缀但尾随子路径确实存在的相对路径。

使用 D3D11 API，您可以在运行时指定路径：

```C
std::string pathName = "path/to/saved/blob.dxbc"; // path name is in UTF-8

ID3D11VertexShader *shader = ...;

// GUID value in renderdoc_app.h
GUID RENDERDOC_ShaderDebugMagicValue = RENDERDOC_ShaderDebugMagicValue_struct;

// string parameter must be NULL-terminated, and in UTF-8
shader->SetPrivateData(RENDERDOC_ShaderDebugMagicValue,
                       (UINT)pathName.length(), pathName.c_str());
```

您还可以使用 Vulkan API 指定它：

```C
std::string pathName = "path/to/saved/blob.dxbc"; // path name is in UTF-8

VkShaderModule shaderModule = ...;

// Both EXT_debug_marker and EXT_debug_utils can be used, this example uses
// EXT_debug_utils as EXT_debug_marker is deprecated
VkDebugUtilsObjectTagInfoEXT tagInfo = {VK_STRUCTURE_TYPE_DEBUG_UTILS_OBJECT_TAG_INFO_EXT};
tagInfo.objectType = VK_OBJECT_TYPE_SHADER_MODULE;
tagInfo.objectHandle = (uint64_t)shaderModule;
// tag value in renderdoc_app.h
tagInfo.tagName = RENDERDOC_ShaderDebugMagicValue_truncated;
tagInfo.pTag = pathName.c_str();
tagInfo.tagSize = pathName.length();

vkSetDebugUtilsObjectTagEXT(device, &tagInfo);
```

D3D12 需要使用着色器编译时说明符。这是通过传递`/Fd`给 fxc 或 dxc 来完成的。此开关需要一个参数，该参数可以是文件或目录的路径。如果您指定一个文件，则该路径将作为绝对路径存储在剥离的 blob 中。如果您指定一个目录，则调试 blob 将使用基于哈希的文件名存储在该目录中。存储在剥离的 blob 中的路径是一个仅包含文件名的*相对*路径。

## See Also

- [How do I debug a shader?](https://renderdoc.org/docs/how/how_debug_shader.html)